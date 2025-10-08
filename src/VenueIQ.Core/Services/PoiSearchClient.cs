using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using VenueIQ.Core.Models;

namespace VenueIQ.Core.Services;

public class PoiSearchClient : IPoiSearchClient
{
    private readonly HttpClient _http;
    private readonly IPoiCategoryMapProvider _categoryMap;
    private readonly IAzureMapsAuthProvider _auth;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PoiSearchClient>? _logger;
    private Dictionary<string, string>? _categoryIdMap; // normalized name/code -> numeric id
    private List<KeyValuePair<string, string>>? _categoryNameList; // (normalized name, id) for fuzzy lookup
    private bool _categoryIdMapLoaded;
    private static readonly TimeSpan CategoryCacheTtl = TimeSpan.FromDays(7);
    private string CategoryCachePath
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VenueIQ", "category-tree-cache.json");

    public PoiSearchClient(HttpClient http, IPoiCategoryMapProvider categoryMap, IAzureMapsAuthProvider auth, IMemoryCache cache, ILogger<PoiSearchClient>? logger = null)
    {
        _http = http;
        _categoryMap = categoryMap;
        _auth = auth;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PoiSearchResult> SearchAsync(AnalysisInput input, CancellationToken ct = default)
    {
        var cacheKey = $"pois|{input.Business}|{input.CenterLat:F5}|{input.CenterLng:F5}|{input.RadiusKm:F2}|{input.Language}";
        if (_cache.TryGetValue<PoiSearchResult>(cacheKey, out var cached))
        {
            _logger?.LogDebug("PoiSearchClient: cache hit for {Key}", cacheKey);
            return cached!;
        }

        var sw = Stopwatch.StartNew();
        var result = new PoiSearchResult { Success = false };
        try
        {
            var (competitorCats, complementCats) = await _categoryMap.GetCategoriesAsync(input.Business, ct).ConfigureAwait(false);
            _logger?.LogInformation("PoiSearchClient: category sets for {Business}: competitors={CompCats} complements={ComplCats}", input.Business, string.Join(',', competitorCats), string.Join(',', complementCats));
            var key = await _auth.GetSubscriptionKeyAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(key))
            {
                result.Meta.ErrorKey = "DataError_MissingApiKey";
                _logger?.LogWarning("PoiSearchClient: missing subscription key");
                return result;
            }

            _logger?.LogInformation("PoiSearchClient: fetching POIs at ({Lat:F5},{Lng:F5}) radius={RadiusKm}km lang={Lang}", input.CenterLat, input.CenterLng, input.RadiusKm, input.Language);
            // Map provided category codes/names to numeric IDs as required by Azure API
            var compIds = await MapToCategoryIdsAsync(competitorCats, input, key, ct).ConfigureAwait(false);
            var comp2Ids = await MapToCategoryIdsAsync(complementCats, input, key, ct).ConfigureAwait(false);
            var compQuery = BuildQueryHint(competitorCats);
            var comp2Query = BuildQueryHint(complementCats);
            var comp = await QueryCategorySetAsync(key, compIds, compQuery, input, PoiKind.Competitor, ct).ConfigureAwait(false);
            var comp2 = await QueryCategorySetAsync(key, comp2Ids, comp2Query, input, PoiKind.Complement, ct).ConfigureAwait(false);

            result.Competitors = comp.items;
            result.Complements = comp2.items;
            result.Meta.CompetitorCount = comp.items.Count;
            result.Meta.ComplementCount = comp2.items.Count;
            result.Meta.Partial = comp.partial || comp2.partial;
            result.Meta.WarningKey = (comp.partial || comp2.partial) ? "DataWarning_PartialResults" : null;
            result.Success = true;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogWarning(ex, "POI fetch HTTP error");
            result.Meta.ErrorKey = "DataError_Http";
        }
        catch (TaskCanceledException)
        {
            result.Meta.ErrorKey = "DataError_Timeout";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "POI fetch unexpected error");
            result.Meta.ErrorKey = "DataError_Unknown";
        }
        finally
        {
            sw.Stop();
            result.Meta.Latency = sw.Elapsed;
        }

        if (result.Success)
        {
            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
            _logger?.LogDebug("PoiSearchClient: cached result under {Key}", cacheKey);
        }
        return result;
    }

    private async Task<(List<PoiSummary> items, bool partial)> QueryCategorySetAsync(string key, IReadOnlyList<string> categorySet, string queryHint, AnalysisInput input, PoiKind kind, CancellationToken ct)
    {
        var list = new List<PoiSummary>();
        var limit = 50;
        var maxPages = 2; // Fetch up to 100 per set for MVP
        var partial = false;
        if (categorySet is null || categorySet.Count == 0)
        {
            _logger?.LogWarning("PoiSearchClient: empty category set for {Kind}; skipping fetch", kind);
            return (list, true);
        }
        for (int start = 0; start < categorySet.Count; start += 10)
        {
            var chunk = categorySet.Skip(start).Take(10).ToArray();
            for (var page = 0; page < maxPages; page++)
            {
                var url = BuildUrl(key, chunk, input, limit, page * limit, useOfs: true, queryHint: queryHint);
                // Avoid logging subscription key by logging only parameters
                _logger?.LogDebug("PoiSearchClient: page {Page} querying categories={Cats} limit={Limit} ofs={Ofs}", page, string.Join(',', chunk), limit, page * limit);
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
                if ((int)resp.StatusCode == 429)
                {
                    // naive backoff with jitter
                    await Task.Delay(TimeSpan.FromMilliseconds(300 + Random.Shared.Next(0, 300)), ct);
                    _logger?.LogWarning("PoiSearchClient: received 429 Too Many Requests; backing off and retrying page {Page}", page);
                    continue;
                }
                if (!resp.IsSuccessStatusCode)
                {
                    partial = true; // treat as partial for this set
                    var body = await SafeReadStringAsync(resp.Content, ct).ConfigureAwait(false);
                    _logger?.LogWarning("PoiSearchClient: non-success status {Status} on page {Page}. Body: {Body}", resp.StatusCode, page, TrimForLog(body));
                    // Fallback: try 'offset' instead of 'ofs' once for this page
                    var altUrl = BuildUrl(key, chunk, input, limit, page * limit, useOfs: false, queryHint: queryHint);
                    using var altReq = new HttpRequestMessage(HttpMethod.Get, altUrl);
                    using var altResp = await _http.SendAsync(altReq, ct).ConfigureAwait(false);
                    if (!altResp.IsSuccessStatusCode)
                    {
                        var altBody = await SafeReadStringAsync(altResp.Content, ct).ConfigureAwait(false);
                        _logger?.LogWarning("PoiSearchClient: fallback with 'offset' also failed: {Status}. Body: {Body}", altResp.StatusCode, TrimForLog(altBody));
                        break;
                    }
                    using var s2 = await altResp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                    using var doc2 = await JsonDocument.ParseAsync(s2, cancellationToken: ct).ConfigureAwait(false);
                    if (doc2.RootElement.TryGetProperty("results", out var results2))
                    {
                        var before2 = list.Count;
                        foreach (var r in results2.EnumerateArray())
                        {
                            TryAddPoi(r, kind, list);
                        }
                        var added2 = list.Count - before2;
                        _logger?.LogDebug("PoiSearchClient: page {Page} (offset) returned {Count} items (total {Total})", page, added2, list.Count);
                        if (results2.GetArrayLength() < limit) break;
                        continue; // go next page
                    }
                    else
                    {
                        _logger?.LogWarning("PoiSearchClient: missing 'results' in fallback response on page {Page}", page);
                        break;
                    }
                }
                using var s = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct).ConfigureAwait(false);
                if (doc.RootElement.TryGetProperty("results", out var results))
                {
                    var before = list.Count;
                    foreach (var r in results.EnumerateArray())
                        TryAddPoi(r, kind, list);
                    var added = list.Count - before;
                    _logger?.LogDebug("PoiSearchClient: page {Page} returned {Count} items (total {Total})", page, added, list.Count);
                    if (results.GetArrayLength() < limit)
                    {
                        _logger?.LogDebug("PoiSearchClient: page {Page} less than limit {Limit}, stopping pagination", page, limit);
                        break; // no more pages for this chunk
                    }
                }
                else
                {
                    _logger?.LogWarning("PoiSearchClient: missing 'results' in response on page {Page}", page);
                    break;
                }
            }
        }
        return (list, partial);
    }

    private static string BuildUrl(string key, IReadOnlyList<string> categorySet, AnalysisInput input, int limit, int offset, bool useOfs, string? queryHint)
    {
        var meters = (int)Math.Round(input.RadiusKm * 1000);
        var cats = string.Join(',', categorySet);
        var ofsParam = useOfs ? $"ofs={offset}" : $"offset={offset}";
        var query = string.IsNullOrWhiteSpace(queryHint) ? "POI" : queryHint; // required by API
        var uri = $"https://atlas.microsoft.com/search/poi/category/json?api-version=1.0&query={Uri.EscapeDataString(query)}&subscription-key={Uri.EscapeDataString(key)}&lat={input.CenterLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={input.CenterLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&radius={meters}&limit={limit}&{ofsParam}&language={Uri.EscapeDataString(input.Language)}&categorySet={Uri.EscapeDataString(cats)}";
        return uri;
    }

    private static string BuildQueryHint(IReadOnlyList<string> originalCats)
    {
        if (originalCats is null || originalCats.Count == 0) return "POI";
        // Prefer first category label; transform CAFE_PUB -> "CAFE PUB" for API which accepts category tokens
        var raw = originalCats[0];
        if (string.IsNullOrWhiteSpace(raw)) return "POI";
        return raw.Replace('_', ' ');
    }

    private static async Task<string> SafeReadStringAsync(HttpContent content, CancellationToken ct)
    {
        try { return await content.ReadAsStringAsync(ct).ConfigureAwait(false); } catch { return string.Empty; }
    }

    private static string TrimForLog(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Replace('\n', ' ').Replace('\r', ' ');
        return s.Length > 400 ? s.Substring(0, 400) + "…" : s;
    }

    private static void TryAddPoi(JsonElement r, PoiKind kind, List<PoiSummary> list)
    {
        try
        {
            var position = r.GetProperty("position");
            var poi = r.GetProperty("poi");
            var cat = poi.TryGetProperty("classifications", out var cl) && cl.GetArrayLength() > 0
                ? cl[0].GetProperty("code").GetString()
                : (poi.TryGetProperty("categories", out var cats) && cats.GetArrayLength() > 0 ? cats[0].GetString() : null);
            list.Add(new PoiSummary
            {
                Id = r.TryGetProperty("id", out var id) ? id.GetString() : null,
                Name = poi.TryGetProperty("name", out var nm) ? nm.GetString() : null,
                Category = cat,
                Lat = position.GetProperty("lat").GetDouble(),
                Lng = position.GetProperty("lon").GetDouble(),
                DistanceMeters = r.TryGetProperty("dist", out var dist) ? dist.GetDouble() : 0,
                Kind = kind
            });
        }
        catch
        {
            // skip malformed entry
        }
    }

    private async Task<IReadOnlyList<string>> MapToCategoryIdsAsync(IReadOnlyList<string> categories, AnalysisInput input, string key, CancellationToken ct)
    {
        if (categories is null || categories.Count == 0) return Array.Empty<string>();
        await EnsureCategoryIdMapAsync(key, input.Language, ct).ConfigureAwait(false);
        var ids = new List<string>(categories.Count);
        foreach (var c in categories)
        {
            if (string.IsNullOrWhiteSpace(c)) continue;
            // If already numeric, accept as-is
            if (IsAllDigits(c)) { ids.Add(c); continue; }
            var norm = NormalizeKey(c);
            if (_categoryIdMap != null && _categoryIdMap.TryGetValue(norm, out var id))
            {
                ids.Add(id);
            }
            else
            {
                // Fuzzy: try to match by tokens within known names
                var fuzzy = FuzzyResolveCategory(c);
                if (!string.IsNullOrEmpty(fuzzy))
                {
                    ids.Add(fuzzy!);
                    _logger?.LogDebug("PoiSearchClient: fuzzy-mapped category '{Cat}' -> id {Id}", c, fuzzy);
                }
                else
                {
                    _logger?.LogWarning("PoiSearchClient: could not resolve category '{Cat}' to numeric id", c);
                }
            }
        }
        return ids;
    }

    private async Task EnsureCategoryIdMapAsync(string key, string language, CancellationToken ct)
    {
        if (_categoryIdMapLoaded && _categoryIdMap is not null) return;
        // Try load from cache first
        if (TryLoadCategoryCache(out var cachedMap, out var cachedList))
        {
            _categoryIdMap = cachedMap;
            _categoryNameList = cachedList;
            _categoryIdMapLoaded = true;
            _logger?.LogInformation("PoiSearchClient: loaded category ids from cache: {Count}", cachedMap?.Count ?? 0);
            return;
        }
        try
        {
            // Fetch category tree in English to avoid localization mismatches when mapping codes
            var url = $"https://atlas.microsoft.com/search/poi/category/tree/json?api-version=1.0&subscription-key={Uri.EscapeDataString(key)}&language=en";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _logger?.LogWarning("PoiSearchClient: failed to load category tree: {Status}", resp.StatusCode);
                _categoryIdMapLoaded = true; // avoid refetching repeatedly
                _categoryIdMap ??= new Dictionary<string, string>();
                return;
            }
            using var s = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct).ConfigureAwait(false);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<KeyValuePair<string, string>>();
            void Traverse(JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.Object)
                {
                    string? id = null; string? name = null; string? code = null;
                    foreach (var prop in el.EnumerateObject())
                    {
                        if (prop.NameEquals("id") && prop.Value.ValueKind == JsonValueKind.Number) id = prop.Value.GetRawText();
                        else if (prop.NameEquals("name") && prop.Value.ValueKind == JsonValueKind.String) name = prop.Value.GetString();
                        else if (prop.NameEquals("code") && prop.Value.ValueKind == JsonValueKind.String) code = prop.Value.GetString();
                    }
                    if (id is not null)
                    {
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            var nk = NormalizeKey(name!);
                            if (!string.IsNullOrEmpty(nk)) { map[nk] = id!; list.Add(new KeyValuePair<string, string>(nk, id!)); }
                        }
                        if (!string.IsNullOrWhiteSpace(code))
                        {
                            var ck = NormalizeKey(code!);
                            if (!string.IsNullOrEmpty(ck)) { map[ck] = id!; }
                        }
                    }
                    foreach (var prop in el.EnumerateObject()) Traverse(prop.Value);
                }
                else if (el.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in el.EnumerateArray()) Traverse(e);
                }
            }
            Traverse(doc.RootElement);
            _categoryIdMap = map;
            _categoryNameList = list;
            _categoryIdMapLoaded = true;
            _logger?.LogInformation("PoiSearchClient: loaded {Count} category ids", map.Count);
            TrySaveCategoryCache(map, list);
        }
        catch (Exception ex)
        {
            _categoryIdMapLoaded = true;
            _categoryIdMap ??= new Dictionary<string, string>();
            _categoryNameList ??= new List<KeyValuePair<string, string>>();
            _logger?.LogError(ex, "PoiSearchClient: error loading category tree");
        }
    }

    private bool TryLoadCategoryCache(out Dictionary<string, string>? map, out List<KeyValuePair<string, string>>? list)
    {
        map = null; list = null;
        try
        {
            var path = CategoryCachePath;
            if (!File.Exists(path)) return false;
            using var s = File.OpenRead(path);
            using var doc = JsonDocument.Parse(s);
            var root = doc.RootElement;
            if (!root.TryGetProperty("expiresUtc", out var expEl) || !root.TryGetProperty("pairs", out var pairsEl)) return false;
            var expTicks = expEl.GetInt64();
            var expires = new DateTimeOffset(expTicks, TimeSpan.Zero);
            if (DateTimeOffset.UtcNow > expires) return false;
            var m = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var l = new List<KeyValuePair<string, string>>();
            foreach (var p in pairsEl.EnumerateArray())
            {
                var k = p.TryGetProperty("k", out var ke) ? ke.GetString() : null;
                var id = p.TryGetProperty("id", out var ie) ? ie.GetString() : null;
                if (!string.IsNullOrWhiteSpace(k) && !string.IsNullOrWhiteSpace(id)) { m[k!] = id!; l.Add(new KeyValuePair<string, string>(k!, id!)); }
            }
            map = m; list = l; return true;
        }
        catch
        {
            return false;
        }
    }

    private void TrySaveCategoryCache(Dictionary<string, string> map, List<KeyValuePair<string, string>> list)
    {
        try
        {
            var dir = Path.GetDirectoryName(CategoryCachePath)!;
            Directory.CreateDirectory(dir);
            var expires = DateTimeOffset.UtcNow + CategoryCacheTtl;
            using var ms = new MemoryStream();
            using (var jw = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
            {
                jw.WriteStartObject();
                jw.WriteNumber("expiresUtc", expires.ToUniversalTime().Ticks);
                jw.WriteStartArray("pairs");
                foreach (var kv in list)
                {
                    jw.WriteStartObject();
                    jw.WriteString("k", kv.Key);
                    jw.WriteString("id", kv.Value);
                    jw.WriteEndObject();
                }
                jw.WriteEndArray();
                jw.WriteEndObject();
            }
            File.WriteAllBytes(CategoryCachePath, ms.ToArray());
        }
        catch
        {
            // ignore
        }
    }

    private static bool IsAllDigits(string s)
    {
        for (int i = 0; i < s.Length; i++) if (!char.IsDigit(s[i])) return false; return s.Length > 0;
    }
    private static string NormalizeKey(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        // Remove diacritics
        var formD = s.Normalize(NormalizationForm.FormD);
        Span<char> tmp = stackalloc char[formD.Length];
        int ti = 0;
        for (int i = 0; i < formD.Length; i++)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(formD[i]);
            if (uc != UnicodeCategory.NonSpacingMark)
                tmp[ti++] = formD[i];
        }
        var ascii = new string(tmp.Slice(0, ti)).Normalize(NormalizationForm.FormC);
        // Keep only letters/digits and lowercase
        Span<char> buf = stackalloc char[ascii.Length];
        int j = 0;
        for (int i = 0; i < ascii.Length; i++)
        {
            var ch = ascii[i];
            if (char.IsLetterOrDigit(ch)) buf[j++] = char.ToLowerInvariant(ch);
        }
        return new string(buf.Slice(0, j));
    }

    private string? FuzzyResolveCategory(string codeOrName)
    {
        if (_categoryNameList is null || _categoryNameList.Count == 0) return null;
        var tokens = codeOrName.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(NormalizeKey)
                               .Where(t => !string.IsNullOrEmpty(t))
                               .ToArray();
        if (tokens.Length == 0) return null;
        KeyValuePair<string, string>? best = null;
        foreach (var kv in _categoryNameList)
        {
            var name = kv.Key;
            bool all = true;
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!name.Contains(tokens[i])) { all = false; break; }
            }
            if (all)
            {
                if (best is null || name.Length < best.Value.Key.Length) best = kv;
            }
        }
        return best?.Value;
    }
}

using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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
            return cached!;
        }

        var sw = Stopwatch.StartNew();
        var result = new PoiSearchResult { Success = false };
        try
        {
            var (competitorCats, complementCats) = await _categoryMap.GetCategoriesAsync(input.Business, ct).ConfigureAwait(false);
            var key = await _auth.GetSubscriptionKeyAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(key))
            {
                result.Meta.ErrorKey = "DataError_MissingApiKey";
                return result;
            }

            var comp = await QueryCategorySetAsync(key, competitorCats, input, PoiKind.Competitor, ct).ConfigureAwait(false);
            var comp2 = await QueryCategorySetAsync(key, complementCats, input, PoiKind.Complement, ct).ConfigureAwait(false);

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
        }
        return result;
    }

    private async Task<(List<PoiSummary> items, bool partial)> QueryCategorySetAsync(string key, IReadOnlyList<string> categorySet, AnalysisInput input, PoiKind kind, CancellationToken ct)
    {
        var list = new List<PoiSummary>();
        var limit = 50;
        var maxPages = 2; // Fetch up to 100 per set for MVP
        var partial = false;
        for (var page = 0; page < maxPages; page++)
        {
            var url = BuildUrl(key, categorySet, input, limit, page * limit);
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            if ((int)resp.StatusCode == 429)
            {
                // naive backoff with jitter
                await Task.Delay(TimeSpan.FromMilliseconds(300 + Random.Shared.Next(0, 300)), ct);
                continue;
            }
            if (!resp.IsSuccessStatusCode)
            {
                partial = true; // treat as partial for this set
                break;
            }
            using var s = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct).ConfigureAwait(false);
            if (doc.RootElement.TryGetProperty("results", out var results))
            {
                foreach (var r in results.EnumerateArray())
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
                if (results.GetArrayLength() < limit)
                {
                    break; // no more pages
                }
            }
            else
            {
                break;
            }
        }
        return (list, partial);
    }

    private static string BuildUrl(string key, IReadOnlyList<string> categorySet, AnalysisInput input, int limit, int offset)
    {
        var meters = (int)Math.Round(input.RadiusKm * 1000);
        var cats = string.Join(',', categorySet);
        var uri = $"https://atlas.microsoft.com/search/poi/category/json?api-version=1.0&subscription-key={Uri.EscapeDataString(key)}&lat={input.CenterLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={input.CenterLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&radius={meters}&limit={limit}&offset={offset}&language={Uri.EscapeDataString(input.Language)}&categorySet={Uri.EscapeDataString(cats)}";
        return uri;
    }
}


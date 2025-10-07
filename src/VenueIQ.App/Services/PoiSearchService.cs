using System.Net.Http;

namespace VenueIQ.App.Services;

public class PoiSearchService
{
    private readonly HttpClient _http = new();

    public Task<object> FetchPoisAsync(string category, double radiusKm, CancellationToken ct = default)
        => Task.FromResult<object>(new());

    public async Task<bool> TestApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        try
        {
            var uri = new Uri($"https://atlas.microsoft.com/search/address/json?api-version=1.0&query=Belgrade&subscription-key={Uri.EscapeDataString(apiKey)}&limit=1");
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                return false;
            // Minimal content check not required; status code is enough
            return true;
        }
        catch
        {
            return false;
        }
    }
}

namespace VenueIQ.App.Services;

public class PoiSearchService
{
    public Task<object> FetchPoisAsync(string category, double radiusKm, CancellationToken ct = default)
        => Task.FromResult<object>(new());
}


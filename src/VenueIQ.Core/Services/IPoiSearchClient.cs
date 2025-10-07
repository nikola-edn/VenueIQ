using VenueIQ.Core.Models;

namespace VenueIQ.Core.Services;

public interface IPoiSearchClient
{
    Task<PoiSearchResult> SearchAsync(AnalysisInput input, CancellationToken ct = default);
}


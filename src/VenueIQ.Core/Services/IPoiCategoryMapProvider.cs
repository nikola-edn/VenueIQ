using VenueIQ.Core.Models;

namespace VenueIQ.Core.Services;

public interface IPoiCategoryMapProvider
{
    Task<(IReadOnlyList<string> competitors, IReadOnlyList<string> complements)> GetCategoriesAsync(BusinessType business, CancellationToken ct = default);
}


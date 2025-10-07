using System.Text.Json;
using VenueIQ.Core.Models;
using VenueIQ.Core.Services;

namespace VenueIQ.App.Services;

public class CategoryMapProvider : IPoiCategoryMapProvider
{
    private class MapEntry { public string[]? competitors { get; set; } public string[]? complements { get; set; } }
    private Dictionary<string, MapEntry>? _cache;

    public async Task<(IReadOnlyList<string> competitors, IReadOnlyList<string> complements)> GetCategoriesAsync(BusinessType business, CancellationToken ct = default)
    {
        _cache ??= await LoadAsync(ct).ConfigureAwait(false);
        var key = business switch
        {
            BusinessType.Coffee => "coffee",
            BusinessType.Pharmacy => "pharmacy",
            BusinessType.Grocery => "grocery",
            BusinessType.Fitness => "fitness",
            BusinessType.KidsServices => "kids_services",
            _ => "coffee"
        };
        if (_cache.TryGetValue(key, out var entry))
        {
            return (entry.competitors ?? Array.Empty<string>(), entry.complements ?? Array.Empty<string>());
        }
        return (Array.Empty<string>(), Array.Empty<string>());
    }

    private static async Task<Dictionary<string, MapEntry>> LoadAsync(CancellationToken ct)
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("Assets/categories.serbia.json").WaitAsync(ct);
        return (await JsonSerializer.DeserializeAsync<Dictionary<string, MapEntry>>(stream, cancellationToken: ct).ConfigureAwait(false))
               ?? new Dictionary<string, MapEntry>();
    }
}


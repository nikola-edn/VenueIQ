using VenueIQ.Core.Services;

namespace VenueIQ.App.Services;

public class AzureMapsAuthProvider : IAzureMapsAuthProvider
{
    private readonly SettingsService _settings;
    public AzureMapsAuthProvider(SettingsService settings) => _settings = settings;
    public async Task<string> GetSubscriptionKeyAsync(CancellationToken ct = default)
        => await _settings.GetApiKeyAsync();
}


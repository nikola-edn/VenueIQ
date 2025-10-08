using Microsoft.Extensions.Logging;
using VenueIQ.Core.Services;

namespace VenueIQ.App.Services;

public class AzureMapsAuthProvider : IAzureMapsAuthProvider
{
    private readonly SettingsService _settings;
    private readonly ILogger<AzureMapsAuthProvider>? _logger;
    public AzureMapsAuthProvider(SettingsService settings, ILogger<AzureMapsAuthProvider>? logger = null)
    {
        _settings = settings; _logger = logger;
    }
    public async Task<string> GetSubscriptionKeyAsync(CancellationToken ct = default)
    {
        var key = (await _settings.GetApiKeyAsync().ConfigureAwait(false)) ?? string.Empty;
        _logger?.LogDebug("AzureMapsAuthProvider: subscription key present? {Present}", string.IsNullOrWhiteSpace(key) ? "no" : "yes");
        return key;
    }
}

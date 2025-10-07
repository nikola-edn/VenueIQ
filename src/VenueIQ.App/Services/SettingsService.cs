using Microsoft.Maui.Storage;

namespace VenueIQ.App.Services;

public class SettingsService
{
    private const string ApiKeyKey = "azureMapsApiKey";

    public async Task<string?> GetApiKeyAsync()
    {
        try { return await SecureStorage.Default.GetAsync(ApiKeyKey); }
        catch { return null; }
    }

    public async Task SetApiKeyAsync(string apiKey)
    {
        try { await SecureStorage.Default.SetAsync(ApiKeyKey, apiKey); }
        catch { /* swallow for now; platform-specific errors handled in later stories */ }
    }
}

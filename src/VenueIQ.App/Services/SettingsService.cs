using Microsoft.Maui.Storage;

namespace VenueIQ.App.Services;

public class SettingsService
{
    private const string ApiKeyKey = "azureMapsApiKey";
    private const string LanguageKey = "preferredLanguage"; // sr-Latn | en
    private const string RadiusKey = "radiusKm";
    private const string WComplementsKey = "w.complements";
    private const string WAccessibilityKey = "w.accessibility";
    private const string WDemandKey = "w.demand";
    private const string WCompetitionKey = "w.competition";

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

    // Language
    public Task<string> GetLanguageAsync() => Task.FromResult(Preferences.Default.Get(LanguageKey, "sr-Latn"));
    public Task SetLanguageAsync(string lang)
    {
        Preferences.Default.Set(LanguageKey, lang);
        return Task.CompletedTask;
    }

    // Radius
    public Task<double> GetRadiusKmAsync() => Task.FromResult(Preferences.Default.Get(RadiusKey, 2.0));
    public Task SetRadiusKmAsync(double km)
    {
        Preferences.Default.Set(RadiusKey, km);
        return Task.CompletedTask;
    }

    // Weights
    public Task<(double complements, double accessibility, double demand, double competition)> GetWeightsAsync()
    {
        var wc = Preferences.Default.Get(WComplementsKey, 0.35);
        var wa = Preferences.Default.Get(WAccessibilityKey, 0.25);
        var wd = Preferences.Default.Get(WDemandKey, 0.25);
        var wq = Preferences.Default.Get(WCompetitionKey, 0.35);
        return Task.FromResult((wc, wa, wd, wq));
    }

    public Task SetWeightsAsync(double complements, double accessibility, double demand, double competition)
    {
        Preferences.Default.Set(WComplementsKey, complements);
        Preferences.Default.Set(WAccessibilityKey, accessibility);
        Preferences.Default.Set(WDemandKey, demand);
        Preferences.Default.Set(WCompetitionKey, competition);
        return Task.CompletedTask;
    }

    public Task ResetToDefaultsAsync()
    {
        Preferences.Default.Set(LanguageKey, "sr-Latn");
        Preferences.Default.Set(RadiusKey, 2.0);
        Preferences.Default.Set(WComplementsKey, 0.35);
        Preferences.Default.Set(WAccessibilityKey, 0.25);
        Preferences.Default.Set(WDemandKey, 0.25);
        Preferences.Default.Set(WCompetitionKey, 0.35);
        return Task.CompletedTask;
    }
}

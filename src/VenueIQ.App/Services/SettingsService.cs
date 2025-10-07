namespace VenueIQ.App.Services;

public class SettingsService
{
    public Task<string?> GetApiKeyAsync() => Task.FromResult<string?>(null);
    public Task SetApiKeyAsync(string apiKey) => Task.CompletedTask;
}


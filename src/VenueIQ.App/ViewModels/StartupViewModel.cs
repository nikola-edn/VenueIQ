namespace VenueIQ.App.ViewModels;

public class StartupViewModel
{
    public string? ApiKey { get; set; }

    public Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        // Placeholder: Implement actual Azure Maps key validation later
        return Task.FromResult(!string.IsNullOrWhiteSpace(ApiKey));
    }
}


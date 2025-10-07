namespace VenueIQ.Core.Services;

public interface IAzureMapsAuthProvider
{
    Task<string> GetSubscriptionKeyAsync(CancellationToken ct = default);
}


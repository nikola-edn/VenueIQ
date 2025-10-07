namespace VenueIQ.App.Helpers;

public static class ServiceHost
{
    public static IServiceProvider? Services { get; set; }

    public static T GetRequiredService<T>() where T : notnull
        => Services is not null ? (T)Services.GetService(typeof(T))! : throw new InvalidOperationException("Services not initialized");
}


namespace VenueIQ.Core.Models;

public enum PoiKind { Competitor, Complement }

public record AnalysisInput(BusinessType Business, double CenterLat, double CenterLng, double RadiusKm, string Language);

public class PoiSummary
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double DistanceMeters { get; set; }
    public PoiKind Kind { get; set; }
}

public class PoiSearchMetadata
{
    public int CompetitorCount { get; set; }
    public int ComplementCount { get; set; }
    public bool Partial { get; set; }
    public string? WarningKey { get; set; }
    public string? ErrorKey { get; set; }
    public TimeSpan? Latency { get; set; }
}

public class PoiSearchResult
{
    public bool Success { get; set; }
    public List<PoiSummary> Competitors { get; set; } = new();
    public List<PoiSummary> Complements { get; set; } = new();
    public PoiSearchMetadata Meta { get; set; } = new();
}


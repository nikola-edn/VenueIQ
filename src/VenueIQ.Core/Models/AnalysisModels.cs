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

public record Weights(double Complements, double Accessibility, double Demand, double Competition);

public class CellScore
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double CI { get; set; }
    public double CoI { get; set; }
    public double AI { get; set; }
    public double DI { get; set; }
    public double Score { get; set; }
    public string? PrimaryBadge { get; set; }
    public List<string> SupportingBadges { get; set; } = new();
    public List<string> RationaleTokens { get; set; } = new();
    public double CoverageConfidence { get; set; }
}

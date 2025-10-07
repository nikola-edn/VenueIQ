# 5. Data Models

## 5.1 Core DTOs
```csharp
public record BusinessType(string Id, string DisplayName, string[] CategoryIds, string[] ComplementCategoryIds);

public record AnalysisInput(
    double CenterLat,
    double CenterLng,
    int RadiusMeters,
    string BusinessTypeId,
    double WCompetition, double WComplements, double WAccessibility, double WDemand);

public record CellScore(
    double Lat, double Lng,
    double Score, double CI, double CoI, double AI, double DI,
    IReadOnlyList<string> TopFactors);

public record ResultItem(
    double Lat, double Lng,
    double Score,
    IReadOnlyList<string> Rationale,
    IReadOnlyList<PoiSummary> NearestCompetitors,
    IReadOnlyList<PoiSummary> NearestComplements);

public record PoiSummary(string Name, string Category, double DistanceMeters, double Lat, double Lng);
```

## 5.2 Category Mapping
`Assets/categories.serbia.json` maps dropdown items → Azure Maps category arrays for **Coffee, Pharmacy, Grocery, Fitness, Kids Services** (extensible).

---

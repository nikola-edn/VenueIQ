namespace VenueIQ.Core.Models;

public class PoiDto { public string? Id { get; set; } public string? Name { get; set; } public double Lat { get; set; } public double Lng { get; set; } }
public class HeatmapCellDto { public double Lat { get; set; } public double Lng { get; set; } public double Intensity { get; set; } }
public class ResultItemDto { public string? Address { get; set; } public double Score { get; set; } }
public class AnalysisResultDto
{
    public List<HeatmapCellDto> Heatmap { get; set; } = new();
    public List<ResultItemDto> Results { get; set; } = new();
    public List<CellScore>? CellDetails { get; set; }
}

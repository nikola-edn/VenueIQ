namespace VenueIQ.App.Services;

public class PdfExportOptions
{
    public string Language { get; set; } = "sr-Latn";
    public string Orientation { get; set; } = "Portrait"; // or "Landscape"
    public bool IncludeMapThumbnails { get; set; } = true;
    public bool IncludePoiTables { get; set; } = true;
    public bool IncludeMethodology { get; set; } = true;
    public bool IncludeExecutiveSummary { get; set; } = false;
    public int MaxResults { get; set; } = 10;
}


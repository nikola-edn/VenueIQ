namespace VenueIQ.App.Services;

public class ExportService
{
    public Task ExportHeatmapAsync(string filePath, CancellationToken ct = default) => Task.CompletedTask;
    public Task ExportResultsPdfAsync(string filePath, CancellationToken ct = default) => Task.CompletedTask;
}


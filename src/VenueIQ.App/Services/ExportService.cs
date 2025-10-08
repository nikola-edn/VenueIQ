using VenueIQ.App.Controls;

namespace VenueIQ.App.Services;

public class ExportService
{
    public async Task<string?> ExportHeatmapAsync(
        MapWebView map,
        string format,
        double scale,
        (double c, double a, double d, double q) weights,
        double radiusKm,
        string business,
        CancellationToken ct = default)
    {
        var fmt = NormalizeFormat(format);
        var bytes = await map.CaptureImageAsync(fmt, scale, ct).ConfigureAwait(false);
        if (bytes is null || bytes.Length == 0) return null;

        var fileName = BuildHeatmapFileName(business, radiusKm, weights, fmt);
        var dir = FileSystem.AppDataDirectory;
        var full = Path.Combine(dir, fileName);
        try
        {
            Directory.CreateDirectory(dir);
            await File.WriteAllBytesAsync(full, bytes, ct).ConfigureAwait(false);
            return full;
        }
        catch
        {
            return null;
        }
    }

    public static string BuildHeatmapFileName(string business, double radiusKm, (double c, double a, double d, double q) w, string format)
    {
        var ts = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
        var biz = string.IsNullOrWhiteSpace(business) ? "Business" : business;
        string weights = $"c{w.c:0.00}_a{w.a:0.00}_d{w.d:0.00}_q{w.q:0.00}";
        var ext = NormalizeFormat(format) == "jpeg" ? "jpg" : "png";
        return $"VenueIQ_Heatmap_{biz}_r{radiusKm:0.0}km_{weights}_{ts}.{ext}";
    }

    private static string NormalizeFormat(string format)
    {
        var f = (format ?? "").Trim().ToLowerInvariant();
        return (f == "jpeg" || f == "jpg") ? "jpeg" : "png";
    }

    public Task ExportResultsPdfAsync(string filePath, CancellationToken ct = default) => Task.CompletedTask;
}

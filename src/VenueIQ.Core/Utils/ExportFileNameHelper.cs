namespace VenueIQ.Core.Utils;

public static class ExportFileNameHelper
{
    public static string BuildHeatmapFileName(string business, double radiusKm, (double c, double a, double d, double q) w, string format)
    {
        var ts = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
        var biz = string.IsNullOrWhiteSpace(business) ? "Business" : business;
        string weights = $"c{w.c:0.00}_a{w.a:0.00}_d{w.d:0.00}_q{w.q:0.00}";
        var f = (format ?? "").Trim().ToLowerInvariant();
        var ext = (f == "jpeg" || f == "jpg") ? "jpg" : "png";
        return $"VenueIQ_Heatmap_{biz}_r{radiusKm:0.0}km_{weights}_{ts}.{ext}";
    }

    public static string BuildPdfFileName(string business, double radiusKm, (double c, double a, double d, double q) weights)
    {
        var ts = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
        var biz = string.IsNullOrWhiteSpace(business) ? "Business" : business;
        string w = $"c{weights.c:0.00}_a{weights.a:0.00}_d{weights.d:0.00}_q{weights.q:0.00}";
        return $"VenueIQ_Report_{biz}_r{radiusKm:0.0}km_{w}_{ts}.pdf";
    }
}


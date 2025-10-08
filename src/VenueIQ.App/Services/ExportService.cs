using VenueIQ.App.Controls;
using VenueIQ.App.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using VenueIQ.Core.Utils;

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

        var fileName = ExportFileNameHelper.BuildHeatmapFileName(business, radiusKm, weights, fmt);
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

    // kept for backward compatibility in codebase (delegates to Core helper)
    public static string BuildHeatmapFileName(string business, double radiusKm, (double c, double a, double d, double q) w, string format)
        => ExportFileNameHelper.BuildHeatmapFileName(business, radiusKm, w, format);

    private static string NormalizeFormat(string format)
    {
        var f = (format ?? "").Trim().ToLowerInvariant();
        return (f == "jpeg" || f == "jpg") ? "jpeg" : "png";
    }

    public Task ExportResultsPdfAsync(string filePath, CancellationToken ct = default) => Task.CompletedTask;

    public async Task<string?> ExportResultsPdfAsync(
        MapWebView map,
        IEnumerable<ResultItemViewModel> results,
        (double c, double a, double d, double q) weights,
        double radiusKm,
        string business,
        PdfExportOptions options,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report("ExportPdf_Building");
        LicenseType license = LicenseType.Community;
        QuestPDF.Settings.License = license;

        // Capture cover map snapshot
        byte[]? cover = await map.CaptureImageAsync("png", 1.0, ct).ConfigureAwait(false);
        var ts = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var top = results.Take(Math.Max(1, options.MaxResults)).ToList();

        var path = Path.Combine(FileSystem.AppDataDirectory, ExportFileNameHelper.BuildPdfFileName(business, radiusKm, weights));
        try
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Set size and orientation
                    if (options.Orientation?.Equals("Landscape", StringComparison.OrdinalIgnoreCase) == true)
                        page.Size(PageSizes.A4.Landscape());
                    else
                        page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Text($"VenueIQ Report — {business}").SemiBold().FontSize(16);

                    page.Content().Column(col =>
                    {
                        // Cover section
                        col.Item().Element(e =>
                        {
                            e.Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"Generated: {ts}");
                                    c.Item().Text($"Radius: {radiusKm:0.0} km");
                                    c.Item().Text($"Weights: C {weights.c:0.00} / A {weights.a:0.00} / D {weights.d:0.00} / Q {weights.q:0.00}");
                                });
                                if (cover is not null && options.IncludeMapThumbnails)
                                {
                                    row.ConstantItem(220).Image(cover);
                                }
                            });
                        });

                        // Results list
                        col.Item().PaddingTop(10).Text("Top Results").Bold().FontSize(12);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(24);
                                columns.RelativeColumn(1);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Text("#").SemiBold();
                                header.Cell().Text("Coordinates").SemiBold();
                                header.Cell().Text("Score").SemiBold();
                                header.Cell().Text("Badges").SemiBold();
                            });
                            foreach (var r in top)
                            {
                                table.Cell().Text(r.Rank.ToString());
                                table.Cell().Text($"{r.Lat:0.00000}, {r.Lng:0.00000}");
                                table.Cell().Text(r.Score.ToString("0.000"));
                                var badges = new List<string>();
                                if (!string.IsNullOrWhiteSpace(r.PrimaryBadgeKey)) badges.Add(r.PrimaryBadgeKey);
                                badges.AddRange(r.SupportingBadgeKeys);
                                table.Cell().Text(string.Join(", ", badges));
                            }
                        });

                        if (options.IncludeMethodology)
                        {
                            col.Item().PageBreak();
                            col.Item().Text("Methodology").Bold().FontSize(12);
                            col.Item().Text("This report summarizes computed scores based on complements, accessibility, demand and competition weights. See product documentation for details.");
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(path);
        }
        catch
        {
            return null;
        }
        progress?.Report("ExportPdf_Success");
        return path;
    }

    public static string BuildPdfFileName(string business, double radiusKm, (double c, double a, double d, double q) weights)
    {
        var ts = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
        var biz = string.IsNullOrWhiteSpace(business) ? "Business" : business;
        string w = $"c{weights.c:0.00}_a{weights.a:0.00}_d{weights.d:0.00}_q{weights.q:0.00}";
        return $"VenueIQ_Report_{biz}_r{radiusKm:0.0}km_{w}_{ts}.pdf";
    }
}

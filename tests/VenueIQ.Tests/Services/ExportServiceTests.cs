using VenueIQ.Core.Utils;
using Xunit;

namespace VenueIQ.Tests.Services;

public class ExportServiceTests
{
    [Fact]
    public void BuildHeatmapFileName_IncludesMetadata_AndExtension()
    {
        var namePng = ExportFileNameHelper.BuildHeatmapFileName("Coffee", 2.0, (0.35, 0.25, 0.25, 0.35), "png");
        Assert.EndsWith(".png", namePng);
        Assert.Contains("VenueIQ_Heatmap_Coffee_r2.0km", namePng);
        Assert.Contains("c0.35_a0.25_d0.25_q0.35", namePng);

        var nameJpg = ExportFileNameHelper.BuildHeatmapFileName("Coffee", 2.0, (0.35, 0.25, 0.25, 0.35), "jpeg");
        Assert.EndsWith(".jpg", nameJpg);
    }
}

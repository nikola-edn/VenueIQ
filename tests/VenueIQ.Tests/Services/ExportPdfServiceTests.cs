using VenueIQ.Core.Utils;
using Xunit;

namespace VenueIQ.Tests.Services;

public class ExportPdfServiceTests
{
    [Fact]
    public void BuildPdfFileName_IncludesMetadata()
    {
        var name = ExportFileNameHelper.BuildPdfFileName("Coffee", 2.0, (0.35, 0.25, 0.25, 0.35));
        Assert.EndsWith(".pdf", name);
        Assert.Contains("VenueIQ_Report_Coffee_r2.0km", name);
        Assert.Contains("c0.35_a0.25_d0.25_q0.35", name);
    }
}

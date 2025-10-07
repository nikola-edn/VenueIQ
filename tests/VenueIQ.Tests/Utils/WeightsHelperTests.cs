using VenueIQ.Core.Utils;
using Xunit;

namespace VenueIQ.Tests.Utils;

public class WeightsHelperTests
{
    [Fact]
    public void FromPercentages_NormalizesPositives_ToSixtyFivePercent()
    {
        var (c, a, d, q) = WeightsHelper.FromPercentages(35, 25, 25, 35);
        // Positive weights should sum to ~0.65
        var pos = c + a + d;
        Assert.InRange(pos, 0.649, 0.651);
        // Competition maps to 35% => 0.35
        Assert.InRange(q, 0.349, 0.351);
    }

    [Fact]
    public void FromPercentages_HandlesZeroPositives()
    {
        var (c, a, d, q) = WeightsHelper.FromPercentages(0, 0, 0, 80);
        Assert.Equal(0, c);
        Assert.Equal(0, a);
        Assert.Equal(0, d);
        Assert.InRange(q, 0.799, 0.801); // 80% -> 0.8
    }
}

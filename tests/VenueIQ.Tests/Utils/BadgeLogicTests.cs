using VenueIQ.Core.Utils;
using Xunit;

namespace VenueIQ.Tests.Utils;

public class BadgeLogicTests
{
    [Fact]
    public void Competition_High_IsWarning()
    {
        var d = BadgeLogic.ForCompetition(0.85);
        Assert.Equal(BadgeSeverity.Warning, d.Severity);
        Assert.Equal("badge_tt_competition_high", d.TooltipKey);
    }

    [Fact]
    public void Complements_High_IsSuccess()
    {
        var d = BadgeLogic.ForComplements(0.8);
        Assert.Equal(BadgeSeverity.Success, d.Severity);
        Assert.Equal("badge_tt_complements_high", d.TooltipKey);
    }

    [Fact]
    public void Accessibility_Low_HiddenOrInfo()
    {
        var d = BadgeLogic.ForAccessibility(0.1);
        Assert.True(d.Severity == BadgeSeverity.None || d.PrimaryMetricValue < BadgeLogic.HideThreshold);
    }

    [Fact]
    public void Demand_Medium_IsInfo()
    {
        var d = BadgeLogic.ForDemand(0.55);
        Assert.Equal(BadgeSeverity.Info, d.Severity);
    }
}


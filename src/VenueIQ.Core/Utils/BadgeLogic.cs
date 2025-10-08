namespace VenueIQ.Core.Utils;

public enum BadgeSeverity { None = 0, Info = 1, Success = 2, Warning = 3 }

public record BadgeDescriptor(string TitleKey, string TooltipKey, BadgeSeverity Severity, double PrimaryMetricValue);

public static class BadgeLogic
{
    public const double HideThreshold = 0.20; // below this, hide badge
    public const double MediumThreshold = 0.50;
    public const double HighThreshold = 0.70;

    public static BadgeDescriptor ForCompetition(double competitionIndex)
    {
        var v = Clamp01(competitionIndex);
        if (v < HideThreshold) return new("badge_factor_competition", "badge_tt_competition_low", BadgeSeverity.None, v);
        if (v >= HighThreshold) return new("badge_factor_competition", "badge_tt_competition_high", BadgeSeverity.Warning, v);
        if (v >= MediumThreshold) return new("badge_factor_competition", "badge_tt_competition_medium", BadgeSeverity.Info, v);
        return new("badge_factor_competition", "badge_tt_competition_low", BadgeSeverity.Info, v);
    }

    public static BadgeDescriptor ForComplements(double complementsIndex)
    {
        var v = Clamp01(complementsIndex);
        if (v < HideThreshold) return new("badge_factor_complements", "badge_tt_complements_low", BadgeSeverity.None, v);
        if (v >= HighThreshold) return new("badge_factor_complements", "badge_tt_complements_high", BadgeSeverity.Success, v);
        if (v >= MediumThreshold) return new("badge_factor_complements", "badge_tt_complements_medium", BadgeSeverity.Info, v);
        return new("badge_factor_complements", "badge_tt_complements_low", BadgeSeverity.Info, v);
    }

    public static BadgeDescriptor ForAccessibility(double accessibilityIndex)
    {
        var v = Clamp01(accessibilityIndex);
        if (v < HideThreshold) return new("badge_factor_accessibility", "badge_tt_accessibility_low", BadgeSeverity.None, v);
        if (v >= HighThreshold) return new("badge_factor_accessibility", "badge_tt_accessibility_high", BadgeSeverity.Success, v);
        if (v >= MediumThreshold) return new("badge_factor_accessibility", "badge_tt_accessibility_medium", BadgeSeverity.Info, v);
        return new("badge_factor_accessibility", "badge_tt_accessibility_low", BadgeSeverity.Info, v);
    }

    public static BadgeDescriptor ForDemand(double demandIndex)
    {
        var v = Clamp01(demandIndex);
        if (v < HideThreshold) return new("badge_factor_demand", "badge_tt_demand_low", BadgeSeverity.None, v);
        if (v >= HighThreshold) return new("badge_factor_demand", "badge_tt_demand_high", BadgeSeverity.Success, v);
        if (v >= MediumThreshold) return new("badge_factor_demand", "badge_tt_demand_medium", BadgeSeverity.Info, v);
        return new("badge_factor_demand", "badge_tt_demand_low", BadgeSeverity.Info, v);
    }

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
}


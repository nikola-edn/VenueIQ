namespace VenueIQ.Core.Utils;

public static class WeightsHelper
{
    // Converts UI percentages (0-100) into normalized decimals used by scoring.
    // Positive factors (C, A, D) are normalized to sum to 0.65 in total.
    // Competition percentage maps to up to 0.35 and is subtracted in the scoring formula.
    public static (double complements, double accessibility, double demand, double competition) FromPercentages(
        double complementsPct, double accessibilityPct, double demandPct, double competitionPct)
    {
        var pos = Math.Max(0.0, complementsPct) + Math.Max(0.0, accessibilityPct) + Math.Max(0.0, demandPct);
        double c = 0, a = 0, d = 0;
        if (pos > 1e-9)
        {
            var scale = 0.65 / pos; // normalize positives to 65%
            c = Math.Max(0.0, complementsPct) * scale;
            a = Math.Max(0.0, accessibilityPct) * scale;
            d = Math.Max(0.0, demandPct) * scale;
        }
        // Competition maps directly to decimal percent (e.g., 35 -> 0.35)
        var q = Math.Clamp(competitionPct, 0.0, 100.0) / 100.0;
        return (c, a, d, q);
    }
}

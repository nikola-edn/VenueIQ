namespace VenueIQ.Core.Services;

public class ScoreCalculator
{
    // Legacy default weighting retained for backward compatibility
    public double CalculateScore(double complements, double accessibility, double demand, double competition)
        => 0.35 * complements + 0.25 * accessibility + 0.25 * demand - 0.35 * competition;

    // Preferred overload: uses user-configured weights
    public double CalculateScore(double complements, double accessibility, double demand, double competition, VenueIQ.Core.Models.Weights weights)
        => (weights.Complements * complements)
         + (weights.Accessibility * accessibility)
         + (weights.Demand * demand)
         - (weights.Competition * competition);
}

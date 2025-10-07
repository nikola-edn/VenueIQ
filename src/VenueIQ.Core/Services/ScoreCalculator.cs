namespace VenueIQ.Core.Services;

public class ScoreCalculator
{
    public double CalculateScore(double complements, double accessibility, double demand, double competition)
        => 0.35 * complements + 0.25 * accessibility + 0.25 * demand - 0.35 * competition;
}


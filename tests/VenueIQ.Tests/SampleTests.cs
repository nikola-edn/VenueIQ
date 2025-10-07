using VenueIQ.Core.Services;
using Xunit;

namespace VenueIQ.Tests;

public class SampleTests
{
    [Fact]
    public void ScoreCalculator_UsesExpectedWeights()
    {
        var sc = new ScoreCalculator();
        var score = sc.CalculateScore(complements: 1.0, accessibility: 1.0, demand: 1.0, competition: 1.0);
        Assert.InRange(score, -0.01, 1.0); // 0.35 + 0.25 + 0.25 - 0.35 = 0.5
        Assert.Equal(0.5, score, precision: 6);
    }
}

using System.Linq;
using VenueIQ.Core.Models;
using VenueIQ.Core.Services;
using Xunit;

namespace VenueIQ.Tests.Services;

public class AnalysisEngineTests
{
    [Fact]
    public void GenerateGrid_ProducesCellsWithinRadius()
    {
        var eng = new AnalysisEngine(new ScoreCalculator());
        var grid = eng.GenerateGrid(44.787, 20.449, 2.0, targetCells: 100);
        Assert.True(grid.Cells.Count > 50);
        foreach (var c in grid.Cells)
        {
            var d = AnalysisEngine.HaversineMeters(44.787, 20.449, c.lat, c.lng);
            Assert.True(d <= 2000 + 1e-6);
        }
    }

    [Fact]
    public void ComputeScores_HandlesNoData()
    {
        var eng = new AnalysisEngine(new ScoreCalculator());
        var grid = eng.GenerateGrid(44.787, 20.449, 1.0, targetCells: 50);
        var cells = eng.ComputeScores(grid, new PoiSummary[0], new PoiSummary[0], new Weights(0.35,0.25,0.25,0.35));
        Assert.Equal(grid.Cells.Count, cells.Count);
        Assert.All(cells, c => Assert.True(c.Score >= 0));
    }
}


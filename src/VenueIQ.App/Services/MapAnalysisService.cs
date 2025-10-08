using System.Diagnostics;
using System.Linq;
using VenueIQ.Core.Models;
using VenueIQ.Core.Services;

namespace VenueIQ.App.Services;

public class MapAnalysisService
{
    private readonly ScoreCalculator _scoreCalculator;
    private readonly IPoiSearchClient _poiClient;
    private readonly AnalysisEngine _engine;
    private AnalysisContext? _last;
    public MapAnalysisService(ScoreCalculator scoreCalculator, IPoiSearchClient poiClient)
    {
        _scoreCalculator = scoreCalculator;
        _poiClient = poiClient;
        _engine = new AnalysisEngine(_scoreCalculator);
    }

    public async Task<AnalysisResultDto> AnalyzeAsync(AnalysisInput input, Weights weights, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var pois = await _poiClient.SearchAsync(input, ct).ConfigureAwait(false);
        var grid = _engine.GenerateGrid(input.CenterLat, input.CenterLng, input.RadiusKm);
        var scores = _engine.ComputeScores(grid, pois.Competitors, pois.Complements, weights);
        var heat = scores.Select(s => new HeatmapCellDto { Lat = s.Lat, Lng = s.Lng, Intensity = s.Score }).ToList();
        var top = scores.OrderByDescending(s => s.Score).Take(10)
            .Select((s, idx) => new ResultItemDto { Address = $"Cell {idx + 1}", Score = s.Score }).ToList();
        sw.Stop();
        // TODO: log sw.Elapsed and flag if > threshold
        var result = new AnalysisResultDto { Heatmap = heat, Results = top, CellDetails = scores };
        _last = new AnalysisContext(input, grid, pois);
        return result;
    }

    public Task<bool> HasCachedAsync() => Task.FromResult(_last is not null);

    public Task<AnalysisResultDto> RecomputeAsync(Weights weights, CancellationToken ct = default)
    {
        if (_last is null)
        {
            throw new InvalidOperationException("No cached analysis available for recompute.");
        }
        ct.ThrowIfCancellationRequested();
        var grid = _last.Grid;
        var pois = _last.Pois;
        var scores = _engine.ComputeScores(grid, pois.Competitors, pois.Complements, weights);
        var heat = scores.Select(s => new HeatmapCellDto { Lat = s.Lat, Lng = s.Lng, Intensity = s.Score }).ToList();
        var top = scores.OrderByDescending(s => s.Score).Take(10)
            .Select((s, idx) => new ResultItemDto { Address = $"Cell {idx + 1}", Score = s.Score }).ToList();
        var result = new AnalysisResultDto { Heatmap = heat, Results = top, CellDetails = scores };
        return Task.FromResult(result);
    }

    private sealed record AnalysisContext(AnalysisInput Input, AnalysisEngine.Grid Grid, PoiSearchResult Pois);
}

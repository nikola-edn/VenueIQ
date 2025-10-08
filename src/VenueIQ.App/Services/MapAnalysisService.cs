using System.Diagnostics;
using System.Linq;
using VenueIQ.Core.Models;
using VenueIQ.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using System.Text;
using System.Globalization;

namespace VenueIQ.App.Services;

public class MapAnalysisService
{
    private readonly ScoreCalculator _scoreCalculator;
    private readonly IPoiSearchClient _poiClient;
    private readonly AnalysisEngine _engine;
    private readonly ILogger<MapAnalysisService>? _logger;
    private AnalysisContext? _last;
    public MapAnalysisService(ScoreCalculator scoreCalculator, IPoiSearchClient poiClient, ILogger<MapAnalysisService>? logger = null)
    {
        _scoreCalculator = scoreCalculator;
        _poiClient = poiClient;
        _engine = new AnalysisEngine(_scoreCalculator);
        _logger = logger;
    }

    public async Task<AnalysisResultDto> AnalyzeAsync(AnalysisInput input, Weights weights, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogInformation("AnalyzeAsync: begin Business={Business} Center=({Lat:F5},{Lng:F5}) RadiusKm={Radius} Lang={Lang}", input.Business, input.CenterLat, input.CenterLng, input.RadiusKm, input.Language);
        _logger?.LogDebug("AnalyzeAsync: weights C={C:0.000} A={A:0.000} D={D:0.000} Q={Q:0.000}", weights.Complements, weights.Accessibility, weights.Demand, weights.Competition);
        var pois = await _poiClient.SearchAsync(input, ct).ConfigureAwait(false);
        _logger?.LogInformation("AnalyzeAsync: POIs Success={Success} Competitors={CompCount} Complements={ComplCount} Partial={Partial}", pois.Success, pois.Meta.CompetitorCount, pois.Meta.ComplementCount, pois.Meta.Partial);
        if ((pois.Competitors?.Count ?? 0) == 0 && (pois.Complements?.Count ?? 0) == 0)
        {
            _logger?.LogWarning("AnalyzeAsync: no POIs fetched; returning empty result");
            _last = null;
            return new AnalysisResultDto { Heatmap = new(), Results = new(), CellDetails = new() };
        }
        var grid = _engine.GenerateGrid(input.CenterLat, input.CenterLng, input.RadiusKm);
        _logger?.LogDebug("AnalyzeAsync: grid cells={Cells} step={Step:0.0}m", grid.Cells.Count, grid.StepMeters);
        var scores = _engine.ComputeScores(grid, pois.Competitors, pois.Complements, weights);
        try
        {
            var min = scores.Min(s => s.Score);
            var max = scores.Max(s => s.Score);
            var avg = scores.Average(s => s.Score);
            _logger?.LogDebug("AnalyzeAsync: score range min={Min:0.000} max={Max:0.000} avg={Avg:0.000}", min, max, avg);

            // Compute percentiles for contrast diagnostics
            double P(IReadOnlyList<double> arr, double p)
            {
                if (arr.Count == 0) return 0;
                var idx = (int)Math.Round(p * (arr.Count - 1));
                idx = Math.Clamp(idx, 0, arr.Count - 1);
                return arr[idx];
            }
            var sorted = scores.Select(s => s.Score).OrderBy(v => v).ToArray();
            var p10 = P(sorted, 0.10);
            var p50 = P(sorted, 0.50);
            var p90 = P(sorted, 0.90);
            _logger?.LogDebug("AnalyzeAsync: percentiles p10={P10:0.000} p50={P50:0.000} p90={P90:0.000}", p10, p50, p90);

            // Write debug CSV with per-cell values (raw, normalized, visualized)
            var csvPath = await WriteCellsCsvAsync(scores, min, max, p10, p90, 0.6, ct).ConfigureAwait(false);
            _logger?.LogInformation("AnalyzeAsync: wrote cells CSV: {Path}", csvPath);
        }
        catch { /* ignore */ }
        var heat = scores.Select(s => new HeatmapCellDto { Lat = s.Lat, Lng = s.Lng, Intensity = s.Score }).ToList();
        var top = scores.OrderByDescending(s => s.Score).Take(10)
            .Select((s, idx) => new ResultItemDto { Address = $"Cell {idx + 1}", Score = s.Score }).ToList();
        sw.Stop();
        _logger?.LogInformation("AnalyzeAsync: completed in {ElapsedMs} ms (cells={Cells}, topN={Top})", sw.ElapsedMilliseconds, grid.Cells.Count, top.Count);
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

    private static async Task<string> WriteCellsCsvAsync(IEnumerable<CellScore> cells, double min, double max, double p10, double p90, double gamma, CancellationToken ct)
    {
        var dir = FileSystem.AppDataDirectory;
        Directory.CreateDirectory(dir);
        var ts = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
        var path = Path.Combine(dir, $"VenueIQ_Cells_{ts}.csv");
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var sw = new StreamWriter(fs, new UTF8Encoding(false));
        await sw.WriteLineAsync("lat,lng,scoreRaw,scoreNorm,scoreVis,ci,coi,ai,di,stepMeters").ConfigureAwait(false);
        double range = Math.Max(1e-9, max - min);
        double stretch = Math.Max(1e-9, p90 - p10);
        foreach (var c in cells)
        {
            var norm = Math.Clamp((c.Score - min) / range, 0.0, 1.0);
            var stretched = Math.Clamp((c.Score - p10) / stretch, 0.0, 1.0);
            var vis = Math.Pow(stretched, gamma);
            var line = string.Format(CultureInfo.InvariantCulture,
                "{0:0.000000},{1:0.000000},{2:0.000},{3:0.000},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000},{9:0.0}",
                c.Lat, c.Lng, c.Score, norm, vis, c.CI, c.CoI, c.AI, c.DI, c.StepMeters);
            await sw.WriteLineAsync(line).ConfigureAwait(false);
        }
        await sw.FlushAsync().ConfigureAwait(false);
        return path;
    }
}

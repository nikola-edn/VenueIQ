using System.Diagnostics;
using System.Linq;
using VenueIQ.Core.Models;

namespace VenueIQ.Core.Services;

public class AnalysisEngine
{
    private readonly ScoreCalculator _score;
    public AnalysisEngine(ScoreCalculator score) => _score = score;

    public record Grid(List<(double lat, double lng)> Cells, double StepMeters);

    public Grid GenerateGrid(double centerLat, double centerLng, double radiusKm, int targetCells = 250)
    {
        var radiusMeters = radiusKm * 1000.0;
        var area = Math.PI * radiusMeters * radiusMeters;
        var step = Math.Max(50.0, Math.Sqrt(area / targetCells));

        var metersPerDegLat = 111_320.0;
        var metersPerDegLon = metersPerDegLat * Math.Cos(centerLat * Math.PI / 180.0);
        var dLat = step / metersPerDegLat;
        var dLon = step / metersPerDegLon;

        var cells = new List<(double lat, double lng)>();
        var halfLat = radiusMeters / metersPerDegLat;
        var halfLon = radiusMeters / metersPerDegLon;
        for (double lat = centerLat - halfLat; lat <= centerLat + halfLat; lat += dLat)
        {
            for (double lon = centerLng - halfLon; lon <= centerLng + halfLon; lon += dLon)
            {
                if (HaversineMeters(centerLat, centerLng, lat, lon) <= radiusMeters)
                    cells.Add((lat, lon));
            }
        }
        return new Grid(cells, step);
    }

    public List<CellScore> ComputeScores(Grid grid, IReadOnlyList<PoiSummary> competitors, IReadOnlyList<PoiSummary> complements, Weights weights)
    {
        var compArr = competitors.ToArray();
        var compoArr = complements.ToArray();
        var ciRaw = new double[grid.Cells.Count];
        var coiRaw = new double[grid.Cells.Count];
        var aiRaw = new double[grid.Cells.Count];
        var diRaw = new double[grid.Cells.Count];

        // Category buckets for AI and DI derivation
        static bool IsAccess(string? code) => code == "POI_PARKING" || code == "POI_PUBLIC_TRANSPORT_STATION";
        static bool IsDemand(string? code) => code == "POI_SCHOOL" || code == "POI_OFFICE" || code == "POI_APARTMENT";

        for (int i = 0; i < grid.Cells.Count; i++)
        {
            var (lat, lng) = grid.Cells[i];
            double ci = 0, co = 0, ai = 0, di = 0;

            foreach (var p in compArr)
            {
                var d = HaversineMeters(lat, lng, p.Lat, p.Lng);
                ci += Math.Exp(-d / 300.0);
            }
            foreach (var p in compoArr)
            {
                var d = HaversineMeters(lat, lng, p.Lat, p.Lng);
                var k = Math.Exp(-d / 200.0);
                co += k;
                if (IsAccess(p.Category)) ai += k;
                if (IsDemand(p.Category)) di += k;
            }
            if (ai == 0) ai = co * 0.5; // fallback heuristic
            if (di == 0) di = co * 0.5; // fallback heuristic
            ciRaw[i] = ci; coiRaw[i] = co; aiRaw[i] = ai; diRaw[i] = di;
        }

        var ciN = Normalize(ciRaw);
        var coiN = Normalize(coiRaw);
        var aiN = Normalize(aiRaw);
        var diN = Normalize(diRaw);

        var list = new List<CellScore>(grid.Cells.Count);
        for (int i = 0; i < grid.Cells.Count; i++)
        {
            var score = _score.CalculateScore(coiN[i], aiN[i], diN[i], ciN[i]);
            var cs = new CellScore
            {
                Lat = grid.Cells[i].lat,
                Lng = grid.Cells[i].lng,
                CI = ciN[i], CoI = coiN[i], AI = aiN[i], DI = diN[i],
                Score = score,
                CoverageConfidence = Math.Clamp((coiN[i] + diN[i]) / 2.0, 0, 1)
            };
            cs.PrimaryBadge = ciN[i] > 0.7 ? "badge.high_competition" : (coiN[i] > 0.7 ? "badge.strong_complements" : null);
            if (aiN[i] > 0.6) cs.SupportingBadges.Add("badge.good_access");
            if (diN[i] > 0.6) cs.SupportingBadges.Add("badge.high_demand");
            list.Add(cs);
        }
        return list;
    }

    public static double[] Normalize(double[] values)
    {
        if (values.Length == 0) return values;
        double min = values[0], max = values[0];
        foreach (var v in values) { if (v < min) min = v; if (v > max) max = v; }
        var range = max - min;
        if (range <= 1e-9) return values.Select(_ => 0.0).ToArray();
        var arr = new double[values.Length];
        for (int i = 0; i < values.Length; i++) arr[i] = (values[i] - min) / range;
        return arr;
    }

    public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}

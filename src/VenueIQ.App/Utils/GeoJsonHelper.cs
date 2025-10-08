using System.Text;
using System.Text.Json;
using VenueIQ.Core.Models;

namespace VenueIQ.App.Utils;

public static class GeoJsonHelper
{
    public static string BuildFeatureCollection(IEnumerable<CellScore> cells)
    {
        // Materialize and normalize scores for map rendering with contrast enhancement
        var list = cells.ToList();
        double min = list.Count > 0 ? list.Min(c => c.Score) : 0.0;
        double max = list.Count > 0 ? list.Max(c => c.Score) : 1.0;
        double range = max - min;
        // Robust percentile stretch (p10..p90) to avoid outliers flattening contrast
        double p10 = min, p90 = max;
        if (list.Count >= 5)
        {
            var sorted = list.Select(c => c.Score).OrderBy(v => v).ToArray();
            double P(double p)
            {
                var idx = (int)Math.Round(p * (sorted.Length - 1));
                idx = Math.Clamp(idx, 0, sorted.Length - 1);
                return sorted[idx];
            }
            p10 = P(0.10);
            p90 = P(0.90);
            if (p90 - p10 < 1e-9) { p10 = min; p90 = max; }
        }
        // Gamma curve to amplify mid/high differences
        const double gamma = 0.6; // <1 boosts contrast
        using var buffer = new MemoryStream();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WriteString("type", "FeatureCollection");
        writer.WritePropertyName("features");
        writer.WriteStartArray();
        foreach (var c in list)
        {
            var normRange = range <= 1e-9 ? 0.0 : Math.Clamp((c.Score - min) / range, 0.0, 1.0);
            var stretched = Math.Clamp((c.Score - p10) / Math.Max(1e-9, (p90 - p10)), 0.0, 1.0);
            var vis = Math.Pow(stretched, gamma);
            writer.WriteStartObject();
            writer.WriteString("type", "Feature");
            writer.WritePropertyName("geometry");
            writer.WriteStartObject();
            writer.WriteString("type", "Polygon");
            writer.WritePropertyName("coordinates");
            // Build a square around the cell center with half-size = step/2 (in meters)
            var half = (c.StepMeters > 0 ? c.StepMeters : 150) / 2.0; // fallback 150m if not set
            var metersPerDegLat = 111320.0;
            var metersPerDegLon = metersPerDegLat * Math.Cos(c.Lat * Math.PI / 180.0);
            var dLat = half / metersPerDegLat;
            var dLon = half / metersPerDegLon;
            writer.WriteStartArray(); // ring array
            writer.WriteStartArray(); // linear ring coordinates
            writer.WriteStartArray(); writer.WriteNumberValue(c.Lng - dLon); writer.WriteNumberValue(c.Lat - dLat); writer.WriteEndArray();
            writer.WriteStartArray(); writer.WriteNumberValue(c.Lng + dLon); writer.WriteNumberValue(c.Lat - dLat); writer.WriteEndArray();
            writer.WriteStartArray(); writer.WriteNumberValue(c.Lng + dLon); writer.WriteNumberValue(c.Lat + dLat); writer.WriteEndArray();
            writer.WriteStartArray(); writer.WriteNumberValue(c.Lng - dLon); writer.WriteNumberValue(c.Lat + dLat); writer.WriteEndArray();
            writer.WriteStartArray(); writer.WriteNumberValue(c.Lng - dLon); writer.WriteNumberValue(c.Lat - dLat); writer.WriteEndArray(); // close ring
            writer.WriteEndArray();
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            writer.WriteNumber("score", normRange);
            writer.WriteNumber("scoreVis", vis);
            // Discrete bucket 1..5 for stepped/matched styling
            int bucket = (int)Math.Floor(vis * 5.0) + 1;
            if (bucket < 1) bucket = 1; if (bucket > 5) bucket = 5;
            writer.WriteNumber("bucket", bucket);
            // Precompute color string to avoid client expression issues
            string color = bucket switch
            {
                1 => "#2C7BB6",
                2 => "#00B3E6",
                3 => "#FFFF66",
                4 => "#FDAE61",
                _ => "#D7191C"
            };
            writer.WriteString("color", color);
            writer.WriteNumber("scoreRaw", c.Score);
            writer.WriteNumber("ci", c.CI);
            writer.WriteNumber("coi", c.CoI);
            writer.WriteNumber("ai", c.AI);
            writer.WriteNumber("di", c.DI);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}

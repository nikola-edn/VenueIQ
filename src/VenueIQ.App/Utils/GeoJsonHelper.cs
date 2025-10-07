using System.Text;
using System.Text.Json;
using VenueIQ.Core.Models;

namespace VenueIQ.App.Utils;

public static class GeoJsonHelper
{
    public static string BuildFeatureCollection(IEnumerable<CellScore> cells)
    {
        using var buffer = new MemoryStream();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WriteString("type", "FeatureCollection");
        writer.WritePropertyName("features");
        writer.WriteStartArray();
        foreach (var c in cells)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "Feature");
            writer.WritePropertyName("geometry");
            writer.WriteStartObject();
            writer.WriteString("type", "Point");
            writer.WritePropertyName("coordinates");
            writer.WriteStartArray();
            writer.WriteNumberValue(c.Lng);
            writer.WriteNumberValue(c.Lat);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            writer.WriteNumber("score", c.Score);
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


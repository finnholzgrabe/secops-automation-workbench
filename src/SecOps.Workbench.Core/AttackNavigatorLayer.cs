using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace SecOps.Workbench.Core;

/// <summary>
/// Builds an ATT&amp;CK Navigator layer (v4.5) JSON document from mapped techniques, so the
/// workbench output can be loaded directly into the MITRE ATT&amp;CK Navigator for visualization.
/// Technique counts become the heatmap score. Output is deterministic.
/// </summary>
public static class AttackNavigatorLayer
{
    public static string Build(string name, string description, IReadOnlyList<TechniqueFrequency> techniques)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(techniques);

        var maxScore = techniques.Count == 0 ? 1 : techniques.Max(technique => technique.Count);

        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, options))
        {
            writer.WriteStartObject();
            writer.WriteString("name", name);
            writer.WriteString("description", description);
            writer.WriteString("domain", "enterprise-attack");

            writer.WriteStartObject("versions");
            writer.WriteString("attack", "14");
            writer.WriteString("navigator", "4.9.1");
            writer.WriteString("layer", "4.5");
            writer.WriteEndObject();

            writer.WriteStartArray("techniques");
            foreach (var technique in techniques)
            {
                writer.WriteStartObject();
                writer.WriteString("techniqueID", technique.TechniqueId);
                writer.WriteNumber("score", technique.Count);
                writer.WriteString("comment", technique.TechniqueName);
                writer.WriteBoolean("enabled", true);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteStartObject("gradient");
            writer.WriteStartArray("colors");
            writer.WriteStringValue("#ffffff");
            writer.WriteStringValue("#ff6666");
            writer.WriteEndArray();
            writer.WriteNumber("minValue", 0);
            writer.WriteNumber("maxValue", maxScore);
            writer.WriteEndObject();

            writer.WriteNumber("sorting", 0);
            writer.WriteBoolean("hideDisabled", false);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}

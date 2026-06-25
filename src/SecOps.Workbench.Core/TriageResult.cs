using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace SecOps.Workbench.Core;

public sealed record TriageResult(
    SecurityAlert Alert,
    IReadOnlyList<string> TechniqueIds,
    string RecommendedPlaybook,
    IReadOnlyList<string> RecommendedActions,
    string Rationale)
{
    /// <summary>Renders the report in the requested format.</summary>
    public string Render(ReportFormat format) => format switch
    {
        ReportFormat.Json => ToJson(),
        _ => ToMarkdown()
    };

    /// <summary>
    /// Serializes the result with a stable, hand-ordered top-level field layout so the
    /// JSON contract stays deterministic and diff-friendly across runs.
    /// </summary>
    public string ToJson()
    {
        // The report is written to a file or stdout, never embedded in HTML, so the relaxed
        // encoder is safe here and keeps characters like apostrophes human-readable.
        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, options))
        {
            writer.WriteStartObject();
            writer.WriteString("alertId", Alert.Id);
            writer.WriteString("title", Alert.Title);
            writer.WriteString("source", Alert.Source);
            writer.WriteString("severity", Alert.Severity.ToString());
            writer.WriteString("category", Alert.Category);
            writer.WriteString("principal", Alert.Principal);
            writer.WriteString("asset", Alert.Asset);

            writer.WriteStartArray("observables");
            foreach (var observable in Alert.Observables)
            {
                writer.WriteStringValue(observable);
            }
            writer.WriteEndArray();

            writer.WriteStartArray("techniqueIds");
            foreach (var technique in TechniqueIds)
            {
                writer.WriteStringValue(technique);
            }
            writer.WriteEndArray();

            writer.WriteString("recommendedPlaybook", RecommendedPlaybook);

            writer.WriteStartArray("recommendedActions");
            foreach (var action in RecommendedActions)
            {
                writer.WriteStringValue(action);
            }
            writer.WriteEndArray();

            writer.WriteString("rationale", Rationale);
            writer.WriteBoolean("dryRun", true);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    public string ToMarkdown()
    {
        var techniques = TechniqueIds.Count == 0 ? "none" : string.Join(", ", TechniqueIds);
        var actions = string.Join(Environment.NewLine, RecommendedActions.Select(action => $"- {action}"));

        return $"""
               # Triage: {Alert.Title}

               - Alert ID: {Alert.Id}
               - Severity: {Alert.Severity}
               - Principal: {Alert.Principal}
               - Asset: {Alert.Asset}
               - Techniques: {techniques}
               - Recommended playbook: {RecommendedPlaybook}

               ## Rationale

               {Rationale}

               ## Recommended safe actions

               {actions}
               """;
    }
}

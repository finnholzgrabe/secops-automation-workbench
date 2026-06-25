using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace SecOps.Workbench.Core;

/// <summary>A single alert's triage outcome, flattened for batch reporting.</summary>
public sealed record AlertOutcome(
    string AlertId,
    AlertSeverity Severity,
    string Category,
    IReadOnlyList<string> TechniqueIds,
    string Playbook);

/// <summary>
/// Aggregated metrics for a batch of triaged alerts: per-alert outcomes plus severity,
/// technique, and playbook distributions. Output is deterministic in JSON or CSV.
/// </summary>
public sealed record TriageSummary(
    IReadOnlyList<AlertOutcome> Outcomes,
    IReadOnlyDictionary<AlertSeverity, int> SeverityCounts,
    IReadOnlyList<TechniqueFrequency> Techniques,
    IReadOnlyDictionary<string, int> PlaybookCounts)
{
    public static TriageSummary From(IEnumerable<TriageResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var outcomes = results
            .Select(result => new AlertOutcome(
                result.Alert.Id,
                result.Alert.Severity,
                result.Alert.Category,
                result.TechniqueIds,
                result.RecommendedPlaybook))
            .ToList();

        var severityCounts = Enum.GetValues<AlertSeverity>()
            .ToDictionary(severity => severity, severity => outcomes.Count(o => o.Severity == severity));

        var techniques = TechniqueFrequency.Tally(outcomes.Select(o => o.TechniqueIds));

        var playbookCounts = outcomes
            .GroupBy(o => o.Playbook, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        return new TriageSummary(outcomes, severityCounts, techniques, playbookCounts);
    }

    public string ToJson()
    {
        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, options))
        {
            writer.WriteStartObject();
            writer.WriteNumber("alertCount", Outcomes.Count);

            writer.WriteStartObject("severityCounts");
            foreach (var severity in Enum.GetValues<AlertSeverity>())
            {
                writer.WriteNumber(severity.ToString(), SeverityCounts.GetValueOrDefault(severity));
            }
            writer.WriteEndObject();

            writer.WriteStartObject("playbookCounts");
            foreach (var (playbook, count) in PlaybookCounts)
            {
                writer.WriteNumber(playbook, count);
            }
            writer.WriteEndObject();

            writer.WriteStartArray("techniques");
            foreach (var technique in Techniques)
            {
                writer.WriteStartObject();
                writer.WriteString("techniqueID", technique.TechniqueId);
                writer.WriteString("name", technique.TechniqueName);
                writer.WriteNumber("count", technique.Count);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteStartArray("alerts");
            foreach (var outcome in Outcomes)
            {
                writer.WriteStartObject();
                writer.WriteString("alertId", outcome.AlertId);
                writer.WriteString("severity", outcome.Severity.ToString());
                writer.WriteString("category", outcome.Category);
                writer.WriteStartArray("techniqueIds");
                foreach (var technique in outcome.TechniqueIds)
                {
                    writer.WriteStringValue(technique);
                }
                writer.WriteEndArray();
                writer.WriteString("playbook", outcome.Playbook);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteBoolean("dryRun", true);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    public string ToCsv()
    {
        var builder = new StringBuilder();
        builder.AppendLine("alertId,severity,category,techniques,playbook");
        foreach (var outcome in Outcomes)
        {
            var techniques = string.Join("|", outcome.TechniqueIds);
            builder.AppendLine($"{outcome.AlertId},{outcome.Severity},{outcome.Category},{techniques},{outcome.Playbook}");
        }

        return builder.ToString().TrimEnd();
    }
}

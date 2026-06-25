using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace SecOps.Workbench.Core;

/// <summary>One step of a simulated incident: an alert and its triage result.</summary>
public sealed record IncidentStep(SecurityAlert Alert, TriageResult Result);

/// <summary>
/// Aggregated outcome of running triage over an ordered scenario: a timeline of steps, the peak
/// severity, the techniques observed across the incident, and the playbooks selected.
/// </summary>
public sealed record IncidentReport(
    string Name,
    string Description,
    IReadOnlyList<IncidentStep> Steps,
    AlertSeverity PeakSeverity,
    IReadOnlyList<TechniqueFrequency> Techniques,
    IReadOnlyList<string> Playbooks)
{
    public string Render(ReportFormat format) => format switch
    {
        ReportFormat.Json => ToJson(),
        _ => ToMarkdown()
    };

    public string ToMarkdown()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Incident: {Name}");
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(Description))
        {
            builder.AppendLine(Description);
            builder.AppendLine();
        }

        builder.AppendLine($"- Alerts: {Steps.Count}");
        builder.AppendLine($"- Peak severity: {PeakSeverity}");
        builder.AppendLine($"- Playbooks: {string.Join(", ", Playbooks)}");
        builder.AppendLine();

        builder.AppendLine("## Timeline");
        builder.AppendLine();
        builder.AppendLine("| time (UTC) | alert | severity | techniques | playbook |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var step in Steps)
        {
            var techniques = step.Result.TechniqueIds.Count == 0 ? "none" : string.Join(", ", step.Result.TechniqueIds);
            builder.AppendLine(
                $"| {step.Alert.TimestampUtc.UtcDateTime:u} | {step.Alert.Id} | {step.Alert.Severity} | {techniques} | {step.Result.RecommendedPlaybook} |");
        }
        builder.AppendLine();

        builder.AppendLine("## ATT&CK technique coverage");
        builder.AppendLine();
        if (Techniques.Count == 0)
        {
            builder.AppendLine("- None mapped.");
        }
        else
        {
            foreach (var technique in Techniques)
            {
                builder.AppendLine($"- {technique.TechniqueId} — {technique.TechniqueName} ({technique.Count})");
            }
        }
        builder.AppendLine();

        builder.AppendLine("_Synthetic scenario; all response steps are dry-run recommendations._");

        return builder.ToString().TrimEnd();
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
            writer.WriteString("name", Name);
            writer.WriteString("description", Description);
            writer.WriteNumber("alertCount", Steps.Count);
            writer.WriteString("peakSeverity", PeakSeverity.ToString());

            writer.WriteStartArray("playbooks");
            foreach (var playbook in Playbooks)
            {
                writer.WriteStringValue(playbook);
            }
            writer.WriteEndArray();

            writer.WriteStartArray("timeline");
            foreach (var step in Steps)
            {
                writer.WriteStartObject();
                writer.WriteString("timestampUtc", step.Alert.TimestampUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                writer.WriteString("alertId", step.Alert.Id);
                writer.WriteString("severity", step.Alert.Severity.ToString());
                writer.WriteStartArray("techniqueIds");
                foreach (var technique in step.Result.TechniqueIds)
                {
                    writer.WriteStringValue(technique);
                }
                writer.WriteEndArray();
                writer.WriteString("playbook", step.Result.RecommendedPlaybook);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

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

            writer.WriteBoolean("dryRun", true);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}

/// <summary>Runs triage over a scenario's alerts (ordered by time) and aggregates the outcome.</summary>
public sealed class ScenarioRunner
{
    private readonly TriageEngine _engine;

    public ScenarioRunner(TriageEngine? engine = null)
    {
        _engine = engine ?? new TriageEngine();
    }

    public IncidentReport Run(Scenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var steps = scenario.Alerts
            .OrderBy(alert => alert.TimestampUtc)
            .Select(alert => new IncidentStep(alert, _engine.Triage(alert)))
            .ToList();

        var peak = steps.Max(step => step.Alert.Severity);
        var techniques = TechniqueFrequency.Tally(steps.Select(step => step.Result.TechniqueIds));
        var playbooks = steps
            .Select(step => step.Result.RecommendedPlaybook)
            .Distinct()
            .OrderBy(playbook => playbook, StringComparer.Ordinal)
            .ToList();

        return new IncidentReport(scenario.Name, scenario.Description, steps, peak, techniques, playbooks);
    }
}

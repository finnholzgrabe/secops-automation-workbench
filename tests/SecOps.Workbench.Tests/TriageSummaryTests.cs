using System.Text.Json;
using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class TriageSummaryTests
{
    private static SecurityAlert Alert(string id, AlertSeverity severity, string category, params string[] observables) =>
        new(id, "title", "source", severity, category, DateTimeOffset.UnixEpoch, "p@example.invalid", "asset", observables);

    private static TriageSummary Summary()
    {
        var engine = new TriageEngine();
        var results = new[]
        {
            engine.Triage(Alert("a-1", AlertSeverity.High, "identity", "mfa_push_spam")),
            engine.Triage(Alert("a-2", AlertSeverity.Medium, "identity", "password_spray")),
            engine.Triage(Alert("a-3", AlertSeverity.Low, "endpoint", "suspicious_process"))
        };

        return TriageSummary.From(results);
    }

    [Fact]
    public void Counts_severities_across_all_levels()
    {
        var summary = Summary();

        Assert.Equal(1, summary.SeverityCounts[AlertSeverity.High]);
        Assert.Equal(1, summary.SeverityCounts[AlertSeverity.Medium]);
        Assert.Equal(1, summary.SeverityCounts[AlertSeverity.Low]);
        Assert.Equal(0, summary.SeverityCounts[AlertSeverity.Critical]);
    }

    [Fact]
    public void Aggregates_playbooks_and_techniques()
    {
        var summary = Summary();

        Assert.True(summary.PlaybookCounts.ContainsKey("generic-alert-triage"));
        Assert.Contains(summary.Techniques, t => t.TechniqueId == "T1110");
        Assert.Equal(3, summary.Outcomes.Count);
    }

    [Fact]
    public void Json_output_has_stable_metric_fields()
    {
        using var document = JsonDocument.Parse(Summary().ToJson());
        var root = document.RootElement;

        Assert.Equal(3, root.GetProperty("alertCount").GetInt32());
        Assert.Equal(1, root.GetProperty("severityCounts").GetProperty("High").GetInt32());
        Assert.True(root.TryGetProperty("playbookCounts", out _));
        Assert.True(root.GetProperty("dryRun").GetBoolean());
    }

    [Fact]
    public void Csv_output_has_header_and_one_row_per_alert()
    {
        var csv = Summary().ToCsv();
        var lines = csv.Split('\n');

        Assert.StartsWith("alertId,severity,category,techniques,playbook", lines[0]);
        Assert.Equal(4, lines.Length); // header + 3 rows
    }
}

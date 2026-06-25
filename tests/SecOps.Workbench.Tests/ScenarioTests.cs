using System.Text.Json;
using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class ScenarioTests
{
    private const string Scenario = """
    {
      "name": "Test intrusion",
      "description": "synthetic",
      "alerts": [
        {
          "id": "s-02", "title": "later", "source": "syn", "severity": "critical",
          "category": "identity", "timestampUtc": "2026-06-25T10:09:00Z",
          "principal": "alice@example.invalid", "asset": "idp-tenant-demo",
          "observables": ["successful_login_after_failures", "new_country"]
        },
        {
          "id": "s-01", "title": "earlier", "source": "syn", "severity": "low",
          "category": "identity", "timestampUtc": "2026-06-25T10:00:00Z",
          "principal": "alice@example.invalid", "asset": "idp-tenant-demo",
          "observables": ["password_spray"]
        }
      ]
    }
    """;

    [Fact]
    public void Parser_reads_name_and_alerts()
    {
        var scenario = ScenarioParser.Parse(Scenario);

        Assert.Equal("Test intrusion", scenario.Name);
        Assert.Equal(2, scenario.Alerts.Count);
    }

    [Fact]
    public void Parser_rejects_scenario_without_alerts()
    {
        Assert.Throws<InvalidDataException>(() => ScenarioParser.Parse("""{ "name": "x" }"""));
    }

    [Fact]
    public void Runner_orders_timeline_by_time_and_reports_peak_severity()
    {
        var report = new ScenarioRunner().Run(ScenarioParser.Parse(Scenario));

        Assert.Equal("s-01", report.Steps[0].Alert.Id);
        Assert.Equal("s-02", report.Steps[1].Alert.Id);
        Assert.Equal(AlertSeverity.Critical, report.PeakSeverity);
    }

    [Fact]
    public void Runner_aggregates_technique_coverage()
    {
        var report = new ScenarioRunner().Run(ScenarioParser.Parse(Scenario));

        Assert.Contains(report.Techniques, t => t.TechniqueId == "T1110");
        Assert.Contains(report.Techniques, t => t.TechniqueId == "T1078");
    }

    [Fact]
    public void Json_report_has_timeline_and_dry_run_flag()
    {
        var report = new ScenarioRunner().Run(ScenarioParser.Parse(Scenario));

        using var document = JsonDocument.Parse(report.Render(ReportFormat.Json));
        var root = document.RootElement;

        Assert.Equal(2, root.GetProperty("alertCount").GetInt32());
        Assert.Equal("Critical", root.GetProperty("peakSeverity").GetString());
        Assert.Equal(2, root.GetProperty("timeline").GetArrayLength());
        Assert.True(root.GetProperty("dryRun").GetBoolean());
    }

    [Fact]
    public void Markdown_report_has_timeline_and_coverage_sections()
    {
        var markdown = new ScenarioRunner().Run(ScenarioParser.Parse(Scenario)).Render(ReportFormat.Markdown);

        Assert.Contains("## Timeline", markdown);
        Assert.Contains("## ATT&CK technique coverage", markdown);
        Assert.Contains("dry-run", markdown, StringComparison.OrdinalIgnoreCase);
    }
}

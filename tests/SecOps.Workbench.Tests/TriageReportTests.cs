using System.Text.Json;
using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class TriageReportTests
{
    private static TriageResult Triaged() =>
        new TriageEngine().Triage(AlertParser.Parse(SampleAlerts.IdentityMfaFatigue));

    [Fact]
    public void Markdown_contains_alert_id_severity_techniques_and_dry_run_wording()
    {
        var markdown = Triaged().Render(ReportFormat.Markdown);

        Assert.Contains("sample-001", markdown);
        Assert.Contains("High", markdown);
        Assert.Contains("T1621", markdown);
        Assert.Contains("dry-run", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("analyst-review", markdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Markdown_contains_synthetic_enrichment_section()
    {
        var markdown = Triaged().Render(ReportFormat.Markdown);

        Assert.Contains("Enrichment (synthetic)", markdown);
        Assert.Contains("criticality", markdown);
        Assert.Contains("risk tier", markdown);
    }

    [Fact]
    public void Json_exposes_stable_top_level_fields()
    {
        var json = Triaged().Render(ReportFormat.Json);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("sample-001", root.GetProperty("alertId").GetString());
        Assert.Equal("High", root.GetProperty("severity").GetString());
        Assert.Equal("identity-mfa-fatigue-triage", root.GetProperty("recommendedPlaybook").GetString());
        Assert.True(root.GetProperty("dryRun").GetBoolean());
        Assert.Equal(3, root.GetProperty("techniqueIds").GetArrayLength());
        Assert.True(root.TryGetProperty("recommendedActions", out _));
        Assert.True(root.TryGetProperty("rationale", out _));

        var enrichment = root.GetProperty("enrichment");
        Assert.Equal("alice@example.invalid", enrichment.GetProperty("identity").GetProperty("principal").GetString());
        Assert.True(enrichment.GetProperty("asset").TryGetProperty("criticality", out _));
        Assert.Equal(4, enrichment.GetProperty("observables").GetArrayLength());
    }

    [Fact]
    public void Json_rendering_is_deterministic()
    {
        var first = Triaged().Render(ReportFormat.Json);
        var second = Triaged().Render(ReportFormat.Json);

        Assert.Equal(first, second);
    }
}

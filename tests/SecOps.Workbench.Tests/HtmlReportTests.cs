using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class HtmlReportTests
{
    private static string Html() =>
        new TriageEngine().Triage(AlertParser.Parse(SampleAlerts.IdentityMfaFatigue)).Render(ReportFormat.Html);

    [Fact]
    public void Is_a_self_contained_html_document()
    {
        var html = Html();

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("</html>", html);
        Assert.Contains("<style>", html);
    }

    [Fact]
    public void Contains_alert_details_and_dry_run_banner()
    {
        var html = Html();

        Assert.Contains("sample-001", html);
        Assert.Contains("T1621", html);
        Assert.Contains("dry-run", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Enrichment (synthetic)", html);
    }

    [Fact]
    public void Encodes_dynamic_values_to_prevent_html_injection()
    {
        var alert = new SecurityAlert(
            "id", "<script>alert(1)</script>", "source", AlertSeverity.High, "identity",
            DateTimeOffset.UnixEpoch, "p@example.invalid", "asset", new[] { "mfa_push_spam" });

        var html = new TriageEngine().Triage(alert).Render(ReportFormat.Html);

        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}

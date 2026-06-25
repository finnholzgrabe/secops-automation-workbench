using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class CaseNoteTests
{
    private static string Note() =>
        CaseNote.Render(new TriageEngine().Triage(AlertParser.Parse(SampleAlerts.IdentityMfaFatigue)));

    [Theory]
    [InlineData("## Summary")]
    [InlineData("## Observables")]
    [InlineData("## Mapped techniques")]
    [InlineData("## Recommended playbook")]
    [InlineData("## Analyst checklist")]
    [InlineData("## Dry-run response plan")]
    [InlineData("## Limitations")]
    public void Contains_required_sections(string heading)
    {
        Assert.Contains(heading, Note());
    }

    [Fact]
    public void Includes_alert_id_playbook_and_technique_names()
    {
        var note = Note();

        Assert.Contains("sample-001", note);
        Assert.Contains("identity-mfa-fatigue-triage", note);
        Assert.Contains("Multi-Factor Authentication Request Generation", note);
    }

    [Fact]
    public void Frames_response_as_dry_run_and_names_limitations()
    {
        var note = Note();

        Assert.Contains("(dry-run)", note);
        Assert.Contains("synthetic", note, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("human analyst", note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyst_checklist_uses_unchecked_boxes()
    {
        Assert.Contains("- [ ]", Note());
    }
}

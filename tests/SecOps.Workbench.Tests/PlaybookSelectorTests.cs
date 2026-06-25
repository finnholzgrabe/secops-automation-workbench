using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class PlaybookSelectorTests
{
    private static readonly PlaybookSelector Selector = new(PlaybookCatalog.Default);

    private static SecurityAlert AlertWith(string category) => new(
        "id", "title", "source", AlertSeverity.High, category,
        DateTimeOffset.UnixEpoch, "principal", "asset", Array.Empty<string>());

    [Fact]
    public void Picks_mfa_fatigue_playbook_when_mfa_technique_is_present()
    {
        var playbook = Selector.Select(AlertWith("identity"), new[] { "T1078", "T1110", "T1621" });

        Assert.Equal("identity-mfa-fatigue-triage", playbook.Id);
    }

    [Theory]
    [InlineData("T1110")]
    public void Picks_basic_identity_playbook_for_identity_alerts_without_mfa(string technique)
    {
        var playbook = Selector.Select(AlertWith("identity"), new[] { technique });

        Assert.Equal("identity-alert-basic-triage", playbook.Id);
    }

    [Fact]
    public void Picks_basic_identity_playbook_when_no_techniques_map()
    {
        var playbook = Selector.Select(AlertWith("identity"), Array.Empty<string>());

        Assert.Equal("identity-alert-basic-triage", playbook.Id);
    }

    [Fact]
    public void Falls_back_to_generic_playbook_for_unrelated_categories()
    {
        var playbook = Selector.Select(AlertWith("endpoint"), new[] { "T1059" });

        Assert.Equal("generic-alert-triage", playbook.Id);
    }

    [Fact]
    public void Rejects_an_empty_playbook_set()
    {
        Assert.Throws<ArgumentException>(() => new PlaybookSelector(Array.Empty<Playbook>()));
    }
}

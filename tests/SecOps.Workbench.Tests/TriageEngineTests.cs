using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class TriageEngineTests
{
    [Fact]
    public void Maps_mfa_fatigue_alert_to_expected_techniques_and_playbook()
    {
        var alert = AlertParser.Parse(SampleAlerts.IdentityMfaFatigue);

        var result = new TriageEngine().Triage(alert);

        Assert.Contains("T1110", result.TechniqueIds);
        Assert.Contains("T1621", result.TechniqueIds);
        Assert.Contains("T1078", result.TechniqueIds);
        Assert.Equal("identity-mfa-fatigue-triage", result.RecommendedPlaybook);
    }

    [Fact]
    public void Always_recommends_dry_run_first_action()
    {
        var alert = AlertParser.Parse(SampleAlerts.IdentityMfaFatigue);

        var result = new TriageEngine().Triage(alert);

        Assert.Contains(result.RecommendedActions, action => action.Contains("dry-run", StringComparison.OrdinalIgnoreCase));
    }
}

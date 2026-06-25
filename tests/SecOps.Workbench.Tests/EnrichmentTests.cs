using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class EnrichmentTests
{
    [Theory]
    [InlineData("admin@example.invalid")]
    [InlineData("svc-backup@example.invalid")]
    [InlineData("ops-deploy@example.invalid")]
    public void Identity_provider_flags_privileged_principals(string principal)
    {
        var context = new MockIdentityContextProvider().GetContext(principal);

        Assert.True(context.IsPrivileged);
        Assert.Equal("high", context.RiskTier);
    }

    [Fact]
    public void Identity_provider_is_deterministic()
    {
        var provider = new MockIdentityContextProvider();

        var first = provider.GetContext("alice@example.invalid");
        var second = provider.GetContext("alice@example.invalid");

        Assert.Equal(first.RiskTier, second.RiskTier);
        Assert.Equal(first.IsPrivileged, second.IsPrivileged);
        Assert.Equal(first.RiskHints, second.RiskHints);
    }

    [Fact]
    public void Asset_provider_marks_identity_infrastructure_high()
    {
        var context = new MockAssetContextProvider().GetContext("idp-tenant-demo");

        Assert.Equal("high", context.Criticality);
    }

    [Fact]
    public void Asset_provider_is_deterministic_for_unmatched_names()
    {
        var provider = new MockAssetContextProvider();

        var first = provider.GetContext("host-1234");
        var second = provider.GetContext("host-1234");

        Assert.Equal(first.Criticality, second.Criticality);
    }

    [Theory]
    [InlineData("impossible_travel", "malicious")]
    [InlineData("failed_login_burst", "suspicious")]
    [InlineData("valid_account_used", "benign")]
    public void Reputation_provider_returns_expected_verdict(string observable, string verdict)
    {
        var reputation = new MockReputationProvider().GetReputation(observable);

        Assert.Equal(verdict, reputation.Verdict);
    }

    [Fact]
    public void Reputation_provider_returns_unknown_for_unmapped_observable()
    {
        var reputation = new MockReputationProvider().GetReputation("totally_unknown_signal");

        Assert.Equal("unknown", reputation.Verdict);
    }

    [Fact]
    public void Enrichment_service_produces_one_reputation_per_observable()
    {
        var alert = AlertParser.Parse(SampleAlerts.IdentityMfaFatigue);

        var enrichment = new EnrichmentService().Enrich(alert);

        Assert.Equal(alert.Observables.Count, enrichment.Observables.Count);
        Assert.Equal(alert.Principal, enrichment.Identity.Principal);
        Assert.Equal(alert.Asset, enrichment.Asset.Asset);
    }
}

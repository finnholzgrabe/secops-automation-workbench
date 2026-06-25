using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class TechniqueMapperTests
{
    [Fact]
    public void Maps_known_signals_to_deterministically_ordered_technique_ids()
    {
        var techniques = new TechniqueMapper().Map(new[] { "mfa_push_spam", "failed_login_burst", "new_country" });

        Assert.Equal(new[] { "T1078", "T1110", "T1621" }, techniques);
    }

    [Fact]
    public void Matching_is_case_insensitive_and_deduplicates_per_technique()
    {
        var techniques = new TechniqueMapper().Map(new[] { "FAILED_LOGIN_BURST", "password_spray" });

        Assert.Equal(new[] { "T1110" }, techniques);
    }

    [Fact]
    public void Ignores_unknown_signals()
    {
        var techniques = new TechniqueMapper().Map(new[] { "totally_unknown_signal" });

        Assert.Empty(techniques);
    }

    [Fact]
    public void Honors_a_custom_catalog()
    {
        var rules = new[]
        {
            new TechniqueRule("T9999", "Custom Technique", new[] { "custom_signal" })
        };

        var techniques = new TechniqueMapper(rules).Map(new[] { "custom_signal", "failed_login_burst" });

        Assert.Equal(new[] { "T9999" }, techniques);
    }
}

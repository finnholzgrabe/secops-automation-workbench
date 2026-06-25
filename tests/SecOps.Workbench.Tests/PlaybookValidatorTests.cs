using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class PlaybookValidatorTests
{
    private static Playbook Valid() => new(
        "identity-basic",
        "Identity basic",
        "A description.",
        "identity",
        new[] { "T1078" },
        new[] { "Open a case note and keep response actions in dry-run mode." },
        DryRunOnly: true);

    [Fact]
    public void Accepts_a_well_formed_playbook()
    {
        Assert.Empty(PlaybookValidator.Validate(Valid()));
    }

    [Fact]
    public void Reports_missing_required_fields()
    {
        var playbook = Valid() with { Id = "", Title = "", Description = "", Category = "" };

        var errors = PlaybookValidator.Validate(playbook);

        Assert.Contains(errors, e => e.Contains("id is required"));
        Assert.Contains(errors, e => e.Contains("title is required"));
        Assert.Contains(errors, e => e.Contains("description is required"));
        Assert.Contains(errors, e => e.Contains("category is required"));
    }

    [Fact]
    public void Rejects_invalid_technique_ids()
    {
        var playbook = Valid() with { Techniques = new[] { "T1078", "not-a-technique" } };

        var errors = PlaybookValidator.Validate(playbook);

        Assert.Contains(errors, e => e.Contains("not-a-technique"));
    }

    [Fact]
    public void Requires_at_least_one_recommended_action()
    {
        var playbook = Valid() with { RecommendedActions = Array.Empty<string>() };

        Assert.Contains(PlaybookValidator.Validate(playbook), e => e.Contains("recommendedAction"));
    }

    [Fact]
    public void Requires_dry_run_only_to_be_true()
    {
        var playbook = Valid() with { DryRunOnly = false };

        Assert.Contains(PlaybookValidator.Validate(playbook), e => e.Contains("dryRunOnly must be true"));
    }
}

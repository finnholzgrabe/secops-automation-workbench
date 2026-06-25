using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class PlaybookParserTests
{
    [Fact]
    public void Parses_a_complete_playbook()
    {
        const string json = """
        {
          "id": "identity-basic",
          "title": "Identity basic",
          "description": "A description.",
          "category": "identity",
          "techniques": ["T1078", "T1110"],
          "recommendedActions": ["Open a case note in dry-run mode."],
          "dryRunOnly": true
        }
        """;

        var playbook = PlaybookParser.Parse(json);

        Assert.Equal("identity-basic", playbook.Id);
        Assert.Equal("identity", playbook.Category);
        Assert.Equal(new[] { "T1078", "T1110" }, playbook.Techniques);
        Assert.Single(playbook.RecommendedActions);
        Assert.True(playbook.DryRunOnly);
    }

    [Fact]
    public void Is_tolerant_of_missing_fields_so_validation_can_report_them()
    {
        var playbook = PlaybookParser.Parse("{}");

        Assert.Equal(string.Empty, playbook.Id);
        Assert.Empty(playbook.Techniques);
        Assert.Empty(playbook.RecommendedActions);
        Assert.False(playbook.DryRunOnly);

        // The empty playbook must then fail validation with readable errors.
        Assert.NotEmpty(PlaybookValidator.Validate(playbook));
    }

    [Fact]
    public void Throws_on_malformed_json()
    {
        Assert.ThrowsAny<Exception>(() => PlaybookParser.Parse("{ not json"));
    }
}

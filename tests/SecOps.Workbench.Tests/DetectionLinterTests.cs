using SecOps.Workbench.Core;
using Xunit;

namespace SecOps.Workbench.Tests;

public class DetectionLinterTests
{
    private const string ValidDetection = """
    title: Example Detection
    status: test
    falsepositives:
      - A legitimate but noisy maintenance job.
    tags:
      - attack.credential_access
      - attack.t1110
    testFixture: fixtures/example.fixture.json
    """;

    [Fact]
    public void Clean_detection_has_no_issues_and_extracts_fixture()
    {
        var result = DetectionLinter.Lint(ValidDetection);

        Assert.True(result.IsClean);
        Assert.Equal("fixtures/example.fixture.json", result.TestFixture);
    }

    [Fact]
    public void Flags_missing_title()
    {
        var content = ValidDetection.Replace("title: Example Detection\n", string.Empty);

        Assert.Contains(DetectionLinter.Lint(content).Issues, i => i.Contains("title"));
    }

    [Fact]
    public void Flags_invalid_status()
    {
        var content = ValidDetection.Replace("status: test", "status: production");

        Assert.Contains(DetectionLinter.Lint(content).Issues, i => i.Contains("status"));
    }

    [Fact]
    public void Flags_missing_attack_tag()
    {
        var content = ValidDetection.Replace("  - attack.t1110\n", string.Empty);

        Assert.Contains(DetectionLinter.Lint(content).Issues, i => i.Contains("ATT&CK"));
    }

    [Fact]
    public void Flags_placeholder_only_false_positives()
    {
        var content = ValidDetection.Replace("  - A legitimate but noisy maintenance job.", "  - Unknown");

        Assert.Contains(DetectionLinter.Lint(content).Issues, i => i.Contains("falsepositives"));
    }

    [Fact]
    public void Flags_missing_false_positives()
    {
        var content = """
        title: Example Detection
        status: test
        tags:
          - attack.t1110
        testFixture: fixtures/example.fixture.json
        """;

        Assert.Contains(DetectionLinter.Lint(content).Issues, i => i.Contains("falsepositives"));
    }

    [Fact]
    public void Flags_missing_test_fixture()
    {
        var content = ValidDetection.Replace("testFixture: fixtures/example.fixture.json", string.Empty);

        var result = DetectionLinter.Lint(content);

        Assert.Null(result.TestFixture);
        Assert.Contains(result.Issues, i => i.Contains("testFixture"));
    }
}

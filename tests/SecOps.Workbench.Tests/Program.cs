using SecOps.Workbench.Core;

namespace SecOps.Workbench.Tests;

internal static class Program
{
    private const string SampleAlertJson = """
    {
      "id": "sample-001",
      "title": "Suspicious login followed by MFA fatigue",
      "source": "synthetic-identity-provider",
      "severity": "high",
      "category": "identity",
      "timestampUtc": "2026-06-25T10:15:00Z",
      "principal": "alice@example.invalid",
      "asset": "idp-tenant-demo",
      "observables": [
        "failed_login_burst",
        "mfa_push_spam",
        "new_country",
        "successful_login_after_failures"
      ]
    }
    """;

    private static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            ("parser reads sample identity alert", ParserReadsIdentityAlert),
            ("triage maps MFA fatigue to expected techniques", TriageMapsExpectedTechniques),
            ("triage markdown contains dry-run safety wording", TriageMarkdownContainsSafetyWording)
        };

        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
            }
        }

        return failed == 0 ? 0 : 1;
    }

    private static void ParserReadsIdentityAlert()
    {
        var alert = AlertParser.Parse(SampleAlertJson);

        AssertEqual("sample-001", alert.Id);
        AssertEqual(AlertSeverity.High, alert.Severity);
        AssertEqual("alice@example.invalid", alert.Principal);
        AssertEqual(4, alert.Observables.Count);
    }

    private static void TriageMapsExpectedTechniques()
    {
        var alert = AlertParser.Parse(SampleAlertJson);
        var result = new TriageEngine().Triage(alert);

        AssertTrue(result.TechniqueIds.Contains("T1110"), "expected brute-force technique mapping");
        AssertTrue(result.TechniqueIds.Contains("T1621"), "expected MFA request generation mapping");
        AssertTrue(result.TechniqueIds.Contains("T1078"), "expected valid accounts mapping");
        AssertEqual("identity-mfa-fatigue-triage", result.RecommendedPlaybook);
    }

    private static void TriageMarkdownContainsSafetyWording()
    {
        var alert = AlertParser.Parse(SampleAlertJson);
        var markdown = new TriageEngine().Triage(alert).ToMarkdown();

        AssertTrue(markdown.Contains("dry-run", StringComparison.OrdinalIgnoreCase), "expected dry-run wording");
        AssertTrue(markdown.Contains("analyst-review", StringComparison.OrdinalIgnoreCase), "expected analyst-review wording");
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

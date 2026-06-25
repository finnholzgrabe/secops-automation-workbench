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
            ("triage markdown contains alert id, severity, techniques, dry-run", TriageMarkdownContainsContractFields),
            ("triage json exposes stable top-level fields", TriageJsonHasStableFields),
            ("report format parser rejects unknown formats", ReportFormatParserRejectsUnknown),
            ("report format parser accepts known formats", ReportFormatParserAcceptsKnown)
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

    private static void TriageMarkdownContainsContractFields()
    {
        var alert = AlertParser.Parse(SampleAlertJson);
        var markdown = new TriageEngine().Triage(alert).Render(ReportFormat.Markdown);

        AssertTrue(markdown.Contains("sample-001", StringComparison.Ordinal), "expected alert id");
        AssertTrue(markdown.Contains("High", StringComparison.Ordinal), "expected severity");
        AssertTrue(markdown.Contains("T1621", StringComparison.Ordinal), "expected mapped technique");
        AssertTrue(markdown.Contains("dry-run", StringComparison.OrdinalIgnoreCase), "expected dry-run wording");
        AssertTrue(markdown.Contains("analyst-review", StringComparison.OrdinalIgnoreCase), "expected analyst-review wording");
    }

    private static void TriageJsonHasStableFields()
    {
        var alert = AlertParser.Parse(SampleAlertJson);
        var json = new TriageEngine().Triage(alert).Render(ReportFormat.Json);

        using var document = System.Text.Json.JsonDocument.Parse(json);
        var root = document.RootElement;

        AssertEqual("sample-001", root.GetProperty("alertId").GetString());
        AssertEqual("High", root.GetProperty("severity").GetString());
        AssertEqual("identity-mfa-fatigue-triage", root.GetProperty("recommendedPlaybook").GetString());
        AssertTrue(root.GetProperty("dryRun").GetBoolean(), "expected dryRun to be true");
        AssertEqual(3, root.GetProperty("techniqueIds").GetArrayLength());
        AssertTrue(root.TryGetProperty("recommendedActions", out _), "expected recommendedActions field");
        AssertTrue(root.TryGetProperty("rationale", out _), "expected rationale field");
    }

    private static void ReportFormatParserRejectsUnknown()
    {
        AssertTrue(!ReportFormats.TryParse("yaml", out _), "expected unknown format to be rejected");
        AssertTrue(!ReportFormats.TryParse("", out _), "expected empty format to be rejected");
        AssertTrue(!ReportFormats.TryParse(null, out _), "expected null format to be rejected");
    }

    private static void ReportFormatParserAcceptsKnown()
    {
        AssertTrue(ReportFormats.TryParse("markdown", out var md) && md == ReportFormat.Markdown, "expected markdown");
        AssertTrue(ReportFormats.TryParse("JSON", out var json) && json == ReportFormat.Json, "expected json (case-insensitive)");
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

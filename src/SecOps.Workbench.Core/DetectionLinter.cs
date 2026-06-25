using System.Text.RegularExpressions;

namespace SecOps.Workbench.Core;

/// <summary>Outcome of linting one detection's content.</summary>
public sealed record DetectionLintResult(IReadOnlyList<string> Issues, string? TestFixture)
{
    public bool IsClean => Issues.Count == 0;
}

/// <summary>
/// A lightweight, dependency-free content linter for Sigma-inspired detection rules. It performs
/// structural/required-field checks on the raw text rather than parsing full YAML, which keeps
/// Core free of external dependencies. It is a content-quality gate, not a Sigma engine.
/// </summary>
public static partial class DetectionLinter
{
    private static readonly string[] ValidStatuses =
        { "stable", "test", "experimental", "deprecated", "unsupported" };

    private static readonly string[] FalsePositivePlaceholders =
        { "", "unknown", "none", "n/a", "na", "tbd" };

    public static DetectionLintResult Lint(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var issues = new List<string>();
        var lines = content.Replace("\r\n", "\n").Split('\n');

        if (!lines.Any(line => TitleLine().IsMatch(line)))
        {
            issues.Add("missing 'title'.");
        }

        CheckStatus(lines, issues);

        if (!AttackTag().IsMatch(content))
        {
            issues.Add("missing an ATT&CK technique tag (expected e.g. 'attack.t1110').");
        }

        CheckFalsePositives(lines, issues);

        var fixture = ExtractScalar(lines, "testFixture");
        if (string.IsNullOrWhiteSpace(fixture))
        {
            issues.Add("missing 'testFixture' reference; every detection should point to a test fixture.");
        }

        return new DetectionLintResult(issues, string.IsNullOrWhiteSpace(fixture) ? null : fixture);
    }

    private static void CheckStatus(IReadOnlyList<string> lines, List<string> issues)
    {
        var value = ExtractScalar(lines, "status");
        if (value is null)
        {
            issues.Add("missing 'status'.");
        }
        else if (!ValidStatuses.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            issues.Add($"status '{value}' is not a valid Sigma status ({string.Join(", ", ValidStatuses)}).");
        }
    }

    private static void CheckFalsePositives(IReadOnlyList<string> lines, List<string> issues)
    {
        var inBlock = false;
        var itemCount = 0;
        var hasMeaningfulItem = false;

        foreach (var raw in lines)
        {
            var trimmed = raw.Trim();

            if (!inBlock)
            {
                if (trimmed.StartsWith("falsepositives:", StringComparison.OrdinalIgnoreCase))
                {
                    inBlock = true;
                }

                continue;
            }

            // A non-indented, non-empty line that is not a list item ends the block (next top-level key).
            if (raw.Length > 0 && !char.IsWhiteSpace(raw[0]) && !trimmed.StartsWith('-'))
            {
                break;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                var item = trimmed[2..].Trim().Trim('"', '\'');
                itemCount++;
                if (!FalsePositivePlaceholders.Contains(item, StringComparer.OrdinalIgnoreCase))
                {
                    hasMeaningfulItem = true;
                }
            }
        }

        if (itemCount == 0)
        {
            issues.Add("missing 'falsepositives' notes.");
        }
        else if (!hasMeaningfulItem)
        {
            issues.Add("'falsepositives' should contain meaningful notes, not just placeholders like 'Unknown'.");
        }
    }

    private static string? ExtractScalar(IReadOnlyList<string> lines, string key)
    {
        var prefix = key + ":";
        foreach (var raw in lines)
        {
            var trimmed = raw.TrimStart();
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[prefix.Length..].Trim().Trim('"', '\'');
            }
        }

        return null;
    }

    [GeneratedRegex(@"^title:\s*\S")]
    private static partial Regex TitleLine();

    [GeneratedRegex(@"attack\.t\d{4}", RegexOptions.IgnoreCase)]
    private static partial Regex AttackTag();
}

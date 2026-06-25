namespace SecOps.Workbench.Core;

public sealed class TriageEngine
{
    public TriageResult Triage(SecurityAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        var techniques = MapTechniques(alert.Observables);
        var actions = RecommendActions(alert, techniques);
        var playbook = SelectPlaybook(alert, techniques);
        var rationale = BuildRationale(alert, techniques);

        return new TriageResult(alert, techniques, playbook, actions, rationale);
    }

    private static IReadOnlyList<string> MapTechniques(IReadOnlyList<string> observables)
    {
        var normalized = observables.Select(item => item.Trim().ToLowerInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var techniques = new SortedSet<string>(StringComparer.Ordinal);

        if (normalized.Contains("failed_login_burst") || normalized.Contains("password_spray"))
        {
            techniques.Add("T1110");
        }

        if (normalized.Contains("mfa_push_spam") || normalized.Contains("mfa_fatigue"))
        {
            techniques.Add("T1621");
        }

        if (normalized.Contains("successful_login_after_failures") || normalized.Contains("valid_account_used"))
        {
            techniques.Add("T1078");
        }

        if (normalized.Contains("new_country") || normalized.Contains("impossible_travel"))
        {
            techniques.Add("T1078");
        }

        return techniques.ToArray();
    }

    private static string SelectPlaybook(SecurityAlert alert, IReadOnlyList<string> techniques)
    {
        if (alert.Category.Equals("identity", StringComparison.OrdinalIgnoreCase)
            && techniques.Contains("T1078")
            && techniques.Contains("T1621"))
        {
            return "identity-mfa-fatigue-triage";
        }

        if (alert.Category.Equals("identity", StringComparison.OrdinalIgnoreCase))
        {
            return "identity-alert-basic-triage";
        }

        return "generic-alert-triage";
    }

    private static IReadOnlyList<string> RecommendActions(SecurityAlert alert, IReadOnlyList<string> techniques)
    {
        var actions = new List<string>
        {
            "Create a case note and keep all response actions in dry-run mode.",
            $"Verify whether principal '{alert.Principal}' recently changed MFA devices, password, or recovery settings.",
            $"Check recent authentication events for asset or tenant '{alert.Asset}'."
        };

        if (techniques.Contains("T1621"))
        {
            actions.Add("Review MFA push volume and ask the user to confirm whether prompts were expected.");
        }

        if (techniques.Contains("T1078"))
        {
            actions.Add("If compromise is suspected, recommend session revocation and password reset as manual analyst-approved steps.");
        }

        return actions;
    }

    private static string BuildRationale(SecurityAlert alert, IReadOnlyList<string> techniques)
    {
        var observableSummary = string.Join(", ", alert.Observables);
        var techniqueSummary = techniques.Count == 0 ? "no mapped techniques" : string.Join(", ", techniques);

        return $"The alert is a {alert.Severity} {alert.Category} event with observables [{observableSummary}], mapped to {techniqueSummary}. The recommendation stays in analyst-review/dry-run mode.";
    }
}

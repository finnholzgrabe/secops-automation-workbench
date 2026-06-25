namespace SecOps.Workbench.Core;

public sealed class TriageEngine
{
    private readonly TechniqueMapper _techniqueMapper;

    public TriageEngine(TechniqueMapper? techniqueMapper = null)
    {
        _techniqueMapper = techniqueMapper ?? new TechniqueMapper();
    }

    public TriageResult Triage(SecurityAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        var techniques = _techniqueMapper.Map(alert.Observables);
        var actions = RecommendActions(alert, techniques);
        var playbook = SelectPlaybook(alert, techniques);
        var rationale = BuildRationale(alert, techniques);

        return new TriageResult(alert, techniques, playbook, actions, rationale);
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

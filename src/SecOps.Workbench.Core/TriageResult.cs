namespace SecOps.Workbench.Core;

public sealed record TriageResult(
    SecurityAlert Alert,
    IReadOnlyList<string> TechniqueIds,
    string RecommendedPlaybook,
    IReadOnlyList<string> RecommendedActions,
    string Rationale)
{
    public string ToMarkdown()
    {
        var techniques = TechniqueIds.Count == 0 ? "none" : string.Join(", ", TechniqueIds);
        var actions = string.Join(Environment.NewLine, RecommendedActions.Select(action => $"- {action}"));

        return $"""
               # Triage: {Alert.Title}

               - Alert ID: {Alert.Id}
               - Severity: {Alert.Severity}
               - Principal: {Alert.Principal}
               - Asset: {Alert.Asset}
               - Techniques: {techniques}
               - Recommended playbook: {RecommendedPlaybook}

               ## Rationale

               {Rationale}

               ## Recommended safe actions

               {actions}
               """;
    }
}

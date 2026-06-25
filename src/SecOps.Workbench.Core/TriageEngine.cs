namespace SecOps.Workbench.Core;

public sealed class TriageEngine
{
    private readonly TechniqueMapper _techniqueMapper;
    private readonly PlaybookSelector _playbookSelector;
    private readonly EnrichmentService _enrichment;

    public TriageEngine(
        IReadOnlyList<Playbook>? playbooks = null,
        TechniqueMapper? techniqueMapper = null,
        EnrichmentService? enrichment = null)
    {
        _techniqueMapper = techniqueMapper ?? new TechniqueMapper();
        _playbookSelector = new PlaybookSelector(playbooks ?? PlaybookCatalog.Default);
        _enrichment = enrichment ?? new EnrichmentService();
    }

    public TriageResult Triage(SecurityAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        var techniques = _techniqueMapper.Map(alert.Observables);
        var playbook = _playbookSelector.Select(alert, techniques);
        var enrichment = _enrichment.Enrich(alert);
        var rationale = BuildRationale(alert, techniques, playbook);

        return new TriageResult(alert, techniques, playbook.Id, playbook.RecommendedActions, rationale, enrichment);
    }

    private static string BuildRationale(SecurityAlert alert, IReadOnlyList<string> techniques, Playbook playbook)
    {
        var observableSummary = string.Join(", ", alert.Observables);
        var techniqueSummary = techniques.Count == 0 ? "no mapped techniques" : string.Join(", ", techniques);

        return $"The alert is a {alert.Severity} {alert.Category} event with observables [{observableSummary}], " +
               $"mapped to {techniqueSummary}. Selected playbook '{playbook.Id}'. " +
               "The recommendation stays in analyst-review/dry-run mode.";
    }
}

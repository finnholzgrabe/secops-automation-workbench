namespace SecOps.Workbench.Core;

/// <summary>
/// Selects the best-fitting playbook for an alert from a supplied set. Selection is
/// data-driven and deterministic: it prefers playbooks whose category matches the alert,
/// then the one covering the most mapped techniques, then the fewest unrelated techniques,
/// then a stable id ordering.
/// </summary>
public sealed class PlaybookSelector
{
    private readonly IReadOnlyList<Playbook> _playbooks;

    public PlaybookSelector(IReadOnlyList<Playbook> playbooks)
    {
        ArgumentNullException.ThrowIfNull(playbooks);
        if (playbooks.Count == 0)
        {
            throw new ArgumentException("At least one playbook is required.", nameof(playbooks));
        }

        _playbooks = playbooks;
    }

    public Playbook Select(SecurityAlert alert, IReadOnlyList<string> techniqueIds)
    {
        ArgumentNullException.ThrowIfNull(alert);
        ArgumentNullException.ThrowIfNull(techniqueIds);

        var techniques = techniqueIds.ToHashSet(StringComparer.Ordinal);

        var pool = _playbooks
            .Where(playbook => playbook.Category.Equals(alert.Category, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (pool.Count == 0)
        {
            pool = _playbooks
                .Where(playbook => playbook.Category.Equals("any", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (pool.Count == 0)
        {
            pool = _playbooks.ToList();
        }

        return pool
            .OrderByDescending(playbook => playbook.Techniques.Count(techniques.Contains))
            .ThenBy(playbook => playbook.Techniques.Count(technique => !techniques.Contains(technique)))
            .ThenBy(playbook => playbook.Id, StringComparer.Ordinal)
            .First();
    }
}

namespace SecOps.Workbench.Core;

/// <summary>How often a technique was mapped across one or more alerts, with its catalog name.</summary>
public sealed record TechniqueFrequency(string TechniqueId, string TechniqueName, int Count)
{
    /// <summary>
    /// Counts technique occurrences across a set of mapped technique-id lists and returns them
    /// ordered by descending count, then technique id, for deterministic output.
    /// </summary>
    public static IReadOnlyList<TechniqueFrequency> Tally(IEnumerable<IReadOnlyList<string>> techniqueLists)
    {
        ArgumentNullException.ThrowIfNull(techniqueLists);

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var list in techniqueLists)
        {
            foreach (var technique in list)
            {
                counts[technique] = counts.GetValueOrDefault(technique) + 1;
            }
        }

        return counts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new TechniqueFrequency(pair.Key, AttackTechniqueCatalog.NameFor(pair.Key), pair.Value))
            .ToList();
    }
}

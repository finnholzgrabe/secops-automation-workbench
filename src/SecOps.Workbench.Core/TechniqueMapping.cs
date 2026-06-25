namespace SecOps.Workbench.Core;

/// <summary>
/// A declarative rule that maps one or more observable signals to a single
/// ATT&amp;CK-style technique. Using data instead of hard-coded if-chains keeps the
/// mapping catalog reviewable, testable, and easy to extend.
/// </summary>
public sealed record TechniqueRule(
    string TechniqueId,
    string TechniqueName,
    IReadOnlyList<string> Signals);

/// <summary>
/// Default, synthetic ATT&amp;CK-style technique catalog used by the workbench.
/// These are illustrative mappings for a demo, not production detections.
/// </summary>
public static class AttackTechniqueCatalog
{
    public static IReadOnlyList<TechniqueRule> Default { get; } = new TechniqueRule[]
    {
        new("T1110", "Brute Force",
            new[] { "failed_login_burst", "password_spray" }),
        new("T1621", "Multi-Factor Authentication Request Generation",
            new[] { "mfa_push_spam", "mfa_fatigue" }),
        new("T1078", "Valid Accounts",
            new[] { "successful_login_after_failures", "valid_account_used", "new_country", "impossible_travel" }),
    };
}

/// <summary>
/// Maps alert observables onto technique IDs using a catalog of <see cref="TechniqueRule"/>.
/// Matching is case-insensitive; results are de-duplicated and ordered deterministically.
/// </summary>
public sealed class TechniqueMapper
{
    private readonly IReadOnlyList<TechniqueRule> _rules;

    public TechniqueMapper(IReadOnlyList<TechniqueRule>? rules = null)
    {
        _rules = rules ?? AttackTechniqueCatalog.Default;
    }

    public IReadOnlyList<string> Map(IReadOnlyList<string> observables)
    {
        ArgumentNullException.ThrowIfNull(observables);

        var signals = observables
            .Select(observable => observable.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matched = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var rule in _rules)
        {
            if (rule.Signals.Any(signal => signals.Contains(signal)))
            {
                matched.Add(rule.TechniqueId);
            }
        }

        return matched.ToArray();
    }
}

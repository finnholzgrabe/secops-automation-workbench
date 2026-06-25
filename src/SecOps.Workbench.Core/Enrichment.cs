namespace SecOps.Workbench.Core;

/// <summary>Synthetic identity context for the alert principal.</summary>
public sealed record IdentityContext(
    string Principal,
    bool IsPrivileged,
    string RiskTier,
    IReadOnlyList<string> RiskHints);

/// <summary>Synthetic asset/criticality context for the affected asset.</summary>
public sealed record AssetContext(
    string Asset,
    string Criticality,
    IReadOnlyList<string> Notes);

/// <summary>Synthetic reputation/context for a single observable.</summary>
public sealed record ObservableReputation(
    string Observable,
    string Verdict,
    string Context);

/// <summary>Aggregated enrichment attached to a triage result.</summary>
public sealed record EnrichmentResult(
    IdentityContext Identity,
    AssetContext Asset,
    IReadOnlyList<ObservableReputation> Observables);

/// <summary>
/// Provides risk context for a principal. Implementations must be offline and deterministic;
/// real identity-provider integrations are intentionally out of scope.
/// </summary>
public interface IIdentityContextProvider
{
    IdentityContext GetContext(string principal);
}

/// <summary>Provides criticality context for an asset. Offline and deterministic.</summary>
public interface IAssetContextProvider
{
    AssetContext GetContext(string asset);
}

/// <summary>Provides reputation/context for an observable. Offline and deterministic.</summary>
public interface IReputationProvider
{
    ObservableReputation GetReputation(string observable);
}

/// <summary>
/// Bundles the three enrichment providers and produces an <see cref="EnrichmentResult"/> for an
/// alert. Defaults to the synthetic mock providers so triage works offline by default.
/// </summary>
public sealed class EnrichmentService
{
    private readonly IIdentityContextProvider _identity;
    private readonly IAssetContextProvider _asset;
    private readonly IReputationProvider _reputation;

    public EnrichmentService(
        IIdentityContextProvider? identity = null,
        IAssetContextProvider? asset = null,
        IReputationProvider? reputation = null)
    {
        _identity = identity ?? new MockIdentityContextProvider();
        _asset = asset ?? new MockAssetContextProvider();
        _reputation = reputation ?? new MockReputationProvider();
    }

    public EnrichmentResult Enrich(SecurityAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        var identity = _identity.GetContext(alert.Principal);
        var asset = _asset.GetContext(alert.Asset);
        var observables = alert.Observables
            .Select(observable => _reputation.GetReputation(observable))
            .ToList();

        return new EnrichmentResult(identity, asset, observables);
    }
}

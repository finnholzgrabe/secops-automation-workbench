using System.Text;

namespace SecOps.Workbench.Core;

/// <summary>
/// Stable, process-independent hash used to derive synthetic risk buckets. Uses FNV-1a rather
/// than <see cref="string.GetHashCode()"/>, which is randomized per process and would make the
/// mock enrichment non-deterministic across runs.
/// </summary>
internal static class DeterministicHash
{
    public static int Bucket(string value, int buckets)
    {
        unchecked
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            var hash = offsetBasis;
            foreach (var b in Encoding.UTF8.GetBytes(value))
            {
                hash ^= b;
                hash *= prime;
            }

            return (int)(hash % (uint)buckets);
        }
    }
}

/// <summary>Synthetic identity context provider. No network calls; fully deterministic.</summary>
public sealed class MockIdentityContextProvider : IIdentityContextProvider
{
    private static readonly string[] PrivilegedMarkers = { "admin", "root", "svc", "sec-", "ops-" };

    public IdentityContext GetContext(string principal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principal);

        var normalized = principal.Trim().ToLowerInvariant();
        var isPrivileged = PrivilegedMarkers.Any(marker => normalized.Contains(marker));

        var hints = new List<string>();
        string riskTier;

        if (isPrivileged)
        {
            riskTier = "high";
            hints.Add("Privileged or service identity; prioritize containment review.");
        }
        else
        {
            riskTier = DeterministicHash.Bucket(normalized, 100) >= 70 ? "elevated" : "low";
            if (riskTier == "elevated")
            {
                hints.Add("Synthetic risk model flags this principal for closer review.");
            }
        }

        hints.Add("Synthetic identity context - not sourced from a real identity provider.");

        return new IdentityContext(principal, isPrivileged, riskTier, hints);
    }
}

/// <summary>Synthetic asset criticality provider. No network calls; fully deterministic.</summary>
public sealed class MockAssetContextProvider : IAssetContextProvider
{
    private static readonly string[] HighCriticalityMarkers = { "prod", "idp", "tenant", "dc-", "domain" };
    private static readonly string[] LowCriticalityMarkers = { "test", "dev", "lab", "demo", "sandbox" };

    public AssetContext GetContext(string asset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(asset);

        var normalized = asset.Trim().ToLowerInvariant();
        var notes = new List<string>();
        string criticality;

        if (HighCriticalityMarkers.Any(marker => normalized.Contains(marker)))
        {
            criticality = "high";
            notes.Add("Asset name suggests production or identity infrastructure.");
        }
        else if (LowCriticalityMarkers.Any(marker => normalized.Contains(marker)))
        {
            criticality = "low";
            notes.Add("Asset name suggests a non-production environment.");
        }
        else
        {
            criticality = DeterministicHash.Bucket(normalized, 100) >= 60 ? "medium" : "low";
        }

        notes.Add("Synthetic asset context - not sourced from a real CMDB.");

        return new AssetContext(asset, criticality, notes);
    }
}

/// <summary>
/// Synthetic observable reputation provider backed by a small, fixed lookup of demo signals.
/// No network calls; unknown observables return an explicit "unknown" verdict.
/// </summary>
public sealed class MockReputationProvider : IReputationProvider
{
    private static readonly IReadOnlyDictionary<string, (string Verdict, string Context)> Known =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["failed_login_burst"] = ("suspicious", "Bursts of failed logins can indicate brute force or password spray."),
            ["password_spray"] = ("suspicious", "Low-and-slow attempts spread across many accounts."),
            ["mfa_push_spam"] = ("suspicious", "Repeated MFA prompts may indicate MFA fatigue or push bombing."),
            ["mfa_fatigue"] = ("suspicious", "A user may approve a prompt simply to stop repeated requests."),
            ["successful_login_after_failures"] = ("suspicious", "A success after many failures can indicate a guessed or sprayed credential."),
            ["valid_account_used"] = ("benign", "Use of a valid account is expected, but the surrounding context still matters."),
            ["new_country"] = ("suspicious", "A first-seen country can indicate account takeover or legitimate travel."),
            ["impossible_travel"] = ("malicious", "Two logins too far apart in time and distance to be the same user."),
        };

    public ObservableReputation GetReputation(string observable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(observable);

        if (Known.TryGetValue(observable.Trim(), out var hit))
        {
            return new ObservableReputation(observable, hit.Verdict, $"{hit.Context} (synthetic)");
        }

        return new ObservableReputation(observable, "unknown", "No synthetic reputation entry for this observable.");
    }
}

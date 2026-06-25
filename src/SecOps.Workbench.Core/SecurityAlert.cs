namespace SecOps.Workbench.Core;

public sealed record SecurityAlert(
    string Id,
    string Title,
    string Source,
    AlertSeverity Severity,
    string Category,
    DateTimeOffset TimestampUtc,
    string Principal,
    string Asset,
    IReadOnlyList<string> Observables);

public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

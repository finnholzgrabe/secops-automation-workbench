namespace SecOps.Workbench.Core;

/// <summary>
/// Code-defined fallback copy of the playbooks shipped under <c>playbooks/</c>. Triage uses
/// this when no playbook directory is supplied, which keeps unit tests and a clean checkout
/// working offline. A test guards that the on-disk files and this catalog stay in sync.
/// </summary>
public static class PlaybookCatalog
{
    public static IReadOnlyList<Playbook> Default { get; } = new Playbook[]
    {
        new(
            "generic-alert-triage",
            "Generic alert triage",
            "Baseline triage steps for any alert that does not match a more specific playbook.",
            "any",
            Array.Empty<string>(),
            new[]
            {
                "Open a case note and keep every response action in dry-run mode.",
                "Confirm the alert is not known maintenance activity or an established false positive.",
                "Escalate to a human analyst for review before any containment action."
            },
            DryRunOnly: true),

        new(
            "identity-alert-basic-triage",
            "Identity alert basic triage",
            "First-line triage for identity alerts such as brute force or use of valid accounts.",
            "identity",
            new[] { "T1078", "T1110" },
            new[]
            {
                "Open a case note and keep every response action in dry-run mode.",
                "Verify whether the principal recently changed password, MFA, or recovery settings.",
                "Review recent authentication events for the affected asset or tenant.",
                "If compromise is suspected, recommend session review and an analyst-approved password reset."
            },
            DryRunOnly: true),

        new(
            "identity-mfa-fatigue-triage",
            "Identity MFA fatigue triage",
            "Triage for suspected MFA fatigue: repeated push prompts followed by an approval.",
            "identity",
            new[] { "T1078", "T1110", "T1621" },
            new[]
            {
                "Open a case note and keep every response action in dry-run mode.",
                "Ask the user to confirm whether the recent MFA prompts were expected.",
                "Review MFA push volume and look for repeated denials followed by a late approval.",
                "If MFA fatigue is confirmed, recommend session revocation and password reset as analyst-approved steps.",
                "Suggest number-matching or prompt throttling as a follow-up hardening recommendation."
            },
            DryRunOnly: true),
    };
}

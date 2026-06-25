# Triage: Suspicious login followed by MFA fatigue

- Alert ID: sample-001
- Severity: High
- Principal: alice@example.invalid
- Asset: idp-tenant-demo
- Techniques: T1078, T1110, T1621
- Recommended playbook: identity-mfa-fatigue-triage

## Rationale

The alert is a High identity event with observables [failed_login_burst, mfa_push_spam, new_country, successful_login_after_failures], mapped to T1078, T1110, T1621. Selected playbook 'identity-mfa-fatigue-triage'. The recommendation stays in analyst-review/dry-run mode.

## Enrichment (synthetic)

- Identity: alice@example.invalid — risk tier low
- Asset: idp-tenant-demo — criticality high
- Observable context:
  - failed_login_burst: suspicious — Bursts of failed logins can indicate brute force or password spray. (synthetic)
  - mfa_push_spam: suspicious — Repeated MFA prompts may indicate MFA fatigue or push bombing. (synthetic)
  - new_country: suspicious — A first-seen country can indicate account takeover or legitimate travel. (synthetic)
  - successful_login_after_failures: suspicious — A success after many failures can indicate a guessed or sprayed credential. (synthetic)

## Recommended safe actions

- Open a case note and keep every response action in dry-run mode.
- Ask the user to confirm whether the recent MFA prompts were expected.
- Review MFA push volume and look for repeated denials followed by a late approval.
- If MFA fatigue is confirmed, recommend session revocation and password reset as analyst-approved steps.
- Suggest number-matching or prompt throttling as a follow-up hardening recommendation.

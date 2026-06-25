# SecOps Automation Workbench

A small .NET-first workbench for security-automation engineering: alert normalization, MITRE ATT&CK-style mapping, deterministic playbook selection, enrichment stubs, and safe simulated response actions.

This repository is intentionally scoped as an engineering workbench, not a production SOAR/SIEM/EDR product.

## What this demonstrates

- Security-alert normalization and triage flow design
- API- and playbook-oriented automation thinking
- Detection/response terminology without overstating SOC production experience
- Clean .NET architecture, deterministic tests, and CI-friendly command-line tooling

## What this is not

- Not a production incident-response platform
- Not a validated SOAR, SIEM, or EDR system
- Not a replacement for security monitoring, case management, or analyst review
- Not connected to real customer, identity, endpoint, or firewall systems by default

## Quick start

```sh
dotnet restore
dotnet build --no-restore
dotnet run --project src/SecOps.Workbench.Cli -- --help
dotnet run --project src/SecOps.Workbench.Cli -- triage samples/alerts/suspicious-login.json
dotnet test
```

Tests use xUnit and run with `dotnet test`. The domain logic in `Core` has no external dependencies, so the interesting behaviour is fast and deterministic to verify.

### Report output formats

The `triage` command renders a Markdown report by default and can emit deterministic JSON. Use `--out` to write to a file instead of stdout:

```sh
# Markdown to stdout (default)
dotnet run --project src/SecOps.Workbench.Cli -- triage samples/alerts/suspicious-login.json

# JSON to stdout
dotnet run --project src/SecOps.Workbench.Cli -- triage samples/alerts/suspicious-login.json --format json

# JSON written to a file (generated reports under artifacts/ are git-ignored)
dotnet run --project src/SecOps.Workbench.Cli -- triage samples/alerts/suspicious-login.json --format json --out artifacts/triage.json
```

The JSON report has a stable top-level shape (`alertId`, `severity`, `techniqueIds`, `recommendedPlaybook`, `recommendedActions`, `rationale`, `dryRun`). An unknown `--format` exits non-zero. Every report keeps `dryRun: true`, reflecting the safe-by-default response model.

### Playbooks

Recommendations are driven by local playbook definitions under [`playbooks/`](playbooks). Each playbook declares `id`, `title`, `description`, `category`, `techniques`, `recommendedActions`, and `dryRunOnly`. Triage selects the best-fitting playbook by category and technique overlap; when no directory is present it falls back to a built-in catalog, so triage works from a clean checkout.

```sh
# List the available playbooks
dotnet run --project src/SecOps.Workbench.Cli -- playbooks list

# Validate the playbook directory (exits non-zero on any invalid file)
dotnet run --project src/SecOps.Workbench.Cli -- playbooks validate
```

Validation enforces the safety model: every playbook must set `dryRunOnly: true`, technique IDs must be ATT&CK-style, and required fields must be present. Invalid playbooks fail with readable, per-field error messages.

### Enrichment (synthetic)

Triage attaches enrichment context behind three interfaces — `IIdentityContextProvider`, `IAssetContextProvider`, and `IReputationProvider`. **Only synthetic mock providers are implemented**: they make no network calls, read no real systems, and are fully deterministic (a stable FNV hash is used instead of `string.GetHashCode`, which is randomized per process). The enrichment adds principal risk hints, asset criticality, and per-observable context to every report, and is clearly labelled `synthetic` in the output. Real identity-provider, CMDB, or threat-intel integrations are intentionally out of scope.

## Current slice

Version 0.1 starts with a tiny but working vertical slice:

1. Load an alert JSON file.
2. Normalize it into a typed domain model.
3. Map selected signals to ATT&CK-style technique IDs.
4. Attach synthetic enrichment (identity, asset, observable context).
5. Select a safe playbook recommendation.
6. Print a deterministic analyst-facing triage summary.

## Architecture

```text
src/SecOps.Workbench.Core  Domain model, parser, triage logic, playbook selection
src/SecOps.Workbench.Cli   File IO, command parsing, user-facing output
tests/SecOps.Workbench.Tests xUnit regression tests for parsing, mapping, triage, and output contracts
docs/                      Architecture, threat model, roadmap, and scope notes
samples/alerts/            Tiny synthetic alerts only
artifacts/                 Generated outputs; heavy or local outputs are ignored
```

## Roadmap

- Sigma-rule test fixtures and detection-content quality checks
- Case-note generation in Markdown/JSON
- Safe response simulator with dry-run and rollback semantics
- Optional adapters for local Wazuh/Shuffle labs, never enabled by default

## Safety

All samples are synthetic. Response actions must be dry-run by default. Never commit real alerts, credentials, customer data, internal logs, or personal security events.

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
dotnet run --project tests/SecOps.Workbench.Tests --no-build
```

The test project is a dependency-free console runner on purpose, so the repository can be built offline from a clean checkout.

## Current slice

Version 0.1 starts with a tiny but working vertical slice:

1. Load an alert JSON file.
2. Normalize it into a typed domain model.
3. Map selected signals to ATT&CK-style technique IDs.
4. Select a safe playbook recommendation.
5. Print a deterministic analyst-facing triage summary.

## Architecture

```text
src/SecOps.Workbench.Core  Domain model, parser, triage logic, playbook selection
src/SecOps.Workbench.Cli   File IO, command parsing, user-facing output
tests/SecOps.Workbench.Tests Dependency-free regression tests
docs/                      Architecture, threat model, roadmap, and scope notes
samples/alerts/            Tiny synthetic alerts only
artifacts/                 Generated outputs; heavy or local outputs are ignored
```

## Roadmap

- YAML-like playbook definitions with validation
- Enrichment interfaces for mock reputation, asset, and identity context
- Sigma-rule test fixtures and detection-content quality checks
- Case-note generation in Markdown/JSON
- Safe response simulator with dry-run and rollback semantics
- Optional adapters for local Wazuh/Shuffle labs, never enabled by default

## Safety

All samples are synthetic. Response actions must be dry-run by default. Never commit real alerts, credentials, customer data, internal logs, or personal security events.

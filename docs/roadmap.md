# Roadmap

## Milestone 1 - publishable skeleton

- [x] .NET solution
- [x] Offline-friendly restore/build
- [x] CLI help/version
- [x] Synthetic alert triage
- [x] Deterministic tests
- [x] CI workflow

## Milestone 2 - playbooks

- Add simple playbook files under `playbooks/`
- Validate required fields
- Select playbooks by alert category, severity, and mapped techniques
- Generate a Markdown case note

## Milestone 3 - enrichment

- Add interfaces for asset, identity, and reputation enrichment
- Implement mock providers
- Keep real network calls disabled by default

## Milestone 4 - detection engineering bridge

- Add Sigma-style detection examples and fixtures
- Add a detection-content linter
- Map rules to ATT&CK-style techniques

## Milestone 5 - response simulator

- Add dry-run response actions
- Add rollback metadata
- Add audit-log output

# Architecture

The workbench is built around a deliberately small pipeline:

```text
raw alert JSON
  -> AlertParser
  -> SecurityAlert
  -> TriageEngine
  -> TriageResult
  -> CLI report / generated case note
```

## Boundaries

- `SecOps.Workbench.Core` contains deterministic domain logic only.
- `SecOps.Workbench.Cli` owns filesystem access and exit codes.
- Tests run the core and selected CLI contracts without external services.

## Design principles

- Keep all default data synthetic.
- Prefer dry-run recommendations over real response actions.
- Keep every public claim backed by code and tests.
- Make gaps explicit: this is an engineering workbench, not production SOC tooling.

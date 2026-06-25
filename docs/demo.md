# Demo

A single, reproducible run of the workbench against the bundled synthetic alert.
All data is synthetic and every response step is a dry-run recommendation.

## Prerequisites

- .NET 8 SDK
- Run all commands from the repository root.

```sh
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

## 1. Triage an alert (Markdown)

```sh
dotnet run --project src/SecOps.Workbench.Cli -- triage samples/alerts/suspicious-login.json
```

This normalizes the alert, maps observables to ATT&CK-style techniques, attaches
synthetic enrichment, and selects a safe playbook. See the captured output in
[examples/triage-report.md](examples/triage-report.md).

## 2. Triage an alert (JSON)

```sh
dotnet run --project src/SecOps.Workbench.Cli -- triage samples/alerts/suspicious-login.json --format json
```

The JSON report has a stable top-level shape and always carries `"dryRun": true`.
See [examples/triage-report.json](examples/triage-report.json).

## 3. Generate an analyst case note

```sh
dotnet run --project src/SecOps.Workbench.Cli -- triage samples/alerts/suspicious-login.json --case-note
```

Produces a Markdown case note with a summary, observables, mapped techniques, the
recommended playbook, an analyst checklist, a dry-run response plan, and an
explicit limitations section. See [examples/case-note.md](examples/case-note.md).

## 4. Validate playbooks

```sh
dotnet run --project src/SecOps.Workbench.Cli -- playbooks list
dotnet run --project src/SecOps.Workbench.Cli -- playbooks validate
```

Validation enforces the safety model (every playbook must be `dryRunOnly`) and
fails with readable, per-field errors.

## 5. Lint detection content

```sh
dotnet run --project src/SecOps.Workbench.Cli -- detections lint
```

Checks each Sigma-inspired rule for a title, valid status, ATT&CK tag, meaningful
false-positive notes, and an existing test fixture.

## Expected results

- `dotnet test` passes.
- Triage prints a deterministic report; re-running produces identical output.
- `playbooks validate` reports `3/3 playbooks valid.`
- `detections lint` reports `2/2 detections passed linting.`

The captured example outputs under [examples/](examples) are produced by the
commands above and are safe to regenerate.

# Changelog

## 1.0.12 — 2026-07-21

- **Diagnostics:** ship `VpHubPluginFileLog` in `libs/LicensingSystem.Revit.Licensing` — write to `%LocalAppData%\VP-Hub\logs\{productCode}.log` so VP-Hub Agent Export diagnostics / Report a problem can pack plugin traces (agent builds with `DiagnosticsLogPackager`).
- Docs: `docs/logging-redaction-policy.md`; onboarding / AGENTS / smoke / integration checklist updated for the log convention.
- Skills & rules: prefer shared SDK helpers; local file-log phase in `vp-hub-revit-integration` and `revit-add-in-licensing`.
- Sync script also copies logging-redaction-policy from LicensingSystem.

## SDK sync 2026-07-20

- Source: LicensingSystem (v0.3.88-23-g5f3aca2)
- libs: Revit.Licensing, Contracts, Agent.Ipc, Agent.Ipc.Revit
- docs: publisher-facing copies (incl. logging policy)

## 1.0.11 — 2026-07-19

- **Fix (Revit 2024 STJ):** `NamedPipeAgentClient` deserializes IPC responses via UTF-8 **string** overload (not `ReadOnlySpan<byte>`), avoiding `MissingMethodException` when another add-in already loaded an older `System.Text.Json` into the shared AppDomain.
- Synced from LicensingSystem: `Agent.Ipc.Revit`, slim `Agent.Ipc` template, Contracts, Revit.Licensing.
- `docs/lp-net48-overlay.md`: document STJ conflict, LP-only hardening (preload / honest UX), smoke row for MissingMethod.
- `SMOKE-TESTS.md`: net48 checklist includes STJ MissingMethod / multi-add-in case.

## SDK sync 2026-07-19

- Source: LicensingSystem (v0.3.88-19-g711d9a6)
- libs: Revit.Licensing, Contracts, Agent.Ipc, Agent.Ipc.Revit
- docs: publisher-facing copies

## 1.0.2 — 2026-07-16

- Packaging: `Resolve-RevitInstallRoot` prefers `revit/revit-api`, then `revit-api`, then Autodesk (`packaging/` + `templates/packaging/`).
- Skills: pitfalls for SDK `**/*.cs` globs / `Compile Remove`, dual API mirrors, commit-before-package, deploy layout confusion, golden net48 Contents check.
- Licensing reference: prefer **FileVersion** for IPC `pluginVersion` when NBGV uses `assemblyVersion.precision: minor`.
- `RELEASE.md`: commit `version.json` before production build; clarify output path; forbid partial-year portal uploads.
- `docs/lp-net48-overlay.md`: LP multi-year packaging marked implemented; keep net48 self-contained DLL checklist.

## 1.0.1 — 2026-07-09

- Working `packaging/` scripts and `plugin.manifest.json` in kit root for smoke builds.
- Integration checklist note for reference implementation.

## 1.0.0 — 2026-07-09

- Initial **VP-Hub Revit Publisher Kit** (packaging + licensing integration).
- Cursor skills: `vp-hub-revit-integration`, `revit-add-in-licensing`, `revit-vp-hub-packaging`.
- Vendored SDK under `libs/` with publisher-slim net8 IPC client.
- Templates, adopt script, release ZIP builder.
- Docs: onboarding, release, smoke tests, integration checklist.

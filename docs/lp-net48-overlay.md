# Handoff: Lightning Protection (`lp-revit-plugin`) — net48 / multi-year packaging

**Audience:** LP / `lp-revit-plugin` maintainers.  
**Blocks:** PC2-class failures (`FileNotFoundException: System.Text.Json` before agent IPC).

LicensingSystem agent and Revit.Licensing UX fixes do **not** replace self-contained net48 folders — Revit 2023–2024 (.NET Framework 4.8) require all runtime DLLs in the bundle.

## Status (2026-07)

Multi-year VP-Hub packaging is **implemented** in `lp-revit-plugin`:

```powershell
powershell -File packaging/Build-LPApplicationPackage.ps1
```

- TFMs: **net48** (2023–2024), **net8.0-windows** (2025–2026)
- Bundle: `LightningProtection.Revit.bundle`
- Output: `artifacts/builds/lightning.protection/<version>/package.zip`

Keep the checklist below as the lasting net48 requirement (do not regress to “main DLL only”).

## Lasting requirement: net48 self-contained Contents

For each Revit year **≤ 2024**, after `dotnet build -f net48`, copy **all** `*.dll` (and optional `*.pdb`) from the TFM output (e.g. `bin\x64\Release\net48\`) into `Contents/<year>/` — same pattern as `Copy-ProbeBuildArtifacts` in this kit’s packaging scripts.

| File | Required |
|------|----------|
| `System.Text.Json.dll` | Yes |
| `LicensingSystem.Agent.Ipc.Revit.dll` | Yes |
| `LicensingSystem.Contracts.dll` | Yes |
| `LicensingSystem.Revit.Licensing.dll` | Yes |
| Main add-in DLL (`LP.dll`) | Yes |

`Test-RevitApplicationPackage.ps1` must fail the release if any of these are missing.

## Release steps (unchanged)

1. Bump and commit `version.json`.
2. Run `packaging/Build-LPApplicationPackage.ps1` (no `-AllowPartialYears` for portal).
3. Record SHA-256; upload `package.zip` (keep previous portal version until smoke passes).

## Smoke test — clean Revit 2024 VM

**Environment:** Windows with Revit 2024 only. **No** Visual Studio, **no** .NET SDK, **no** manual DLL copies.

| Step | Action | Pass criterion |
|------|--------|----------------|
| 1 | Install VP-Hub agent; sign in | Status OK |
| 2 | Products → Install / update `lightning.protection` | No deploy error |
| 3 | Restart Revit 2024 | Add-in appears |
| 4 | Run licensed command | No `FileNotFoundException` dialog |
| 5 | Export diagnostics | `environment.json` → `hasSystemTextJson: true` for LP bundle |
| 6 | Check `%LocalAppData%\VP-Hub\agent-namedpipe.log` | At least one `type='canRun'` after command |

## Related docs

- [revit-bundle-packaging.md](revit-bundle-packaging.md)
- [revit-add-in-onboarding.md](revit-add-in-onboarding.md)
- [revit-pr-checklist.md](revit-pr-checklist.md)
- Kit [`RELEASE.md`](../RELEASE.md)

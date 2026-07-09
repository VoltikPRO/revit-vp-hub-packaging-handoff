# Handoff: Lightning Protection (`lp-revit-plugin`) — Revit 2024 net48 DLL packaging

**Audience:** LP / `lp-revit-plugin` maintainers.  
**Blocks:** PC2-class failures (`FileNotFoundException: System.Text.Json` before agent IPC).

LicensingSystem agent and Revit.Licensing UX fixes do **not** replace this handoff — Revit 2024 (.NET Framework 4.8) requires self-contained runtime DLLs in the bundle.

## Reference implementation (this repo)

Copy the artifact pattern from [revit/build-license-probe-package.ps1](../../revit/build-license-probe-package.ps1) → `Copy-ProbeBuildArtifacts`:

```powershell
foreach ($pat in '*.dll', '*.pdb', '*.deps.json', '*.runtimeconfig.json', '*.dll.config') {
    Get-ChildItem -Path $BuildOut -Filter $pat -File -ErrorAction SilentlyContinue |
        ForEach-Object { Copy-Item $_.FullName $DestDir -Force }
}
```

Build output for Revit 2024 must be **net48** with project reference to `LicensingSystem.Agent.Ipc.Revit`.

## Required changes in `lp-revit-plugin`

### 1. Packaging scripts

In `packaging/Build-LPAllRevitYears.ps1` and/or `packaging/Build-RevitApplicationPackage.ps1`:

- For each Revit year **≤ 2024**, after `dotnet build -f net48`, copy **all** `*.dll` (and optional `*.pdb`) from `bin\Release\net48\` into `Contents/<year>/` inside the bundle.

### 2. `Test-RevitApplicationPackage.ps1` (CI gate)

Fail the build if any net48 year folder is missing:

| File | Required |
|------|----------|
| `System.Text.Json.dll` | Yes |
| `LicensingSystem.Agent.Ipc.Revit.dll` | Yes (or actual IPC assembly name) |
| `LicensingSystem.Contracts.dll` | Yes |
| Main add-in DLL | Yes |

### 3. New manifest entry

1. Bump product version (for example `2026.5.15` → `2026.5.16`).
2. Build `package.zip` with single root `LightningProtection.Revit.bundle`.
3. Compute SHA-256; add row to `artifacts/manifest.json`.
4. **Do not remove** the previous version until smoke passes.

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

## Bennett validation (customer)

After agent + LP releases:

- **PC2:** Install / update to new LP version → restart Revit → license check reaches agent (no dependency error).
- If manual `System.Text.Json.dll` was added earlier: **Uninstall** → **Install / update** for a clean bundle.

## Related docs

- [revit-bundle-packaging.md](../architecture/revit-bundle-packaging.md)
- [revit-add-in-onboarding.md](revit-add-in-onboarding.md)
- [revit-pr-checklist.md](revit-pr-checklist.md)

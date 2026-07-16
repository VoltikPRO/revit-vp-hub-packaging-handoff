---
name: revit-vp-hub-packaging
description: >-
  Builds and validates Autodesk Revit ApplicationPlugins VP-Hub bundles
  (multi-year package.zip, PackageContents.xml, deploy folders) for licensed
  and generic Revit add-ins. Use when packaging a Revit add-in for VP-Hub,
  creating packaging scripts, fixing bundle layout, revit-api builds, net48
  AssemblyResolve, or artifacts/manifest.json release entries.
disable-model-invocation: true
---

# Revit VP-Hub Bundle Packaging

Workflow for producing a **valid, runnable** Revit ApplicationPlugins bundle for VP-Hub distribution.

Licensing integration: use [`revit-add-in-licensing`](../revit-add-in-licensing/SKILL.md) in parallel for VP-Hub licensed products.

## Success levels

| Level | Criterion | Required |
|-------|-----------|----------|
| **A — Valid package** | `Test-RevitApplicationPackage.ps1` passes | Yes |
| **B — Runnable plugin** | Revit loads add-in; command runs; no `FileNotFoundException` | Yes |
| **C — Licensed product** | VP-Hub agent `canRun`, publisher pinning verified | Yes for VP-Hub products |

## Project manifest

Create `packaging/plugin.manifest.json`:

```json
{
  "productCode": "my.product",
  "bundleName": "MyProduct.Revit.bundle",
  "mainAssembly": "MyProduct.dll",
  "addinFileName": "MyProduct.addin",
  "entryClass": "MyProduct.App.App",
  "displayName": "My Product",
  "companyName": "MyCompany",
  "vendorId": "MYC",
  "description": "Short product description for PackageContents.xml.",
  "supportedYears": [2023, 2024, 2025, 2026],
  "csprojPath": "src/MyProduct/MyProduct.csproj",
  "requiredDlls": {
    "net48": [
      "MyProduct.dll",
      "System.Text.Json.dll",
      "LicensingSystem.Agent.Ipc.Revit.dll",
      "LicensingSystem.Contracts.dll",
      "LicensingSystem.Revit.Licensing.dll"
    ],
    "net8": ["MyProduct.dll", "LicensingSystem.Revit.Licensing.dll"]
  },
  "minDllCount": { "net48": 5, "net8": 2 }
}
```

- Populate `requiredDlls` after first build; for **licensed VP-Hub** net48, minimum list above is mandatory (agent `RevitNet48BundleValidator`).
- `bundleName` should end with `.Revit.bundle` (convention).

## .addin template

Use `ClientId` (not `AddInId`):

```xml
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>{displayName}</Name>
    <Assembly>.\{mainAssembly}</Assembly>
    <ClientId>{unique-guid}</ClientId>
    <FullClassName>{entryClass}</FullClassName>
    <VendorId>{vendorId}</VendorId>
    <VendorDescription>{companyName}</VendorDescription>
  </AddIn>
</RevitAddIns>
```

## Production build

```powershell
powershell -File packaging/Build-ProductApplicationPackage.ps1
```

Pipeline: `Build-ProductAllRevitYears.ps1` → `Build-RevitApplicationPackage.ps1` → `Test-RevitApplicationPackage.ps1`

Dev only (do not publish):

```powershell
powershell -File packaging/Build-ProductApplicationPackage.ps1 -AllowPartialYears -RevitYears 2024
```

## PackageContents.xml

Generate via script — do not hand-edit releases.

| Rule | Value |
|------|-------|
| `SeriesMin` / `SeriesMax` | `R<yyyy>` — **not** `R24.0` |
| Min = Max | Same per year block |
| `ModuleName` | `./Contents/<year>/<addinFileName>` |

## VP-Hub release (Level C)

After `package.zip` is built:

1. Script prints **SHA-256** hex.
2. Add entry to `artifacts/manifest.json` (see [`RELEASE.md`](../../../RELEASE.md), [`docs/manifest.example.json`](../../../docs/manifest.example.json)).
3. Upload via publisher portal; users run **Install / update** in VP-Hub agent.
4. Agent deploys bundle to `%AppData%\Autodesk\ApplicationPlugins\`.

## net48 runtime

1. `CopyLocalLockFileAssemblies=true` when NuGet deps exist
2. Copy **all** `*.dll` from build output into `Contents/<year>/`
3. `PluginAssemblyResolver.Register()` first in `OnStartup`

## Forbidden

- Publishing `-AllowPartialYears` to production manifest
- Main DLL only in net48 `Contents/<year>/`
- `SeriesMin`/`SeriesMax` as `R24.0`
- ZIP with bare `Contents/` at root
- Skipping licensed net48 DLL validation for VP-Hub products

## Pitfalls (from publisher integrations)

### SDK default globs at repo root

A root-level `Microsoft.NET.Sdk` / `WindowsDesktop` project compiles `**/*.cs` under the repo, including vendored trees (`_handoff-extract/`, misplaced `libs/` sources). That can pull Agent.Core file-scoped namespaces into the add-in and fail under older `LangVersion` (e.g. 7.3).

Exclude vendored trees from compile:

```xml
<Compile Remove="_handoff-extract\**" />
<Compile Remove="packaging\**" />
<!-- also exclude libs source trees if they are not ProjectReference-only -->
```

Prefer `ProjectReference` to `libs/LicensingSystem.Revit.Licensing` (or equivalent) — do not compile LicensingSystem sources into the add-in project.

### Local Revit API mirrors

`Resolve-RevitInstallRoot` tries, in order: `revit/revit-api`, then `revit-api`, then Autodesk install folders. Layout:

```text
revit/revit-api/   (or revit-api/)
  Revit 2023/  ← RevitAPI.dll, RevitAPIUI.dll
  Revit 2024/
  ...
```

### Version bump before packaging

1. Bump and **commit** `version.json` (Nerdbank.GitVersioning reads committed version).
2. Run production packaging (no `-AllowPartialYears`).
3. Upload `package.zip` + SHA-256.

### Two deploy layouts — do not confuse

| Layout | Purpose |
|--------|---------|
| `deploy/<year>/` → `*.bundle` → `package.zip` | **Portal / VP-Hub** (only this) |
| `bin\...` or ad-hoc `deploy/publish/RevitYYYY` | Local smoke only — not an update package |

### Golden check after build

Compare `Contents/2024/` (or net48 year) to a known-good ApplicationPlugins install: must include `System.Text.Json.dll`, `LicensingSystem.Agent.Ipc.Revit.dll`, `LicensingSystem.Contracts.dll`, `LicensingSystem.Revit.Licensing.dll`, and the main add-in DLL.

## Additional resources

- [reference.md](reference.md)
- [ADOPTION.md](../../../ADOPTION.md)
- Templates: [`templates/packaging/`](../../../templates/packaging/)

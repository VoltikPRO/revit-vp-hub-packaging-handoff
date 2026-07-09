---
name: revit-vp-hub-packaging-handoff
description: >-
  Builds and validates Autodesk Revit ApplicationPlugins VP-Hub bundles
  (multi-year package.zip, PackageContents.xml, deploy folders) without
  licensing platform dependencies. Use when packaging a Revit add-in for
  VP-Hub or ApplicationPlugins distribution, creating packaging scripts,
  fixing bundle layout, revit-api builds, or net48 AssemblyResolve issues.
disable-model-invocation: true
---

# Revit VP-Hub Bundle Packaging

Generic workflow for producing a **valid, runnable** Revit ApplicationPlugins bundle. No LicensingSystem, VP-Hub agent, or `canRun` IPC required.

## Success levels

| Level | Criterion | Required |
|-------|-----------|----------|
| **A — Valid package** | `Test-RevitApplicationPackage.ps1` passes | Yes |
| **B — Runnable plugin** | Revit loads add-in; at least one command runs; no `FileNotFoundException` for transitive DLLs | Yes |
| **C — Licensed product** | VP-Hub agent `canRun`, publisher pinning | No (project-specific overlay) |

A valid ZIP is **not** sufficient for net48 if NuGet transitive assemblies are used — Level B requires `AssemblyResolve` (see below).

## When to use

- First-time VP-Hub packaging setup for a Revit plugin
- Release build of `package.zip` for artifact storage / VP-Hub upload
- Debugging bundle layout, missing DLLs, or `PackageContents.xml` issues
- Adapting packaging from a reference implementation (e.g. sibling `LP/` repo)

**Not in scope:** licensing agent setup, entitlement manifests, publisher pinning.

## Project manifest

Discover or create `packaging/plugin.manifest.json` before adapting scripts:

```json
{
  "productCode": "my.product",
  "bundleName": "MyProduct.Revit.bundle",
  "mainAssembly": "MyProduct.dll",
  "addinFileName": "MyProduct.addin",
  "entryClass": "MyProduct.App.App",
  "displayName": "My Product",
  "companyName": "MyCompany",
  "description": "Short product description for PackageContents.xml.",
  "supportedYears": [2023, 2024, 2025, 2026],
  "requiredDlls": {
    "net48": [],
    "net8": []
  },
  "minDllCount": {
    "net48": 3,
    "net8": 2
  }
}
```

- `requiredDlls` — leave empty on first setup; populate after first successful per-year build (see [reference.md](reference.md)).
- `entryClass` must match `FullClassName` in the `.addin` template (typically `{Namespace}.App.App`).
- `bundleName` must end with `.Revit.bundle`.

## One-time setup checklist

Copy this checklist and track progress:

```
Setup progress:
- [ ] plugin.manifest.json created
- [ ] Multi-TFM csproj (net48 + net8.0-windows)
- [ ] Directory.Build.props with RevitInstallPath + revit-api auto-prefer
- [ ] packaging/ scripts (5 files — see reference.md script map)
- [ ] .addin template under packaging/<bundleName>/
- [ ] net48: CopyLocalLockFileAssemblies if NuGet deps exist
- [ ] net48: PluginAssemblyResolver in App.OnStartup (before heavy init)
- [ ] version.json for release versioning
```

### csproj essentials

- `TargetFrameworks`: `net48;net8.0-windows`
- Revit API references via `RevitInstallPath` (not copied to output — `Private=false`)
- TFM split: Revit year **≤ 2024** → `net48`; **≥ 2025** → `net8.0-windows`
- x64 platform required for Revit

### Directory.Build.props pattern

```xml
<RevitInstallPath Condition="'$(RevitInstallPath)' == '' and Exists('$(MSBuildThisFileDirectory)revit-api\Revit 2024\RevitAPI.dll')">
  $(MSBuildThisFileDirectory)revit-api\Revit 2024
</RevitInstallPath>
<RevitInstallPath Condition="'$(RevitInstallPath)' == ''">
  C:\Program Files\Autodesk\Revit 2024
</RevitInstallPath>
```

### .addin template

```xml
<AddIn Type="Application">
  <Name>{displayName}</Name>
  <Assembly>.\{mainAssembly}</Assembly>
  <FullClassName>{entryClass}</FullClassName>
  <AddInId>{unique-guid}</AddInId>
  <VendorId>{vendorId}</VendorId>
  <VendorDescription>{companyName}</VendorDescription>
</AddIn>
```

## Packaging folder structure (target project)

```text
packaging/
  plugin.manifest.json
  Packaging.Common.ps1
  Build-<Product>AllRevitYears.ps1
  Build-RevitApplicationPackage.ps1
  Build-<Product>ApplicationPackage.ps1
  Test-RevitApplicationPackage.ps1
  <BundleName>/
    <addinFileName>          # template copied into Contents/<year>/
revit-api/
  Revit 2023/ ... Revit 2026/   # RevitAPI.dll + RevitAPIUI.dll each
deploy/
  2023/ ... 2026/               # per-year build artifacts (gitignored)
artifacts/builds/<productCode>/<version>/
  <BundleName>/
  package.zip
```

Adapt script names from a reference implementation if available (e.g. `LP/packaging/`). Read reference scripts; parameterize product-specific strings from `plugin.manifest.json`.

## Production build flow

From repo root:

```powershell
powershell -File packaging/Build-<Product>ApplicationPackage.ps1
```

Pipeline:

1. **Build-*AllRevitYears.ps1** — per year: `dotnet build -c Release -f <tfm> -p:RevitInstallPath="..."` → `Copy-ProbeBuildArtifacts` → `deploy/<year>/`
2. **Build-RevitApplicationPackage.ps1** — copy `deploy/<year>/` into bundle `Contents/<year>/` → generate `PackageContents.xml` → `package.zip`
3. **Test-RevitApplicationPackage.ps1** — validation gate (fail build on error)

### Dev only (do NOT publish)

```powershell
powershell -File packaging/Build-<Product>ApplicationPackage.ps1 -AllowPartialYears -RevitYears 2024
```

## PackageContents.xml (summary)

**Generate via script — do not hand-edit for releases.**

| Rule | Value |
|------|-------|
| Location | `<bundleName>/PackageContents.xml` |
| `SeriesMin` / `SeriesMax` | `R<yyyy>` (e.g. `R2024`) — **not** `R24.0` |
| Min = Max | Same value per year block |
| `ModuleName` | `./Contents/<year>/<addinFileName>` |
| Years in XML | Must match `Contents/<year>/` folders 1:1 |
| `AppVersion` | 4-part (e.g. `1.2.3.0`) |
| `FriendlyVersion` | From `version.json` (e.g. `1.2.3`) |

Full examples and validation rules: [reference.md](reference.md).

## ZIP / bundle layout

- ZIP root = **exactly one** `*.Revit.bundle` folder
- **No** bare `Contents/` at ZIP root
- Each `Contents/<year>/` must contain: `.addin`, main assembly, all runtime DLLs

## net48 runtime (Revit 2023–2024)

CLR does **not** probe the add-in folder for transitive NuGet assemblies. If the plugin uses NuGet packages (e.g. `System.Text.Json`):

1. `CopyLocalLockFileAssemblies=true` in csproj for net48
2. `Copy-ProbeBuildArtifacts` copies **all** `*.dll` from build output
3. Register `AppDomain.AssemblyResolve` in `App.OnStartup` **before** any code that loads transitive assemblies

Pattern (idempotent `Register()`, load `{Name}.dll` from plugin directory):

```csharp
#if NETFRAMEWORK
PluginAssemblyResolver.Register(); // first line in OnStartup
#endif
```

## Forbidden

- Publishing `-AllowPartialYears` packages to production manifest
- Copying only the main DLL into `Contents/<year>/` (net48)
- `SeriesMin`/`SeriesMax` format `R24.0` instead of `R2024`
- ZIP with bare `Contents/` at root (must be inside `.bundle`)
- MSI as primary distribution when VP-Hub bundle is the target
- Hardcoding another project's licensing DLL names in validation

## LP reference vs generic

| Copy from LP `packaging/` | Strip / replace for generic |
|---------------------------|----------------------------|
| Script structure and flow | `LP` → product name in filenames |
| `Copy-ProbeBuildArtifacts` | Same |
| `Write-*PackageContentsXml` | Use manifest `displayName`, `companyName`, `addinFileName` |
| `Resolve-RevitInstallRoot` | Same |
| `Test-RevitApplicationPackage.ps1` layout checks | Replace `Get-LpRequiredNet48Dlls` with manifest `requiredDlls` |
| Multi-TFM build loop | Same |
| `PluginAssemblyResolver` | Same if NuGet deps on net48 |
| `LicensingSystem.*.dll` required list | **Remove** — use manifest-driven list |
| `LicensedCommand` / agent IPC smoke | **Remove** — use command smoke only |
| `RELEASE-CHECKLIST.md` licensing steps | Optional appendix only |

If sibling `LP/` repo exists, read `LP/packaging/` as reference implementation. Do not require `LicensingSystem` repo for generic projects.

## Release checklist

```
Release progress:
- [ ] version.json bumped
- [ ] Production build (all supportedYears, no -AllowPartialYears)
- [ ] Test-RevitApplicationPackage.ps1 passed
- [ ] SHA-256 recorded for artifact manifest
- [ ] Smoke: Revit loads add-in from ApplicationPlugins bundle
- [ ] No legacy ProgramData Addins/<year>/*.addin overriding bundle
```

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| `FileNotFoundException` for NuGet DLL on Revit 2024 | Missing `AssemblyResolve` or incomplete DLL copy |
| Add-in not listed in Revit | Wrong `PackageContents.xml` series (`R24.0`) or bad `ModuleName` |
| Plugin loads from dev path, not bundle | Legacy `ProgramData\...\Addins\<year>\*.addin` |
| Build fails: Revit API not found | Populate `revit-api/Revit <year>/` or pass `-RevitInstallRoot` |
| Validation: missing year folder | Partial build published without `-AllowPartialYears` guard |

## Additional resources

- Detailed rules: [reference.md](reference.md)
- Human install guide: [ADOPTION.md](ADOPTION.md)
- Reference implementation (if available): sibling `LP/packaging/`

# Revit `.bundle` packaging guide (`ApplicationPlugins` autoloader)

This document is the canonical packaging instruction for Revit add-ins in this repository.

It clarifies the difference between:

- `.bundle` (Autodesk autoloader package),
- `PackageContents.xml` (bundle metadata / routing),
- `.addin` (Revit add-in manifest that points to your DLL and entry class).

> Terminology note: the correct Autodesk term is `.bundle` (not "bandle").

## 1) What Revit loads

Revit scans `ApplicationPlugins` folders for `<AppName>.bundle`.

Inside each `.bundle`, Revit reads `PackageContents.xml`, selects compatible components via
`RuntimeRequirements`, then follows `ComponentEntry.ModuleName` to a `.addin` file.
The `.addin` manifest finally points to your assembly/class (`IExternalApplication` and/or `IExternalCommand`).

## 2) Official install locations

Revit supports both all-users and per-user locations:

- All users: `%ProgramData%\Autodesk\ApplicationPlugins\`
- Per user: `%AppData%\Autodesk\ApplicationPlugins\`

For this repository, per-user deployment is the default operational path unless explicitly overridden.

## 3) Standard `.bundle` layouts

### 3.1 Single-version layout

```text
MyPlugin.bundle/
  PackageContents.xml
  Contents/
    2026/
      MyPlugin.addin
      MyPlugin.dll
      (other runtime files)
      Resources/
        icon.png
        help.html
```

### 3.2 Multi-version layout

```text
MyPlugin.bundle/
  PackageContents.xml
  Contents/
    2024/
      MyPlugin.addin
      MyPlugin.dll
    2025/
      MyPlugin.addin
      MyPlugin.dll
    2026/
      MyPlugin.addin
      MyPlugin.dll
```

`PackageContents.xml` controls which version folder is used by `RuntimeRequirements` (`SeriesMin`/`SeriesMax`) and `ComponentEntry.ModuleName`.

## 4) Minimal `PackageContents.xml` example

Revit matches components using **internal series** strings on `RuntimeRequirements`, not the marketing year folder name alone.
For each `Contents/<year>/` block, set **`SeriesMin` and `SeriesMax` to the same value** `R<yyyy>` (for example `R2024` for Revit 2024). Do **not** use `R24.0` / `R24.9` ranges or other two-digit series forms — Revit autoloader will not load the add-in.

Reference: [`revit/build-license-probe-package.ps1`](../../revit/build-license-probe-package.ps1) `Write-PackageContentsXml`.

`AppNameSpace` is common for App Store bundles; internal or VP-Hub–distributed packages may omit it or use another namespace per your signing or store policy.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0"
                    AutodeskProduct="Revit"
                    ProductType="Application"
                    Name="My Plugin"
                    AppVersion="1.0.0"
                    AppNameSpace="appstore.exchange.autodesk.com">
  <Components Description="Revit 2026">
    <RuntimeRequirements OS="Win64" Platform="Revit"
                         SeriesMin="R2026" SeriesMax="R2026" />
    <ComponentEntry AppName="My Plugin"
                    Version="1.0.0"
                    ModuleName="./Contents/2026/MyPlugin.addin"
                    AppDescription="My Plugin for Revit 2026" />
  </Components>
</ApplicationPackage>
```

## 5) Minimal `.addin` example (relative assembly path)

```xml
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>My Plugin</Name>
    <Assembly>.\MyPlugin.dll</Assembly>
    <ClientId>PUT-YOUR-GUID-HERE</ClientId>
    <FullClassName>MyCompany.MyPlugin.App</FullClassName>
    <VendorId>MYCO</VendorId>
    <VendorDescription>MyCompany</VendorDescription>
  </AddIn>
</RevitAddIns>
```

The `.addin` file must be located exactly at the path referenced by `ModuleName` in `PackageContents.xml`.

## 6) In-session loading

Revit autoloader supports in-session behavior controls (for example `AllowLoadIntoExistingSession`).
If your plugin requires restart semantics, set/handle this policy explicitly in package metadata and release notes.

## 7) Repository-specific packaging workflow

1. Build the plugin binaries.
2. Produce a `.bundle` with `PackageContents.xml` and `Contents/<RevitYear>/...`.
3. Build transport ZIP for the agent where ZIP root contains exactly one `*.bundle`.
4. Agent installs by copying that bundle to:
   - `%AppData%\Autodesk\ApplicationPlugins\` (default in this repo).
5. Restart Revit if bundle was replaced while Revit was running.

### 7.1 Lightning Protection Revit plugin (`lp-revit-plugin`)

Reference implementation that follows this document’s layout (multi-year `Contents/<year>/`, single bundle root in `package.zip`):

| Script | Role |
| --- | --- |
| `packaging/Build-LPAllRevitYears.ps1` | Builds the add-in per Revit year into `deploy/<year>/`. |
| `packaging/Build-LPApplicationPackage.ps1` | Reads `version.json`, calls the generator below, emits `package.zip` + `manifest.json`, runs `Test-RevitApplicationPackage.ps1`. |
| `packaging/Build-RevitApplicationPackage.ps1` | Generic bundle + ZIP assembly; series ids via `Get-RevitSeriesId` (aligned with section 4 / probe script). |

Related operational docs:

- `revit/README.md`
- `docs/architecture/plugin-lifecycle-onboarding.md`
- `docs/architecture/artifacts.md`

## 8) Validation checklist (pre-release)

- [ ] `PackageContents.xml` exists at bundle root.
- [ ] `ComponentEntry.ModuleName` points to an existing `.addin`.
- [ ] `.addin` uses a valid relative `Assembly` path.
- [ ] `SeriesMin`/`SeriesMax` match supported Revit versions.
- [ ] ZIP root contains one `*.bundle` (no flattened `Contents` mistake).
- [ ] Install/uninstall flow verified via agent.
- [ ] **Revit 2023–2024 (net48):** each `Contents/<year>/` folder is **self-contained** — all runtime `*.dll` from the net48 build output are copied (including `System.Text.Json.dll`, `LicensingSystem.Agent.Ipc.Revit.dll`, `LicensingSystem.Contracts.dll`). Reference: [build-license-probe-package.ps1](../../revit/build-license-probe-package.ps1) `Copy-ProbeBuildArtifacts`.
- [ ] Smoke on a machine **without** Visual Studio / .NET SDK: Revit 2024 loads the add-in and IPC `canRun` succeeds.

## 9) References (Autodesk)

- Autodesk Revit API docs: Add-in registration / manifest basics.
- Autodesk app packaging guidance (`PackageContents.xml`, autoloader `.bundle`, App Store submission guidance).
- Autodesk APS blog/docs on Revit bundle + `ModuleName` resolution.
- Autodesk open-source sample: `revit-ifc` `PackageContents.xml`.

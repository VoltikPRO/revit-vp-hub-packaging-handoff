# Revit VP-Hub Packaging — Reference

Detailed rules for adapting packaging scripts into a target Revit plugin project. Licensing-neutral.

## A. Script responsibilities

Create these five scripts in `packaging/`. Names are parameterized by product (replace `<Product>` / read from `plugin.manifest.json`).

| Script | Responsibility |
|--------|----------------|
| `Packaging.Common.ps1` | Shared helpers: TFM map, year resolution, `Copy-ProbeBuildArtifacts`, `Write-PackageContentsXml`, `Resolve-RevitInstallRoot`, `Test-PackageContentsSeriesFormat`, version helpers |
| `Build-<Product>AllRevitYears.ps1` | Loop supported years: `dotnet build` per TFM + copy all artifacts to `deploy/<year>/` |
| `Build-RevitApplicationPackage.ps1` | Assemble `<bundleName>/` from `deploy/`, write `PackageContents.xml`, produce `package.zip` |
| `Build-<Product>ApplicationPackage.ps1` | Orchestrator: build → pack → test |
| `Test-RevitApplicationPackage.ps1` | CI/release gate on `package.zip` |

Reference implementation: sibling `LP/packaging/` (if available).

### Packaging.Common.ps1 — key functions

**Get-TfmForRevitYear**

```powershell
function Get-TfmForRevitYear {
    param([int] $Year)
    if ($Year -le 2024) { return "net48" }
    return "net8.0-windows"
}
```

**Copy-ProbeBuildArtifacts** — copy ALL runtime files, not just main DLL:

```powershell
foreach ($pat in '*.dll', '*.pdb', '*.deps.json', '*.runtimeconfig.json', '*.dll.config') {
    Get-ChildItem -Path $BuildOut -Filter $pat -File -ErrorAction SilentlyContinue |
        ForEach-Object { Copy-Item $_.FullName $DestDir -Force }
}
```

Also copy the `.addin` template into each `deploy/<year>/`.

**Resolve-BuildOut** — locate build output:

```text
bin/x64/Release/<tfm>   (preferred)
bin/Release/<tfm>       (fallback)
```

**Resolve-RevitInstallRoot** — auto-prefer `revit-api/` when all requested year folders contain both API DLLs; otherwise use `C:\Program Files\Autodesk`.

**Normalize-AppVersionFour** — ensure 4-part `AppVersion`:

```powershell
if ($Version -match '^\d+\.\d+\.\d+$') { return "$Version.0" }
return $Version
```

**Write-PackageContentsXml** — generate from manifest fields (`displayName`, `companyName`, `addinFileName`, `description`, `supportedYears`).

**Test-PackageContentsSeriesFormat** — fail if `SeriesMin`/`SeriesMax` are not `R<yyyy>` or if min ≠ max.

### Build-<Product>AllRevitYears.ps1

Per year in `supportedYears`:

1. Resolve `RevitInstallPath` → `revit-api/Revit <year>/` or Program Files
2. Skip or fail if API DLLs missing (`-AllowPartialYears` for dev only)
3. `dotnet build <csproj> -c Release -f <tfm> -p:RevitInstallPath="<path>"`
4. `Copy-ProbeBuildArtifacts` → `deploy/<year>/`
5. Assert minimum DLL count (from manifest `minDllCount`)

Default production behavior: **require all** `supportedYears` (throw if any missing).

### Build-RevitApplicationPackage.ps1

1. Read `deploy/<year>/` for each year
2. Copy into `<bundleName>/Contents/<year>/`
3. Ensure `.addin` exists (copy template if missing)
4. Call `Write-PackageContentsXml`
5. Copy bundle to `artifacts/builds/<productCode>/<version>/`
6. `Compress-Archive` → `package.zip` (bundle folder as single root entry)
7. Output SHA-256

### Build-<Product>ApplicationPackage.ps1

1. Read version from `version.json` (or `-Version` param)
2. Call `Build-<Product>AllRevitYears.ps1` (unless `-SkipBuild`)
3. Call `Build-RevitApplicationPackage.ps1`
4. Call `Test-RevitApplicationPackage.ps1` (unless `-SkipTest`)

### Test-RevitApplicationPackage.ps1

Checks:

- ZIP exists; expands to temp
- Exactly one `*.bundle` at root
- No bare `Contents/` at ZIP root
- `PackageContents.xml` exists inside bundle
- Series format `R<yyyy>`; min = max
- Every declared year has `Contents/<year>/` with `.addin` and required DLLs
- Every `Contents/<year>/` folder has matching XML declaration
- DLL count ≥ manifest `minDllCount` per TFM
- Each file in manifest `requiredDlls.<tfm>` exists (if list non-empty)

## B. PackageContents.xml

### Single year example (2024)

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationPackage SchemaVersion="1.0"
                    AutodeskProduct="Revit"
                    ProductType="Application"
                    Name="My Product"
                    Description="Short product description."
                    AppVersion="1.2.3.0"
                    FriendlyVersion="1.2.3">
  <CompanyDetails Name="MyCompany" Url="" Email="" />
  <Components Description="Revit 2024">
    <RuntimeRequirements OS="Win64" Platform="Revit" SeriesMin="R2024" SeriesMax="R2024" />
    <ComponentEntry AppName="My Product"
                    Version="1.2.3"
                    ModuleName="./Contents/2024/MyProduct.addin"
                    AppDescription="My Product for Revit 2024." />
  </Components>
</ApplicationPackage>
```

### Multi-year

Add one `<Components>` block per year. Sort years ascending. Each block:

- `RuntimeRequirements SeriesMin/Max` = `R<year>`
- `ModuleName` = `./Contents/<year>/<addinFileName>`

### Validation rules

| Rule | Invalid example |
|------|-----------------|
| `SeriesMin` matches `^R\d{4}$` | `R24.0`, `24.0` |
| `SeriesMax` matches `^R\d{4}$` | `R24` |
| `SeriesMin` = `SeriesMax` | `R2023` / `R2024` |
| XML years = folder years | XML has 2024 but no `Contents/2024/` |
| `ModuleName` points to `.addin` | `./Contents/2024/MyProduct.dll` |

### Version fields

| Field | Source | Example |
|-------|--------|---------|
| `FriendlyVersion` | `version.json` | `1.2.3` |
| `AppVersion` | 4-part normalized | `1.2.3.0` |
| `ComponentEntry Version` | Same as FriendlyVersion | `1.2.3` |

## C. Required DLL validation (generic)

**Do not** hardcode `LicensingSystem.*.dll` or other project-specific names in the generic skill.

### Recommended workflow

1. Run first successful `Build-<Product>AllRevitYears.ps1` for one net48 year and one net8 year
2. List `deploy/<year>/*.dll`
3. Record names in `plugin.manifest.json`:

```json
"requiredDlls": {
  "net48": ["MyProduct.dll", "System.Text.Json.dll", "..."],
  "net8": ["MyProduct.dll", "..."]
},
"minDllCount": {
  "net48": 8,
  "net8": 4
}
```

4. `Test-RevitApplicationPackage.ps1` reads manifest for per-year checks

### Fallback (minimal projects)

If `requiredDlls` arrays are empty, validate only:

- `mainAssembly` exists
- `minDllCount.<tfm>` total DLL count met

Increase `minDllCount` after first build reveals actual transitive dependency count.

## D. revit-api mirror

### Layout

```text
revit-api/
  Revit 2023/
    RevitAPI.dll
    RevitAPIUI.dll
  Revit 2024/
    RevitAPI.dll
    RevitAPIUI.dll
  ...
```

Folder names: `Revit 2023` (with space), not `Revit2023`.

### What belongs here

- **Only** compile-time Revit API assemblies
- **Not** plugin runtime DLLs, NuGet packages, or bundle contents

### Auto-detect behavior

When **all** requested `supportedYears` have both DLLs under `<repo>/revit-api/Revit <year>/`, packaging scripts prefer `revit-api/` over Program Files. No manual `-RevitInstallRoot` needed.

### Manual override

```powershell
powershell -File packaging/Build-<Product>ApplicationPackage.ps1 `
  -RevitInstallRoot "C:\path\to\revit-api"
```

## E. net48 AssemblyResolve

Required when net48 build output includes NuGet transitive assemblies that Revit CLR will not auto-load from the add-in folder.

### Implementation pattern

```csharp
internal static class PluginAssemblyResolver
{
    private static bool _registered;
    private static string _pluginDirectory;

    internal static void Register()
    {
        if (_registered) return;
        _pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        _registered = true;
    }

    private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        var path = Path.Combine(_pluginDirectory, name + ".dll");
        return File.Exists(path) ? Assembly.LoadFrom(path) : null;
    }
}
```

Register as **first operation** in `IExternalApplication.OnStartup`, before logging, licensing, or IPC.

Reference: `LP/Helpers/PluginAssemblyResolver.cs`.

## F. Smoke test (no licensing)

### Environment

- Clean Windows VM or machine with target Revit installed
- No Visual Studio, no SDK, no manual DLL copies
- Bundle installed to `%AppData%\Autodesk\ApplicationPlugins\<bundleName>\`

### Steps

1. Copy or install `package.zip` (VP-Hub install or manual extract)
2. Remove conflicting legacy add-ins:
   - `C:\ProgramData\Autodesk\Revit\Addins\<year>\<plugin>.addin` if it points to dev build path
3. Start Revit for target year
4. Verify add-in appears (ribbon or Add-Ins list)
5. Run at least one command successfully
6. Check logs for `FileNotFoundException` on any DLL

### Explicitly excluded from generic smoke

- VP-Hub / LicensingSystem agent running
- `canRun` in `agent-namedpipe.log`
- Entitlement sync or publisher pinning

## G. LP licensing overlay (optional)

If a project later integrates a licensing platform (as LP does):

- Add licensing DLLs to `requiredDlls` after build reveals names
- Add agent IPC smoke (`canRun`) to project-specific release checklist
- See `LP/packaging/RELEASE-CHECKLIST.md` for LP-specific gates

This appendix is **not** required for generic VP-Hub bundle packaging.

## H. CI fixture validation (optional)

Projects may add a workflow that:

1. Builds fixture bundles (complete / incomplete / bad ZIP root)
2. Runs `Test-RevitApplicationPackage.ps1` and asserts pass/fail
3. Does **not** require full Revit install if only layout fixtures are tested

Reference: `LP/.github/workflows/lp-package-validation.yml`.

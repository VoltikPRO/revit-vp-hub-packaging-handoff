# Revit VP-Hub Packaging — Reference

Detailed rules for packaging scripts. See [`templates/packaging/`](../../../templates/packaging/) for parameterized scripts.

## Licensed net48 required DLLs (VP-Hub)

For Revit 2023–2024 licensed add-ins, each `Contents/<year>/` **must** include:

| File | Required |
|------|----------|
| Main add-in DLL | Yes |
| `System.Text.Json.dll` | Yes |
| `LicensingSystem.Agent.Ipc.Revit.dll` | Yes |
| `LicensingSystem.Contracts.dll` | Yes |
| `LicensingSystem.Revit.Licensing.dll` | Yes |

`Test-RevitApplicationPackage.ps1` reads `plugin.manifest.json` → `requiredDlls.net48`.

See also [`docs/lp-net48-overlay.md`](../../../docs/lp-net48-overlay.md).

## Script map

| Script | Role |
|--------|------|
| `Packaging.Common.ps1` | TFM map, `Copy-ProbeBuildArtifacts`, `Write-PackageContentsXml`, validators |
| `Build-ProductAllRevitYears.ps1` | Per-year `dotnet build` → `deploy/<year>/` |
| `Build-RevitApplicationPackage.ps1` | Bundle + `package.zip` + SHA-256 |
| `Build-ProductApplicationPackage.ps1` | Orchestrator |
| `Test-RevitApplicationPackage.ps1` | Release gate |

## Copy-ProbeBuildArtifacts

```powershell
foreach ($pat in '*.dll', '*.pdb', '*.deps.json', '*.runtimeconfig.json', '*.dll.config') {
    Get-ChildItem -Path $BuildOut -Filter $pat -File -ErrorAction SilentlyContinue |
        ForEach-Object { Copy-Item $_.FullName $DestDir -Force }
}
```

## PackageContents.xml validation

| Rule | Invalid |
|------|---------|
| `^R\d{4}$` series | `R24.0`, `R24` |
| Min = Max per block | `R2023` / `R2024` |
| XML years = folder years | mismatch |

## VP-Hub manifest entry

After build, add to `artifacts/manifest.json`:

```json
{
  "productCode": "my.product",
  "version": "1.2.3",
  "channel": "stable",
  "sha256": "<from build output>",
  "relativePath": "builds/my.product/1.2.3/package.zip",
  "fileName": "package.zip"
}
```

## Smoke (packaging)

1. Install bundle to ApplicationPlugins (VP-Hub or manual)
2. Revit loads add-in; no `FileNotFoundException`
3. Licensed: `canRun` in `%LocalAppData%\VP-Hub\agent-namedpipe.log` — see [`SMOKE-TESTS.md`](../../../SMOKE-TESTS.md)

## net48 AssemblyResolve

See [`templates/PluginAssemblyResolver.template.cs`](../../../templates/PluginAssemblyResolver.template.cs).

Register **before** licensing or IPC in `OnStartup`.

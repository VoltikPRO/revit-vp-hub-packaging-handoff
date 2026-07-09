# Read-only reference — do not ship as a product

This folder is a **License Probe** sample from the VP-Hub / LicensingSystem ecosystem. Copy **patterns**, not this assembly, into your shipping add-in.

## What to read

1. `LicenseProbeCommand.cs` — command + `LicenseProbeEnsureLicensed.BuildStatusReportAsync`
2. `LicenseProbeEnsureLicensed.cs` — central gate (`TryAllow` + `RevitLicenseCanRunReport`)
3. `LicenseProbeConstants.cs` + `LicenseProbePinningFactory.cs` + `LicenseProbePinnedEcKey.cs` — pinning
4. `LicenseProbeApplication.cs` — ribbon / `LicenseProbeAssemblyResolver` (net48)
5. `LicenseProbeAboutCommand.cs` — gated with `TryAllow`

## Packaging

Requires Revit API installed. See `packaging/plugin.manifest.json`.

```powershell
powershell -File packaging/Build-ProductApplicationPackage.ps1 -AllowPartialYears -RevitYears 2026
```

## Build (optional)

```powershell
dotnet build reference/LicenseProbe/LicensingSystem.Revit.LicenseProbe.csproj -c Release `
  -p:RevitInstallPath="C:\Program Files\Autodesk\Revit 2026"
```

Project references point to `../../libs/`.

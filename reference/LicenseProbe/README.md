# Read-only reference — do not ship as a product

This folder is a **License Probe** sample from the VP-Hub / LicensingSystem ecosystem. Copy **patterns**, not this assembly, into your shipping add-in.

## What to read

1. `LicenseProbeCommand.cs` — command gating + `RevitLicenseCanRunReport`
2. `LicenseProbeConstants.cs` + `LicenseProbePinningFactory.cs` + `LicenseProbePinnedEcKey.cs` — pinning
3. `LicenseProbeApplication.cs` — ribbon / app entry

## Build (optional)

Requires Revit API installed:

```powershell
dotnet build reference/LicenseProbe/LicensingSystem.Revit.LicenseProbe.csproj -c Release `
  -p:RevitInstallPath="C:\Program Files\Autodesk\Revit 2026"
```

Project references point to `../../libs/`.

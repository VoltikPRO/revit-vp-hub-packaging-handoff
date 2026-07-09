# LicensingSystem.Revit.Licensing

Shared helpers for Revit add-ins on **LicensingSystem**: named-pipe `canRun` to the local Windows agent, **ES256** grant proof verification, and optional formatted status text (`RevitLicenseCanRunReport`).

- **Targets:** `net48` (Revit 2023–2024) and `net8.0-windows` (Revit 2025+).
- **Policy:** see repo root `AGENTS.md` and `docs/architecture/revit-licensing.md`.
- **Publisher onboarding:** `docs/publishers/revit-add-in-onboarding.md`.

## Consume from this monorepo

Add a **ProjectReference** to `LicensingSystem.Revit.Licensing.csproj` from your add-in (same TFM split as `LicensingSystem.Revit.LicenseProbe`).

## Optional: pack a NuGet for an internal feed

```bash
dotnet pack revit/LicensingSystem.Revit.Licensing/LicensingSystem.Revit.Licensing.csproj -c Release -o ./dist
```

The package lists dependencies on `LicensingSystem.Contracts` and the appropriate `LicensingSystem.Agent.Ipc` assembly; those must be available on the same feed (or use project references instead). See `docs/publishers/nuget.md`.

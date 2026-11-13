# LicensingSystem.Revit.Licensing

Shared helpers for Revit add-ins on **LicensingSystem**: named-pipe `canRun` to the local Windows agent, **ES256** grant proof verification, optional formatted status text (`RevitLicenseCanRunReport`), and local file diagnostics (`VpHubPluginFileLog`).

- **Targets:** `net48` (Revit 2023–2024) and `net8.0-windows` (Revit 2025+).
- **Policy:** see repo root `AGENTS.md` and `docs/architecture/revit-licensing.md`.
- **Publisher onboarding:** `docs/publishers/revit-add-in-onboarding.md`.
- **File logs:** `%LocalAppData%\VP-Hub\logs\{productCode}.log` via `VpHubPluginFileLog` — packed by the agent into Export diagnostics (see `docs/architecture/logging-redaction-policy.md`).
- **Report a problem:** `VpHubPluginProblemReport.SendInBackground(...)` from a ribbon command — the agent packs the logs and sends them to the platform; the person sees the result with a report number. Needs an agent build with `submitPluginReport`.

## Consume from this monorepo

Add a **ProjectReference** to `LicensingSystem.Revit.Licensing.csproj` from your add-in (TFM: `net48` for Revit 2023–2024, `net8.0-windows` for Revit 2025+). External vendors: prefer publisher kit `libs/` via [`docs/publishers/revit-publisher-handoff.md`](../../docs/publishers/revit-publisher-handoff.md).

## Optional: pack a NuGet for an internal feed

```bash
dotnet pack revit/LicensingSystem.Revit.Licensing/LicensingSystem.Revit.Licensing.csproj -c Release -o ./dist
```

The package lists dependencies on `LicensingSystem.Contracts` and the appropriate `LicensingSystem.Agent.Ipc` assembly; those must be available on the same feed (or use project references instead). See `docs/publishers/nuget.md`.

# LicensingSystem.Revit.Licensing

Shared helpers for Revit add-ins on **VP-Hub**: named-pipe `canRun`, **ES256** proof verification, `RevitLicenseCanRunReport`, and local file diagnostics (`VpHubPluginFileLog`).

- **Targets:** `net48` (Revit 2023–2024) and `net8.0-windows` (Revit 2025+).
- **Policy:** [`docs/AGENTS.md`](../../docs/AGENTS.md), [`docs/revit-licensing.md`](../../docs/revit-licensing.md).
- **Onboarding:** [`docs/revit-add-in-onboarding.md`](../../docs/revit-add-in-onboarding.md).
- **File logs:** `%LocalAppData%\VP-Hub\logs\{productCode}.log` via `VpHubPluginFileLog` — packed by the agent into Export diagnostics ([`docs/logging-redaction-policy.md`](../../docs/logging-redaction-policy.md)).

## Consume from this kit

Add a **ProjectReference** to this `.csproj` from your add-in (TFM: `net48` for Revit 2023–2024, `net8.0-windows` for Revit 2025+). See also [`docs/nuget.md`](../../docs/nuget.md).

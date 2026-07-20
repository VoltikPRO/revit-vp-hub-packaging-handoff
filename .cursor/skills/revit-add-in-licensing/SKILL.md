---
name: revit-add-in-licensing
description: >-
  Guides VP-Hub / LicensingSystem Revit add-ins: named-pipe IPC to the Windows agent,
  CanRunProofDto ES256 verification with RevitLicenseProofVerifier, RevitLicensePinning,
  IExternalCommand gating, anti-replay nonce/timestamp, VpHubPluginFileLog diagnostics.
  Use when editing or creating Revit add-ins, NamedPipeAgentClient,
  RevitLicenseCanRunReport, plugin/product pinning, canRun flows, net48 vs net8 targets,
  or anything touching libs/LicensingSystem.Revit.* or libs/LicensingSystem.Agent.Ipc*.
---

# Revit add-in licensing (VP-Hub publisher kit)

Navigator for agents integrating licensed Revit add-ins. Policy: [`docs/AGENTS.md`](../../../docs/AGENTS.md).

## Read first (in order)

1. [`docs/AGENTS.md`](../../../docs/AGENTS.md)
2. [`docs/revit-licensing.md`](../../../docs/revit-licensing.md)
3. [`libs/LicensingSystem.Revit.Licensing/`](../../../libs/LicensingSystem.Revit.Licensing/) — `RevitLicenseProofVerifier`, `RevitLicenseCanRunReport`, `VpHubPluginFileLog`
4. IPC clients:
   - [`libs/LicensingSystem.Agent.Ipc/NamedPipeAgentClient.cs`](../../../libs/LicensingSystem.Agent.Ipc/NamedPipeAgentClient.cs) — net8 (Revit 2025+)
   - [`libs/LicensingSystem.Agent.Ipc.Revit/NamedPipeAgentClient.cs`](../../../libs/LicensingSystem.Agent.Ipc.Revit/NamedPipeAgentClient.cs) — net48 (Revit 2023–2024)
5. [`libs/LicensingSystem.Contracts/Agent/Grants/Grants.cs`](../../../libs/LicensingSystem.Contracts/Agent/Grants/Grants.cs) — `CanRunProofDto`
6. [`docs/revit-add-in-onboarding.md`](../../../docs/revit-add-in-onboarding.md)
7. [`docs/plugin-brand-book.md`](../../../docs/plugin-brand-book.md)
8. [`docs/logging-redaction-policy.md`](../../../docs/logging-redaction-policy.md)

## Non-negotiables

- **No** direct Worker/cloud API calls from Revit.
- **No** tokens or private signing keys in the add-in.
- Named-pipe IPC only (`CanRunAsync` / `type = canRun`).
- **Never** trust `Allowed` without `RevitLicenseProofVerifier.TryVerifyProof`.
- Pin publisher identity + `ProductCode`; nonce + timestamp freshness; deny by default.
- Cache verified OK (30–120 s; `RevitLicenseCanRunReport` default 60 s).

## TFM / IPC matrix

| Revit years | TFM | IPC package |
|-------------|-----|-------------|
| 2023, 2024 | net48 | `LicensingSystem.Agent.Ipc.Revit` |
| 2025+ | net8.0-windows | `LicensingSystem.Agent.Ipc` |

## Implementation workflow

1. Portal: product code, publisher keys (ES256), pinning snippet → constants.
2. Project references to `libs/LicensingSystem.Revit.Licensing` + IPC per matrix. NuGet: [`docs/nuget.md`](../../../docs/nuget.md).
3. `RevitLicensePinning` from constants; net48 X/Y sync with PEM.
4. Gate every licensed entry point; prefer `RevitLicenseCanRunReport.BuildAsync`.
5. Central `EnsureLicensed(...)` for all commands.
6. Local file log: `VpHubPluginFileLog.Write(productCode, message, correlationId)` → `%LocalAppData%\VP-Hub\logs\{productCode}.log`.
7. Packaging: sibling skill [`revit-vp-hub-packaging`](../revit-vp-hub-packaging/SKILL.md).

## Local diagnostics

- Path convention: `%LocalAppData%\VP-Hub\logs\{productCode}.log`
- Helper: `libs/LicensingSystem.Revit.Licensing/VpHubPluginFileLog.cs`
- Agent Export diagnostics / Report a problem packs `logs\*.log` (requires agent with `DiagnosticsLogPackager`)
- Do not invent another folder; do not stream full logs over IPC
- Optional: `LogPluginEventAsync` for short cloud telemetry only

## Definition of done

Per `docs/AGENTS.md`: all commands gated, verification centralized, anti-replay present, failure denies, file logs under VP-Hub\logs when diagnostics are used.

## Reference

[`reference.md`](reference.md)

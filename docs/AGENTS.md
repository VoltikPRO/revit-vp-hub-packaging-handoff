## Cursor / AI agent: Revit plugin licensing integration (must follow)

This publisher kit contains the VP-Hub licensing SDK for Windows desktop + Revit add-ins.
When editing any Revit add-in, treat this document as a strict implementation policy.

## Core architecture (non-negotiable)

- **Revit add-in** must NOT call the cloud API directly.
- **Windows agent** owns authentication, sync, and online communication.
- Add-ins must talk to the agent via **Named Pipe IPC** and receive a **publisher-signed proof**.

## Source of truth (read first)

- `libs/LicensingSystem.Revit.Licensing/*` — shared proof verification, canRun report, `VpHubPluginFileLog`
- `libs/LicensingSystem.Agent.Ipc/NamedPipeAgentClient.cs` — IPC client for **.NET 8** (Revit 2025+)
- `libs/LicensingSystem.Agent.Ipc.Revit/NamedPipeAgentClient.cs` — same wire protocol for **Revit 2023–2024** (.NET Framework 4.8)
- `libs/LicensingSystem.Contracts/Agent/Grants/Grants.cs` — contracts (`CanRunProofDto`, grants)
- `docs/revit-licensing.md` — architecture and verification requirements
- `docs/revit-add-in-onboarding.md` — publisher onboarding checklist
- `docs/plugin-brand-book.md` — plugin messaging and UX conventions
- `docs/logging-redaction-policy.md` — what may/must not be logged (incl. local file logs)

## Required command gating flow

Gate every licensed entry point (each `IExternalCommand.Execute` and any non-command entry point that does licensed work):

1. Generate request values:
   - `nonce` (random GUID in `"N"` format)
   - `timestampUtc` (`DateTimeOffset.UtcNow`)
   - `pluginVersion` (assembly version)
2. Call:
   - `NamedPipeAgentClient.CanRunAsync(productCode, pluginVersion, nonce, timestampUtc)`
3. Deny execution if IPC call fails (agent unavailable / not signed in).
4. Deny execution if `Allowed == false`; show user-friendly reason.
5. If `Allowed == true`, still deny unless cryptographic verification succeeds.

Prefer a shared helper/SDK so all commands call a single `EnsureLicensed(...)` entry point.

## Hard security requirements

- Never trust a bare boolean `Allowed`.
- Always verify publisher-signed grant material in `CanRunProofDto`.
- Pin publisher identity in the add-in:
  - `ExpectedPublisherId`
  - `ExpectedKid`
  - `ExpectedPublisherPublicKeyPem`
  - optional `ExpectedAudienceServerId`
- Verification must confirm:
  - product binding (`ProductCode` and grant payload match expected product)
  - freshness (`RequestNonce` exact match)
  - timestamp echo skew <= 2 minutes
  - ES256 signature validity over canonical payload bytes
- Signature parsing must accept both encodings used by ecosystem signers:
  - ASN.1 DER
  - raw 64-byte `R||S`

If any verification step fails, treat the result as unlicensed and deny execution.

## Local diagnostics (file logs)

- Write under `%LocalAppData%\VP-Hub\logs\{productCode}.log` via `VpHubPluginFileLog` in `libs/LicensingSystem.Revit.Licensing`.
- VP-Hub Agent **Export diagnostics** / **Report a problem** packs `logs\*.log` (agent builds with `DiagnosticsLogPackager`).
- Do not invent another log directory; do not stream full file logs over IPC.
- Optional cloud telemetry: `LogPluginEventAsync` (`run` / `success` / `fail`) — separate from the file log.
- Follow `docs/logging-redaction-policy.md`.

## UX and runtime behavior rules

- Agent unreachable: show a short actionable message ("Licensing agent is not running / not signed in").
- Not allowed: map `Reason` to clear next steps (sign in, sync, contact admin).
- Verification failure: show a support-oriented error and deny execution.

## Performance requirements

- Cache verified "allow" results in memory (typical TTL: 30-120 seconds).
- Avoid repeated IPC calls in a single user action.
- Keep UI responsive; avoid long blocking operations on the Revit thread.

## Forbidden patterns

- No direct add-in calls to Cloudflare Worker API.
- No token/secret storage inside add-ins.
- No "fallback allow" behavior when verification fails.

## Definition of done for licensing edits

Changes are not complete unless all of the following are true:

- every licensed command path is gated
- proof verification is mandatory and centrally reused
- nonce/timestamp anti-replay checks are present
- pinned publisher checks are present
- failure paths deny execution by default

## Localization (EN + UK)

User-facing Revit copy should be culture-aware where the brand book requires it. Do not translate technical identifiers (product codes, UUIDs, PEM).

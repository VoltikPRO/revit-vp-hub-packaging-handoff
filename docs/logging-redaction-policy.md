# Licensing logging and redaction policy

This policy defines what may and may not be logged in licensing flows across:

- Revit add-ins
- agent (`agent/src`)
- Worker API (`worker-api`)

## 1) Red lines (MUST NOT log)

Never log these values in plaintext:

- access tokens, refresh tokens, session tokens, admin session tokens
- lease tokens
- complete JWT strings
- full signatures (`SignatureBase64`) or raw signature bytes
- private keys, PEM blobs, or cryptographic key material
- full grant payload JSON
- user passwords
- direct PII (full personal names, phone numbers, addresses)

If the code currently logs any of these, treat it as a bug and remove/redact.

## 2) Allowed fields (safe observability set)

The following are safe and encouraged:

- `reasonCode` (standard taxonomy)
- high-level status (`allowed`, `denied`, `verify_failed`, `agent_unavailable`)
- product code
- plugin/agent version
- correlation id / request id
- event timestamp (UTC)
- coarse HTTP status code
- operation stage (`run`, `success`, `fail`)

## 3) Structured logging format

Use key/value structured logs where possible.

Recommended minimal shape:

```json
{
  "component": "agent",
  "event": "licensing.can_run",
  "result": "denied",
  "reasonCode": "NOT_ENTITLED",
  "productCode": "revit.licenseprobe",
  "correlationId": "..."
}
```

## 4) User-facing messages (B2B-safe)

User-facing dialogs/messages must:

- be actionable ("sign in", "sync", "contact admin")
- avoid raw cryptographic internals
- avoid leaking token/session diagnostics

Detailed diagnostics should stay in support logs (already redacted).

## 5) Error reporting rules

When an exception occurs:

- map to standard `reasonCode`
- log concise exception class + coarse message
- avoid dumping stack traces into user dialogs
- stack traces are acceptable only in developer logs if redaction is guaranteed

## 6) Local file logs (agent + plugins)

Shared folder on the end-user PC:

```text
%LocalAppData%\VP-Hub\logs\
```

| Writer | File name | Helper |
|--------|-----------|--------|
| Agent licensing diag | `agent.licensing.log` | `AgentDiagLog` |
| Revit add-in | `{productCode}.log` (sanitized) | `VpHubPluginFileLog` in `LicensingSystem.Revit.Licensing` |

Conventions for publishers:

- Write only under `%LocalAppData%\VP-Hub\logs\` — never into the Revit install or `.bundle` folder.
- Prefer `VpHubPluginFileLog.Write(productCode, message, correlationId)` so Export diagnostics / Report a problem pick the file up automatically.
- Line shape: UTC timestamp + optional `pid` / `cid` + message (no secrets).
- Keep detailed traces in the **file**; use IPC `logPluginEvent` only for short cloud telemetry (`run` / `success` / `fail`). Do not stream full file logs over the named pipe.

Agent packing (`DiagnosticsLogPackager` / `ExportDiagnostics`):

- Includes legacy root logs (`agent-startup.log`, `agent-namedpipe.log`, `agent-worker.log`, `ui-startup.log`) and every `logs\*.log`.
- Caps per-file size and file count; oversized logs contribute trailing bytes only (`logs-manifest.json` records truncation).

Open logs in the agent UI resolves to the `logs\` folder (`getLogsFolder`).

## 7) Review checklist

- [ ] No forbidden values are logged.
- [ ] All deny paths emit a standard reason code.
- [ ] User messages are actionable and non-sensitive.
- [ ] Correlation id is present for support triage.
- [ ] Plugin file logs use `%LocalAppData%\VP-Hub\logs\{productCode}.log`.

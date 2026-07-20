# Revit add-ins: licensing integration (agent + signed proofs)

This document defines how Revit add-ins must integrate with **LicensingSystem** so that licensing decisions are:

- **Centralized** (agent owns auth and cloud connectivity)
- **Enforceable** (seat leasing online, offline grace policy)
- **Tamper-resistant** (publisher-signed grant verification inside the add-in)

## Model overview

### Components

- **Revit add-in** (your .NET add-in DLL/bundle)
  - calls the local Windows agent over **Named Pipe**
  - receives a **`CanRunProofDto`**
  - verifies the proof cryptographically and gates execution

- **Windows agent**
  - authenticates to the Cloudflare Worker API
  - syncs entitlements + signed grants
  - enforces online seat leases (concurrent usage)
  - returns proofs to local add-ins

- **Cloud API (Worker + D1)**
  - stores entitlements, publisher keys, license grants
  - issues/renews seat leases to the agent

### Why the add-in must verify signatures

If the add-in only trusts `Allowed == true`, then any local process could spoof the agent or IPC response.
Therefore, the add-in must verify:

- the **publisher key is pinned** (trusted anchor)
- the **grant is signed** (ES256) and matches the expected product
- the **response freshness** (nonce/timestamp echo) to prevent replay

## Required runtime flow (command gating)

For each licensed entry point (typically each `IExternalCommand.Execute`):

1. Generate:
   - `nonce` (random GUID, `"N"` format)
   - `timestampUtc` (`DateTimeOffset.UtcNow`)
   - `pluginVersion` (assembly version string)
2. Call the agent IPC:
   - `NamedPipeAgentClient.CanRunAsync(productCode, pluginVersion, nonce, timestampUtc)`
3. If agent call fails:
   - deny execution and show a user message (agent not running / not signed in)
4. If `proof.Allowed == false`:
   - deny execution and show a user message based on `proof.Reason`
5. If `proof.Allowed == true`:
   - still deny execution unless `TryVerifyProof(...)` succeeds

Shared helpers (prefer these):

- `libs/LicensingSystem.Revit.Licensing/RevitLicenseCanRunReport.cs`
- `libs/LicensingSystem.Revit.Licensing/RevitLicenseProofVerifier.cs`
- `libs/LicensingSystem.Revit.Licensing/VpHubPluginFileLog.cs`

## Proof verification requirements

The add-in MUST verify all of:

- **Product binding**
  - `proof.ProductCode == expectedProductCode` (case-insensitive)
  - `proof.Grant.Payload.ProductCode == expectedProductCode`
- **Freshness**
  - `proof.RequestNonce == expectedNonce`
  - `|proof.RequestTimestampUtc - expectedTimestampUtc| <= 2 minutes`
- **Presence**
  - `proof.Grant != null` and `proof.PublisherKey != null`
- **Pinned publisher**
  - `PublisherId`, `Kid`, and `PublicKeyPem` must match pinned constants
  - `Algorithm` must be `es256`
  - optional: `AudienceServerId` must match pinned expected audience (if set)
- **Signature validity**
  - Verify ES256 (ECDSA P-256 + SHA-256) over canonical payload bytes
  - `Grant.SignatureBase64` may be **either** ASN.1 DER (typical for many .NET signers) **or** raw 64-byte R||S (Web Crypto / browser signing). Add-ins must accept both so grants verified by the Worker are also accepted locally (see `libs/LicensingSystem.Revit.Licensing/RevitLicenseProofVerifier.cs`).

### Pinning strategy

Pin these values in the add-in (as constants or generated at build time):

- `ExpectedPublisherId`
- `ExpectedKid`
- `ExpectedPublisherPublicKeyPem`
- optional: `ExpectedAudienceServerId`

Pinning prevents a compromised or spoofed local agent from minting arbitrary “allowed” responses.

## Offline behavior

Offline behavior is decided by the agent policy:

- The agent can allow execution offline for a limited grace window after the last successful sync.
- The add-in still verifies the signed grant locally; it must never “invent” offline allowance.

## User messaging guidance

### Agent unreachable

Show a short message:

- Agent service not running
- Not signed in / no org
- How to open the agent UI and sign in

### Not allowed / reasons

Show:

- A user-readable explanation mapped from `proof.Reason`
- A “what to do next” hint (sign in, sync, ask admin for seats/license)

### Verification failed

Treat as **not licensed** and show:

- “License verification failed; please contact support.”
- Optionally include a short internal error code for diagnostics

## Performance guidance

- Cache “verified OK” results in memory for 30–120 seconds.
- Avoid making multiple IPC calls per command; do one check at command start.
- Do not perform network calls in the add-in.

## Recommended packaging / reuse (SDK)

If you maintain multiple add-ins, create a shared library (internal SDK) used by all add-ins:

- `LicensingSystem.Revit.Sdk` (example name)
  - wraps IPC + proof verification + caching
  - exposes one method like `EnsureLicensed(productCode, pluginVersion)`

The SDK must depend only on:

- `LicensingSystem.Agent.Ipc`
- `LicensingSystem.Contracts`

and should remain free of Revit-specific types so it can be tested easily.

## Implementation checklist (copy into PR descriptions)

Use this as a merge gate for Revit add-in licensing changes:

- [ ] Licensed entry points call a single shared gating helper (`EnsureLicensed(...)` or equivalent).
- [ ] Helper generates `nonce`, `timestampUtc`, and `pluginVersion` per check.
- [ ] Helper calls `NamedPipeAgentClient.CanRunAsync(...)` (no direct cloud/API calls).
- [ ] `Allowed == false` is denied with a user-facing reason.
- [ ] `Allowed == true` still requires full proof verification.
- [ ] Product, nonce, and timestamp echo checks are enforced.
- [ ] Publisher pinning checks (`PublisherId`, `Kid`, `PublicKeyPem`) are enforced.
- [ ] Signature verification accepts DER and raw `R||S` ES256 signatures.
- [ ] Verification or IPC failures default to deny.
- [ ] Verified allow results are cached in memory for a short TTL.

## Common implementation mistakes

Reject changes exhibiting any of these anti-patterns:

- Trusting only `Allowed` without signature and pin verification.
- Skipping nonce/timestamp comparison because "IPC is local".
- Adding direct calls from add-in code to Worker/cloud endpoints.
- Storing refresh/access tokens in add-in settings or local files.
- Adding permissive fallback logic (for example, "allow on verification error").
- Repeating custom verification logic per command instead of centralizing it.


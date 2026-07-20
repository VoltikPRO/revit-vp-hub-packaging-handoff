# PR / release checklist — Revit add-in licensing

Copy into a PR description when changing licensing, IPC, or publisher pins.

## Security (must)

- [ ] Add-in does **not** call the cloud Worker/API directly from Revit.
- [ ] No secrets in the add-in (no tokens, no private grant-signing PEM).
- [ ] Every licensed command path calls the **same** gate (helper / `RevitLicenseCanRunReport` / equivalent).
- [ ] Gate does **not** treat `Allowed == true` as sufficient without **`RevitLicenseProofVerifier.TryVerifyProof`** (or equivalent).
- [ ] Nonce + timestamp echo checked; skew within policy (see `SecurityConstants.ClockSkewSeconds`).

## Pinning (must)

- [ ] `ProductCode` matches **Products** in admin / D1.
- [ ] `ExpectedPublisherId`, `ExpectedKid`, `ExpectedPublisherPublicKeyPem` match an **active** publisher key row (and grant signing uses that key).
- [ ] If grants use `audienceServerId`, `ExpectedAudienceServerId` is set; otherwise it stays empty.
- [ ] After PEM changes: **Revit 2024 / net48** embedded P-256 coordinates (`ECParameters`) updated if applicable.

## Packaging (must for Revit 2023–2024)

- [ ] **Bundle self-contained for net48:** `Contents/<year>/` includes all transitive `*.dll` from the net48 build (not only the main add-in DLL). Minimum: `System.Text.Json.dll`, IPC client (`LicensingSystem.Agent.Ipc.Revit.dll`), `LicensingSystem.Contracts.dll`.
- [ ] Packaging script copies all build-output DLLs into `Contents/<year>/` (see [`revit-bundle-packaging.md`](revit-bundle-packaging.md) and publisher kit templates).
- [ ] CI or script fails if required net48 DLLs are missing from the ZIP.

## UX / support

- [ ] User-facing strings for agent unreachable, missing plugin dependency, not entitled, and verification failure follow [`plugin-brand-book.md`](plugin-brand-book.md) where relevant.
- [ ] File diagnostics use `VpHubPluginFileLog` → `%LocalAppData%\VP-Hub\logs\{productCode}.log` (not a custom folder).

## Tests / smoke

- [ ] Manual smoke: agent running + signed in → allow path; agent off → safe deny.
- [ ] Revit 2024 on a clean VM (no SDK): add-in loads; `canRun` appears in agent named-pipe log.
- [ ] Plugin log appears under `%LocalAppData%\VP-Hub\logs\`; Export diagnostics ZIP includes `logs\*.log`.
- [ ] If IPC or contract changed, note compatibility with **minimum agent version** in PR text.

## Tests / smoke

- [ ] Manual smoke: agent running + signed in → allow path; agent off → safe deny.
- [ ] Revit 2024 on a clean VM (no SDK): add-in loads; `canRun` appears in agent named-pipe log.
- [ ] Plugin log appears under `%LocalAppData%\VP-Hub\logs\`; Export diagnostics ZIP includes `logs\*.log`.
- [ ] If IPC or contract changed, note compatibility with **minimum agent version** in PR text.

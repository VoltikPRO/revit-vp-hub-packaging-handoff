# Revit add-in publisher onboarding

Use this checklist when integrating a **new** or **updated** Revit add-in with VP-Hub. Policy: [`AGENTS.md`](AGENTS.md), [`revit-licensing.md`](revit-licensing.md).

**Terminology (do not conflate):** **organization** = customer tenant (users of the licensed add-in). **Publisher** = vendor who authors the add-in (`PublisherId`, publisher keys, pinning).

## 1. Cloud (product identity)

1. Register a **product** whose **Code** equals add-in `ProductCode` (e.g. `revit.myproduct` or `lightning.protection`).
2. Ensure **publisher keys** exist (`PublisherId`, `Kid`, algorithm **`es256`**, public PEM). Grant signing key must match pins in the add-in.
3. Use **Publisher → Pinning & keys** to align ProductCode / PublisherId / Kid / PEM (and optional audience); copy the C# snippet into constants.
4. Confirm **entitlements** and **license grants** for customer orgs use the same `productCode` and publisher material.

## 2. Add-in project (code)

1. Add a **project reference** to [`libs/LicensingSystem.Revit.Licensing`](../libs/LicensingSystem.Revit.Licensing/LicensingSystem.Revit.Licensing.csproj). Target **net48** for Revit 2023–2024 and **net8.0-windows** for Revit 2025+ (`Agent.Ipc.Revit` vs `Agent.Ipc`). Optional NuGet: [`nuget.md`](nuget.md).
2. Build **`RevitLicensePinning`** from constants. For **net48**, keep embedded P-256 **X/Y** in sync with the PEM.
3. **Gate every licensed entry point**:
   - `nonce` (GUID `N`), `timestampUtc`, `pluginVersion`
   - Named pipe `canRun` via `RevitLicenseCanRunReport.BuildAsync` **or** IPC + `RevitLicenseProofVerifier.TryVerifyProof`
4. **Never** trust `Allowed` alone. Deny on verification failure.
5. Do **not** call the Worker/cloud API from Revit. Do **not** store tokens or private grant-signing keys.
6. Short in-memory cache for verified-OK (e.g. 60 s via `RevitLicenseCanRunReport` defaults).
7. **Local diagnostics:** `%LocalAppData%\VP-Hub\logs\{productCode}.log` via [`VpHubPluginFileLog`](../libs/LicensingSystem.Revit.Licensing/VpHubPluginFileLog.cs). Agent Export diagnostics packs that folder. Policy: [`logging-redaction-policy.md`](logging-redaction-policy.md).

## 3. End-user machine

1. VP-Hub agent installed, running, **signed in**.
2. Entitlements synced; seat lease policy satisfied.

## 4. Verification before release

1. Happy path: agent signed in → command runs after proof verification.
2. Agent stopped / IPC failure → actionable deny message.
3. `Allowed == false` → map `Reason` (see Contracts `CanRunReasonMessages`).
4. Wrong pins → verification fails → deny.
5. Revit 2024 / net48: self-contained `Contents/<year>/` (see [`revit-bundle-packaging.md`](revit-bundle-packaging.md)).
6. Diagnostics: after a licensed command, `%LocalAppData%\VP-Hub\logs\{productCode}.log` exists; Export diagnostics ZIP contains `logs/{productCode}.log`.

## 5. References

- Shared SDK: `libs/LicensingSystem.Revit.Licensing` (`RevitLicenseCanRunReport`, `RevitLicenseProofVerifier`, `VpHubPluginFileLog`)
- Bundle layout: [`revit-bundle-packaging.md`](revit-bundle-packaging.md)
- PR checklist: [`revit-pr-checklist.md`](revit-pr-checklist.md)
- Brand / UX: [`plugin-brand-book.md`](plugin-brand-book.md)
- Logging policy: [`logging-redaction-policy.md`](logging-redaction-policy.md)

# Revit add-in publisher onboarding

Use this checklist when integrating a **new** or **updated** Revit add-in with LicensingSystem. Policy details: [`AGENTS.md`](../../AGENTS.md), [`../architecture/revit-licensing.md`](../architecture/revit-licensing.md).

**Terminology (do not conflate):** In this system, **organization** means the **customer** tenant — the org whose people **use** the licensed add-in (portal, entitlements, `customerOrgId` in grants). **Publisher** means the **vendor** who **authors** the add-in and distributes it through LicensingSystem (`PublisherId`, publisher keys, pinning in the add-in). A publisher identity is not the same role as a customer organization, even though both may appear as rows under Admin → Organizations for operational reasons.

## 1. Cloud (product identity)

**Plugin publishers** (vendor accounts with portal access) use **Organization portal → Plugin publisher** (`/publisher/*` on the customer portal): products, pinning & keys, license grants, update packages, and publisher kit download. Platform operators still use **Admin** for org-wide entitlements and assigning publisher memberships (**Admin → Publishers**, **Users → Publisher access**).

1. Register a **product** whose **Code** equals the string you will use as `ProductCode` in the add-in (for example `revit.myproduct`). Publishers create their own products in the portal; superadmins can also create products in Admin.
2. Ensure **publisher keys** exist (`PublisherId`, `Kid`, algorithm **`es256`**, public PEM). The signing key that issues **license grants** must match what you pin in the add-in.
3. Use **Publisher → Pinning & keys** in the portal (or Admin **Publisher & add-in pinning** for platform ops) to register the public key, align **ProductCode**, **ExpectedPublisherId**, **ExpectedKid**, **ExpectedPublisherPublicKeyPem**, and optional **ExpectedAudienceServerId**, and copy the generated C# snippet into your constants (same values as a `RevitLicensePinning` instance).
4. Confirm **entitlements** (customer org admin / superadmin) and **license grants** (publisher portal or admin) for customer orgs reference the same `productCode` and publisher material.

## 2. Add-in project (code)

1. Add a **project reference** to [`revit/LicensingSystem.Revit.Licensing`](../../revit/LicensingSystem.Revit.Licensing/LicensingSystem.Revit.Licensing.csproj) (or consume the same sources if you mirror the repo). Target **net48** for Revit 2023–2024 and **net8.0-windows** for Revit 2025+ to match [`LicensingSystem.Agent.Ipc.Revit`](../../agent/src/LicensingSystem.Agent.Ipc.Revit) vs [`LicensingSystem.Agent.Ipc`](../../agent/src/LicensingSystem.Agent.Ipc). Optional NuGet / feed notes: [`nuget.md`](nuget.md).
2. Build a **`RevitLicensePinning`** from your constants (product + publisher PEM + optional audience + **P-256 public `ECParameters`**). For **Revit 2024 / .NET Framework**, keep embedded **X/Y** coordinates for the pinned public key in sync with the PEM.
3. **Gate every licensed entry point** (each `IExternalCommand.Execute` and any other entry that runs licensed work):
   - Generate `nonce` (GUID `N`), `timestampUtc`, and pass **assembly version** as `pluginVersion`.
   - Call the agent over the named pipe (`type = canRun`, …). Use `RevitLicenseCanRunReport.BuildAsync` **or** your own IPC wrapper followed by **`RevitLicenseProofVerifier.TryVerifyProof`**.
4. **Never** trust `Allowed` alone. If verification fails, **deny** execution and show a support-safe message.
5. Do **not** call the Worker/cloud API from Revit. Do **not** store access tokens, refresh tokens, or private grant-signing keys in the add-in.
6. Use a **short in-memory cache** (for example 60 seconds) for verified-OK results if you use `RevitLicenseCanRunReport` defaults, or implement the same TTL yourself.
7. **Local diagnostics file log:** write under `%LocalAppData%\VP-Hub\logs\{productCode}.log` via [`VpHubPluginFileLog`](../../revit/LicensingSystem.Revit.Licensing/VpHubPluginFileLog.cs). Agent **Export diagnostics** / **Report a problem** pack that folder. Do not invent another log directory. Cloud `logPluginEvent` is optional telemetry, not a substitute for the file. Policy: [`../architecture/logging-redaction-policy.md`](../architecture/logging-redaction-policy.md).

## 3. End-user machine

1. **Windows agent** installed, running, and **signed in** for the user.
2. Entitlements synced; seat lease policy satisfied for the product (agent enforces this before returning `Allowed`).

## 4. Verification before release

1. **Happy path:** agent signed in → command runs → licensed path executes after proof verification.
2. **Agent stopped / IPC failure:** user sees an actionable message (start agent, sign in).
3. **`Allowed == false`:** map `Reason` using [`CanRunReasonMessages`](../../backend/src/LicensingSystem.Contracts/Agent/CanRunReasonMessages.cs) or equivalent UX.
4. **Wrong pins / wrong product:** verification fails; add-in must deny (no fallback allow).
5. **Revit 2024 / net48 packaging:** bundle `Contents/<year>/` includes all runtime DLLs from build output (see [`revit-bundle-packaging.md`](../architecture/revit-bundle-packaging.md) and publisher kit templates). Test on a PC **without** Visual Studio — missing `System.Text.Json.dll` must not occur.
6. **Diagnostics:** after a licensed command, `%LocalAppData%\VP-Hub\logs\{productCode}.log` exists; agent Export diagnostics ZIP contains `logs/{productCode}.log` (agent build that includes `DiagnosticsLogPackager`).
7. **Report a problem:** add a ribbon button whose command calls `VpHubPluginProblemReport.SendInBackground(productCode, pluginVersion, "Revit " + version)` and returns. Do not gate it: somebody whose license check fails is who it is for. Clicking it sends the logs through the agent and shows the result with a report number; with the agent stopped it says so. The tooltip must say what is sent. See `LicenseProbeReportProblemCommand` for the whole command. If another add-in in the same Revit may ship an older copy of the shared assemblies, whichever loads first wins and the new type is missing: keep the same logic inside your own add-in, calling only `CallAsync(object, CancellationToken)`, as SEL's `SelProblemReport` does.

## 5. References

- Shared SDK: [`revit/LicensingSystem.Revit.Licensing`](../../revit/LicensingSystem.Revit.Licensing) (`RevitLicenseCanRunReport`, `RevitLicenseProofVerifier`, `VpHubPluginFileLog`).
- Bundle layout: [`../architecture/revit-bundle-packaging.md`](../architecture/revit-bundle-packaging.md); overview: [`revit/README.md`](../../revit/README.md).
- Org/product onboarding (IT): [`../architecture/plugin-lifecycle-onboarding.md`](../architecture/plugin-lifecycle-onboarding.md).
- PR checklist: [revit-pr-checklist.md](revit-pr-checklist.md).
- **Operator guide:** quality bar for publisher handoff kits and Cursor Skills (what to include, secrets, phases A–E, support contact): [`revit-publisher-handoff-and-skill-quality.md`](revit-publisher-handoff-and-skill-quality.md) (Ukrainian).
- **Publisher kit (canonical):** public GitHub [revit-vp-hub-packaging-handoff](https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff) (clone / Releases / `adopt-into-project.ps1`). Portal fallback: Admin → Publisher kit. Details: [`revit-publisher-handoff.md`](revit-publisher-handoff.md).

## Reporting a failure so somebody can act on it

When a command fails, send a canonical `errorCode` from
[`../architecture/reason-codes.md`](../architecture/reason-codes.md) — `NOT_ENTITLED`,
`LEASE_EXHAUSTED`, `AGENT_UNAVAILABLE`, `SIGNATURE_INVALID` and the rest.

A generic code such as `RESULT_FAILED` or `EXCEPTION` is accepted and stored, but it is
reported as **unclassified** and counted separately, because it tells an operator nothing
the failure event did not already say. Every plugin failure in production in October 2026
arrived this way, which left the failure path unreadable at exactly the moment it mattered.

If no canonical code fits, send the closest one and put the detail in `errorMessage`.

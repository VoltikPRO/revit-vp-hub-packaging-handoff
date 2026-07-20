# Revit add-in licensing — extended reference (publisher kit)

Companion to `SKILL.md`. Docs: [`docs/revit-licensing.md`](../../../docs/revit-licensing.md), [`docs/revit-add-in-onboarding.md`](../../../docs/revit-add-in-onboarding.md).

## Terminology

- **Organization (customer)** — consumes the licensed add-in (entitlements, portal).
- **Publisher (vendor)** — authors the add-in; `PublisherId`, keys, pinning in DLL.

## Publisher onboarding (condensed)

1. Register **product** `Code` = add-in `ProductCode`.
2. Publisher keys: `PublisherId`, `Kid`, es256, public PEM.
3. Portal **Pinning & keys** → paste C# into constants / `RevitLicensePinning`.
4. Entitlements and grants use same `productCode`.

## Add-in code

1. `ProjectReference` → `libs/LicensingSystem.Revit.Licensing/LicensingSystem.Revit.Licensing.csproj`
2. net48: keep P-256 X/Y in sync with PEM when embedding `ECParameters`
3. Gate every entry point; cache verified OK (~60s)
4. File log: `VpHubPluginFileLog.Write(productCode, …)` → `%LocalAppData%\VP-Hub\logs\{productCode}.log`

### `pluginVersion` for canRun / logPluginEvent

Pass a version string that identifies the **product release**, not only the assembly identity.

With **Nerdbank.GitVersioning**, `assemblyVersion.precision: minor` keeps `AssemblyVersion` at `major.minor.0.0` (avoids .NET Framework binding-redirect churn). In that setup:

- Prefer **`FileVersion`** (or InformationalVersion without git metadata) for IPC `pluginVersion`
- Do **not** use only `Assembly.GetName().Version` — it will not distinguish patches such as `2026.6.0` vs `2026.5.18`

Example: read `FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion` with a fallback to the assembly version.

## Smoke tests

From `docs/revit-add-in-onboarding.md` §4:

1. Happy path — agent signed in → verify → allow
2. Agent off — actionable deny message
3. `Allowed == false` — map `Reason` (`libs/LicensingSystem.Contracts/Agent/CanRunReasonMessages.cs`)
4. Wrong pins — verification fails, deny
5. File log under `%LocalAppData%\VP-Hub\logs\{productCode}.log`; Export diagnostics includes `logs/*.log`

## net48 bundle overlay

See [`docs/lp-net48-overlay.md`](../../../docs/lp-net48-overlay.md).

## Additional docs

- PR checklist: [`docs/revit-pr-checklist.md`](../../../docs/revit-pr-checklist.md)
- NuGet: [`docs/nuget.md`](../../../docs/nuget.md)
- Bundle: [`docs/revit-bundle-packaging.md`](../../../docs/revit-bundle-packaging.md)
- Logging: [`docs/logging-redaction-policy.md`](../../../docs/logging-redaction-policy.md)

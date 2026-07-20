---
name: vp-hub-revit-integration
description: >-
  End-to-end VP-Hub Revit add-in integration: audit project, wire LicensingSystem SDK
  (named pipe canRun, ES256 proof verification, pinning), gate IExternalCommand entry
  points, package multi-year ApplicationPlugins bundle (package.zip), and release smoke
  tests. Use when integrating a Revit plugin with VP-Hub, LicensingSystem, publisher
  portal pinning, or adopting this publisher kit into an existing add-in repo.
---

# VP-Hub Revit integration (orchestrator)

Use this skill first when adapting a publisher's Revit add-in for VP-Hub. Delegate detail to sibling skills:

- Licensing code: [`revit-add-in-licensing`](../revit-add-in-licensing/SKILL.md)
- Bundle / ZIP: [`revit-vp-hub-packaging`](../revit-vp-hub-packaging/SKILL.md)

Policy: [`docs/AGENTS.md`](../../../docs/AGENTS.md). Human guides: [`ONBOARDING.md`](../../../ONBOARDING.md), [`RELEASE.md`](../../../RELEASE.md), [`SMOKE-TESTS.md`](../../../SMOKE-TESTS.md).

## Phases

### Phase A — Audit

1. List supported Revit years and map to TFM (≤2024 → net48; ≥2025 → net8.0-windows).
2. Find every licensed entry point (`IExternalCommand.Execute`, non-command paths that run licensed work).
3. Confirm `productCode` with publisher portal (must match entitlements and pinning).
4. Check whether kit `libs/` or NuGet is used ([`docs/nuget.md`](../../../docs/nuget.md)).

### Phase B — SDK and pinning

1. Add `ProjectReference` to `libs/LicensingSystem.Revit.Licensing` (or NuGet equivalent).
2. Reference correct IPC project per TFM matrix (see licensing skill).
3. Create constants from portal **Pinning & keys** (C# snippet): `ProductCode`, publisher PEM, Kid, PublisherId.
4. On **net48**, embed P-256 **X/Y** consistent with PEM when using `ECParameters`.
5. Implement central `EnsureLicensed(...)` using `RevitLicenseCanRunReport.BuildAsync` (templates in `templates/`).
6. Local diagnostics: `VpHubPluginFileLog.Write(productCode, …)` → `%LocalAppData%\VP-Hub\logs\{productCode}.log`.

### Phase C — Gate commands

1. Every entry point: nonce (GUID `"N"`), `timestampUtc`, `pluginVersion` → `canRun` → verify proof → deny on any failure.
2. UX per [`docs/plugin-brand-book.md`](../../../docs/plugin-brand-book.md).
3. net48: `PluginAssemblyResolver.Register()` first line in `OnStartup`.

### Phase D — Packaging

1. Run [`revit-vp-hub-packaging`](../revit-vp-hub-packaging/SKILL.md): `packaging/plugin.manifest.json`, five scripts, production `package.zip`.
2. Licensed net48: populate `requiredDlls` including `LicensingSystem.*` (see packaging skill § licensed overlay).
3. `Test-RevitApplicationPackage.ps1` must pass before release.

### Phase E — Release and smoke

1. Bump `version.json`; production build (all years, no `-AllowPartialYears`).
2. Record SHA-256; add manifest entry per [`RELEASE.md`](../../../RELEASE.md).
3. Upload via publisher portal; Install/Update through VP-Hub agent.
4. Run [`SMOKE-TESTS.md`](../../../SMOKE-TESTS.md) (include `canRun` in agent log and `{productCode}.log` under `VP-Hub\logs`).

## Definition of done

See [`INTEGRATION-CHECKLIST.md`](../../../INTEGRATION-CHECKLIST.md).

## Suggested prompt

> Integrate this Revit add-in with VP-Hub using vp-hub-revit-integration. Follow phases A–E. Gate every command. Use VpHubPluginFileLog for local diagnostics. Do not call the cloud API from Revit.

Extended checklist: [`reference.md`](reference.md).

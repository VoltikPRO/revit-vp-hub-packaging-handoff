# Integration checklist (Definition of Done)

Copy into PR descriptions when integrating VP-Hub licensing and packaging.

## Portal / identity

- [ ] `ProductCode` matches publisher portal product **Code**
- [ ] Pinning constants match portal **Pinning & keys** (PublisherId, Kid, PEM)
- [ ] net48: P-256 X/Y updated if PEM changed

## Security

- [ ] Add-in does **not** call cloud Worker/API from Revit
- [ ] No secrets in add-in (no tokens, no private signing PEM)
- [ ] Every licensed command uses the same gate (`EnsureLicensed` / `RevitLicenseCanRunReport`)
- [ ] Gate does **not** trust `Allowed` without `RevitLicenseProofVerifier`
- [ ] Nonce + timestamp echo checked

## Diagnostics

- [ ] File logs use `VpHubPluginFileLog` → `%LocalAppData%\VP-Hub\logs\{productCode}.log`
- [ ] No secrets in file logs ([`docs/logging-redaction-policy.md`](docs/logging-redaction-policy.md))

## Packaging

- [ ] Multi-year `.bundle` with `Contents/<year>/`
- [ ] net48 folders self-contained (all runtime DLLs)
- [ ] Licensed net48 includes `System.Text.Json.dll`, IPC.Revit, Contracts, Revit.Licensing
- [ ] `Test-RevitApplicationPackage.ps1` passes on release ZIP
- [ ] `PluginAssemblyResolver` on net48 if NuGet transitive deps

## Release

- [ ] `version.json` bumped
- [ ] SHA-256 recorded in `artifacts/manifest.json`
- [ ] Smoke tests in [`SMOKE-TESTS.md`](SMOKE-TESTS.md) passed

## UX

- [ ] User messages follow [`docs/plugin-brand-book.md`](docs/plugin-brand-book.md)

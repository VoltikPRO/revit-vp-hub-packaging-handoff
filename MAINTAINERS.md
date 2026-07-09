# Maintainer guide

This kit is maintained in [revit-vp-hub-packaging-handoff](https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff). **Do not modify** the LicensingSystem monorepo when refreshing publisher content.

## Sync SDK and docs from LicensingSystem

From handoff repo root (LicensingSystem as sibling `../LicensingSystem`):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/sync-from-licensingsystem.ps1
```

Optional:

```powershell
powershell -File scripts/sync-from-licensingsystem.ps1 -LicensingSystemRoot "D:\path\LicensingSystem"
```

### What sync copies (read-only source)

- `libs/` — Revit.Licensing, Contracts, Agent.Ipc, Agent.Ipc.Revit
- `reference/LicenseProbe/`
- `docs/` — publisher-facing copies

### Hand-maintained in handoff (not overwritten by sync)

- `.cursor/skills/*`, `.cursor/rules/*`
- `templates/` including slim `NamedPipeAgentClient` for net8 (`libs/.../NamedPipeAgentClient.cs` is restored after sync — re-run patch or keep template copy step in sync)
- Root guides: `ONBOARDING.md`, `RELEASE.md`, etc.
- `scripts/adopt-into-project.ps1`, `build-publisher-kit-zip.ps1`

After sync, verify:

```powershell
dotnet build libs/LicensingSystem.Revit.Licensing/LicensingSystem.Revit.Licensing.csproj -c Release
```

## Release kit ZIP

```powershell
powershell -File scripts/build-publisher-kit-zip.ps1 -Version 1.0.0
```

Upload output to platform **Admin → Publisher kit** or publish GitHub Release.

## Versioning

Tag handoff repo (`v1.0.0`). Record SDK sync in `CHANGELOG.md`.

# VP-Hub Revit Publisher Kit

Public handoff package for **Revit plugin publishers** integrating with [VP-Hub / LicensingSystem](https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff). Includes Cursor Agent skills, vendored SDK (`libs/`), packaging templates, and documentation — **no access to the LicensingSystem monorepo required**.

Canonical repo: [github.com/VoltikPRO/revit-vp-hub-packaging-handoff](https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff)

## What you get

| Area | Contents |
|------|----------|
| **AI skills** | `.cursor/skills/vp-hub-revit-integration` (start here), `revit-add-in-licensing`, `revit-vp-hub-packaging` |
| **Policy** | `.cursor/rules/vp-hub-revit-licensing.mdc` |
| **SDK** | `libs/` — Revit.Licensing (incl. `VpHubPluginFileLog`), Contracts, Agent.Ipc* (or NuGet — [`docs/nuget.md`](docs/nuget.md)) |
| **Templates** | C# pinning/gate, `.addin`, `packaging/*.ps1` |
| **Docs** | [`docs/AGENTS.md`](docs/AGENTS.md), onboarding, bundle packaging, brand book, logging policy |

## Quick start

### 1. Clone

```powershell
git clone https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff.git
```

### 2. Adopt into your plugin repo (recommended)

```powershell
cd C:\dev\MyRevitPlugin
..\revit-vp-hub-packaging-handoff\scripts\adopt-into-project.ps1 -TargetRepo .
```

### 3. Cursor prompt

> Integrate this Revit add-in with VP-Hub using **vp-hub-revit-integration** skill. Follow phases A–E. Gate every command. Do not call the cloud API from Revit.

### 4. Human checklists

- Portal setup: [`ONBOARDING.md`](ONBOARDING.md)
- Release: [`RELEASE.md`](RELEASE.md)
- Smoke: [`SMOKE-TESTS.md`](SMOKE-TESTS.md)
- Done criteria: [`INTEGRATION-CHECKLIST.md`](INTEGRATION-CHECKLIST.md)

## Adoption levels

| Level | Command | Copies |
|-------|---------|--------|
| **Full** | `adopt-into-project.ps1 -TargetRepo .` | skills, rules, `libs/`, `packaging/`, templates |
| **Skills only** | `... -Level SkillsOnly` | `.cursor/skills/*`, `.cursor/rules/*` |

Details: [`ADOPTION.md`](ADOPTION.md)

## Packaging only

If you only need `package.zip` / `.bundle` layout without licensing code, use skill **revit-vp-hub-packaging** (explicit invoke — `disable-model-invocation: true`).

## Releases

Download **VP-Hub-RevitPublisherKit-&lt;version&gt;.zip** from [GitHub Releases](https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff/releases) or build locally:

```powershell
powershell -File scripts/build-publisher-kit-zip.ps1 -Version 1.0.0
```

Platform operators may upload the ZIP to Admin → Publisher kit.

## Maintainers

See [`MAINTAINERS.md`](MAINTAINERS.md). SDK refresh is **read-only** from sibling `LicensingSystem` — that monorepo is not modified by this kit.

## Support

Operator fills [`SUPPORT.md`](SUPPORT.md) (agent min version, portal URL, NuGet feed).

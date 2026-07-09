# Adopting the VP-Hub Revit Publisher Kit

Guide for plugin teams and Cursor users.

**Repository:** [github.com/VoltikPRO/revit-vp-hub-packaging-handoff](https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff)

## What this package contains

```text
revit-vp-hub-packaging-handoff/
  .cursor/skills/          # vp-hub-revit-integration, licensing, packaging
  .cursor/rules/           # vp-hub-revit-licensing.mdc
  libs/                    # vendored LicensingSystem SDK
  reference/LicenseProbe/  # read-only sample
  templates/               # C#, packaging PS1, manifest JSON
  docs/                    # AGENTS.md, architecture copies
  scripts/                 # adopt, build ZIP, maintainer sync
```

## Option A — adopt script (recommended)

From your plugin repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File path\to\revit-vp-hub-packaging-handoff\scripts\adopt-into-project.ps1 -TargetRepo .
```

- **Full** (default): skills + rules + `libs/` + `packaging/` + `docs/templates/`
- **SkillsOnly**: `-Level SkillsOnly`

Then edit `packaging/plugin.manifest.json` and portal pinning constants.

## Option B — personal Cursor skills

Copy skill folders to:

```text
~/.cursor/skills/vp-hub-revit-integration/
~/.cursor/skills/revit-add-in-licensing/
~/.cursor/skills/revit-vp-hub-packaging/
```

Copy rule: `~/.cursor/rules/vp-hub-revit-licensing.mdc`

Invoke: *"Use vp-hub-revit-integration skill to integrate VP-Hub licensing."*

## Option C — project-local skills

Copy `.cursor/skills/*` and `.cursor/rules/*` into **your plugin repo** `.cursor/` and commit for the team.

## Option D — release ZIP

Download or build `VP-Hub-RevitPublisherKit-<version>.zip`, unzip, then run `adopt-into-project.ps1` pointing at your repo.

## First-time publisher workflow

1. Complete [`ONBOARDING.md`](ONBOARDING.md) (portal product + pinning)
2. Run adopt script
3. Cursor + **vp-hub-revit-integration** (phases A–E)
4. Production `package.zip` per [`RELEASE.md`](RELEASE.md)
5. [`SMOKE-TESTS.md`](SMOKE-TESTS.md)

## SDK: libs vs NuGet

- **Default:** `libs/` project references (copied by Full adopt)
- **Alternative:** [`docs/nuget.md`](docs/nuget.md) when your operator publishes packages to a feed

## Updating the kit

```powershell
git -C revit-vp-hub-packaging-handoff pull
# Re-run adopt (skills overwrite; libs/ skipped if already present — merge manually)
```

Maintainers: [`MAINTAINERS.md`](MAINTAINERS.md)

## Legacy root SKILL.md

The file [`SKILL.md`](SKILL.md) at repo root redirects to `.cursor/skills/revit-vp-hub-packaging/` for backward compatibility with older clone-to-`~/.cursor/skills/` instructions.

# Adopting the Revit VP-Hub Packaging Skill

Short guide for humans moving this handoff package into another project or Cursor environment.

**Canonical repository:** [github.com/VoltikPRO/revit-vp-hub-packaging-handoff](https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff)

## What this package contains

```text
revit-vp-hub-packaging-handoff/
  README.md       # Repo overview
  SKILL.md        # Agent instructions (main)
  reference.md    # Detailed packaging rules
  ADOPTION.md     # This file
```

No PowerShell scripts are included. The agent adapts packaging scripts into the target project using `SKILL.md` and optional reference implementation (`LP/packaging/`).

## Get the repo

### Option A — git clone (recommended)

```powershell
git clone https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff.git
```

Place it as a sibling of your plugin repo when possible:

```text
Plugin/
├── MyRevitPlugin/
└── revit-vp-hub-packaging-handoff/
```

### Option B — copy into Cursor skills

| Location | Path | Scope |
|----------|------|-------|
| Personal (recommended) | `~/.cursor/skills/revit-vp-hub-packaging-handoff/` | All your projects |
| Project | `<target-repo>/.cursor/skills/revit-vp-hub-packaging-handoff/` | Shared with repo |

Clone or copy the repo folder to the chosen path. Ensure `SKILL.md` with YAML frontmatter is present.

Invoke in Cursor: *"Use revit-vp-hub-packaging-handoff skill to set up VP-Hub packaging"*

The skill uses `disable-model-invocation: true` — name it explicitly in the prompt when packaging.

## First adoption in a new plugin project

1. **Create manifest** — agent creates `packaging/plugin.manifest.json` with your product names
2. **Dev build (one year)** — start with a single year (e.g. 2024) using `-AllowPartialYears`
3. **Populate requiredDlls** — after first build, record DLL list from `deploy/<year>/`
4. **Expand to all years** — add `revit-api/` folders or Revit installs for remaining years
5. **Production build** — full build without `-AllowPartialYears`
6. **Smoke test** — Revit loads add-in from ApplicationPlugins bundle

## Reference implementation

If the **LP** repo is available as a sibling (`Plugin/LP/`), the agent may read `LP/packaging/` for concrete script patterns. LP is not required — `reference.md` is sufficient.

Generic projects must **not** depend on `LicensingSystem` repo.

## Handoff between teams

Share the repository URL:

```text
https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff
```

Optionally point them to `LP/packaging/` as a worked example. Provide `revit-api/` DLLs separately (not redistributable via git in most setups).

## Updating the skill

1. Edit `SKILL.md` or `reference.md` in this repo
2. Commit and push to `main`
3. Pull or re-copy to `~/.cursor/skills/` on other machines

Keep `SKILL.md` under 500 lines; use progressive disclosure via `reference.md`.

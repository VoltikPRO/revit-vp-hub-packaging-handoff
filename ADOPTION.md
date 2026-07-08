# Adopting the Revit VP-Hub Packaging Skill

Short guide for humans moving this handoff package into another project or Cursor environment.

## What this package contains

```text
revit-vp-hub-packaging/
  SKILL.md        # Agent instructions (main)
  reference.md    # Detailed packaging rules
  ADOPTION.md     # This file
```

No PowerShell scripts are included. The agent adapts packaging scripts into the target project using `SKILL.md` and optional reference implementation (`LP/packaging/`).

## Install the skill in Cursor

Choose one location:

| Location | Path | Scope |
|----------|------|-------|
| Personal (recommended) | `~/.cursor/skills/revit-vp-hub-packaging/` | All your projects |
| Project | `<target-repo>/.cursor/skills/revit-vp-hub-packaging/` | Shared with repo |

### Steps

1. Copy the entire `revit-vp-hub-packaging/` folder to the chosen path
2. Ensure the folder contains `SKILL.md` with YAML frontmatter
3. In Cursor, invoke explicitly: *"Use revit-vp-hub-packaging skill to set up VP-Hub packaging"*

The skill uses `disable-model-invocation: true` by default in Cursor skills — name it in the prompt when packaging.

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

To give another team everything they need:

1. Copy `revit-vp-hub-packaging/` folder (this package)
2. Optionally point them to `LP/packaging/` as a worked example (read-only)
3. Provide `revit-api/` DLLs separately (not redistributable via git in most setups)

## Updating the skill

When packaging lessons are learned (e.g. new Revit year, new validation rule):

1. Update `SKILL.md` (workflow / checklist)
2. Put detailed rules in `reference.md`
3. Re-copy to `~/.cursor/skills/` or project `.cursor/skills/`

Keep `SKILL.md` under 500 lines; use progressive disclosure via `reference.md`.

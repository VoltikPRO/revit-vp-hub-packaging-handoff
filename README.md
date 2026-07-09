# revit-vp-hub-packaging-handoff

Portable handoff for building valid Revit VP-Hub ApplicationPlugins bundles (`package.zip`) **without** LicensingSystem access.

Derived from the [LP](https://github.com/VoltikPRO/lp-revit-plugin) packaging workflow.

## Contents

| File | Purpose |
|------|---------|
| [SKILL.md](SKILL.md) | Cursor Agent Skill — main packaging workflow |
| [reference.md](reference.md) | Detailed rules: scripts, `PackageContents.xml`, DLL validation, smoke |
| [ADOPTION.md](ADOPTION.md) | How to install the skill in another project |

## Clone (canonical)

```powershell
git clone https://github.com/VoltikPRO/revit-vp-hub-packaging-handoff.git
```

Recommended layout next to a plugin repo:

```text
Plugin/
├── LP/                              # reference implementation (optional)
└── revit-vp-hub-packaging-handoff/  # this repo
```

## Cursor

Install as a personal skill:

```text
~/.cursor/skills/revit-vp-hub-packaging-handoff/
```

Then invoke: *"Use revit-vp-hub-packaging-handoff skill to set up VP-Hub packaging"*

See [ADOPTION.md](ADOPTION.md) for details.

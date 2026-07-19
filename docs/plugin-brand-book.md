# Plugin Brand Book (Revit add-ins)

This brand book applies to **all plugins/add-ins** distributed via **VP-Hub** (agent installer + portal).
It is written to be actionable for humans and Cursor/AI agents generating new add-ins.

## Brand identity (baseline)

- **Platform (user-facing):** VP-Hub — portal, agent, catalog
- **Publisher (product cards / plugin identity):** VoltikPRO for Voltik products; other publishers keep their own names
- **Code / repo identity:** LicensingSystem (namespaces, assemblies, named pipe, admin console) — do not rename for branding
- **Voice:** professional, concise, engineering-first
- **Promises we do NOT make:** “unbreakable”, “100% secure”, “always online”
- **Primary goal:** clarity and predictability for B2B engineering workflows

## Naming & wording

### Plugin naming

- **ProductCode**: stable, lowercase, dot-delimited, no spaces (example: `revit.myplugin`)
- **Display name**: short and descriptive (example: “My Plugin”)
- **Publisher**: one publisher identity per org/vendor; do not invent new publisher names per plugin

### Text tone

- Prefer **short sentences**.
- Prefer **actionable guidance** over generic errors.
- Avoid slang, jokes, or casual language.

## UI rules (Revit constraints)

Revit UI is not a web app; keep UI minimal and consistent.

### Ribbon

- Prefer one top-level tab (if you own multiple tools) or integrate under an existing company tab.
- Use a consistent panel structure across plugins:
  - Panel “Licensing” (or “Help”) contains: “License status”, “Diagnostics” (optional)

### Dialogs (TaskDialog)

Use TaskDialog for:

- license denied
- agent unreachable
- verification failed

Rules:

- Title: `Licensing` or the plugin name + ` — Licensing`
- Content: 1–3 lines describing the issue
- Expanded content (optional): a short “How to fix” checklist
- Do not dump stack traces in user dialogs (log them separately if needed)

### Colors & icons

- Avoid custom color palettes; rely on Revit’s native UI.
- Icons:
  - Keep a consistent style (flat, simple, monochrome where possible).
  - Prefer SVG (if supported in your packaging) or high-DPI PNG.
  - Do not use multiple accent colors; if an accent is needed, use blue.

## Licensing UX requirements (brand + security)

When license gating blocks an action, the user must receive:

- a **clear reason** (“No license”, “Seat limit reached”, “Offline grace expired”)
- a **next step** (“Open the agent and sign in”, “Ask your org admin for seats”, “Connect to internet and sync”)

### Standard messages (recommended)

#### Agent unreachable

- Summary: “Licensing agent is not running or not signed in.”
- Next steps:
  - “Start VP-Hub Agent”
  - “Sign in and sync entitlements”

#### Missing plugin dependency (Revit 2024 / net48)

Use when load fails **before** IPC (for example `FileNotFoundException` for `System.Text.Json`).

- Summary: “Plugin dependency missing. Reinstall or update this product from VP-Hub.”
- Next steps:
  - “In VP-Hub → Products, run Install / update”
  - “Restart Revit”
  - “In VP-Hub → About, use Report a problem (or Export diagnostics) if the problem continues”


Do **not** use the agent-unreachable summary for missing-assembly errors.

#### License denied (policy)

- Summary: “This product is not licensed for this machine.”
- Include reason code in a subtle way (optional): `Reason: <CODE>`

#### Verification failed (tamper / mismatch)

- Summary: “License verification failed.”
- Next steps: “Contact support and include diagnostics.”

## Versioning & support signals

- Always include plugin version in “License status” output.
- Prefer **VP-Hub → Products → Update** on the VP-Hub row when a newer agent MSI is published (same notifications / yellow Update button as plugins). For diagnostics-only, use **About → Report a problem** (sends email via Resend automatically) or **Export diagnostics**.
- In **Developer mode**, Products shows a version picker so users can install any published version (including older). Superadmin sets package **Availability** to *All users* or *Advanced only* in Release index.
- Provide a “Diagnostics export” path where appropriate (the agent already supports diagnostics export; plugins should point users to Report a problem / Products Update / Export diagnostics instead of reinventing).

## What AI agents must do

When generating a new Revit add-in in this repo:

- Follow licensing doc: `docs/architecture/revit-licensing.md`
- Follow project instructions: `AGENTS.md`
- Follow this brand book for:
  - ribbon naming
  - dialog tone
  - consistent user-facing messaging

# Smoke tests (VP-Hub licensed Revit add-in)

Run before marking a release done. See also `docs/revit-add-in-onboarding.md` §4.

## Environment

| Case | Environment |
|------|-------------|
| net48 packaging | Clean Windows + Revit 2024, **no** Visual Studio / SDK |
| General | VP-Hub agent installed for test user |

## Checklist

### 1. Happy path

- [ ] VP-Hub agent running and signed in
- [ ] Install / update product in VP-Hub → Products
- [ ] Restart Revit (target year)
- [ ] Add-in appears (ribbon or Add-Ins)
- [ ] Licensed command runs after proof verification
- [ ] `%LocalAppData%\VP-Hub\agent-namedpipe.log` contains `canRun` after command

### 2. Agent unavailable

- [ ] Stop agent → command shows actionable message (start/sign in agent)
- [ ] Add-in denies execution (no fallback allow)

### 3. Not entitled

- [ ] User without entitlement → `Allowed == false` with clear reason
- [ ] Map reason per `docs/plugin-brand-book.md`

### 4. Wrong pins

- [ ] Tampered pinning constants → verification fails → deny

### 5. net48 dependencies (Revit 2023–2024)

- [ ] No `FileNotFoundException` for `System.Text.Json` or `LicensingSystem.*`
- [ ] No `MissingMethodException` on `JsonSerializer.Deserialize(ReadOnlySpan<byte>, …)` (IPC uses string Deserialize)
- [ ] Bundle `Contents/<year>/` is self-contained (no manual DLL copies)
- [ ] With another STJ-using add-in loaded (if available): license check reaches agent, or shows dependency-conflict UX — **not** “agent not running”

### 6. Packaging layout

- [ ] ZIP root = exactly one `*.bundle`
- [ ] `PackageContents.xml` uses `R<yyyy>` series (not `R24.0`)

## Legacy add-in conflict

Remove dev overrides:

`C:\ProgramData\Autodesk\Revit\Addins\<year>\*.addin` pointing to non-bundle paths.

# Release workflow (package.zip → VP-Hub)

## 1. Version

Bump `version.json` at the plugin repo root:

```json
{ "version": "1.2.3" }
```

## 2. Production build

From plugin repo root (after adopting packaging scripts):

```powershell
powershell -File packaging/Build-ProductApplicationPackage.ps1
```

Requirements:

- All `supportedYears` in `packaging/plugin.manifest.json`
- No `-AllowPartialYears`
- `Test-RevitApplicationPackage.ps1` passes

Output: `artifacts/builds/<productCode>/<version>/package.zip` and SHA-256 on console.

## 3. Manifest entry

Add to `artifacts/manifest.json` (shape in [`docs/manifest.example.json`](docs/manifest.example.json)):

```json
{
  "productCode": "revit.myproduct",
  "version": "1.2.3",
  "channel": "stable",
  "sha256": "<lowercase hex from build>",
  "relativePath": "builds/revit.myproduct/1.2.3/package.zip",
  "fileName": "package.zip"
}
```

- `channel` must match customer entitlement channel (default `stable`).
- Optional: `signatureAlgorithm` + `signatureBase64` when artifact signing is enabled (operator provides key policy).

## 4. Upload

1. Upload `package.zip` and updated manifest via **publisher portal → update packages** (or operator admin artifacts flow).
2. Users run **Install / update** in VP-Hub agent (interactive Windows user, not Windows Service).
3. Agent extracts to `%LocalAppData%\VP-Hub\plugins\...` and deploys `*.bundle` to `%AppData%\Autodesk\ApplicationPlugins\`.
4. Restart Revit if the bundle was replaced while Revit was running.

## 5. Verify

Run [`SMOKE-TESTS.md`](SMOKE-TESTS.md) before marking the release done.

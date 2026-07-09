# NuGet / feed (optional)

The **default** in this publisher kit is **ProjectReference** to vendored `libs/` (copied by `adopt-into-project.ps1`).

## Packing from libs/ (operator)

From a tree that includes `libs/LicensingSystem.Revit.Licensing`:

```bash
dotnet pack libs/LicensingSystem.Revit.Licensing/LicensingSystem.Revit.Licensing.csproj -c Release -o ./dist
```

Produces **`LicensingSystem.Revit.Licensing.<version>.nupkg`** (`net48`, `net8.0-windows`).

## Feed dependencies

The package depends on:

- **`LicensingSystem.Contracts`**
- **`LicensingSystem.Agent.Ipc`** (net8.0-windows)
- **`LicensingSystem.Agent.Ipc.Revit`** (net48)

Publish all to the **same feed** with aligned versions, or keep **project references** to avoid skew.

## Consumer feed URL

Set in [`SUPPORT.md`](../SUPPORT.md) by the platform operator (placeholder until published).

## Warnings

- Do not publish private signing keys; pinning uses **public** PEM only.
- Prefer `libs/` snapshot version that matches the operator's tested VP-Hub agent release.

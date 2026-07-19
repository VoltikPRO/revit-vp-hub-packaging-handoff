# NuGet / feed (optional)

The recommended way to consume **`LicensingSystem.Revit.Licensing`** today is a **ProjectReference** from your add-in inside the same monorepo (or a git submodule / subtree that includes `revit/LicensingSystem.Revit.Licensing`, `backend/src/LicensingSystem.Contracts`, and `agent/src/LicensingSystem.Agent.Ipc*`).

## Packing the library

From the repository root:

```bash
dotnet pack revit/LicensingSystem.Revit.Licensing/LicensingSystem.Revit.Licensing.csproj -c Release -o ./dist
```

Produces **`LicensingSystem.Revit.Licensing.<version>.nupkg`** with multi-target (`net48`, `net8.0-windows`) lib folders.

## Dependencies on an internal feed

The package declares dependencies on:

- **`LicensingSystem.Contracts`**
- **`LicensingSystem.Agent.Ipc`** (for `net8.0-windows` consumers)
- **`LicensingSystem.Agent.Ipc.Revit`** (for `net48` consumers; package id may match assembly name)

Those projects must be **packable and published** to the same NuGet feed with compatible versions, **or** consumers should keep using **project references** instead of the nupkg to avoid version skew.

## Warnings

- **`NU5104`** (stable package depends on prerelease): suppressed in the licensing csproj for local `MinVer`/git-derived dependency versions; align versions when you publish real packages to a feed.
- Do not publish private signing keys; pinning in the add-in uses **public** PEM only.

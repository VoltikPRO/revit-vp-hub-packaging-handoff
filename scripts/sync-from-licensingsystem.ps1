<#
.SYNOPSIS
  One-way sync of SDK libs, reference sample, docs, and skills from LicensingSystem monorepo.
  Does NOT modify the LicensingSystem repository.

.EXAMPLE
  pwsh -NoProfile -File scripts/sync-from-licensingsystem.ps1
.EXAMPLE
  pwsh -NoProfile -File scripts/sync-from-licensingsystem.ps1 -LicensingSystemRoot "D:\LicensingSystem"
#>
[CmdletBinding()]
param(
    [string] $LicensingSystemRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$HandoffRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $LicensingSystemRoot) {
    $LicensingSystemRoot = Join-Path (Split-Path $HandoffRoot -Parent) "LicensingSystem"
}
$LicensingSystemRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($LicensingSystemRoot)

if (-not (Test-Path -LiteralPath $LicensingSystemRoot)) {
    throw "LicensingSystem root not found: $LicensingSystemRoot"
}

function Invoke-RobocopyMirror {
    param(
        [Parameter(Mandatory)][string] $SourceRelative,
        [Parameter(Mandatory)][string] $DestRelative
    )
    $src = Join-Path $LicensingSystemRoot $SourceRelative.TrimEnd('\', '/')
    $dst = Join-Path $HandoffRoot $DestRelative.TrimStart('\', '/')
    if (-not (Test-Path -LiteralPath $src)) {
        throw "Missing source: $src"
    }
    New-Item -ItemType Directory -Path $dst -Force | Out-Null
    $exitCode = 0
    & robocopy.exe $src $dst /E /XD bin obj .vs node_modules /NFL /NDL /NJH /NJS /NC /NS | Out-Null
    $exitCode = $LASTEXITCODE
    if ($exitCode -gt 7) {
        throw "robocopy failed ($exitCode): $SourceRelative"
    }
}

function Copy-Doc {
    param(
        [string] $SourceRelative,
        [string] $DestRelative
    )
    $src = Join-Path $LicensingSystemRoot $SourceRelative
    $dst = Join-Path $HandoffRoot $DestRelative
    if (-not (Test-Path -LiteralPath $src)) { throw "Missing doc: $src" }
    $destDir = Split-Path -Parent $dst
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    Copy-Item -LiteralPath $src -Destination $dst -Force
}

Write-Host "Syncing from: $LicensingSystemRoot" -ForegroundColor Cyan
Write-Host "Handoff root:  $HandoffRoot" -ForegroundColor Cyan

# --- libs ---
Invoke-RobocopyMirror "revit/LicensingSystem.Revit.Licensing" "libs/LicensingSystem.Revit.Licensing"
Invoke-RobocopyMirror "backend/src/LicensingSystem.Contracts" "libs/LicensingSystem.Contracts"
Invoke-RobocopyMirror "agent/src/LicensingSystem.Agent.Ipc" "libs/LicensingSystem.Agent.Ipc"
Invoke-RobocopyMirror "agent/src/LicensingSystem.Agent.Ipc.Revit" "libs/LicensingSystem.Agent.Ipc.Revit"
Copy-Doc "revit/Directory.Build.props" "libs/Directory.Build.props"

# Remove server-side IPC (not needed for add-in publishers)
$serverFile = Join-Path $HandoffRoot "libs/LicensingSystem.Agent.Ipc/NamedPipeAgentServer.cs"
if (Test-Path -LiteralPath $serverFile) {
    Remove-Item -LiteralPath $serverFile -Force
}

# --- reference sample ---
Invoke-RobocopyMirror "revit/LicensingSystem.Revit.LicenseProbe" "reference/LicenseProbe"

# --- docs ---
$docMap = @{
    "AGENTS.md"                                      = "docs/AGENTS.md"
    "docs/architecture/revit-licensing.md"           = "docs/revit-licensing.md"
    "docs/architecture/revit-bundle-packaging.md"    = "docs/revit-bundle-packaging.md"
    "docs/brand/plugin-brand-book.md"                = "docs/plugin-brand-book.md"
    "docs/publishers/revit-add-in-onboarding.md"     = "docs/revit-add-in-onboarding.md"
    "docs/publishers/revit-pr-checklist.md"          = "docs/revit-pr-checklist.md"
    "docs/publishers/nuget.md"                       = "docs/nuget.md"
    "docs/publishers/lp-revit-plugin-net48-handoff.md" = "docs/lp-net48-overlay.md"
    "shared/manifest.example.json"                   = "docs/manifest.example.json"
}
foreach ($entry in $docMap.GetEnumerator()) {
    Copy-Doc $entry.Key $entry.Value
}

# --- Fix ProjectReference paths in libs ---
$licensingCsproj = Join-Path $HandoffRoot "libs/LicensingSystem.Revit.Licensing/LicensingSystem.Revit.Licensing.csproj"
$licensingXml = Get-Content -LiteralPath $licensingCsproj -Raw
$licensingXml = $licensingXml -replace '\.\.\\\.\.\\backend\\src\\LicensingSystem\.Contracts', '..\LicensingSystem.Contracts'
$licensingXml = $licensingXml -replace '\.\.\\\.\.\\agent\\src\\LicensingSystem\.Agent\.Ipc\.Revit', '..\LicensingSystem.Agent.Ipc.Revit'
$licensingXml = $licensingXml -replace '\.\.\\\.\.\\agent\\src\\LicensingSystem\.Agent\.Ipc\\', '..\LicensingSystem.Agent.Ipc\'
$licensingXml = $licensingXml -replace '<InternalsVisibleTo Include="LicensingSystem\.Agent\.Core\.Tests" />', ''
Set-Content -LiteralPath $licensingCsproj -Value $licensingXml -Encoding UTF8 -NoNewline

$ipcCsproj = Join-Path $HandoffRoot "libs/LicensingSystem.Agent.Ipc/LicensingSystem.Agent.Ipc.csproj"
$ipcXml = @'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>LicensingSystem.Agent.Ipc</RootNamespace>
    <AssemblyName>LicensingSystem.Agent.Ipc</AssemblyName>
    <Description>Named-pipe IPC client for Revit 2025+ (.NET 8). Vendored publisher kit slice.</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\LicensingSystem.Contracts\LicensingSystem.Contracts.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="System.IO.Pipes.AccessControl" Version="6.0.0-preview.5.21301.5" />
  </ItemGroup>

</Project>
'@
Set-Content -LiteralPath $ipcCsproj -Value $ipcXml -Encoding UTF8

$ipcRevitCsproj = Join-Path $HandoffRoot "libs/LicensingSystem.Agent.Ipc.Revit/LicensingSystem.Agent.Ipc.Revit.csproj"
$ipcRevitXml = Get-Content -LiteralPath $ipcRevitCsproj -Raw
$ipcRevitXml = $ipcRevitXml -replace '\.\.\\\.\.\\\.\.\\backend\\src\\LicensingSystem\.Contracts', '..\LicensingSystem.Contracts'
Set-Content -LiteralPath $ipcRevitCsproj -Value $ipcRevitXml -Encoding UTF8 -NoNewline

# Publisher-slim net8 IPC client (no Agent.Core dependency)
$slimClient = Join-Path $HandoffRoot "templates/vendored/NamedPipeAgentClient.net8.cs"
$ipcClient = Join-Path $HandoffRoot "libs/LicensingSystem.Agent.Ipc/NamedPipeAgentClient.cs"
if (-not (Test-Path -LiteralPath $slimClient)) {
    throw "Missing templates/vendored/NamedPipeAgentClient.net8.cs"
}
Copy-Item -LiteralPath $slimClient -Destination $ipcClient -Force

# --- Fix reference/LicenseProbe ProjectReferences ---
$probeCsproj = Join-Path $HandoffRoot "reference/LicenseProbe/LicensingSystem.Revit.LicenseProbe.csproj"
$probeXml = Get-Content -LiteralPath $probeCsproj -Raw
$probeXml = $probeXml -replace '\.\.\\\.\.\\backend\\src\\LicensingSystem\.Contracts', '..\..\libs\LicensingSystem.Contracts'
$probeXml = $probeXml -replace '\.\.\\LicensingSystem\.Revit\.Licensing', '..\..\libs\LicensingSystem.Revit.Licensing'
$probeXml = $probeXml -replace '\.\.\\\.\.\\agent\\src\\LicensingSystem\.Agent\.Ipc\.Revit', '..\..\libs\LicensingSystem.Agent.Ipc.Revit'
$probeXml = $probeXml -replace '\.\.\\\.\.\\agent\\src\\LicensingSystem\.Agent\.Ipc\\', '..\..\libs\LicensingSystem.Agent.Ipc\'
Set-Content -LiteralPath $probeCsproj -Value $probeXml -Encoding UTF8 -NoNewline

# --- CHANGELOG entry ---
$changelogPath = Join-Path $HandoffRoot "CHANGELOG.md"
$snapshotDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd")
$gitDescribe = ""
try {
    Push-Location $LicensingSystemRoot
    $gitDescribe = (git describe --tags --always 2>$null)
} finally {
    Pop-Location
}
$entry = @"

## SDK sync $snapshotDate

- Source: LicensingSystem $(if ($gitDescribe) { "($gitDescribe)" } else { "(local)" })
- libs: Revit.Licensing, Contracts, Agent.Ipc, Agent.Ipc.Revit
- reference: LicenseProbe
- docs: publisher-facing copies
"@
if (Test-Path -LiteralPath $changelogPath) {
    $existing = Get-Content -LiteralPath $changelogPath -Raw
    if ($existing -notmatch [regex]::Escape("SDK sync $snapshotDate")) {
        Set-Content -LiteralPath $changelogPath -Value ($entry.TrimStart() + "`n`n" + $existing) -Encoding UTF8
    }
} else {
    Set-Content -LiteralPath $changelogPath -Value ("# Changelog`n`n" + $entry.TrimStart()) -Encoding UTF8
}

Write-Host "Sync complete." -ForegroundColor Green

<#
.SYNOPSIS
  Builds VP-Hub-RevitPublisherKit-<Version>.zip for GitHub Release or Admin publisher kit upload.

.EXAMPLE
  pwsh -File scripts/build-publisher-kit-zip.ps1 -Version 1.0.0
#>
[CmdletBinding()]
param(
    [string] $Version = "1.0.0",
    [string] $OutputDirectory = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$KitRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $KitRoot "dist"
}
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$stagingName = "VP-Hub-RevitPublisherKit-$Version"
$staging = Join-Path ([System.IO.Path]::GetTempPath()) ("vp-kit-" + [Guid]::NewGuid().ToString("n"))
$stagingRoot = Join-Path $staging $stagingName
New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

$include = @(
    ".cursor",
    "docs",
    "libs",
    "reference",
    "templates",
    "scripts",
    "README.md",
    "ADOPTION.md",
    "ONBOARDING.md",
    "RELEASE.md",
    "SMOKE-TESTS.md",
    "INTEGRATION-CHECKLIST.md",
    "SUPPORT.md",
    "MAINTAINERS.md",
    "CHANGELOG.md",
    "SKILL.md"
)

foreach ($item in $include) {
    $src = Join-Path $KitRoot $item
    if (-not (Test-Path -LiteralPath $src)) { continue }
    $dst = Join-Path $stagingRoot $item
    if (Test-Path -LiteralPath $src -PathType Container) {
        Copy-Item -LiteralPath $src -Destination $dst -Recurse -Force
    } else {
        $parent = Split-Path -Parent $dst
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
        Copy-Item -LiteralPath $src -Destination $dst -Force
    }
}

# Exclude build artifacts from libs
Get-ChildItem -LiteralPath (Join-Path $stagingRoot "libs") -Directory -Recurse -Filter "bin" -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }
Get-ChildItem -LiteralPath (Join-Path $stagingRoot "libs") -Directory -Recurse -Filter "obj" -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }

$zipName = "$stagingName.zip"
$zipPath = Join-Path $OutputDirectory $zipName
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }

Compress-Archive -Path $stagingRoot -DestinationPath $zipPath -Force
Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue

$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Created: $zipPath" -ForegroundColor Green
Write-Host "SHA256: $hash"

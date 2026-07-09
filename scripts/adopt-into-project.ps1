<#
.SYNOPSIS
  Copies VP-Hub publisher kit into a target Revit plugin repository.

.EXAMPLE
  pwsh -File scripts/adopt-into-project.ps1 -TargetRepo "C:\dev\MyRevitPlugin"
.EXAMPLE
  pwsh -File scripts/adopt-into-project.ps1 -TargetRepo . -Level SkillsOnly
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $TargetRepo,
    [ValidateSet("Full", "SkillsOnly")]
    [string] $Level = "Full"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$KitRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$TargetRepo = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($TargetRepo)

if (-not (Test-Path -LiteralPath $TargetRepo)) {
    throw "Target repo not found: $TargetRepo"
}

function Copy-TreeIfMissing {
    param([string] $Source, [string] $Dest)
    if (Test-Path -LiteralPath $Dest) {
        Write-Host "Skip (exists): $Dest"
        return
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $Dest) -Force | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Dest -Recurse -Force
    Write-Host "Copied: $Dest"
}

# Cursor skills + rules
$skillsSrc = Join-Path $KitRoot ".cursor\skills"
$skillsDst = Join-Path $TargetRepo ".cursor\skills"
New-Item -ItemType Directory -Path $skillsDst -Force | Out-Null
Get-ChildItem -LiteralPath $skillsSrc -Directory | ForEach-Object {
    $dest = Join-Path $skillsDst $_.Name
    if (Test-Path -LiteralPath $dest) {
        Remove-Item -LiteralPath $dest -Recurse -Force
    }
    Copy-Item -LiteralPath $_.FullName -Destination $dest -Recurse -Force
    Write-Host "Skill: $($_.Name)"
}

$rulesSrc = Join-Path $KitRoot ".cursor\rules"
$rulesDst = Join-Path $TargetRepo ".cursor\rules"
if (Test-Path -LiteralPath $rulesSrc) {
    New-Item -ItemType Directory -Path $rulesDst -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $rulesSrc "*") -Destination $rulesDst -Force
    Write-Host "Rules copied."
}

if ($Level -eq "SkillsOnly") {
    Write-Host "Adopt complete (SkillsOnly)." -ForegroundColor Green
    return
}

# libs/
$libsDst = Join-Path $TargetRepo "libs"
if (Test-Path -LiteralPath $libsDst) {
    Write-Warning "libs/ already exists — not overwriting. Merge manually or delete libs/ first."
} else {
    Copy-TreeIfMissing -Source (Join-Path $KitRoot "libs") -Dest $libsDst
}

# packaging scripts
$packDst = Join-Path $TargetRepo "packaging"
if (-not (Test-Path -LiteralPath $packDst)) {
    Copy-TreeIfMissing -Source (Join-Path $KitRoot "templates\packaging") -Dest $packDst
}

# templates for publisher
$tplDst = Join-Path $TargetRepo "docs\templates"
New-Item -ItemType Directory -Path $tplDst -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $KitRoot "templates") -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $tplDst $_.Name) -Force
}
Write-Host "Templates: docs/templates/"

if (-not (Test-Path -LiteralPath (Join-Path $TargetRepo "version.json"))) {
    Copy-Item -LiteralPath (Join-Path $KitRoot "templates\version.json") -Destination (Join-Path $TargetRepo "version.json")
    Write-Host "Created version.json"
}

if (-not (Test-Path -LiteralPath (Join-Path $TargetRepo "packaging\plugin.manifest.json"))) {
    Copy-Item -LiteralPath (Join-Path $KitRoot "templates\plugin.manifest.json") -Destination (Join-Path $packDst "plugin.manifest.json")
    Write-Host "Created packaging/plugin.manifest.json (edit productCode and paths)"
}

Write-Host ""
Write-Host "Adopt complete (Full). Next: edit packaging/plugin.manifest.json, portal pinning, then use vp-hub-revit-integration skill." -ForegroundColor Green

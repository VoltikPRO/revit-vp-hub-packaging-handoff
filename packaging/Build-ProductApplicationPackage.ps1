param(
    [string] $Version = "",
    [string] $RevitInstallRoot = "",
    [int[]] $RevitYears = @(),
    [switch] $AllowPartialYears,
    [switch] $SkipBuild,
    [switch] $SkipTest
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if (-not $SkipBuild) {
    $buildArgs = @("-File", (Join-Path $PSScriptRoot "Build-ProductAllRevitYears.ps1"))
    if ($RevitInstallRoot) { $buildArgs += @("-RevitInstallRoot", $RevitInstallRoot) }
    if ($RevitYears.Count -gt 0) { $buildArgs += @("-RevitYears") + $RevitYears }
    if ($AllowPartialYears) { $buildArgs += "-AllowPartialYears" }
    & powershell @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "Build-ProductAllRevitYears failed" }
}

$packArgs = @("-File", (Join-Path $PSScriptRoot "Build-RevitApplicationPackage.ps1"))
if ($Version) { $packArgs += @("-Version", $Version) }
& powershell @packArgs
if ($LASTEXITCODE -ne 0) { throw "Build-RevitApplicationPackage failed" }

if (-not $SkipTest) {
    $testArgs = @("-File", (Join-Path $PSScriptRoot "Test-RevitApplicationPackage.ps1"))
    if ($Version) { $testArgs += @("-Version", $Version) }
    & powershell @testArgs
    if ($LASTEXITCODE -ne 0) { throw "Test-RevitApplicationPackage failed" }
}

Write-Host "Build-ProductApplicationPackage: OK" -ForegroundColor Green

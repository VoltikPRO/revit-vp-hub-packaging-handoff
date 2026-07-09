param(
    [string] $RevitInstallRoot = "",
    [int[]] $RevitYears = @(),
    [switch] $AllowPartialYears
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
. (Join-Path $PSScriptRoot "Packaging.Common.ps1")

$manifest = Get-PluginManifest -RepoRoot $repoRoot
$csproj = Join-Path $repoRoot $manifest.csprojPath
if (-not (Test-Path -LiteralPath $csproj)) {
    throw "csproj not found: $($manifest.csprojPath)"
}
$csprojDir = Split-Path -Parent $csproj

$years = if ($RevitYears.Count -gt 0) { $RevitYears } else { @($manifest.supportedYears) }
if (-not $RevitInstallRoot) {
    $RevitInstallRoot = Resolve-RevitInstallRoot -RepoRoot $repoRoot -Years $years
}

$addinTemplate = Join-Path $PSScriptRoot $manifest.addinFileName
if (-not (Test-Path -LiteralPath $addinTemplate)) {
    $addinTemplate = Join-Path $PSScriptRoot (Join-Path $manifest.bundleName $manifest.addinFileName)
}
if (-not (Test-Path -LiteralPath $addinTemplate)) {
    throw "Missing .addin template under packaging/ (expected $($manifest.addinFileName))"
}

$deployRoot = Join-Path $repoRoot "deploy"
if (Test-Path -LiteralPath $deployRoot) {
    Remove-Item -LiteralPath $deployRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $deployRoot -Force | Out-Null

foreach ($y in ($years | Sort-Object -Unique)) {
    if (-not (Test-RevitApi -Root $RevitInstallRoot -Year $y)) {
        $msg = "Revit $y API not found under $(Get-RevitApiPath -Root $RevitInstallRoot -Year $y)"
        if ($AllowPartialYears) {
            Write-Warning $msg
            continue
        }
        throw $msg
    }

    $tfm = Get-TfmForRevitYear -Year $y
    $revitPath = Get-RevitApiPath -Root $RevitInstallRoot -Year $y
    Write-Host "Building $tfm for Revit $y..." -ForegroundColor Cyan
    dotnet build $csproj -c Release -f $tfm -p:RevitInstallPath="$revitPath"
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for Revit $y / $tfm" }

    $buildOut = Resolve-BuildOut -CsprojDir $csprojDir -Tfm $tfm
    if (-not $buildOut) { throw "Build output not found for $tfm" }

    $dest = Join-Path $deployRoot "$y"
    Copy-ProbeBuildArtifacts -BuildOut $buildOut -DestDir $dest -AddinTemplatePath $addinTemplate -AddinFileName $manifest.addinFileName

    $dllCount = (Get-ChildItem -Path $dest -Filter "*.dll" -File).Count
    $minKey = if ($tfm -eq "net48") { "net48" } else { "net8" }
    $minCount = [int]$manifest.minDllCount.$minKey
    if ($dllCount -lt $minCount) {
        throw "deploy/$y has $dllCount DLLs; minDllCount.$minKey is $minCount"
    }
}

if (-not (Get-ChildItem -LiteralPath $deployRoot -Directory)) {
    throw "No deploy/<year>/ folders produced."
}

Write-Host "Build-ProductAllRevitYears: OK" -ForegroundColor Green

param([string] $Version = "")

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
. (Join-Path $PSScriptRoot "Packaging.Common.ps1")

$manifest = Get-PluginManifest -RepoRoot $repoRoot
if (-not $Version) { $Version = Read-VersionJson -RepoRoot $repoRoot }

$zipPath = Join-Path $repoRoot "artifacts\builds\$($manifest.productCode)\$Version\package.zip"
if (-not (Test-Path -LiteralPath $zipPath)) {
    throw "Missing package.zip: $zipPath"
}

$temp = Join-Path $env:TEMP ("vp-hub-test-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $temp -Force | Out-Null
try {
    Expand-Archive -LiteralPath $zipPath -DestinationPath $temp -Force

    $bundles = Get-ChildItem -LiteralPath $temp -Directory -Filter "*.bundle"
    if ($bundles.Count -ne 1) {
        throw "ZIP root must contain exactly one *.bundle folder (found $($bundles.Count))."
    }

    $bareContents = Join-Path $temp "Contents"
    if (Test-Path -LiteralPath $bareContents) {
        throw "ZIP root must not contain bare Contents/ — bundle folder required."
    }

    $bundleDir = $bundles[0].FullName
    $pcXml = Join-Path $bundleDir "PackageContents.xml"
    if (-not (Test-Path -LiteralPath $pcXml)) {
        throw "Missing PackageContents.xml in bundle."
    }
    Test-PackageContentsSeriesFormat -PackageContentsPath $pcXml

    [xml] $doc = Get-Content -LiteralPath $pcXml
    foreach ($comp in $doc.ApplicationPackage.Components) {
        $moduleName = $comp.ComponentEntry.ModuleName -replace '^\./', ''
        $yearFolder = ($moduleName -split '/')[1]
        $yearPath = Join-Path $bundleDir "Contents\$yearFolder"
        if (-not (Test-Path -LiteralPath $yearPath)) {
            throw "PackageContents references Contents/$yearFolder but folder missing."
        }
        $addinPath = Join-Path $yearPath $manifest.addinFileName
        if (-not (Test-Path -LiteralPath $addinPath)) {
            throw "Missing .addin in Contents/$yearFolder"
        }
        $mainDll = Join-Path $yearPath $manifest.mainAssembly
        if (-not (Test-Path -LiteralPath $mainDll)) {
            throw "Missing main assembly $($manifest.mainAssembly) in Contents/$yearFolder"
        }

        $tfmKey = if ([int]$yearFolder -le 2024) { "net48" } else { "net8" }
        $dllCount = (Get-ChildItem -Path $yearPath -Filter "*.dll" -File).Count
        $minCount = [int]$manifest.minDllCount.$tfmKey
        if ($dllCount -lt $minCount) {
            throw "Contents/$yearFolder has $dllCount DLLs; minDllCount.$tfmKey is $minCount"
        }

        $required = @($manifest.requiredDlls.$tfmKey)
        if ($required -and $required.Count -gt 0) {
            foreach ($dll in $required) {
                if (-not (Test-Path -LiteralPath (Join-Path $yearPath $dll))) {
                    throw "Missing required DLL $dll in Contents/$yearFolder"
                }
            }
        }
    }
}
finally {
    if (Test-Path -LiteralPath $temp) {
        Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Test-RevitApplicationPackage: OK" -ForegroundColor Green

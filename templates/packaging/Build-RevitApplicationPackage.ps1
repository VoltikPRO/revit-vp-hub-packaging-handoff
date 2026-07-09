param(
    [string] $Version = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
. (Join-Path $PSScriptRoot "Packaging.Common.ps1")

$manifest = Get-PluginManifest -RepoRoot $repoRoot
if (-not $Version) { $Version = Read-VersionJson -RepoRoot $repoRoot }

$deployRoot = Join-Path $repoRoot "deploy"
if (-not (Test-Path -LiteralPath $deployRoot)) {
    throw "Missing deploy/ — run Build-ProductAllRevitYears.ps1 first"
}

$years = Get-ChildItem -LiteralPath $deployRoot -Directory | ForEach-Object { [int]$_.Name } | Sort-Object
if ($years.Count -eq 0) { throw "deploy/ has no year folders" }

$staging = Join-Path $env:TEMP ("vp-hub-bundle-" + [Guid]::NewGuid().ToString("N"))
$bundleOut = Join-Path $staging $manifest.bundleName
$contentsOut = Join-Path $bundleOut "Contents"
New-Item -ItemType Directory -Path $contentsOut -Force | Out-Null

foreach ($y in $years) {
    $src = Join-Path $deployRoot "$y"
    $dst = Join-Path $contentsOut "$y"
    Copy-Item -LiteralPath $src -Destination $dst -Recurse -Force
}

$friendly = $Version
$appVer = Normalize-AppVersionFour -Version $Version
Write-PackageContentsXml -Path (Join-Path $bundleOut "PackageContents.xml") -Manifest $manifest -AppVersion $appVer -FriendlyVersion $friendly -Years $years
Test-PackageContentsSeriesFormat -PackageContentsPath (Join-Path $bundleOut "PackageContents.xml")

$artifactDir = Join-Path $repoRoot "artifacts\builds\$($manifest.productCode)\$Version"
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
$bundleCopy = Join-Path $artifactDir $manifest.bundleName
if (Test-Path -LiteralPath $bundleCopy) { Remove-Item -LiteralPath $bundleCopy -Recurse -Force }
Copy-Item -LiteralPath $bundleOut -Destination $bundleCopy -Recurse -Force

$zipPath = Join-Path $artifactDir "package.zip"
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -LiteralPath $bundleCopy -DestinationPath $zipPath -Force
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()

Write-Host ""
Write-Host "Package: $zipPath" -ForegroundColor Green
Write-Host "SHA256: $hash"
Write-Host ""
Write-Host "Manifest entry (artifacts/manifest.json):"
Write-Host @"
{
  "productCode": "$($manifest.productCode)",
  "version": "$Version",
  "channel": "stable",
  "sha256": "$hash",
  "relativePath": "builds/$($manifest.productCode)/$Version/package.zip",
  "fileName": "package.zip"
}
"@

if (Test-Path -LiteralPath $staging) {
    Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
}

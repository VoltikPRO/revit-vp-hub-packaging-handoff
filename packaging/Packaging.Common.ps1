# Shared packaging helpers — dot-source from other packaging/*.ps1
$ErrorActionPreference = "Stop"

function Get-PluginManifest {
    param([string] $RepoRoot)
    $path = Join-Path $RepoRoot "packaging/plugin.manifest.json"
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing packaging/plugin.manifest.json"
    }
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

function Get-TfmForRevitYear {
    param([int] $Year)
    if ($Year -le 2024) { return "net48" }
    return "net8.0-windows"
}

function Get-RevitApiPath {
    param([string] $Root, [int] $Year)
    Join-Path $Root "Revit $Year"
}

function Test-RevitApi {
    param([string] $Root, [int] $Year)
    $revitPath = Get-RevitApiPath -Root $Root -Year $Year
    (Test-Path (Join-Path $revitPath "RevitAPI.dll")) -and (Test-Path (Join-Path $revitPath "RevitAPIUI.dll"))
}

function Resolve-RevitInstallRoot {
    param(
        [string] $RepoRoot,
        [int[]] $Years,
        [string] $Fallback = "C:\Program Files\Autodesk"
    )

    foreach ($candidate in @(
            (Join-Path $RepoRoot "revit\revit-api"),
            (Join-Path $RepoRoot "revit-api")
        )) {
        $allLocal = $true
        foreach ($y in $Years) {
            if (-not (Test-RevitApi -Root $candidate -Year $y)) {
                $allLocal = $false
                break
            }
        }
        if ($allLocal) { return $candidate }
    }

    return $Fallback
}

function Resolve-BuildOut {
    param([string] $CsprojDir, [string] $Tfm)
    $candidates = @(
        (Join-Path $CsprojDir "bin\x64\Release\$Tfm"),
        (Join-Path $CsprojDir "bin\Release\$Tfm")
    )
    foreach ($p in $candidates) {
        if (Test-Path -LiteralPath $p) { return $p }
    }
    return $null
}

function Copy-ProbeBuildArtifacts {
    param(
        [string] $BuildOut,
        [string] $DestDir,
        [string] $AddinTemplatePath,
        [string] $AddinFileName
    )
    New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
    Copy-Item -LiteralPath $AddinTemplatePath -Destination (Join-Path $DestDir $AddinFileName) -Force
    foreach ($pat in '*.dll', '*.pdb', '*.deps.json', '*.runtimeconfig.json', '*.dll.config') {
        Get-ChildItem -Path $BuildOut -Filter $pat -File -ErrorAction SilentlyContinue |
            ForEach-Object { Copy-Item $_.FullName $DestDir -Force }
    }
}

function Normalize-AppVersionFour {
    param([string] $Version)
    if ($Version -match '^\d+\.\d+\.\d+$') { return "$Version.0" }
    return $Version
}

function Write-PackageContentsXml {
    param(
        [string] $Path,
        [object] $Manifest,
        [string] $AppVersion,
        [string] $FriendlyVersion,
        [int[]] $Years
    )
    $sorted = $Years | Sort-Object -Unique
    $xml = New-Object System.Text.StringBuilder
    [void]$xml.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
    [void]$xml.AppendLine('<ApplicationPackage SchemaVersion="1.0"')
    [void]$xml.AppendLine('                    AutodeskProduct="Revit"')
    [void]$xml.AppendLine('                    ProductType="Application"')
    [void]$xml.AppendLine(('                    Name="{0}"' -f $Manifest.displayName))
    [void]$xml.AppendLine(('                    Description="{0}"' -f $Manifest.description))
    [void]$xml.AppendLine(('                    AppVersion="{0}"' -f $AppVersion))
    [void]$xml.AppendLine(('                    FriendlyVersion="{0}">' -f $FriendlyVersion))
    [void]$xml.AppendLine(('  <CompanyDetails Name="{0}" Url="" Email="" />' -f $Manifest.companyName))
    foreach ($y in $sorted) {
        $r = "R$y"
        [void]$xml.AppendLine(('  <Components Description="Revit {0}">' -f $y))
        [void]$xml.AppendLine(('    <RuntimeRequirements OS="Win64" Platform="Revit" SeriesMin="{0}" SeriesMax="{0}" />' -f $r))
        [void]$xml.AppendLine(('    <ComponentEntry AppName="{0}"' -f $Manifest.displayName))
        [void]$xml.AppendLine(('                    Version="{0}"' -f $FriendlyVersion))
        [void]$xml.AppendLine(('                    ModuleName="./Contents/{0}/{1}"' -f $y, $Manifest.addinFileName))
        [void]$xml.AppendLine(('                    AppDescription="{0} for Revit {1}." />' -f $Manifest.displayName, $y))
        [void]$xml.AppendLine('  </Components>')
    }
    [void]$xml.AppendLine('</ApplicationPackage>')
    [System.IO.File]::WriteAllText($Path, $xml.ToString(), [System.Text.UTF8Encoding]::new($false))
}

function Test-PackageContentsSeriesFormat {
    param([string] $PackageContentsPath)
    [xml] $doc = Get-Content -LiteralPath $PackageContentsPath
    foreach ($comp in $doc.ApplicationPackage.Components) {
        $min = $comp.RuntimeRequirements.SeriesMin
        $max = $comp.RuntimeRequirements.SeriesMax
        if ($min -notmatch '^R\d{4}$' -or $max -notmatch '^R\d{4}$') {
            throw "Invalid SeriesMin/SeriesMax in PackageContents.xml: $min / $max (expected R<yyyy>)."
        }
        if ($min -ne $max) {
            throw "SeriesMin must equal SeriesMax per year block: $min vs $max"
        }
    }
}

function Read-VersionJson {
    param([string] $RepoRoot)
    $vPath = Join-Path $RepoRoot "version.json"
    if (-not (Test-Path -LiteralPath $vPath)) {
        throw "Missing version.json at repo root"
    }
    $v = (Get-Content -LiteralPath $vPath -Raw | ConvertFrom-Json).version
    if ([string]::IsNullOrWhiteSpace($v)) { throw "version.json.version is empty" }
    return [string]$v
}

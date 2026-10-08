param(
    [string]$AssetsPath = "",
    [string]$TargetFramework = "net9.0",
    [string]$OutputPath = ""
)

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($AssetsPath)) {
    $AssetsPath = Join-Path $repoRoot "Cashere.Desktop\obj\project.assets.json"
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot "Cashere\Assets\ThirdPartyLicenses.tsv"
}
if (-not (Test-Path -LiteralPath $AssetsPath)) {
    throw "NuGet assets file not found: $AssetsPath. Restore Cashere.Desktop first."
}

$assets = Get-Content -LiteralPath $AssetsPath -Raw | ConvertFrom-Json
$target = $assets.targets.$TargetFramework
if ($null -eq $target) {
    throw "Target framework '$TargetFramework' was not found in $AssetsPath."
}

$packageRoot = $assets.packageFolders.PSObject.Properties.Name | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($packageRoot)) {
    $packageRoot = $env:NUGET_PACKAGES
}
if ([string]::IsNullOrWhiteSpace($packageRoot)) {
    throw "NuGet package folder could not be determined from the assets file."
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("Package`tVersion`tLicense`tLicenseUrl`tPackageUrl")
foreach ($targetEntry in $target.PSObject.Properties | Sort-Object Name) {
    $parts = $targetEntry.Name -split "/", 2
    if ($parts.Count -ne 2) { continue }

    $library = $assets.libraries.PSObject.Properties[$targetEntry.Name]
    if ($null -eq $library -or $library.Value.type -ne "package") { continue }
    if (-not $targetEntry.Value.runtime -and -not $targetEntry.Value.native) { continue }

    $packageId = $parts[0]
    $version = $parts[1]
    $packageUrl = "https://www.nuget.org/packages/$packageId/$version"
    $license = "See package metadata"
    $licenseUrl = $packageUrl
    $nuspecPath = Join-Path (Join-Path (Join-Path $packageRoot $packageId.ToLowerInvariant()) $version.ToLowerInvariant()) "$($packageId.ToLowerInvariant()).nuspec"

    if (Test-Path -LiteralPath $nuspecPath) {
        [xml]$nuspec = Get-Content -LiteralPath $nuspecPath -Raw
        $metadata = $nuspec.package.metadata
        if ($metadata.license) {
            $licenseType = [string]$metadata.license.type
            $licenseValue = [string]$metadata.license.InnerText
            if ($licenseType -eq "expression" -and -not [string]::IsNullOrWhiteSpace($licenseValue)) {
                $license = $licenseValue.Trim()
                $licenseUrl = "https://licenses.nuget.org/$license"
            } elseif (-not [string]::IsNullOrWhiteSpace($licenseValue)) {
                $license = "Included file: $($licenseValue.Trim())"
            }
        } elseif (-not [string]::IsNullOrWhiteSpace([string]$metadata.licenseUrl)) {
            $licenseUrl = ([string]$metadata.licenseUrl).Trim()
            $license = "See license link"
        }
    }

    $lines.Add("$packageId`t$version`t$license`t$licenseUrl`t$packageUrl")
}

$directory = Split-Path -Parent $OutputPath
if (-not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}
[System.IO.File]::WriteAllLines($OutputPath, $lines, [System.Text.UTF8Encoding]::new($false))
Write-Output "Wrote $($lines.Count - 1) runtime packages to $OutputPath"

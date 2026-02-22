[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$VersionFile,

    [switch]$Prerelease,

    [string]$TagPrefix = 'v'
)

# Import Common
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir "../common/Common.ps1")

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Get-FileVersion {
    param([string]$VersionFile)

    if (-not (Test-Path $VersionFile)) {
        Write-Error "❌ Error: $VersionFile not found"
        exit 1
    }

    $content = (Get-Content $VersionFile).Trim()
    try {
        $version = [System.Management.Automation.SemanticVersion]::Parse($content)
        Write-Host "📋 File version: $version"
        return $version
    }
    catch {
        Write-Error "❌ Error: Invalid semantic version in file: $content"
        exit 1
    }
}

function Get-LatestRelease {
    param(
        [int]$MajorVersion,
        [string]$TagPrefix,
        [bool]$IncludePreRelease = $true
    )

    $tagPattern = "${TagPrefix}${MajorVersion}.*"
    $versions = git tag -l $tagPattern 2>$null | Where-Object { $_ } |
    ForEach-Object {
        try {
            # Use proper prefix removal instead of Replace to avoid removing characters from middle of string
            $versionString = if ($_.StartsWith($TagPrefix)) {
                $_.Substring($TagPrefix.Length)
            }
            else {
                $_
            }
            [System.Management.Automation.SemanticVersion]($versionString)
        }
        catch { $null }
    } | Where-Object { $_ }

    # Filter out pre-releases if needed
    if (-not $IncludePreRelease) {
        $versions = $versions | Where-Object { -not $_.PreReleaseLabel }
    }

    $latest = $versions | Sort-Object -Descending | Select-Object -First 1

    if (-not $latest) {
        $type = if ($IncludePreRelease) { "releases" } else { "stable releases" }
        Write-Host "ℹ️  No previous $type found for major version $MajorVersion"
    }

    return $latest
}

function Get-NextPrereleaseNumber {
    param([string]$PrereleaseString)

    if ([string]::IsNullOrEmpty($PrereleaseString)) { return 1 }

    if ($PrereleaseString -match '\.(\d+)$' -or $PrereleaseString -match '^(\d+)$') {
        return [int]$Matches[1] + 1
    }
    return 1
}

function Test-SameBaseVersion {
    param(
        [System.Management.Automation.SemanticVersion]$Version1,
        [System.Management.Automation.SemanticVersion]$Version2
    )

    return ($Version1.Major -eq $Version2.Major -and
        $Version1.Minor -eq $Version2.Minor -and
        $Version1.Patch -eq $Version2.Patch)
}

function Get-PrereleaseLabel {
    param([System.Management.Automation.SemanticVersion]$Version)

    if ($Version.PreReleaseLabel) {
        return ($Version.PreReleaseLabel -split '\.')[0]
    }
    return $null
}

# Main script
Write-Host "🔍 Calculating version..."
Write-Host "   Version file: $VersionFile"
Write-Host "   Prerelease: $($Prerelease.IsPresent)"
Write-Host "   Tag prefix: $TagPrefix"

# Get file version and latest releases
$fileVersion = Get-FileVersion -VersionFile $VersionFile
$latestRelease = Get-LatestRelease -MajorVersion $fileVersion.Major -TagPrefix $TagPrefix
$latestStable = Get-LatestRelease -MajorVersion $fileVersion.Major -TagPrefix $TagPrefix -IncludePreRelease $false

# Output previous version info
# previous-tag is context-aware: in stable mode it points to the previous stable tag
# so release notes compare stable-to-stable, not stable-to-preview
$previousTagVersion = if ($Prerelease) { $latestRelease } else { $latestStable }

if ($latestRelease) {
    Write-GitHubOutput -Name "previous-version" -Value $latestRelease.ToString()
    Write-GitHubOutput -Name "previous-tag" -Value $(if ($previousTagVersion) { "${TagPrefix}$previousTagVersion" } else { "" })
    Write-GitHubOutput -Name "previous-stable-version" -Value $(if ($latestStable) { $latestStable.ToString() } else { "" })
    Write-GitHubOutput -Name "previous-stable-tag" -Value $(if ($latestStable) { "${TagPrefix}$latestStable" } else { "" })
}
else {
    Write-GitHubOutput -Name "previous-version" -Value ""
    Write-GitHubOutput -Name "previous-tag" -Value ""
    Write-GitHubOutput -Name "previous-stable-version" -Value ""
    Write-GitHubOutput -Name "previous-stable-tag" -Value ""
}

# Calculate new version
if ($Prerelease) {
    Write-Host "🔧 Calculating pre-release version..."

    if (-not $latestRelease) {
        # First pre-release
        $label = if ($fileVersion.PreReleaseLabel) { ($fileVersion.PreReleaseLabel -split '\.')[0] } else { "preview" }
        $calculatedVersion = "$($fileVersion.Major).$($fileVersion.Minor).$($fileVersion.Patch)-$label.1"
    }
    elseif (Test-SameBaseVersion $fileVersion $latestRelease) {
        # Same base version
        if ($latestRelease.PreReleaseLabel) {
            $fileLabel = Get-PrereleaseLabel $fileVersion
            $latestLabel = Get-PrereleaseLabel $latestRelease

            if ($fileLabel -and $fileLabel -ne $latestLabel) {
                # Different prerelease label
                $calculatedVersion = "$($fileVersion.Major).$($fileVersion.Minor).$($fileVersion.Patch)-$fileLabel.1"
            }
            else {
                # Increment existing prerelease
                $label = if ($latestLabel) { $latestLabel } else { "preview" }
                $nextNum = Get-NextPrereleaseNumber $latestRelease.PreReleaseLabel
                $calculatedVersion = "$($latestRelease.Major).$($latestRelease.Minor).$($latestRelease.Patch)-$label.$nextNum"
            }
        }
        else {
            # Latest is stable, create next patch pre-release
            $calculatedVersion = "$($latestRelease.Major).$($latestRelease.Minor).$($latestRelease.Patch + 1)-preview.1"
        }
    }
    elseif ($fileVersion -gt $latestRelease) {
        # File version is newer
        $label = if ($fileVersion.PreReleaseLabel) { ($fileVersion.PreReleaseLabel -split '\.')[0] } else { "preview" }
        $calculatedVersion = "$($fileVersion.Major).$($fileVersion.Minor).$($fileVersion.Patch)-$label.1"
    }
    else {
        # File version is older
        if ($latestRelease.PreReleaseLabel) {
            # Increment existing prerelease
            $label = Get-PrereleaseLabel $latestRelease
            $nextNum = Get-NextPrereleaseNumber $latestRelease.PreReleaseLabel
            $calculatedVersion = "$($latestRelease.Major).$($latestRelease.Minor).$($latestRelease.Patch)-$label.$nextNum"
        }
        else {
            # Create next patch pre-release
            $calculatedVersion = "$($latestRelease.Major).$($latestRelease.Minor).$($latestRelease.Patch + 1)-preview.1"
        }
    }
}
else {
    Write-Host "🔧 Calculating stable release version..."

    if (-not $latestRelease -or $fileVersion -gt $latestRelease) {
        # No previous release or file version is newer
        $calculatedVersion = "$($fileVersion.Major).$($fileVersion.Minor).$($fileVersion.Patch)"
    }
    elseif ($latestRelease.PreReleaseLabel) {
        # Promote pre-release to stable
        $calculatedVersion = "$($latestRelease.Major).$($latestRelease.Minor).$($latestRelease.Patch)"
    }
    else {
        # Increment patch
        $calculatedVersion = "$($latestRelease.Major).$($latestRelease.Minor).$($latestRelease.Patch + 1)"
    }
}

# Convert string to SemanticVersion for consistency
$calculatedVersion = [System.Management.Automation.SemanticVersion]::Parse($calculatedVersion)
$calculatedTag = "${TagPrefix}${calculatedVersion}"

# Output results
Write-Host "✅ Version calculation complete:"
Write-Host "   Calculated version: $calculatedVersion"
Write-Host "   Calculated tag: $calculatedTag"

Write-GitHubOutput -Name "next-version" -Value $calculatedVersion.ToString()
Write-GitHubOutput -Name "next-tag" -Value $calculatedTag

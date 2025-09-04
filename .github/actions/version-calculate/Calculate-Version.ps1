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
    param(
        [Parameter(Mandatory = $true)]
        [string]$VersionFile
    )

    if (-not (Test-Path $VersionFile)) {
        Write-Error "❌ Error: $VersionFile not found"
        exit 1
    }

    $fileVersionContent = (Get-Content $VersionFile).Trim()

    try {
        # Parse as semantic version
        $fileVersion = [System.Management.Automation.SemanticVersion]::Parse($fileVersionContent)
        Write-Host "📋 File version: $fileVersion"
        return $fileVersion
    }
    catch {
        Write-Error "❌ Error: Invalid semantic version format in file: $fileVersionContent"
        exit 1
    }
}

function Get-LatestRelease {
    param(
        [Parameter(Mandatory = $true)]
        [int]$MajorVersion,

        [Parameter(Mandatory = $true)]
        [string]$TagPrefix
    )

    # Get all tags matching the major version pattern
    $tagPattern = "${TagPrefix}${MajorVersion}.*"
    $allTags = @(git tag -l $tagPattern --sort=-version:refname)

    if ($allTags.Count -eq 0) {
        Write-Host "ℹ️  No previous releases found for major version $MajorVersion"
        return $null
    }

    # Find the latest stable or pre-release version
    foreach ($tag in $allTags) {
        $versionString = $tag -replace "^${TagPrefix}", ""
        try {
            $version = [System.Management.Automation.SemanticVersion]::Parse($versionString)
            Write-Host "📋 Latest release found: $version"
            return $version
        }
        catch {
            Write-Warning "⚠️  Skipping invalid version tag: $tag"
            continue
        }
    }

    return $null
}

function Get-NextPrereleaseNumber {
    param(
        [string]$PrereleaseString
    )

    if ([string]::IsNullOrEmpty($PrereleaseString)) {
        return 1
    }

    # Match patterns like "preview.1", "alpha.1", "beta.2", or just "1"
    if ($PrereleaseString -match '\.(\d+)$') {
        return [int]$Matches[1] + 1
    }
    elseif ($PrereleaseString -match '^(\d+)$') {
        return [int]$Matches[1] + 1
    }

    return 1
}

function New-PrereleaseVersionFromFile {
    param(
        [Parameter(Mandatory = $true)]
        [System.Management.Automation.SemanticVersion]$FileVersion
    )

    $prereleaseLabel = if ($FileVersion.PreReleaseLabel) {
        $labelParts = $FileVersion.PreReleaseLabel -split '\.'
        $labelParts[0]
    }
    else {
        "preview"
    }

    return [System.Management.Automation.SemanticVersion]::new(
        $FileVersion.Major,
        $FileVersion.Minor,
        $FileVersion.Patch,
        "$prereleaseLabel.1"
    )
}

# Main script logic
Write-Host "🔍 Calculating version..."
Write-Host "   Version file: $VersionFile"
Write-Host "   Prerelease: $($Prerelease.IsPresent)"
Write-Host "   Tag prefix: $TagPrefix"

# Get the version from file
$fileVersion = Get-FileVersion -VersionFile $VersionFile

# Get the latest release for this major version
$latestRelease = Get-LatestRelease -MajorVersion $fileVersion.Major -TagPrefix $TagPrefix

# Initialize output variables
$calculatedVersion = $null
$calculatedTag = ""
$previousVersion = ""
$previousTag = ""
$previousStableVersion = ""

# Store previous version info
if ($latestRelease) {
    $previousVersion = $latestRelease.ToString()
    $previousTag = "${TagPrefix}${previousVersion}"
    Write-GitHubOutput -Name "previous-version" -Value $previousVersion
    Write-GitHubOutput -Name "previous-tag" -Value $previousTag

    # Find the latest stable version (without pre-release)
    if (-not $latestRelease.PreReleaseLabel) {
        # Latest IS the stable version
        $previousStableVersion = $previousVersion
    }
    else {
        # Latest is a pre-release, find the actual stable version
        $stableTags = @(git tag -l "${TagPrefix}$($fileVersion.Major).*.*" --sort=-version:refname | Where-Object { $_ -notmatch '-' })
        if ($stableTags.Count -gt 0) {
            $previousStableVersion = $stableTags[0] -replace "^${TagPrefix}", ""
        }
    }
    Write-GitHubOutput -Name "previous-stable-version" -Value $previousStableVersion
}
else {
    Write-GitHubOutput -Name "previous-version" -Value ""
    Write-GitHubOutput -Name "previous-tag" -Value ""
    Write-GitHubOutput -Name "previous-stable-version" -Value ""
}

# Version calculation logic
if ($Prerelease) {
    Write-Host "🔧 Calculating pre-release version..."

    if (-not $latestRelease -or $fileVersion.CompareTo($latestRelease) -gt 0) {
        # File version is greater than latest - use file version as base
        $calculatedVersion = New-PrereleaseVersionFromFile -FileVersion $fileVersion

        if (-not $latestRelease) {
            Write-Host "ℹ️  First pre-release for version $($fileVersion.Major).$($fileVersion.Minor).$($fileVersion.Patch)"
        }
        else {
            Write-Host "ℹ️  File version ($fileVersion) > latest release ($latestRelease)"
            Write-Host "ℹ️  Creating pre-release: $calculatedVersion"
        }
    }
    else {
        # File version <= latest release - work from latest release
        if ($latestRelease.PreReleaseLabel) {
            # Latest is already a pre-release - increment it
            $labelParts = $latestRelease.PreReleaseLabel -split '\.'
            $prereleaseLabel = $labelParts[0]
            $nextNumber = Get-NextPrereleaseNumber -PrereleaseString $latestRelease.PreReleaseLabel

            $calculatedVersion = [System.Management.Automation.SemanticVersion]::new(
                $latestRelease.Major,
                $latestRelease.Minor,
                $latestRelease.Patch,
                "$prereleaseLabel.$nextNumber"
            )
            Write-Host "ℹ️  Incrementing pre-release: $latestRelease → $calculatedVersion"
        }
        else {
            # Latest is stable - create new pre-release for next patch version
            $calculatedVersion = [System.Management.Automation.SemanticVersion]::new(
                $latestRelease.Major,
                $latestRelease.Minor,
                $latestRelease.Patch + 1,
                "preview.1"
            )
            Write-Host "ℹ️  Creating pre-release from stable: $latestRelease → $calculatedVersion"
        }
    }
}
else {
    Write-Host "🔧 Calculating stable release version..."

    if (-not $latestRelease -or $fileVersion.CompareTo($latestRelease) -gt 0) {
        # No previous releases OR file version is greater than latest - use file version
        $calculatedVersion = [System.Management.Automation.SemanticVersion]::new(
            $fileVersion.Major,
            $fileVersion.Minor,
            $fileVersion.Patch
        )

        if (-not $latestRelease) {
            Write-Host "ℹ️  First release for major version $($fileVersion.Major)"
        }
        else {
            Write-Host "ℹ️  File version ($fileVersion) > latest release ($latestRelease)"
            Write-Host "ℹ️  Creating stable release: $calculatedVersion"
        }
    }
    else {
        # File version <= latest release - work from latest release
        if ($latestRelease.PreReleaseLabel) {
            # Latest is a pre-release - promote to stable
            $calculatedVersion = [System.Management.Automation.SemanticVersion]::new(
                $latestRelease.Major,
                $latestRelease.Minor,
                $latestRelease.Patch
            )
            Write-Host "ℹ️  Promoting pre-release to stable: $latestRelease → $calculatedVersion"
        }
        else {
            # Latest is stable - increment patch
            $calculatedVersion = [System.Management.Automation.SemanticVersion]::new(
                $latestRelease.Major,
                $latestRelease.Minor,
                $latestRelease.Patch + 1
            )
            Write-Host "ℹ️  Incrementing patch: $latestRelease → $calculatedVersion"
        }
    }
}

# Generate the tag
$calculatedTag = "${TagPrefix}${calculatedVersion}"

# Output results
Write-Host "✅ Version calculation complete:"
Write-Host "   Calculated version: $calculatedVersion"
Write-Host "   Calculated tag: $calculatedTag"

# Write GitHub outputs (using the naming convention from action.yml)
Write-GitHubOutput -Name "next-version" -Value $calculatedVersion.ToString()
Write-GitHubOutput -Name "next-tag" -Value $calculatedTag

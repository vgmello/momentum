[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$VersionFile = "libs/Momentum/version.txt",

    [ValidateSet("stable", "prerelease")]
    [string]$ReleaseType = "stable"
)

# Import Common
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir "../common/Common.ps1")

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Compare-Version {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Version1,
        [Parameter(Mandatory=$true)]
        [string]$Version2
    )
    try {
        $v1 = [Version]::Parse($Version1)
        $v2 = [Version]::Parse($Version2)
        return $v1.CompareTo($v2)
    }
    catch {
        Write-Error "Failed to compare versions '$Version1' and '$Version2': $_"
        throw
    }
}

# Read version file
if (-not (Test-Path $VersionFile)) {
    Write-Error "❌ Error: $VersionFile not found"
    exit 1
}

$fileVersion = (Get-Content $VersionFile).Trim()

if ($fileVersion -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "❌ Error: Invalid version format: $fileVersion"
    exit 1
}

Write-Host "📋 File version: $fileVersion"

# Determine tag prefix
$tagPrefix = "v"
if ($VersionFile -like "*.template.config/version.txt*") {
    $tagPrefix = "template-v"
    Write-Host "Using template tag format (detected from version file: $VersionFile)"
}
else {
    Write-Host "Using standard tag format"
}

# Find previous stable releases
$allTags = git tag -l "${tagPrefix}*.*.*" --sort=-creatordate
$prevTag = $allTags | Where-Object { $_ -notmatch 'pre' } | Select-Object -First 1

if (-not $prevTag) {
    Write-Host "No previous stable release found"
    $prevTag = "${tagPrefix}0.0.0"
}

Write-Host "Previous stable release: $prevTag"
$prevVersion = $prevTag -replace "^$tagPrefix", ""

Write-GitHubOutput -Name "prev_tag" -Value $prevTag
Write-GitHubOutput -Name "tag_prefix" -Value $tagPrefix
Write-GitHubOutput -Name "previous_stable_version" -Value $prevVersion

# Check for pre-releases
$latestPrerelease = git tag -l "${tagPrefix}${fileVersion}-pre.*" --sort=-creatordate | Select-Object -First 1
$hasPrerelease = $false
$prereleaseSequence = 0

if ($latestPrerelease) {
    Write-Host "Found pre-release: $latestPrerelease"
    $hasPrerelease = $true
    Write-GitHubOutput -Name "has_prerelease" -Value "true"
    Write-GitHubOutput -Name "latest_prerelease" -Value $latestPrerelease

    if ($latestPrerelease -match '-pre\.(\d+)$') {
        $prereleaseSequence = [int]$Matches[1]
    }
    Write-GitHubOutput -Name "prerelease_sequence" -Value $prereleaseSequence.ToString()
}
else {
    Write-GitHubOutput -Name "has_prerelease" -Value "false"
    Write-GitHubOutput -Name "prerelease_sequence" -Value "0"
}

# Calculate version
$calculatedVersion = ""
$calculatedTag = ""

if ($ReleaseType -eq "prerelease") {
    # Calculate pre-release version
    if ($hasPrerelease) {
        $nextSequence = $prereleaseSequence + 1
        $calculatedVersion = "${fileVersion}-pre.${nextSequence}"
        Write-Host "ℹ️  Incrementing pre-release: sequence $prereleaseSequence → $nextSequence"
    }
    else {
        $calculatedVersion = "${fileVersion}-pre.1"
        Write-Host "ℹ️  First pre-release for version $fileVersion"
    }
}
else {
    # Calculate stable release version
    if ($hasPrerelease) {
        $calculatedVersion = $fileVersion
        Write-Host "ℹ️  Transitioning from pre-release to stable: $calculatedVersion"
    }
    # Check if the version was bumped manually in the version file
    elseif ((Compare-Version -Version1 $fileVersion -Version2 $prevVersion) -gt 0) {
        $calculatedVersion = $fileVersion
        Write-Host "ℹ️  Using file version: $calculatedVersion (> $prevVersion)"
    }
    else {
        $versionParts = $prevVersion.Split('.')
        $major = [int]$versionParts[0]
        $minor = [int]$versionParts[1]
        $patch = [int]$versionParts[2]
        $calculatedVersion = "$major.$minor.$($patch + 1)"
        Write-Host "ℹ️  Incrementing patch: $prevVersion → $calculatedVersion"
    }
}

$calculatedTag = "${tagPrefix}${calculatedVersion}"

Write-Host "📋 Calculated version: $calculatedVersion"
Write-Host "📋 Calculated tag: $calculatedTag"

Write-GitHubOutput -Name "version" -Value $calculatedVersion
Write-GitHubOutput -Name "tag" -Value $calculatedTag

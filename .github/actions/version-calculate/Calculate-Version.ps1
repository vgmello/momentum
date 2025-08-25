[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$VersionFile = "libs/Momentum/version.txt",
    
    [ValidateSet("stable", "prerelease")]
    [string]$ReleaseType = "stable",
    
    [ValidateSet("true", "false")]
    [string]$CheckChanges = "false",
    
    [ValidateNotNullOrEmpty()]
    [string]$ChangePath = "libs/Momentum/src"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Write-GitHubOutput {
    param([string]$Name, [string]$Value)
    if ($env:GITHUB_OUTPUT) {
        "$Name=$Value" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
    Write-Host "Output: $Name=$Value"
}

function Compare-Version {
    param([string]$Version1, [string]$Version2)
    $v1 = [Version]::Parse($Version1)
    $v2 = [Version]::Parse($Version2)
    return $v1.CompareTo($v2)
}

# Check for consumer-visible changes
$skip = $false
if ($CheckChanges -eq "true") {
    Write-Host "🔍 Checking for consumer-visible changes in $ChangePath..."
    
    try {
        $changedFiles = git diff --name-only HEAD~1 HEAD | Where-Object { $_ -like "$ChangePath*" }
        
        if (-not $changedFiles) {
            Write-Host "No changes in $ChangePath, skipping release"
            $skip = $true
        }
        else {
            $consumerChanges = $changedFiles | Where-Object { 
                $_ -match '\.(cs|csproj|props|targets)$' -and 
                $_ -notmatch '(Test|\.Tests\.|\.md$|\.gitignore$|\.editorconfig$)'
            }
            
            if (-not $consumerChanges) {
                Write-Host "No consumer-visible changes, skipping release"
                $skip = $true
            }
            else {
                Write-Host "✅ Consumer-visible changes detected"
            }
        }
    }
    catch {
        Write-Error "❌ Failed to check for changes: $_"
        exit 1
    }
}

Write-GitHubOutput -Name "skip" -Value $skip.ToString().ToLower()

if ($skip) {
    exit 0
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
elseif ($env:GITHUB_WORKFLOW -eq "Deploy Momentum Template") {
    $tagPrefix = "template-v"
    Write-Host "Using template tag format (detected from workflow: $env:GITHUB_WORKFLOW)"
}
elseif ($env:GITHUB_REF -like "refs/tags/template-*") {
    $tagPrefix = "template-v"
    Write-Host "Using template tag format (detected from tag: $env:GITHUB_REF)"
}
else {
    Write-Host "Using standard tag format"
}

# Find previous releases
$allTags = git tag -l "${tagPrefix}*.*.*" --sort=-v:refname
$prevTag = $allTags | Where-Object { $_ -notmatch 'pre' } | Select-Object -First 1

if (-not $prevTag) {
    Write-Host "No previous regular release found"
    $prevTag = "${tagPrefix}0.0.0"
}

Write-Host "Previous regular release: $prevTag"
$prevVersion = $prevTag -replace "^$tagPrefix", ""

Write-GitHubOutput -Name "prev_tag" -Value $prevTag
Write-GitHubOutput -Name "tag_prefix" -Value $tagPrefix
Write-GitHubOutput -Name "prev_version" -Value $prevVersion

# Check for pre-releases
$latestPrerelease = git tag -l "${tagPrefix}${fileVersion}-pre.*" --sort=-v:refname | Select-Object -First 1
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
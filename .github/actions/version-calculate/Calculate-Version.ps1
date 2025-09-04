[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$VersionFile,

    [string]$Prerelease = 'false',

    [string]$TagPrefix = 'v'
)

# Import Common
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir "../common/Common.ps1")

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Get-FileVersion {
    <#
    .SYNOPSIS
    Gets the version from the version file
    .DESCRIPTION
    Reads the version file and parses it as a semantic version
    .PARAMETER VersionFile
    Path to the version file
    .OUTPUTS
    System.Management.Automation.SemanticVersion
    #>
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

function Get-LatestReleaseForMajor {
    <#
    .SYNOPSIS
    Gets the latest release tag for a specific major version
    .DESCRIPTION
    Searches git tags for the latest release matching the major version
    .PARAMETER MajorVersion
    The major version to search for
    .PARAMETER TagPrefix
    The tag prefix to use when searching
    .OUTPUTS
    System.Management.Automation.SemanticVersion or $null if no tags found
    #>
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

function Get-LatestPrereleaseForVersion {
    <#
    .SYNOPSIS
    Gets the latest pre-release for a specific version
    .DESCRIPTION
    Searches git tags for the latest pre-release matching the base version
    .PARAMETER BaseVersion
    The base version to search for (e.g., "1.0.0")
    .PARAMETER PreReleaseLabel
    The pre-release label to search for (e.g., "alpha", "beta", "preview")
    .PARAMETER TagPrefix
    The tag prefix to use when searching
    .OUTPUTS
    System.Management.Automation.SemanticVersion or $null if no pre-releases found
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$BaseVersion,
        
        [string]$PreReleaseLabel,
        
        [Parameter(Mandatory = $true)]
        [string]$TagPrefix
    )
    
    # Build tag pattern based on whether we have a pre-release label
    if ($PreReleaseLabel) {
        $tagPattern = "${TagPrefix}${BaseVersion}-${PreReleaseLabel}*"
    }
    else {
        $tagPattern = "${TagPrefix}${BaseVersion}-*"
    }
    
    $prereleaseTags = @(git tag -l $tagPattern --sort=-version:refname)
    
    if ($prereleaseTags.Count -eq 0) {
        return $null
    }
    
    # Return the first valid pre-release version found
    foreach ($tag in $prereleaseTags) {
        $versionString = $tag -replace "^${TagPrefix}", ""
        try {
            $version = [System.Management.Automation.SemanticVersion]::Parse($versionString)
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
    <#
    .SYNOPSIS
    Extracts and increments the pre-release number
    .DESCRIPTION
    Parses a pre-release string like "alpha.1" and returns the next number
    .PARAMETER PrereleaseString
    The pre-release string to parse
    .OUTPUTS
    The next pre-release number
    #>
    param(
        [string]$PrereleaseString
    )
    
    if ([string]::IsNullOrEmpty($PrereleaseString)) {
        return 1
    }
    
    # Match patterns like "alpha.1", "beta.2", "preview.3", or just "1"
    if ($PrereleaseString -match '\.(\d+)$') {
        return [int]$Matches[1] + 1
    }
    elseif ($PrereleaseString -match '^(\d+)$') {
        return [int]$Matches[1] + 1
    }
    
    return 1
}

# Main script logic
Write-Host "🔍 Calculating version..."
Write-Host "   Version file: $VersionFile"
Write-Host "   Prerelease: $Prerelease"
Write-Host "   Tag prefix: $TagPrefix"

# Parse the Prerelease parameter as boolean
$IsPrerelease = $Prerelease -eq 'true' -or $Prerelease -eq '1' -or $Prerelease -eq 'yes'

# Get the version from file
$fileVersion = Get-FileVersion -VersionFile $VersionFile

# Get core version components
$baseVersionString = "$($fileVersion.Major).$($fileVersion.Minor).$($fileVersion.Patch)"
$filePreReleaseLabel = $fileVersion.PreReleaseLabel

# Get the latest release for this major version
$latestRelease = Get-LatestReleaseForMajor -MajorVersion $fileVersion.Major -TagPrefix $TagPrefix

# Initialize output variables
$calculatedVersion = $null
$calculatedTag = ""
$previousVersion = ""
$previousTag = ""

# Store previous version info
if ($latestRelease) {
    $previousVersion = $latestRelease.ToString()
    $previousTag = "${TagPrefix}${previousVersion}"
    Write-GitHubOutput -Name "previous-version" -Value $previousVersion
    Write-GitHubOutput -Name "previous-tag" -Value $previousTag
    
    # Find the latest stable version (without pre-release)
    $stableTags = @(git tag -l "${TagPrefix}$($fileVersion.Major).*.*" --sort=-version:refname | Where-Object { $_ -notmatch '-' })
    if ($stableTags.Count -gt 0) {
        $stableVersionString = $stableTags[0] -replace "^${TagPrefix}", ""
        Write-GitHubOutput -Name "previous-stable-version" -Value $stableVersionString
    }
    else {
        Write-GitHubOutput -Name "previous-stable-version" -Value ""
    }
}
else {
    Write-GitHubOutput -Name "previous-version" -Value ""
    Write-GitHubOutput -Name "previous-tag" -Value ""
    Write-GitHubOutput -Name "previous-stable-version" -Value ""
}

# Version calculation logic
if ($IsPrerelease) {
    Write-Host "🔧 Calculating pre-release version..."
    
    # Determine the pre-release label from file version or use default
    $prereleaseLabel = if ($filePreReleaseLabel) { 
        # Extract just the label part (e.g., "alpha" from "alpha.1")
        $labelParts = $filePreReleaseLabel -split '\.'
        $labelParts[0]
    } else { 
        "preview" 
    }
    
    # Check if there's already a stable release with the file version
    $stableTag = "${TagPrefix}${baseVersionString}"
    $stableExists = $null -ne (git tag -l $stableTag 2>$null)
    
    if ($stableExists) {
        Write-Host "ℹ️  Stable version $baseVersionString already exists"
        
        # Need to find next available version for pre-release
        $major = $fileVersion.Major
        $minor = $fileVersion.Minor
        $patch = $fileVersion.Patch
        
        do {
            $patch++
            $nextBaseVersion = "$major.$minor.$patch"
            $nextStableTag = "${TagPrefix}${nextBaseVersion}"
            $nextStableExists = $null -ne (git tag -l $nextStableTag 2>$null)
        } while ($nextStableExists)
        
        $calculatedVersion = [System.Management.Automation.SemanticVersion]::new(
            $major, $minor, $patch, "$prereleaseLabel.1"
        )
        Write-Host "ℹ️  Creating first pre-release for next available version: $calculatedVersion"
    }
    else {
        # Check for existing pre-releases for this version
        $latestPrerelease = Get-LatestPrereleaseForVersion `
            -BaseVersion $baseVersionString `
            -PreReleaseLabel $prereleaseLabel `
            -TagPrefix $TagPrefix
        
        if ($latestPrerelease) {
            # Increment the pre-release number
            $nextNumber = Get-NextPrereleaseNumber -PrereleaseString $latestPrerelease.PreReleaseLabel
            $calculatedVersion = [System.Management.Automation.SemanticVersion]::new(
                $fileVersion.Major, 
                $fileVersion.Minor, 
                $fileVersion.Patch, 
                "$prereleaseLabel.$nextNumber"
            )
            Write-Host "ℹ️  Incrementing pre-release: $latestPrerelease → $calculatedVersion"
        }
        else {
            # First pre-release for this version
            $calculatedVersion = [System.Management.Automation.SemanticVersion]::new(
                $fileVersion.Major, 
                $fileVersion.Minor, 
                $fileVersion.Patch, 
                "$prereleaseLabel.1"
            )
            Write-Host "ℹ️  First pre-release for version $baseVersionString"
        }
    }
}
else {
    Write-Host "🔧 Calculating stable release version..."
    
    # Check if file version is greater than latest release
    if (-not $latestRelease -or $fileVersion.CompareTo($latestRelease) -gt 0) {
        # User wants to create a new version (file version is greater)
        $calculatedVersion = [System.Management.Automation.SemanticVersion]::new(
            $fileVersion.Major, 
            $fileVersion.Minor, 
            $fileVersion.Patch
        )
        
        if ($latestRelease) {
            Write-Host "ℹ️  File version ($fileVersion) > latest release ($latestRelease)"
        }
        else {
            Write-Host "ℹ️  First release for major version $($fileVersion.Major)"
        }
        Write-Host "ℹ️  Creating stable release: $calculatedVersion"
    }
    elseif ($fileVersion.CompareTo($latestRelease) -eq 0 -and $latestRelease.PreReleaseLabel) {
        # File version equals latest, but latest is a pre-release - promote to stable
        $calculatedVersion = [System.Management.Automation.SemanticVersion]::new(
            $fileVersion.Major, 
            $fileVersion.Minor, 
            $fileVersion.Patch
        )
        Write-Host "ℹ️  Promoting pre-release to stable: $latestRelease → $calculatedVersion"
    }
    else {
        # File version is less than or equal to latest stable - increment patch
        if ($latestRelease.PreReleaseLabel) {
            # Latest is a pre-release, get the base version
            $major = $latestRelease.Major
            $minor = $latestRelease.Minor
            $patch = $latestRelease.Patch
        }
        else {
            # Latest is stable, increment patch
            $major = $latestRelease.Major
            $minor = $latestRelease.Minor
            $patch = $latestRelease.Patch + 1
        }
        
        $calculatedVersion = [System.Management.Automation.SemanticVersion]::new($major, $minor, $patch)
        Write-Host "ℹ️  Incrementing version: $latestRelease → $calculatedVersion"
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
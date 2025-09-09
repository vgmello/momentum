[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Tag,

    [string]$PreviousTag = ""
)

# Import Common
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir "../common/Common.ps1")

$ErrorActionPreference = "Stop"

# Generate output filename (sanitize version for filename)
$sanitizedVersion = $Version -replace '[<>:"/\\|?*]', '_'
$OutputFile = "release_notes_${sanitizedVersion}.md"
Write-Host "📝 Generating release notes file: $OutputFile"

# Derive release type from version
try {
    $semVer = [System.Management.Automation.SemanticVersion]::Parse($Version)
    $ReleaseType = if ($semVer.PreReleaseLabel) { "prerelease" } else { "stable" }
    Write-Host "🔍 Derived release type: $ReleaseType (from version: $Version)"
}
catch {
    Write-Host "⚠️  Failed to parse version as semantic version, defaulting to stable"
    $ReleaseType = "stable"
}

# Auto-detect project type from tag pattern
$projectType = "libraries"

if ($Tag -match '^template-v') {
    $projectType = "template"
}

# Get repository information for GitHub links
$repoUrl = git config --get remote.origin.url
if ($repoUrl -match 'github\.com[:/](.+?)(?:\.git)?$') {
    $repoPath = $Matches[1]
    $githubBaseUrl = "https://github.com/$repoPath"
}
else {
    $githubBaseUrl = $null
}

# Function to generate Full Changelog link
function Get-ChangelogLink {
    param($LastRelease, $Tag, $GithubBaseUrl)

    if ($GithubBaseUrl) {
        $changelogUrl = if ($LastRelease) {
            "$GithubBaseUrl/compare/$LastRelease...$Tag"
        }
        else {
            "$GithubBaseUrl/commits/$Tag"
        }
        return "**Full Changelog**: [$changelogUrl]($changelogUrl)"
    }
    else {
        return "**Full Changelog**: $(if ($LastRelease) { $LastRelease } else { 'beginning' })...$Tag"
    }
}

Write-Host "🔍 Auto-detected project type: $projectType (from tag: $Tag)"

# Use provided previous tag
$lastRelease = if (-not [string]::IsNullOrWhiteSpace($PreviousTag)) { $PreviousTag } else { $null }
Write-Host "📌 Previous tag: $(if ($lastRelease) { $lastRelease } else { 'none (first release)' })"

# Get commit information
if ($lastRelease) {
    $commits = @(git log --pretty=format:"- %s (%an)" "${lastRelease}..HEAD" --no-merges |
        Where-Object { $_ -notmatch "skip ci" } |
        Select-Object -First 20)

    $commitCount = git rev-list --count "${lastRelease}..HEAD" --no-merges
    $filesChanged = (git diff --name-only "${lastRelease}..HEAD").Count
}
else {
    $commits = @(git log --pretty=format:"- %s (%an)" --no-merges |
        Where-Object { $_ -notmatch "skip ci" } |
        Select-Object -First 20)

    $commitCount = git rev-list --count HEAD --no-merges
    $filesChanged = "N/A"
}

# Handle case where no commits are found
if ($commits.Count -eq 0) {
    $commits = @("- No significant changes")
}

# Build release notes content
$content = @()

if ($ReleaseType -eq "prerelease") {
    $content += "## What's Changed"
    $content += ""
    $content += $commits
    $content += ""
    $content += (Get-ChangelogLink -LastRelease $lastRelease -Tag $Tag -GithubBaseUrl $githubBaseUrl)
    $content += ""
    $content += "---"
    $content += "📊 **Statistics**: $commitCount commits | $filesChanged files changed"
}
else {
    # Add package list for libraries
    if ($projectType -eq "libraries") {
        $content += ""
        $content += "## Packages Published"
        $content += ""
        $content += "- Momentum.Extensions"
        $content += "- Momentum.ServiceDefaults"
        $content += "- Momentum.Extensions.SourceGenerators"
        $content += "- Momentum.Extensions.EventMarkdownGenerator"
        $content += "- Momentum.ServiceDefaults.Api"
        $content += "- Momentum.Extensions.Abstractions"
        $content += "- Momentum.Extensions.Messaging.Kafka"
        $content += "- Momentum.Extensions.XmlDocs"
    }

    $content += ""
    $content += "## What's Changed"
    $content += ""
    $content += $commits
    $content += ""
    $content += (Get-ChangelogLink -LastRelease $lastRelease -Tag $Tag -GithubBaseUrl $githubBaseUrl)
    $content += ""
    $content += "---"
    $content += "📊 **Statistics**: $commitCount commits | $filesChanged files changed"
}

# Write to file with UTF8 encoding (no BOM)
$content | Out-File -FilePath $OutputFile -Encoding utf8NoBOM

Write-Host "📝 Generated $projectType release notes in $OutputFile"

# Output the filename for the action
Write-GitHubOutput -Name "notes-file" -Value $OutputFile

Write-Host "Contents:"
Get-Content $OutputFile | Write-Host

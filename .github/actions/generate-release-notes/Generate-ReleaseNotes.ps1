[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$Version,

    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$Tag,

    [ValidateSet("stable", "prerelease")]
    [string]$ReleaseType = "stable",

    [string]$OutputFile = "release_notes.md"
)

$ErrorActionPreference = "Stop"

# Auto-detect project type from tag pattern
$projectType = "libraries"
$tagPattern = "v*"
$projectName = "Momentum Libraries"

if ($Tag -match '^template-v') {
    $projectType = "template"
    $tagPattern = "template-v*"
    $projectName = "Momentum Template"
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
        } else {
            "$GithubBaseUrl/commits/$Tag"
        }
        return "**Full Changelog**: [$changelogUrl]($changelogUrl)"
    }
    else {
        return "**Full Changelog**: $(if ($LastRelease) { $LastRelease } else { 'beginning' })...$Tag"
    }
}

Write-Host "🔍 Auto-detected project type: $projectType (from tag: $Tag)"

# Find the last release tag (exclude current tag if it exists)
$allTags = git tag -l $tagPattern --sort=-v:refname
$lastRelease = $allTags | Where-Object { $_ -ne $Tag } | Select-Object -First 1

# Get commit information
if ($lastRelease) {
    $commits = git log --pretty=format:"- %s (%an)" "${lastRelease}..HEAD" --no-merges |
               Where-Object { $_ -notmatch "skip ci" } |
               Select-Object -First 20

    $commitCount = git rev-list --count "${lastRelease}..HEAD" --no-merges
    $filesChanged = (git diff --name-only "${lastRelease}..HEAD").Count
}
else {
    $commits = git log --pretty=format:"- %s (%an)" --no-merges |
               Where-Object { $_ -notmatch "skip ci" } |
               Select-Object -First 20

    $commitCount = git rev-list --count HEAD --no-merges
    $filesChanged = "N/A"
}

# Build release notes content
$content = @()

if ($ReleaseType -eq "prerelease") {
    $content += "# ${projectName} ${Version}"
    $content += ""
    $content += "🚧 **Pre-release Version**"
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
else {
    $content += "# ${projectName} v${Version}"

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

# Write to file
$content | Out-File -FilePath $OutputFile -Encoding utf8

Write-Host "📝 Generated $projectType release notes in $OutputFile"
Write-Host "Contents:"
Get-Content $OutputFile | Write-Host

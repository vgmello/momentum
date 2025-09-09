[CmdletBinding()]
param(
    [string]$DeployType = ""
)

# Import Common
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir "../common/Common.ps1")

$ErrorActionPreference = "Stop"

# Set defaults
$deployPrerelease = $false
$deployDocs = $false

# Set prerelease flag (only when explicitly needed)
$githubEventName = $env:GITHUB_EVENT_NAME
$githubRef = $env:GITHUB_REF
if (($githubEventName -eq "push" -and $githubRef -eq "refs/heads/main") -or
    ($githubEventName -eq "workflow_dispatch" -and $DeployType -like "*prerelease")) {
    # We want all commits to main that are not a explicit 'release' tag to be a pre-release
    $deployPrerelease = $true
}

# Set docs flag for library releases
$githubRefName = $env:GITHUB_REF_NAME
if ($githubRefName -eq "release" -or $githubEventName -eq "workflow_dispatch" -and $DeployType -eq "release") {
    $deployDocs = $true
}

# Output configuration
Write-GitHubOutput -Name "prerelease" -Value $deployPrerelease.ToString().ToLower()
Write-GitHubOutput -Name "deploy-docs" -Value $deployDocs.ToString().ToLower()

Write-Host "### Deployment Configuration"
Write-Host "- Prerelease: $deployPrerelease | Docs: $deployDocs"
Write-Host "- Trigger: $githubEventName | Ref: $githubRef | Ref Name: $githubEventName | Type: $DeployType"

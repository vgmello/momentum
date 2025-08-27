[CmdletBinding()]
param(
    [string]$DeployType = "",
    [string]$NugetSourceOverride = "",
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$NugetApiKey,
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$NugetTestApiKey
)

$ErrorActionPreference = "Stop"

function Write-GitHubOutput {
    param([string]$Name, [string]$Value)
    if ($env:GITHUB_OUTPUT) {
        "$Name=$Value" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
    Write-Host "Output: $Name=$Value"
}

# Initialize variables (defaults)
$deployPrerelease = $false
$deployDocs = $false
$isTest = $false
$nugetSource = "https://api.nuget.org/v3/index.json"

# Test deployments
if ($DeployType -like "test-*") {
    $isTest = $true
    $nugetSource = "https://apiint.nugettest.org/v3/index.json"
    if (-not [string]::IsNullOrWhiteSpace($NugetSourceOverride)) {
        $nugetSource = $NugetSourceOverride
    }
}

# Set prerelease flag (only when explicitly needed)
$githubEventName = $env:GITHUB_EVENT_NAME
$githubRef = $env:GITHUB_REF

if (($githubEventName -eq "push" -and $githubRef -eq "refs/heads/main") -or
    ($githubEventName -eq "workflow_dispatch" -and $DeployType -like "*prerelease")) {
    $deployPrerelease = $true
}

# Set docs flag for library releases
$githubRefName = $env:GITHUB_REF_NAME
if ($githubRef -match '^refs/tags/v' -or
    $githubRef -match '^refs/tags/template-v' -or
    $githubRefName -eq "release" -or
    $DeployType -eq "docs-only") {
    $deployDocs = $true
}

# API key selection
$apiKey = if ($isTest) { $NugetTestApiKey } else { $NugetApiKey }

# Output configuration
Write-GitHubOutput -Name "deploy-prerelease" -Value $deployPrerelease.ToString().ToLower()
Write-GitHubOutput -Name "deploy-docs" -Value $deployDocs.ToString().ToLower()
Write-GitHubOutput -Name "is-test" -Value $isTest.ToString().ToLower()
Write-GitHubOutput -Name "nuget-source" -Value $nugetSource
Write-GitHubOutput -Name "nuget-api-key" -Value $apiKey

# Debug logging
Write-Host "### Deployment Configuration"
Write-Host "- Prerelease: $deployPrerelease | Docs: $deployDocs | Test: $isTest"
Write-Host "- Source: $nugetSource"
Write-Host "- Trigger: $githubEventName | Ref: $githubRef | Type: $DeployType"

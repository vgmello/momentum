param(
    [string]$DeployPrerelease = "true",
    [string]$TagPrefix = "template-v"
)

function Write-GitHubOutput {
    param([string]$Name, [string]$Value)
    if ($env:GITHUB_OUTPUT) {
        "$Name=$Value" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
    Write-Host "Output: $Name=$Value"
}

Write-Host "Using template tag format with prefix: $TagPrefix"

$tagSuffix = if ($DeployPrerelease -eq "true") { "-pre" } else { "" }

# Find latest template release tag (excluding pre-releases)
$allTags = git tag -l "${TagPrefix}*.*.*" --sort=-v:refname
$currentTag = $allTags | Where-Object { $_ -notmatch 'pre' } | Select-Object -First 1

if (-not $currentTag) {
    Write-Host "No previous template release found"
    $currentTag = "${TagPrefix}0.0.1"
}

Write-Host "Current template release: $currentTag"
$currentVersion = $currentTag -replace "^$TagPrefix", ""

Write-GitHubOutput -Name "current_version" -Value $currentVersion
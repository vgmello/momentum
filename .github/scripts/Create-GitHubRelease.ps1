[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$Tag,
    
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$Title,
    
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$NotesFile,
    
    [switch]$Prerelease,
    
    [switch]$Draft,
    
    [ValidateNotNullOrEmpty()]
    [string]$Target = "main"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Check if gh CLI is available
$ghVersion = gh --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ GitHub CLI (gh) is not installed or not in PATH"
    exit 1
}

# Check if notes file exists
if (-not (Test-Path $NotesFile)) {
    Write-Error "❌ Release notes file not found: $NotesFile"
    exit 1
}

# Build gh release create command
$releaseArgs = @(
    "release", "create", $Tag,
    "--title", $Title,
    "--notes-file", $NotesFile,
    "--target", $Target
)

if ($Prerelease) {
    $releaseArgs += "--prerelease"
}

if (-not $Draft) {
    $releaseArgs += "--draft=false"
}

Write-Host "Creating GitHub release..."
Write-Host "  Tag: $Tag"
Write-Host "  Title: $Title"
Write-Host "  Notes: $NotesFile"
Write-Host "  Target: $Target"
Write-Host "  Prerelease: $($Prerelease.IsPresent)"
Write-Host "  Draft: $($Draft.IsPresent)"

# Execute gh command
& gh @releaseArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Failed to create GitHub release"
    exit 1
}

Write-Host "✅ GitHub release created successfully"
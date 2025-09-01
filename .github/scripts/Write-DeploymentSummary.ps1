param(
    [switch]$DeployPrerelease,
    [string]$ReleaseVersion = "",
    [string]$ReleaseTag = "",
    [string]$NugetSource = "",
    [switch]$DeployDocs,
    [string]$DocsUrl = "",
    [string]$DocsPath = ""
)

Write-Host "# 📊 Deployment Summary"
Write-Host ""

if ($DeployPrerelease) {
    if (-not [string]::IsNullOrWhiteSpace($PrereleaseVersion)) {
        Write-Host "✅ **Pre-release deployed**"
        Write-Host "   - Version: $PrereleaseVersion"
        Write-Host "   - Tag: $PrereleaseTag"
        Write-Host "   - NuGet: $NugetSource"
        if (-not $IsTest) {
            Write-Host "   - GitHub Release: Created"
        }
    }
    else {
        Write-Host "❌ **Pre-release failed**"
    }
}

if (-not $DeployPrerelease) {
    if (-not [string]::IsNullOrWhiteSpace($ReleaseVersion)) {
        Write-Host "✅ **Release deployed**"
        Write-Host "   - Version: $ReleaseVersion"
        Write-Host "   - Tag: $ReleaseTag"
        Write-Host "   - NuGet: $NugetSource"
        if (-not $IsTest) {
            Write-Host "   - GitHub Release: Created"
        }
    }
    else {
        Write-Host "❌ **Release failed**"
    }
}

if ($DeployDocs) {
    if (-not [string]::IsNullOrWhiteSpace($DocsUrl)) {
        Write-Host "✅ **Documentation deployed**"
        Write-Host "   - URL: $DocsUrl"
        if (-not [string]::IsNullOrWhiteSpace($DocsPath)) {
            Write-Host "   - Source: $DocsPath"
        }
    }
    else {
        Write-Host "❌ **Documentation failed**"
    }
}

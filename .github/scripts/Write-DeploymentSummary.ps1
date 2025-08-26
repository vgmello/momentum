param(
    [string]$IsTest = "false",
    [string]$DeployPrerelease = "false",
    [string]$PrereleaseSkip = "false",
    [string]$PrereleaseVersion = "",
    [string]$PrereleaseTag = "",
    [string]$ReleaseVersion = "",
    [string]$ReleaseTag = "",
    [string]$NugetSource = "",
    [string]$DeployDocs = "false",
    [string]$DocsUrl = "",
    [string]$DocsPath = ""
)

Write-Host "# 📊 Deployment Summary"
Write-Host ""

if ($IsTest -eq "true") {
    Write-Host "🧪 **Test Deployment Mode**"
    Write-Host "   - NuGet Source: $NugetSource"
    Write-Host ""
}

if ($DeployPrerelease -eq "true") {
    if ($PrereleaseSkip -eq "true") {
        Write-Host "⏭️ Pre-release skipped (no consumer-visible changes)"
    }
    elseif ($PrereleaseVersion) {
        Write-Host "✅ **Pre-release deployed**"
        Write-Host "   - Version: $PrereleaseVersion"
        Write-Host "   - Tag: $PrereleaseTag"
        Write-Host "   - NuGet: $NugetSource"
        if ($IsTest -ne "true") {
            Write-Host "   - GitHub Release: Created"
        }
    }
    else {
        Write-Host "❌ **Pre-release failed**"
    }
}

if ($DeployPrerelease -eq "false") {
    if ($ReleaseVersion) {
        Write-Host "✅ **Release deployed**"
        Write-Host "   - Version: $ReleaseVersion"
        Write-Host "   - Tag: $ReleaseTag"
        Write-Host "   - NuGet: $NugetSource"
        if ($IsTest -ne "true") {
            Write-Host "   - GitHub Release: Created"
        }
    }
    else {
        Write-Host "❌ **Release failed**"
    }
}

if ($DeployDocs -eq "true") {
    if ($DocsUrl) {
        Write-Host "✅ **Documentation deployed**"
        Write-Host "   - URL: $DocsUrl"
        if ($DocsPath) {
            Write-Host "   - Source: $DocsPath"
        }
    }
    else {
        Write-Host "❌ **Documentation failed**"
    }
}
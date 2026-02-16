param(
    [switch]$DeployPrerelease,
    [string]$ReleaseVersion = "",
    [string]$ReleaseTag = "",
    [string]$NugetSource = "",
    [switch]$DeployDocs,
    [string]$DocsUrl = "",
    [string]$DocsPath = ""
)

$summary = [System.Text.StringBuilder]::new()
[void]$summary.AppendLine("# 📊 Deployment Summary")
[void]$summary.AppendLine("")

if ($DeployPrerelease) {
    if (-not [string]::IsNullOrWhiteSpace($ReleaseVersion)) {
        [void]$summary.AppendLine("✅ **Pre-release deployed**")
        [void]$summary.AppendLine("   - Version: ``$ReleaseVersion``")
        [void]$summary.AppendLine("   - Tag: ``$ReleaseTag``")
        [void]$summary.AppendLine("   - NuGet: $NugetSource")
        [void]$summary.AppendLine("   - GitHub Release: Created")
    }
    else {
        [void]$summary.AppendLine("❌ **Pre-release failed**")
    }
}

if (-not $DeployPrerelease) {
    if (-not [string]::IsNullOrWhiteSpace($ReleaseVersion)) {
        [void]$summary.AppendLine("✅ **Release deployed**")
        [void]$summary.AppendLine("   - Version: ``$ReleaseVersion``")
        [void]$summary.AppendLine("   - Tag: ``$ReleaseTag``")
        [void]$summary.AppendLine("   - NuGet: $NugetSource")
        [void]$summary.AppendLine("   - GitHub Release: Created")
    }
    else {
        [void]$summary.AppendLine("❌ **Release failed**")
    }
}

if ($DeployDocs) {
    if (-not [string]::IsNullOrWhiteSpace($DocsUrl)) {
        [void]$summary.AppendLine("✅ **Documentation deployed**")
        [void]$summary.AppendLine("   - URL: $DocsUrl")
        if (-not [string]::IsNullOrWhiteSpace($DocsPath)) {
            [void]$summary.AppendLine("   - Source: ``$DocsPath``")
        }
    }
    else {
        [void]$summary.AppendLine("❌ **Documentation failed**")
    }
}

$content = $summary.ToString()

# Write to console for log visibility
Write-Host $content

# Write to GitHub Actions Job Summary
if ($env:GITHUB_STEP_SUMMARY) {
    $content | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}

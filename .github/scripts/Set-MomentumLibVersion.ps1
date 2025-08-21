param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [string]$FilePath = "Directory.Packages.props"
)

Write-Host "Updating $FilePath with version: $Version"

# Read the file content
$content = Get-Content $FilePath -Raw

# Replace the version placeholder
$updatedContent = $content -replace '\$\(TemplateMomentumVersion\)', $Version

# Write the updated content back
$updatedContent | Set-Content $FilePath -Encoding utf8

Write-Host "✅ Updated $FilePath with version: $Version"
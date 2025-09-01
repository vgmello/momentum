[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$Path,

    [string]$Config = "Release",

    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$PackageVersion,

    [string[]]$PackArgs = @(),

    [Parameter(Mandatory=$true)]
    [ValidateNotNullOrEmpty()]
    [string]$NugetApiKey,

    [string]$NugetSource = "https://api.nuget.org/v3/index.json",

    [switch]$SkipDuplicate,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Write-GitHubOutput {
    param([string]$Name, [string]$Value)
    if ($env:GITHUB_OUTPUT) {
        "$Name=$Value" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
    Write-Host "Output: $Name=$Value"
}

function Write-GitHubMultilineOutput {
    param([string]$Name, [string[]]$Values)
    if ($env:GITHUB_OUTPUT) {
        "$Name<<EOF" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
        $Values | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
        "EOF" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
}

$nugetOutputDir = "./_tmp_nuget_output"

Write-Host "🔍 Debug information:"
Write-Host "   Current directory: $(Get-Location)"
Write-Host "   Target path: $Path"
Write-Host "   Package version: $PackageVersion"
Write-Host "   Configuration: $Config"

if (-not (Test-Path $Path)) {
    Write-Error "❌ Target path '$Path' does not exist"
    Write-Host "   Working directory contents:"
    Get-ChildItem | Format-Table Name
    exit 1
}

Write-Host "🧹 Cleaning output directory: $nugetOutputDir"
if (Test-Path $nugetOutputDir) {
    Remove-Item -Path $nugetOutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $nugetOutputDir | Out-Null

Write-Host "📦 Packing packages with version $PackageVersion..."

$packArguments = @(
    "pack",
    $Path,
    "--configuration", $Config,
    "--no-build",
    "-p:PackageVersion=$PackageVersion",
    "--output", $nugetOutputDir
)

if ($PackArgs.Count -gt 0) {
    $packArguments += $PackArgs
}

$commandDisplay = "dotnet " + ($packArguments -join " ")
Write-Host "Executing: $commandDisplay"
& dotnet @packArguments

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Package creation failed"
    exit 1
}

Write-Host "✅ Packages created successfully in $nugetOutputDir"
Write-GitHubOutput -Name "output_dir" -Value $nugetOutputDir

Write-Host "🔍 Finding NuGet packages in '$nugetOutputDir'..."

$packages = Get-ChildItem -Path $nugetOutputDir -Filter "*.nupkg" | Sort-Object Name

if ($packages.Count -eq 0) {
    Write-Error "❌ No packages found to publish in $nugetOutputDir"
    Write-Host "Directory contents:"
    Get-ChildItem $nugetOutputDir | Format-Table Name
    exit 1
}

Write-Host "Found packages:"
foreach ($pkg in $packages) {
    Write-Host "  - $($pkg.Name)"
}

Write-GitHubMultilineOutput -Name "packages" -Values ($packages | ForEach-Object { $_.FullName })

# Publish to NuGet
if (-not $DryRun) {
    Write-Host "🚀 Publishing packages..."
    Write-Host "   Source: $NugetSource"

    $publishedList = @()
    $successCount = 0
    $totalCount = $packages.Count
    $currentPackage = 0

    foreach ($package in $packages) {
        $currentPackage++
        $pkgName = $package.Name
        Write-Host ""
        Write-Host "[$currentPackage/$totalCount] Publishing $pkgName..."

        $pushArgs = @(
            "nuget", "push", $package.FullName,
            "--source", $NugetSource,
            "--no-symbols"
        )

        if ($SkipDuplicate) {
            $pushArgs += "--skip-duplicate"
        }

        # Add API key last to make it easier to filter from logs
        $pushArgs += "--api-key", $NugetApiKey

        # Execute without displaying the full command (which contains the API key)
        $result = & dotnet @pushArgs 2>&1
        $exitCode = $LASTEXITCODE

        if ($exitCode -eq 0) {
            Write-Host "   ✅ Successfully published $pkgName"
            $publishedList += $pkgName
            $successCount++
        }
        elseif ($exitCode -eq 1 -and $SkipDuplicate -and $result -match "already exists") {
            Write-Host "   ⏭️ Skipped $pkgName (may already exist)"
            $successCount++
        }
        else {
            Write-Error "   ❌ Failed to publish $pkgName"
            Write-Host $result
            exit 1
        }
    }

    Write-Host ""
    Write-Host "🎉 Published $successCount/$totalCount packages !"
    Write-GitHubMultilineOutput -Name "packages" -Values $publishedList
}
else {
    # Dry run summary
    Write-Host "🔍 DRY RUN - The following packages would be published:"
    foreach ($package in $packages) {
        Write-Host "  - $($package.Name)"
    }
    Write-Host ""
    Write-Host "Target: $NugetSource"
}

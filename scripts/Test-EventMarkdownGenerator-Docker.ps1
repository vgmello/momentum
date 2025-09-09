#!/usr/bin/env pwsh

<#
.SYNOPSIS
Tests the EventMarkdownGenerator tool in Docker using a locally built package.

.DESCRIPTION
This script builds the EventMarkdownGenerator tool as a local NuGet package,
then tests it in a Docker container to verify it works correctly when installed
as a global tool.

.EXAMPLE
./scripts/Test-EventMarkdownGenerator-Docker.ps1
#>

param(
    [switch]$SkipBuild,
    [switch]$KeepPackage
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Define paths
$repoRoot = Split-Path -Parent $PSScriptRoot
$toolProject = Join-Path $repoRoot "libs/Momentum/src/Momentum.Extensions.EventMarkdownGenerator"
$docsDir = Join-Path $repoRoot "docs"
$tempDir = Join-Path $repoRoot ".test-docker"

Write-Host "Testing EventMarkdownGenerator in Docker..." -ForegroundColor Cyan

try {
    # Create temp directory
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $tempDir | Out-Null

    if (-not $SkipBuild) {
        # Build and pack the tool
        Write-Host "`nBuilding and packing EventMarkdownGenerator..." -ForegroundColor Yellow
        Push-Location $toolProject
        try {
            dotnet build --configuration Release
            dotnet pack --configuration Release --output $tempDir
        }
        finally {
            Pop-Location
        }
    }

    # Copy the test Dockerfile
    Copy-Item -Path (Join-Path $docsDir "Dockerfile.test") -Destination $tempDir

    # Build Docker image
    Write-Host "`nBuilding Docker image with local tool..." -ForegroundColor Yellow
    Push-Location $tempDir
    try {
        # Copy necessary files for Docker build context
        Copy-Item -Path (Join-Path $docsDir "package.json") -Destination $tempDir
        Copy-Item -Path (Join-Path $docsDir "pnpm-lock.yaml") -Destination $tempDir

        # Create docs directory structure for Docker context
        New-Item -ItemType Directory -Path (Join-Path $tempDir "docs") -Force | Out-Null
        Move-Item -Path (Join-Path $tempDir "package.json") -Destination (Join-Path $tempDir "docs")
        Move-Item -Path (Join-Path $tempDir "pnpm-lock.yaml") -Destination (Join-Path $tempDir "docs")

        # Build the Docker image
        docker build -f Dockerfile.test -t momentum-docsgen-test .

        Write-Host "`nDocker image built successfully!" -ForegroundColor Green
        Write-Host "The events-docsgen tool was successfully installed and tested in Docker." -ForegroundColor Green

        # Test the tool version in the container
        Write-Host "`nTesting tool version in container..." -ForegroundColor Yellow
        docker run --rm momentum-docsgen-test events-docsgen --version
    }
    finally {
        Pop-Location
    }
}
finally {
    # Cleanup
    if (-not $KeepPackage -and (Test-Path $tempDir)) {
        Write-Host "`nCleaning up temporary files..." -ForegroundColor Yellow
        Remove-Item -Path $tempDir -Recurse -Force
    }
}

Write-Host "`nDocker test completed successfully!" -ForegroundColor Green
#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Simple test for Momentum template that works with .NET 8
#>

param(
    [string]$TestName = "TestTemplate",
    [string]$Parameters = ""
)

$ErrorActionPreference = 'Stop'

Write-Host "Testing template: $TestName with parameters: $Parameters"

# Create test directory
$testDir = "_test_$TestName"
if (Test-Path $testDir) {
    Remove-Item -Path $testDir -Recurse -Force
}

try {
    # Generate template
    Write-Host "Generating template..."
    $templateArgs = @('new', 'mmt', '-n', $TestName, '--allow-scripts', 'yes', '--output', $testDir)
    if ($Parameters) {
        $templateArgs += ($Parameters -split '\s+' | Where-Object { $_ })
    }
    
    & dotnet $templateArgs 2>&1 | Out-String | Write-Host
    
    if ($LASTEXITCODE -ne 0) {
        throw "Template generation failed"
    }
    
    # Find and build projects (skip .slnx files for .NET 8)
    Write-Host "Building projects..."
    $projects = Get-ChildItem -Path $testDir -Filter "*.csproj" -Recurse | 
                Where-Object { $_.FullName -notmatch "post-setup" }
    
    $buildFailed = $false
    foreach ($project in $projects) {
        Write-Host "Building: $($project.Name)"
        & dotnet build $project.FullName --verbosity quiet 2>&1 | Out-String | Write-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build failed for: $($project.Name)" -ForegroundColor Red
            $buildFailed = $true
            break
        }
    }
    
    if ($buildFailed) {
        throw "Build failed"
    }
    
    Write-Host "Test passed!" -ForegroundColor Green
    return $true
}
catch {
    Write-Host "Test failed: $_" -ForegroundColor Red
    return $false
}
finally {
    # Cleanup
    if (Test-Path $testDir) {
        Remove-Item -Path $testDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
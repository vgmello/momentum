[CmdletBinding()]
param(
    [string]$DocsPath = "libs/Momentum/docs",
    [switch]$CI
)

# Import Common
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir "../common/Common.ps1")

$ErrorActionPreference = "Stop"

Write-Host "📦 Installing DocFX..."
dotnet tool install -g docfx --verbosity normal

# Ensure .NET tools are in PATH
$dotnetToolsPath = Join-Path $env:HOME ".dotnet/tools"
$env:PATH = "$env:PATH;$dotnetToolsPath"

if ($env:GITHUB_PATH) {
    $dotnetToolsPath | Out-File -FilePath $env:GITHUB_PATH -Append -Encoding utf8
}

Write-Host "📋 Verifying DocFX installation..."
try {
    $docfxVersion = docfx --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "DocFX returned non-zero exit code"
    }
    Write-Host "✅ DocFX version: $docfxVersion"
}
catch {
    Write-Error "❌ DocFX installation failed or not in PATH"
    Write-Host "Diagnostic information:"
    Write-Host "   PATH: $env:PATH"
    Write-Host "   HOME: $env:HOME"

    if (Test-Path $dotnetToolsPath) {
        Write-Host "   Tools directory contents:"
        Get-ChildItem $dotnetToolsPath | Format-Table Name
    }
    else {
        Write-Host "   Tools directory not found: $dotnetToolsPath"
    }
    exit 1
}

# Change to docs directory
Push-Location $DocsPath

try {
    Write-Host "📦 Installing dependencies with Bun..."
    if ($CI) {
        bun install --frozen-lockfile
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "⚠️ Lockfile is out of sync with package.json. Retrying without --frozen-lockfile to keep docs deployment running."
            bun install
        }
    } else {
        bun install
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Dependency installation failed"
        exit 1
    }

    Write-Host "📚 Building documentation with VitePress..."
    bun run docs:build

    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ Documentation build failed"
        exit 1
    }

    Write-Host "✅ Documentation built successfully"

    # Set output paths
    $distPath = Join-Path (Get-Location) ".vitepress/dist"
    Write-GitHubOutput -Name "dist" -Value $distPath
}
finally {
    Pop-Location
}

[CmdletBinding()]
param(
    [string]$DocsPath = "libs/Momentum/docs",
    # Pinned: docfx is a global tool Dependabot doesn't track, and unpinned installs picked up 2.81.0,
    # whose generated reference markdown fails the VitePress (Vue) build. Bump deliberately, together with
    # docs/Dockerfile, libs/Momentum/docs/Dockerfile and the install hints in both docs-dotnet.ts scripts.
    [string]$DocfxVersion = "2.78.5",
    [switch]$CI
)

# Import Common
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptDir "../common/Common.ps1")

$ErrorActionPreference = "Stop"

Write-Host "📦 Installing DocFX..."
dotnet tool install -g docfx --version $DocfxVersion --verbosity normal
if ($LASTEXITCODE -ne 0) {
    # Already installed (possibly at another version): move it to the pinned one
    dotnet tool update -g docfx --version $DocfxVersion --allow-downgrade --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install DocFX $DocfxVersion"
    }
}

# Ensure .NET tools are in PATH
$dotnetToolsPath = Join-Path $env:HOME ".dotnet/tools"
# Prepend so the pinned global tool wins over any other docfx already on PATH
$env:PATH = "$dotnetToolsPath$([IO.Path]::PathSeparator)$env:PATH"

if ($env:GITHUB_PATH) {
    $dotnetToolsPath | Out-File -FilePath $env:GITHUB_PATH -Append -Encoding utf8
}

Write-Host "📋 Verifying DocFX installation..."
try {
    # Not "$docfxVersion": PowerShell variable names are case-insensitive, so that would overwrite the
    # $DocfxVersion parameter and the check below would compare the output with itself
    $installedDocfx = docfx --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "DocFX returned non-zero exit code"
    }
    if (-not "$installedDocfx".StartsWith($DocfxVersion)) {
        throw "Expected DocFX $DocfxVersion but found $installedDocfx"
    }
    Write-Host "✅ DocFX version: $installedDocfx"
}
catch {
    # Write-Host before Write-Error: under ErrorActionPreference=Stop, Write-Error terminates immediately
    Write-Host "❌ DocFX installation failed or not in PATH: $($_.Exception.Message)"
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
    Write-Error "❌ DocFX installation failed or not in PATH"
    exit 1
}

# Change to docs directory
Push-Location $DocsPath

try {
    Write-Host "📦 Installing dependencies with Bun..."
    if ($CI) {
        bun install --frozen-lockfile 2>&1 | Tee-Object -Variable installOutput
        if ($LASTEXITCODE -ne 0) {
            if (($installOutput -join "`n") -match "lockfile had changes, but lockfile is frozen") {
                Write-Warning "⚠️ Lockfile is out of sync with package.json. Retrying without --frozen-lockfile. Please run 'bun install' locally and commit the updated lockfile."
                bun install
                if ($LASTEXITCODE -ne 0) {
                    Write-Error "❌ Dependency installation failed after retrying without frozen lockfile"
                    exit 1
                }
            }
            else {
                Write-Error "❌ Dependency installation failed"
                exit 1
            }
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

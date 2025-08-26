param(
    [string]$NodeVersion = "22",
    [string]$PnpmVersion = "9",
    [string]$DocsPath = "libs/Momentum/docs",
    [string]$BuildDotnet = "true"
)

function Write-GitHubOutput {
    param([string]$Name, [string]$Value)
    if ($env:GITHUB_OUTPUT) {
        "$Name=$Value" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
    Write-Host "Output: $Name=$Value"
}

# Build .NET projects if requested
if ($BuildDotnet -eq "true") {
    Write-Host "📦 Building .NET projects..."
    dotnet build libs/Momentum/Momentum.slnx --configuration Release
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ .NET build failed"
        exit 1
    }
}

# Install DocFX
Write-Host "📦 Installing DocFX..."
dotnet tool install -g docfx --verbosity normal

# Ensure .NET tools are in PATH
$dotnetToolsPath = Join-Path $env:HOME ".dotnet/tools"
$env:PATH = "$env:PATH;$dotnetToolsPath"

if ($env:GITHUB_PATH) {
    $dotnetToolsPath | Out-File -FilePath $env:GITHUB_PATH -Append -Encoding utf8
}

Write-Host "📋 Verifying DocFX installation..."
$docfxVersion = docfx --version 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ DocFX installation failed or not in PATH"
    Write-Host "PATH: $env:PATH"
    Write-Host "HOME: $env:HOME"
    
    if (Test-Path $dotnetToolsPath) {
        Write-Host "Tools directory contents:"
        Get-ChildItem $dotnetToolsPath | Format-Table Name
    }
    else {
        Write-Host "Tools directory not found"
    }
    exit 1
}

Write-Host "DocFX version: $docfxVersion"

# Change to docs directory
Push-Location $DocsPath

try {
    # Install npm dependencies
    Write-Host "📦 Installing npm dependencies..."
    pnpm install --no-frozen-lockfile
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "❌ npm dependency installation failed"
        exit 1
    }
    
    # Build documentation
    Write-Host "📚 Building documentation with VitePress..."
    pnpm docs:build
    
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
#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Run JetBrains code inspection on the solution.

.DESCRIPTION
    Runs JetBrains inspectcode CLI to analyze code quality, producing the same
    diagnostics visible in Rider/ReSharper from the command line.

    Requires: dotnet tool restore (installs JetBrains.ReSharper.GlobalTools)

.PARAMETER Solution
    Solution file to inspect. Defaults to AppDomain.slnx.

.PARAMETER Severity
    Minimum severity level to report. Valid values: INFO, HINT, SUGGESTION, WARNING, ERROR.
    Defaults to SUGGESTION.

.PARAMETER Format
    Output format. Valid values: Text, Xml, Html, Sarif.
    Defaults to Text (console-friendly).

.PARAMETER Output
    Output file path. If not specified, writes to a temp file and displays to console.

.PARAMETER Include
    Wildcard pattern to include specific files (e.g., "src/AppDomain/**/*.cs").

.PARAMETER Exclude
    Wildcard pattern to exclude files from analysis.

.PARAMETER WarningsOnly
    Only show warnings and errors (shortcut for -Severity WARNING).

.EXAMPLE
    ./scripts/Run-CodeInspection.ps1

.EXAMPLE
    ./scripts/Run-CodeInspection.ps1 -Severity WARNING

.EXAMPLE
    ./scripts/Run-CodeInspection.ps1 -Solution libs/Momentum/Momentum.slnx

.EXAMPLE
    ./scripts/Run-CodeInspection.ps1 -Include "src/AppDomain.Api/**/*.cs"
#>

param(
    [string]$Solution = "AppDomain.slnx",
    [ValidateSet("INFO", "HINT", "SUGGESTION", "WARNING", "ERROR")]
    [string]$Severity = "SUGGESTION",
    [ValidateSet("Text", "Xml", "Html", "Sarif")]
    [string]$Format = "Text",
    [string]$Output,
    [string]$Include,
    [string]$Exclude,
    [switch]$WarningsOnly
)

$ErrorActionPreference = "Stop"

# Ensure we're in the repo root
$repoRoot = git rev-parse --show-toplevel 2>$null
if ($repoRoot) { Push-Location $repoRoot }

try {
    # Ensure tools are restored
    Write-Host "Restoring dotnet tools..." -ForegroundColor Cyan
    dotnet tool restore 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to restore dotnet tools. Run 'dotnet tool restore' manually." -ForegroundColor Red
        exit 1
    }

    # Determine severity
    $effectiveSeverity = if ($WarningsOnly) { "WARNING" } else { $Severity }

    # Build output path
    $outputFile = if ($Output) {
        $Output
    } else {
        $ext = switch ($Format) {
            "Text" { "txt" }
            "Xml" { "xml" }
            "Html" { "html" }
            "Sarif" { "sarif.json" }
        }
        [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "inspectcode-results.$ext")
    }

    # Build command arguments
    $inspectArgs = @(
        "jb", "inspectcode", $Solution,
        "--output=$outputFile",
        "--format=$Format",
        "--severity=$effectiveSeverity"
    )

    if ($Include) { $inspectArgs += "--include=$Include" }
    if ($Exclude) { $inspectArgs += "--exclude=$Exclude" }

    Write-Host "Running code inspection on $Solution (severity: $effectiveSeverity)..." -ForegroundColor Cyan
    Write-Host ""

    # Discard stderr (JetBrains Roslyn analyzer compat noise), filter progress lines from stdout
    & dotnet @inspectArgs 2>$null | Where-Object { $_ -notmatch "^Inspecting " }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Inspection failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    # Display results
    if (-not $Output -and $Format -eq "Text" -and (Test-Path $outputFile)) {
        Write-Host ""
        Get-Content $outputFile
        Write-Host ""

        $lineCount = (Get-Content $outputFile | Measure-Object -Line).Lines
        $issueCount = (Get-Content $outputFile | Where-Object { $_ -match "^\s{6}" } | Measure-Object -Line).Lines
        Write-Host "Found $issueCount issue(s) across the solution." -ForegroundColor $(if ($issueCount -eq 0) { "Green" } else { "Yellow" })
    } else {
        Write-Host "Report written to: $outputFile" -ForegroundColor Green
    }
} finally {
    if ($repoRoot) { Pop-Location }
}

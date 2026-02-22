#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Momentum Template Test Suite

.DESCRIPTION
    Comprehensive test suite for the Momentum .NET template (mmt) covering all critical scenarios:

    Categories:
    • component-isolation    - Test each component in isolation
    • port-config           - Test port boundaries and common conflicts
    • org-names             - Validate special characters handling
    • library-config        - Test library reference combinations
    • real-world-patterns   - Validate common deployment scenarios
    • orleans-combinations  - Test stateful processing configurations
    • edge-cases           - Test boundary conditions and special modes

    Features:
    • 7 test categories with ~20 parametrized tests
    • Parallel execution by default (4 concurrent tests)
    • Immediate cleanup after each test to conserve disk space
    • Selective preservation of failed tests for debugging
    • Early exit on maximum failures
    • Comprehensive logging and colored output

.PARAMETER Category
    Run specific test category. Valid values:
    • component-isolation    - Test each component in isolation
    • port-config           - Test port boundaries and common conflicts
    • org-names             - Validate special characters handling
    • library-config        - Test library reference combinations
    • real-world-patterns   - Validate common deployment scenarios
    • orleans-combinations  - Test stateful processing configurations
    • edge-cases           - Test boundary conditions and special modes

.PARAMETER ListCategories
    List all available test categories with descriptions

.PARAMETER KeepResults
    Keep all test results after execution (disables cleanup)

.PARAMETER KeepResultsWithErrors
    Keep test results only when errors occur (default: true)
    Ignored if KeepResults is specified.
    Individual test cleanup happens immediately - this only affects failed tests.

.PARAMETER MaxFailures
    Maximum number of test failures before early exit (default: 0)
    • 0 = run all tests regardless of failures
    • N = exit after N failures

.PARAMETER IncludeE2E
    Include E2E tests in the test run. By default, E2E tests are excluded.

.PARAMETER SkipRestoreTemplate
    Skip post-test template restoration (default: false).
    When absent (default): uninstalls the clean export template, deletes its directory, and
    reinstalls the template from the repo root so the developer's environment is ready.
    When present: leaves the clean export directory and its template installation in place.
    Use in CI where no developer environment needs restoring.
    Note: all existing Momentum templates are always uninstalled before testing regardless
    of this flag to avoid false positives.

.PARAMETER Parallel
    Run tests in parallel using PowerShell 7 ForEach-Object -Parallel.
    Tests run 4-at-a-time. Console output is batched per test (not interleaved).
    Default: true (parallel execution). Pass -Parallel $false for sequential.
    Note: In parallel mode, all tests run to completion. MaxFailures only controls
    whether remaining results are processed after failures are detected.

.EXAMPLE
    ./Run-TemplateTests.ps1
    Run all test categories

.EXAMPLE
    ./Run-TemplateTests.ps1 -Category component-isolation
    Run only component isolation tests

.EXAMPLE
    ./Run-TemplateTests.ps1 -MaxFailures 3 -KeepResultsWithErrors
    Stop after 3 failures and preserve failed test directories

.EXAMPLE
    ./Run-TemplateTests.ps1 -SkipRestoreTemplate
    Run tests without reinstalling the repo root template afterwards

.EXAMPLE
    ./Run-TemplateTests.ps1 -Parallel $false
    Run all tests sequentially (one at a time)
#>

[CmdletBinding(DefaultParameterSetName = 'Run')]
param(
    [Parameter(ParameterSetName = 'Run')]
    [ValidateSet(
        'component-isolation', 'port-config', 'org-names',
        'library-config', 'real-world-patterns', 'orleans-combinations', 'edge-cases'
    )]
    [string]$Category,

    [Parameter(ParameterSetName = 'List', Mandatory)]
    [switch]$ListCategories,

    [Parameter(ParameterSetName = 'Run')]
    [switch]$KeepResults,

    [Parameter(ParameterSetName = 'Run')]
    [switch]$KeepResultsWithErrors = $true,

    [Parameter(ParameterSetName = 'Run')]
    [ValidateRange(0, [int]::MaxValue)]
    [int]$MaxFailures = 0,

    [Parameter(ParameterSetName = 'Run')]
    [switch]$IncludeE2E,

    [Parameter(ParameterSetName = 'Run')]
    [switch]$SkipRestoreTemplate,

    [Parameter(ParameterSetName = 'Run')]
    [bool]$Parallel = $true
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Prevent MSBuild from keeping long-lived server nodes that accumulate memory across tests
$env:MSBUILDDISABLENODEREUSE = '1'
$env:DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER = '1'

# Copy parameter to script scope so it's reliably accessible during Ctrl+C cleanup
$script:RestoreTemplateEnabled = -not $SkipRestoreTemplate

# Script-level variables
$script:TotalTests = 0
$script:PassedTests = 0
$script:FailedTests = 0
$script:TestResults = @{}
$script:TestCompleted = $false
$script:SuiteStopwatch = $null

# Initialize test environment with shared run ID
$scriptDir = Split-Path -Parent $PSCommandPath
$script:RepoRoot = (Resolve-Path (Join-Path -Path $scriptDir -ChildPath '..')).Path
$script:TemplateTestsRoot = Join-Path -Path $script:RepoRoot -ChildPath '_template_tests'
$script:RunId = [System.Guid]::NewGuid().ToString('N').Substring(0, 8)
$script:CleanExportDir = Join-Path -Path $script:TemplateTestsRoot -ChildPath "mmt-$($script:RunId)"
$script:ExecutionTestDir = Join-Path -Path $script:TemplateTestsRoot -ChildPath "mmt-$($script:RunId)-results"

New-Item -ItemType Directory -Path $script:ExecutionTestDir -Force | Out-Null

$LogFile = Join-Path -Path $script:ExecutionTestDir -ChildPath 'template-test-results.log'
"Momentum Template Test Suite - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" |
Out-File -FilePath $LogFile -Encoding UTF8

$Colors = @{
    Red    = if ($IsWindows) { 'Red' } else { "`e[31m" }
    Green  = if ($IsWindows) { 'Green' } else { "`e[32m" }
    Yellow = if ($IsWindows) { 'Yellow' } else { "`e[33m" }
    Blue   = if ($IsWindows) { 'Blue' } else { "`e[34m" }
    Reset  = if ($IsWindows) { 'White' } else { "`e[0m" }
}

function Get-ErrorSummary {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$ErrorOutput,

        [Parameter()]
        [ValidateRange(1, 10)]
        [int]$MaxLines = 3,

        [Parameter()]
        [string]$Filter = '.*'
    )

    if ([string]::IsNullOrWhiteSpace($ErrorOutput)) {
        return ''
    }

    $errorLines = $ErrorOutput -split '[\r\n]+' |
    Where-Object { $_.Trim() -and $_ -match $Filter } |
    Select-Object -First $MaxLines

    if (@($errorLines).Count -gt 0) {
        return ' - ' + ($errorLines -join '; ')
    }

    return ''
}

function Format-Duration {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [System.Diagnostics.Stopwatch]$Stopwatch
    )

    $elapsed = $Stopwatch.Elapsed
    if ($elapsed.TotalSeconds -lt 1) {
        return "$([int]$elapsed.TotalMilliseconds)ms"
    }
    elseif ($elapsed.TotalMinutes -lt 1) {
        return "$([math]::Round($elapsed.TotalSeconds, 1))s"
    }
    else {
        return "$([int][math]::Floor($elapsed.TotalMinutes))m $($elapsed.Seconds)s"
    }
}

function Write-ColoredMessage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('INFO', 'SUCCESS', 'ERROR', 'WARN')]
        [string]$Level,

        [Parameter(Mandatory)]
        [string]$Message
    )

    $timeStamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timeStamp] [$Level] $Message"

    # Write to log file
    $logMessage | Out-File -FilePath $LogFile -Append -Encoding UTF8

    # Write to console with colors
    if ($IsWindows) {
        $color = switch ($Level) {
            'INFO' { 'Blue' }
            'SUCCESS' { 'Green' }
            'ERROR' { 'Red' }
            'WARN' { 'Yellow' }
        }
        Write-Host "[$timeStamp] " -ForegroundColor DarkGray -NoNewline
        Write-Host "[$Level]" -ForegroundColor $color -NoNewline
        Write-Host " $Message"
    }
    else {
        $colorCode = switch ($Level) {
            'INFO' { $Colors.Blue }
            'SUCCESS' { $Colors.Green }
            'ERROR' { $Colors.Red }
            'WARN' { $Colors.Yellow }
        }
        Write-Host "`e[90m[$timeStamp]`e[0m $colorCode[$Level]$($Colors.Reset) $Message"
    }
}

function Invoke-SingleTest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [AllowEmptyString()] [string]$Parameters,
        [Parameter(Mandatory)] [string]$TestCategory,
        [Parameter(Mandatory)] [string]$ExecutionTestDir,
        [Parameter()] [hashtable[]]$ContentMustNotContain = @(),
        [Parameter()] [bool]$IncludeE2ETests = $false
    )

    $messages = [System.Collections.Generic.List[hashtable]]::new()
    $testSw = [System.Diagnostics.Stopwatch]::StartNew()

    $result = @{
        Name          = $Name
        Category      = $TestCategory
        Result        = 'FAILED'
        Messages      = $null
        GenDuration   = $null
        BuildDuration = $null
        TestDuration  = $null
        TotalDuration = $null
        ProjectCount  = 0
        ErrorSummary  = ''
    }

    $paramDisplay = if ($Parameters.Trim()) { "with params: $Parameters" } else { "(no parameters)" }
    $messages.Add(@{ Level = 'INFO'; Message = "[$TestCategory] Testing: $Name $paramDisplay" })

    $tempDir = Join-Path -Path $ExecutionTestDir -ChildPath $Name
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    try {
        # Build template arguments
        $templateArgs = @('new', 'mmt', '-n', $Name, '--allow-scripts', 'yes', '--local')
        if ($Parameters.Trim()) {
            $templateArgs += ($Parameters -split '\s+' | Where-Object { $_ })
        }

        $generationOut = Join-Path -Path $tempDir -ChildPath "01-generation-stdout.log"
        $generationErr = Join-Path -Path $tempDir -ChildPath "01-generation-stderr.log"

        # Generation
        $genSw = [System.Diagnostics.Stopwatch]::StartNew()
        $process = Start-Process -FilePath 'dotnet' -ArgumentList $templateArgs `
            -WorkingDirectory $tempDir -NoNewWindow -Wait -PassThru `
            -RedirectStandardOutput $generationOut -RedirectStandardError $generationErr
        $genSw.Stop()
        $result.GenDuration = $genSw.Elapsed

        if ($process.ExitCode -ne 0) {
            $errorOutput = Get-Content $generationErr -Raw -ErrorAction SilentlyContinue
            $errorSummary = Get-ErrorSummary -ErrorOutput $errorOutput -MaxLines 3
            $genDur = $genSw.Elapsed.TotalSeconds.ToString('F1')
            $messages.Add(@{ Level = 'ERROR'; Message = "[$TestCategory] $Name`: Generation failed ($($genDur)s)$errorSummary" })
            $result.ErrorSummary = "Generation failed$errorSummary"
            $result.Messages = $messages.ToArray()
            $result.TotalDuration = $testSw.Elapsed
            return $result
        }

        $genDur = $genSw.Elapsed.TotalSeconds.ToString('F1')
        $messages.Add(@{ Level = 'SUCCESS'; Message = "[$TestCategory] $Name`: Generation succeeded ($($genDur)s)" })

        $projectDir = Join-Path -Path $tempDir -ChildPath $Name
        if (-not (Test-Path -Path $projectDir -PathType Container)) {
            $messages.Add(@{ Level = 'ERROR'; Message = "[$TestCategory] $Name`: Generation succeeded but directory not found" })
            $result.ErrorSummary = 'Directory not found after generation'
            $result.Messages = $messages.ToArray()
            $result.TotalDuration = $testSw.Elapsed
            return $result
        }

        $projects = @(Get-ChildItem -Path $projectDir -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue)
        $result.ProjectCount = @($projects).Count

        $buildOut = Join-Path -Path $tempDir -ChildPath "02-build-stdout.log"
        $buildErr = Join-Path -Path $tempDir -ChildPath "02-build-stderr.log"

        # Build
        $buildSw = [System.Diagnostics.Stopwatch]::StartNew()
        $buildProcess = Start-Process -FilePath 'dotnet' `
            -ArgumentList @('build', '--verbosity', 'normal', '-nodeReuse:false') `
            -WorkingDirectory $projectDir -NoNewWindow -Wait -PassThru `
            -RedirectStandardOutput $buildOut -RedirectStandardError $buildErr
        $buildSw.Stop()
        $result.BuildDuration = $buildSw.Elapsed

        if ($buildProcess.ExitCode -ne 0) {
            $buildOutput = Get-Content $buildErr -Raw -ErrorAction SilentlyContinue
            $buildErrorSummary = Get-ErrorSummary -ErrorOutput $buildOutput -MaxLines 3 -Filter '(error|Error|CS[0-9]+|MSB[0-9]+)'
            $buildDur = $buildSw.Elapsed.TotalSeconds.ToString('F1')
            $messages.Add(@{ Level = 'ERROR'; Message = "[$TestCategory] $Name`: Build failed ($($buildDur)s)$buildErrorSummary" })
            $result.ErrorSummary = "Build failed$buildErrorSummary"
            $result.Messages = $messages.ToArray()
            $result.TotalDuration = $testSw.Elapsed
            return $result
        }

        $buildDur = $buildSw.Elapsed.TotalSeconds.ToString('F1')
        $messages.Add(@{ Level = 'SUCCESS'; Message = "[$TestCategory] $Name`: Build succeeded ($($result.ProjectCount) projects, $($buildDur)s)" })

        # Tests
        $testsDir = Join-Path -Path $projectDir -ChildPath 'tests'
        if (Test-Path -Path $testsDir -PathType Container) {
            $testOut = Join-Path -Path $tempDir -ChildPath "03-test-stdout.log"
            $testErr = Join-Path -Path $tempDir -ChildPath "03-test-stderr.log"

            $testFilter = if ($IncludeE2ETests) { 'Type!=Integration' } else { 'Type!=E2E&Type!=Integration' }
            $testArgs = @('test', '--verbosity', 'normal', '--filter', $testFilter)

            $testRunSw = [System.Diagnostics.Stopwatch]::StartNew()
            $testProcess = Start-Process -FilePath 'dotnet' -ArgumentList $testArgs `
                -WorkingDirectory $projectDir -NoNewWindow -Wait -PassThru `
                -RedirectStandardOutput $testOut -RedirectStandardError $testErr
            $testRunSw.Stop()
            $result.TestDuration = $testRunSw.Elapsed

            if ($testProcess.ExitCode -eq 0) {
                $excludedMsg = if ($IncludeE2ETests) { 'Integration tests excluded' } else { 'E2E and Integration tests excluded' }
                $testDur = $testRunSw.Elapsed.TotalSeconds.ToString('F1')
                $messages.Add(@{ Level = 'SUCCESS'; Message = "[$TestCategory] $Name`: Tests passed ($excludedMsg, $($testDur)s)" })
            }
            else {
                $testOutput = Get-Content $testErr -Raw -ErrorAction SilentlyContinue
                $testErrorSummary = Get-ErrorSummary -ErrorOutput $testOutput -MaxLines 2 -Filter '(Failed|Error|Exception|Assert)'
                $testDur = $testRunSw.Elapsed.TotalSeconds.ToString('F1')
                $messages.Add(@{ Level = 'ERROR'; Message = "[$TestCategory] $Name`: Tests failed ($($testDur)s)$testErrorSummary" })
                $result.ErrorSummary = "Tests failed$testErrorSummary"
                $result.Messages = $messages.ToArray()
                $result.TotalDuration = $testSw.Elapsed
                return $result
            }
        }

        # Content exclusion checks
        if ($ContentMustNotContain.Count -gt 0) {
            $contentFailed = $false
            foreach ($check in $ContentMustNotContain) {
                $filePath = Join-Path -Path $projectDir -ChildPath $check.File
                if (Test-Path -Path $filePath) {
                    $fileContent = Get-Content -Path $filePath -Raw
                    if ($fileContent -match $check.Pattern) {
                        $messages.Add(@{ Level = 'ERROR'; Message = "[$TestCategory] $Name`: Content check failed - '$($check.Pattern)' found in $($check.File)" })
                        $contentFailed = $true
                    }
                }
                else {
                    $messages.Add(@{ Level = 'WARN'; Message = "[$TestCategory] $Name`: Content check skipped - file not found: $($check.File)" })
                }
            }
            if ($contentFailed) {
                $result.ErrorSummary = 'Content check failed'
                $result.Messages = $messages.ToArray()
                $result.TotalDuration = $testSw.Elapsed
                return $result
            }
        }

        # All passed
        $testSw.Stop()
        $result.Result = 'PASSED'
        $result.TotalDuration = $testSw.Elapsed

        $genFmt = $genSw.Elapsed.TotalSeconds.ToString('F1')
        $buildFmt = $buildSw.Elapsed.TotalSeconds.ToString('F1')
        $totalFmt = $testSw.Elapsed.TotalSeconds.ToString('F1')
        $messages.Add(@{ Level = 'INFO'; Message = "[$TestCategory] $Name`: Total time: $($totalFmt)s (gen: $($genFmt)s, build: $($buildFmt)s)" })
    }
    catch {
        $messages.Add(@{ Level = 'ERROR'; Message = "[$TestCategory] $Name`: Exception occurred - $($_.Exception.Message)" })
        $result.ErrorSummary = $_.Exception.Message
    }

    $result.Messages = $messages.ToArray()
    $result.TotalDuration = $testSw.Elapsed
    return $result
}

function Invoke-IndividualTestCleanup {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$TestName,

        [Parameter(Mandatory)]
        [ValidateSet('PASSED', 'FAILED')]
        [string]$TestResult
    )

    # Skip cleanup if user wants to keep all results
    if ($KeepResults) {
        return
    }

    # Skip cleanup if user wants to keep failed tests and this test failed
    if ($KeepResultsWithErrors -and $TestResult -eq 'FAILED') {
        return
    }

    $testDir = Join-Path -Path $script:ExecutionTestDir -ChildPath $TestName
    if (Test-Path -Path $testDir) {
        try {
            [System.IO.Directory]::Delete($testDir, $true)
            Write-ColoredMessage -Level 'INFO' -Message "Cleaned up test directory: $TestName"
        }
        catch {
            try {
                Remove-Item -Path $testDir -Recurse -Force -ErrorAction Stop
                Write-ColoredMessage -Level 'INFO' -Message "Cleaned up test directory (fallback): $TestName"
            }
            catch {
                Write-ColoredMessage -Level 'WARN' -Message "Failed to clean up test directory: $TestName - $($_.Exception.Message)"
            }
        }
    }
}

function Get-TestDefinitions {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet(
            'component-isolation', 'port-config', 'org-names',
            'library-config', 'real-world-patterns', 'orleans-combinations', 'edge-cases'
        )]
        [string]$TestCategory
    )

    $tests = @()

    switch ($TestCategory) {
        'component-isolation' {
            $components = @(
                @{ Name = 'api'; TestName = 'TestApiOnly' },
                @{ Name = 'backoffice'; TestName = 'TestBackOfficeOnly' },
                @{ Name = 'orleans'; TestName = 'TestOrleansOnly' },
                @{ Name = 'aspire'; TestName = 'TestAspireOnly' },
                @{ Name = 'docs'; TestName = 'TestDocsOnly' }
            )
            foreach ($component in $components) {
                $otherComponents = $components |
                    Where-Object { $_.Name -ne $component.Name } |
                    ForEach-Object { "--$($_.Name) false" }
                $params = "--$($component.Name) true " + ($otherComponents -join ' ')
                $tests += @{ Name = $component.TestName; Parameters = $params; TestCategory = 'Component Isolation'; ContentMustNotContain = @() }
            }
        }

        'port-config' {
            $ports = @(1024, 5000, 9000, 65000)
            foreach ($port in $ports) {
                $tests += @{ Name = "TestPort$port"; Parameters = "--port $port"; TestCategory = 'Port Config'; ContentMustNotContain = @() }
            }
        }

        'org-names' {
            $tests += @{ Name = 'TestOrgSpecial'; Parameters = '--org "Company-Name.Inc"'; TestCategory = 'Organization Names'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestOrgNumbers'; Parameters = '--org "123 Corp"'; TestCategory = 'Organization Names'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestOrgAmpersand'; Parameters = '--org "My Company & Partners"'; TestCategory = 'Organization Names'; ContentMustNotContain = @() }
        }

        'library-config' {
            $libraries = @(
                @{ Name = 'defaults'; TestName = 'TestLibDefaults' },
                @{ Name = 'api'; TestName = 'TestLibApi' },
                @{ Name = 'ext'; TestName = 'TestLibExt' },
                @{ Name = 'kafka'; TestName = 'TestLibKafka' },
                @{ Name = 'generators'; TestName = 'TestLibGenerators' }
            )
            foreach ($lib in $libraries) {
                $tests += @{ Name = $lib.TestName; Parameters = "--libs $($lib.Name)"; TestCategory = 'Library Config'; ContentMustNotContain = @() }
            }
            $tests += @{ Name = 'TestLibMulti'; Parameters = '--libs defaults --libs api --libs kafka'; TestCategory = 'Library Config'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestLibCustomName'; Parameters = '--libs defaults --libs ext --lib-name CustomPlatform'; TestCategory = 'Library Config'; ContentMustNotContain = @() }
        }

        'real-world-patterns' {
            $tests += @{ Name = 'TestDefault'; Parameters = ''; TestCategory = 'Real-World Patterns'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestDefaultNoSample'; Parameters = '--no-sample'; TestCategory = 'Real-World Patterns'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestDefaultWithLibs'; Parameters = '--libs defaults api kafka ext --lib-name Platform'; TestCategory = 'Real-World Patterns'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestApiNoBackOffice'; Parameters = '--backoffice false'; TestCategory = 'Real-World Patterns'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestBackOfficeNoApi'; Parameters = '--api false'; TestCategory = 'Real-World Patterns'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestAPISimple'; Parameters = '--no-sample --backoffice false --docs false --kafka false'; TestCategory = 'Real-World Patterns'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestFullStack'; Parameters = '--orleans true'; TestCategory = 'Real-World Patterns'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestBffEnabled'; Parameters = '--bff'; TestCategory = 'Real-World Patterns'; ContentMustNotContain = @() }

            $bffExclusionChecks = @(
                @{ File = 'src/TestBffDisabled.Api/Program.cs'; Pattern = 'FrontendIntegration' },
                @{ File = 'src/TestBffDisabled.Api/appsettings.json'; Pattern = '"Cors"|"SecurityHeaders"' }
            )
            $tests += @{ Name = 'TestBffDisabled'; Parameters = '--bff false'; TestCategory = 'Real-World Patterns'; ContentMustNotContain = $bffExclusionChecks }
        }

        'orleans-combinations' {
            $tests += @{ Name = 'TestOrleansAPI'; Parameters = '--orleans true --api true --aspire true'; TestCategory = 'Orleans Combinations'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestOrleansFullStack'; Parameters = '--orleans true --api true --backoffice true --aspire true'; TestCategory = 'Orleans Combinations'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestOrleansNoKafka'; Parameters = '--orleans true --kafka false'; TestCategory = 'Orleans Combinations'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestOrleansNoSample'; Parameters = '--orleans true --no-sample'; TestCategory = 'Orleans Combinations'; ContentMustNotContain = @() }
        }

        'edge-cases' {
            $tests += @{ Name = 'TestMinimal'; Parameters = '--project-only --no-sample'; TestCategory = 'Edge Cases'; ContentMustNotContain = @() }
            $tests += @{ Name = 'TestBareMin'; Parameters = '--api false --backoffice false --aspire false --docs false'; TestCategory = 'Edge Cases'; ContentMustNotContain = @() }
            $allFalseParams = '--api false --backoffice false --orleans false --docs false --aspire false --kafka false'
            $tests += @{ Name = 'TestAllFalse'; Parameters = $allFalseParams; TestCategory = 'Edge Cases'; ContentMustNotContain = @() }
            $allTrueParams = '--api true --backoffice true --orleans true --docs true --aspire true --kafka true'
            $tests += @{ Name = 'TestAllTrue'; Parameters = $allTrueParams; TestCategory = 'Edge Cases'; ContentMustNotContain = @() }
        }
    }

    return $tests
}

function Invoke-TestExecution {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable[]]$TestDefinitions
    )

    $script:TotalTests += $TestDefinitions.Count

    if ($Parallel) {
        Write-ColoredMessage -Level 'INFO' -Message "Running $($TestDefinitions.Count) tests in parallel (4 concurrent)..."

        # Variables to pass into the parallel scope
        $executionTestDir = $script:ExecutionTestDir
        $includeE2ETests = [bool]$IncludeE2E

        # ForEach-Object -Parallel runs each iteration in a separate runspace.
        # Functions defined in the script are NOT available — we must pass the
        # function definitions as strings and dot-source them inside the block.
        $funcGetErrorSummary = ${function:Get-ErrorSummary}.ToString()
        $funcInvokeSingleTest = ${function:Invoke-SingleTest}.ToString()

        $results = $TestDefinitions | ForEach-Object -Parallel {
            # Re-create functions in this runspace
            ${function:Get-ErrorSummary} = $using:funcGetErrorSummary
            ${function:Invoke-SingleTest} = $using:funcInvokeSingleTest

            $testDef = $_
            Invoke-SingleTest `
                -Name $testDef.Name `
                -Parameters $testDef.Parameters `
                -TestCategory $testDef.TestCategory `
                -ExecutionTestDir $using:executionTestDir `
                -ContentMustNotContain $testDef.ContentMustNotContain `
                -IncludeE2ETests $using:includeE2ETests
        } -ThrottleLimit 4

        # Process results on the main thread
        foreach ($testResult in $results) {
            # Replay messages
            foreach ($msg in $testResult.Messages) {
                Write-ColoredMessage -Level $msg.Level -Message $msg.Message
            }

            # Update counters
            if ($testResult.Result -eq 'PASSED') {
                $script:PassedTests++
            }
            else {
                $script:FailedTests++
            }
            $script:TestResults[$testResult.Name] = $testResult.Result

            # Cleanup individual test
            Invoke-IndividualTestCleanup -TestName $testResult.Name -TestResult $testResult.Result

            Write-Host '---'

            # Check max failures
            if ($MaxFailures -gt 0 -and $script:FailedTests -ge $MaxFailures) {
                Write-ColoredMessage -Level 'ERROR' -Message "Reached maximum failures ($MaxFailures). Stopping."
                # Adjust TotalTests to reflect only processed results
                $processed = $script:PassedTests + $script:FailedTests
                $script:TotalTests -= ($TestDefinitions.Count - $processed)
                break
            }
        }

        # Shut down build servers once after all parallel tests
        & dotnet build-server shutdown 2>$null | Out-Null
    }
    else {
        # Sequential mode — existing behavior via Invoke-SingleTest
        foreach ($testDef in $TestDefinitions) {
            $testResult = Invoke-SingleTest `
                -Name $testDef.Name `
                -Parameters $testDef.Parameters `
                -TestCategory $testDef.TestCategory `
                -ExecutionTestDir $script:ExecutionTestDir `
                -ContentMustNotContain $testDef.ContentMustNotContain `
                -IncludeE2ETests ([bool]$IncludeE2E)

            # Replay messages
            foreach ($msg in $testResult.Messages) {
                Write-ColoredMessage -Level $msg.Level -Message $msg.Message
            }

            # Update counters
            if ($testResult.Result -eq 'PASSED') {
                $script:PassedTests++
            }
            else {
                $script:FailedTests++
            }
            $script:TestResults[$testResult.Name] = $testResult.Result

            # Cleanup individual test
            Invoke-IndividualTestCleanup -TestName $testResult.Name -TestResult $testResult.Result

            # Shut down build servers per test in sequential mode
            & dotnet build-server shutdown 2>$null | Out-Null

            Write-Host '---'

            # Check max failures
            if ($MaxFailures -gt 0 -and $script:FailedTests -ge $MaxFailures) {
                Write-ColoredMessage -Level 'ERROR' -Message "Reached maximum failures ($MaxFailures). Exiting early."
                Show-Results
                exit 1
            }
        }
    }
}

function Show-Categories {
    [CmdletBinding()]
    param()

    Write-Host 'Available Test Categories:' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Use -MaxFailures N to exit after N failures (default: 0 = run all)' -ForegroundColor Yellow
    Write-Host ''

    $categories = @(
        @{ Number = '1'; Name = 'component-isolation'; Description = 'Test each component in isolation' }
        @{ Number = '2'; Name = 'port-config'; Description = 'Test port boundaries and common conflicts' }
        @{ Number = '3'; Name = 'org-names'; Description = 'Validate special characters handling' }
        @{ Number = '4'; Name = 'library-config'; Description = 'Test library reference combinations' }
        @{ Number = '5'; Name = 'real-world-patterns'; Description = 'Validate common deployment scenarios' }
        @{ Number = '6'; Name = 'orleans-combinations'; Description = 'Test stateful processing configurations' }
        @{ Number = '7'; Name = 'edge-cases'; Description = 'Test boundary conditions and special modes' }
    )

    foreach ($category in $categories) {
        $paddedName = $category.Name.PadRight(20)
        Write-Host "$($category.Number). $paddedName - $($category.Description)" -ForegroundColor White
    }
}

function Show-Results {
    [CmdletBinding()]
    param()

    Write-Host ''
    Write-Host ('=' * 50) -ForegroundColor Cyan
    Write-ColoredMessage -Level 'INFO' -Message 'TEST SUITE COMPLETED'
    Write-Host ('=' * 50) -ForegroundColor Cyan
    Write-Host "Total Tests: $script:TotalTests" -ForegroundColor White

    if ($IsWindows) {
        Write-Host 'Passed: ' -NoNewline -ForegroundColor White
        Write-Host "$script:PassedTests" -ForegroundColor Green
        Write-Host 'Failed: ' -NoNewline -ForegroundColor White
        Write-Host "$script:FailedTests" -ForegroundColor Red
    }
    else {
        Write-Host "Passed: $($Colors.Green)$script:PassedTests$($Colors.Reset)"
        Write-Host "Failed: $($Colors.Red)$script:FailedTests$($Colors.Reset)"
    }

    Write-Host ''
    if ($script:SuiteStopwatch) {
        $script:SuiteStopwatch.Stop()
        Write-ColoredMessage -Level 'INFO' -Message "Total time: $(Format-Duration $script:SuiteStopwatch)"
    }
    Write-ColoredMessage -Level 'INFO' -Message "Run ID: mmt-$($script:RunId)"
    Write-ColoredMessage -Level 'INFO' -Message "Results: $script:ExecutionTestDir"
    Write-ColoredMessage -Level 'INFO' -Message "Log: $LogFile"

    # Mark test as completed for cleanup handler
    $script:TestCompleted = $true

    if ($script:FailedTests -eq 0) {
        Write-ColoredMessage -Level 'SUCCESS' -Message 'All tests passed! 🎉'
        exit 0
    }
    else {
        Write-ColoredMessage -Level 'ERROR' -Message "$script:FailedTests tests failed. Check log for details."
        exit 1
    }
}

function Invoke-Cleanup {
    [CmdletBinding()]
    param()

    # Always clean up the temporary clean export
    Remove-CleanExport

    if ($KeepResults) {
        Write-ColoredMessage -Level 'INFO' -Message "Keeping all test results: $script:ExecutionTestDir"
        return
    }

    # Check for remaining test directories (failed tests when KeepResultsWithErrors is true)
    $remainingDirs = @()
    if (Test-Path -Path $script:ExecutionTestDir) {
        $remainingDirs = @(Get-ChildItem -Path $script:ExecutionTestDir -Directory -ErrorAction SilentlyContinue)
    }

    if ($KeepResultsWithErrors -and $script:FailedTests -gt 0 -and @($remainingDirs).Count -gt 0) {
        Write-ColoredMessage -Level 'WARN' -Message "Failed test results preserved: $script:ExecutionTestDir"
        Write-ColoredMessage -Level 'INFO' -Message "$(@($remainingDirs).Count) test directories preserved"
        return
    }

    # Clean up the execution directory if empty or not preserving
    if ((Test-Path -Path $script:ExecutionTestDir) -and (@($remainingDirs).Count -eq 0)) {
        try {
            [System.IO.Directory]::Delete($script:ExecutionTestDir, $true)
        }
        catch {
            try {
                Remove-Item -Path $script:ExecutionTestDir -Recurse -Force -ErrorAction Stop
            }
            catch {
                Write-Host "$($Colors.Yellow)[WARN]$($Colors.Reset) Failed to clean up execution directory: mmt-$($script:RunId)-results - $($_.Exception.Message)"
            }
        }
    }
}

function Uninstall-AllMomentumTemplates {
    <#
    .SYNOPSIS
        Uninstalls all installed Momentum template packages.
    #>
    [CmdletBinding()]
    param()

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Write-ColoredMessage -Level 'INFO' -Message 'Uninstalling all Momentum template installations...'

    # dotnet new uninstall with no args lists all installed template packages
    # Output format:
    #    /path/to/template
    #       Templates:
    #          Template Name (shortname) Language
    #       Uninstall Command:
    #          dotnet new uninstall /path/to/template
    $uninstallOutput = & dotnet new uninstall 2>&1 | Out-String

    # Extract package paths from "dotnet new uninstall <path>" lines that mention momentum
    $momentumPackages = @()
    $currentBlock = @()
    foreach ($line in ($uninstallOutput -split '[\r\n]+')) {
        $currentBlock += $line
        if ($line -match '^\s+dotnet new uninstall\s+(.+)$') {
            $package = $Matches[1].Trim()
            $blockText = $currentBlock -join "`n"
            if ($blockText -match '(?i)momentum') {
                $momentumPackages += $package
            }
            $currentBlock = @()
        }
    }

    foreach ($package in $momentumPackages) {
        Write-ColoredMessage -Level 'INFO' -Message "Uninstalling: $package"
        & dotnet new uninstall $package 2>&1 | Out-Null
    }

    $sw.Stop()
    if ($momentumPackages.Count -eq 0) {
        Write-ColoredMessage -Level 'INFO' -Message "No existing Momentum templates found ($(Format-Duration $sw))"
    }
    else {
        Write-ColoredMessage -Level 'SUCCESS' -Message "Uninstalled $($momentumPackages.Count) Momentum template package(s) ($(Format-Duration $sw))"
    }
}

function Install-TemplateFromCleanExport {
    <#
    .SYNOPSIS
        Creates a clean git export and installs the template from it.
    .DESCRIPTION
        The dotnet template engine walks ALL files in the source directory during generation,
        even those matching exclude patterns. A dirty working tree with bin/, obj/, node_modules/
        etc. causes the engine to enumerate tens of thousands of unnecessary files, making each
        template generation 8-13x slower than necessary.

        This function creates a clean git archive export (tracked files only) under
        _template_tests/mmt-{runId}, copies local NuGet configuration files, and installs
        the template from the clean directory.

        When RestoreTemplate is enabled, all existing Momentum templates are uninstalled first
        to ensure a clean slate.
    #>
    [CmdletBinding()]
    param()

    $setupSw = [System.Diagnostics.Stopwatch]::StartNew()

    # Always uninstall all existing Momentum templates to avoid false positives
    Uninstall-AllMomentumTemplates

    Write-ColoredMessage -Level 'INFO' -Message 'Creating clean git export for fast template generation...'

    New-Item -ItemType Directory -Path $script:CleanExportDir -Force | Out-Null

    # Export only git-tracked files (excludes bin, obj, node_modules, etc.)
    # Use a temp tar file because PowerShell pipelines mangle binary data
    $archiveTar = Join-Path $script:CleanExportDir '.archive.tar'
    try {
        & git -C $script:RepoRoot archive HEAD -o $archiveTar 2>&1
        if ($LASTEXITCODE -ne 0) { throw "git archive failed" }
        & tar -x -C $script:CleanExportDir -f $archiveTar 2>&1
        if ($LASTEXITCODE -ne 0) { throw "tar extract failed" }
    }
    catch {
        Write-ColoredMessage -Level 'WARN' -Message "Git archive failed, falling back to direct install: $_"
        Remove-Item -Path $script:CleanExportDir -Recurse -Force -ErrorAction SilentlyContinue
        $script:CleanExportDir = $null
        return $script:RepoRoot
    }
    finally {
        Remove-Item -Path $archiveTar -Force -ErrorAction SilentlyContinue
    }

    # Copy local NuGet configuration files (gitignored but needed for --local flag)
    foreach ($localFile in @('local-mmt-version.txt', 'local-feed-path.txt')) {
        $sourcePath = Join-Path $script:RepoRoot $localFile
        if (Test-Path $sourcePath) {
            Copy-Item -Path $sourcePath -Destination $script:CleanExportDir
        }
    }

    $cleanFileCount = @(Get-ChildItem -Path $script:CleanExportDir -Recurse -File).Count
    Write-ColoredMessage -Level 'INFO' -Message "Clean export: $cleanFileCount files in mmt-$($script:RunId)/"

    # Install template from clean export
    $installOutput = & dotnet new install $script:CleanExportDir --force 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-ColoredMessage -Level 'WARN' -Message "Template install from clean export failed, falling back to direct install"
        Remove-Item -Path $script:CleanExportDir -Recurse -Force -ErrorAction SilentlyContinue
        $script:CleanExportDir = $null
        return $script:RepoRoot
    }

    $setupSw.Stop()
    Write-ColoredMessage -Level 'SUCCESS' -Message "Template installed from clean export (setup: $(Format-Duration $setupSw))"
    return $script:CleanExportDir
}

function Remove-CleanExport {
    <#
    .SYNOPSIS
        Removes the clean export and restores the repo root template when RestoreTemplate is true.
        When RestoreTemplate is false, the clean export directory and its template installation
        are left in place (useful in CI where no developer environment needs restoring).
    #>
    [CmdletBinding()]
    param()

    if (-not $script:RestoreTemplateEnabled) {
        return
    }

    # Uninstall the clean export template and delete its directory
    if ($script:CleanExportDir -and (Test-Path $script:CleanExportDir)) {
        & dotnet new uninstall $script:CleanExportDir 2>$null | Out-Null
        Remove-Item -Path $script:CleanExportDir -Recurse -Force -ErrorAction SilentlyContinue
        $script:CleanExportDir = $null
    }

    # Restore the template from the repo root so the developer's environment is ready
    Write-ColoredMessage -Level 'INFO' -Message 'Restoring template from repo root...'
    $restoreOutput = & dotnet new install $script:RepoRoot --force 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-ColoredMessage -Level 'SUCCESS' -Message "Template restored from: $($script:RepoRoot)"
    }
    else {
        Write-ColoredMessage -Level 'WARN' -Message "Failed to restore template from repo root. Run manually: dotnet new install $($script:RepoRoot) --force"
    }
}

function Invoke-Main {
    [CmdletBinding()]
    param()

    try {
        if ($ListCategories) {
            Show-Categories
            return
        }

        # Install template from clean git export for faster generation
        $script:SuiteStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $templateSource = Install-TemplateFromCleanExport

        # Verify template is installed
        $templateList = & dotnet new list 2>$null
        if (-not ($templateList -match 'mmt')) {
            Write-ColoredMessage -Level 'ERROR' -Message 'Momentum template (mmt) not installed.'
            exit 1
        }

        Write-ColoredMessage -Level 'INFO' -Message 'Starting Momentum Template Comprehensive Test Suite (PowerShell)'
        Write-ColoredMessage -Level 'INFO' -Message "Log file: $LogFile"

        if ($MaxFailures -gt 0) {
            Write-ColoredMessage -Level 'INFO' -Message "Early exit enabled: Will stop after $MaxFailures failure(s)"
        }
        else {
            Write-ColoredMessage -Level 'INFO' -Message 'Running all tests regardless of failures'
        }

        Write-Host ''

        # Collect test definitions
        $allTests = @()
        if ($Category) {
            Write-ColoredMessage -Level 'INFO' -Message "Collecting tests for category: $Category"
            $allTests = @(Get-TestDefinitions -TestCategory $Category)
        }
        else {
            $allCategories = @(
                'component-isolation',
                'port-config',
                'org-names',
                'library-config',
                'real-world-patterns',
                'orleans-combinations',
                'edge-cases'
            )

            foreach ($cat in $allCategories) {
                $allTests += @(Get-TestDefinitions -TestCategory $cat)
            }
        }

        Write-ColoredMessage -Level 'INFO' -Message "Collected $($allTests.Count) test(s)"
        if ($Parallel) {
            Write-ColoredMessage -Level 'INFO' -Message 'Execution mode: parallel (4 concurrent)'
        }

        Write-Host ''

        # Execute tests
        Invoke-TestExecution -TestDefinitions $allTests

        Show-Results
    }
    finally {
        Invoke-Cleanup
    }
}

# Register cleanup handler for interrupts
Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action {
    if (-not $script:TestCompleted) {
        Invoke-Cleanup
    }
}

# Execute main function
Invoke-Main

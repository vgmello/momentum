#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Momentum Template Test Suite

.DESCRIPTION
    Comprehensive test suite for the Momentum .NET template (mmt) covering all critical scenarios:
    
    • Component isolation testing
    • Infrastructure variation testing  
    • Configuration edge case testing
    • Real-world pattern testing
    • Automated validation
    
    Features:
    • 8 test categories with ~25 parametrized tests
    • Immediate cleanup after each test to conserve disk space
    • Selective preservation of failed tests for debugging
    • Early exit on maximum failures
    • Comprehensive logging and colored output

.PARAMETER Category
    Run specific test category. Valid values:
    • component-isolation    - Test each component in isolation
    • database-config        - Validate all database setup options
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

.EXAMPLE
    ./Run-TemplateTests.ps1
    Run all test categories

.EXAMPLE
    ./Run-TemplateTests.ps1 -Category component-isolation
    Run only component isolation tests

.EXAMPLE
    ./Run-TemplateTests.ps1 -MaxFailures 3 -KeepResultsWithErrors
    Stop after 3 failures and preserve failed test directories
#>

[CmdletBinding(DefaultParameterSetName = 'Run')]
param(
    [Parameter(ParameterSetName = 'Run')]
    [ValidateSet(
        'component-isolation', 'database-config', 'port-config', 'org-names',
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
    [int]$MaxFailures = 0
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Script-level variables
$script:TotalTests = 0
$script:PassedTests = 0
$script:FailedTests = 0
$script:TestResults = @{}
$script:TestCompleted = $false

# Initialize test environment
$script:ExecutionTimestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$scriptDir = Split-Path -Parent $PSCommandPath
$script:ExecutionTestDir = Join-Path -Path $scriptDir -ChildPath '..' |
    Join-Path -ChildPath '_template_tests' |
    Join-Path -ChildPath $script:ExecutionTimestamp

if (-not (Test-Path -Path $script:ExecutionTestDir)) {
    New-Item -ItemType Directory -Path $script:ExecutionTestDir -Force | Out-Null
}

$LogFile = Join-Path -Path $script:ExecutionTestDir -ChildPath 'template-test-results.log'
"Momentum Template Test Suite - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" |
    Out-File -FilePath $LogFile -Encoding UTF8

# Cross-platform color configuration
$Colors = @{
    Red    = if ($IsWindows) { 'Red' } else { "`e[31m" }
    Green  = if ($IsWindows) { 'Green' } else { "`e[32m" }
    Yellow = if ($IsWindows) { 'Yellow' } else { "`e[33m" }
    Blue   = if ($IsWindows) { 'Blue' } else { "`e[34m" }
    Reset  = if ($IsWindows) { 'White' } else { "`e[0m" }
}

function Get-ErrorSummary {
    <#
    .SYNOPSIS
        Extract and format error summary from command output
    #>
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

function Test-MaxFailuresReached {
    <#
    .SYNOPSIS
        Check if maximum failures reached and exit early if needed
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$TestName,

        [Parameter()]
        [string[]]$TempFiles = @()
    )

    if ($MaxFailures -gt 0 -and $script:FailedTests -ge $MaxFailures) {
        Write-ColoredMessage -Level 'ERROR' -Message "Reached maximum failures ($MaxFailures). Exiting early."
        
        # Clean up any temp files passed in
        if ($TempFiles.Count -gt 0) {
            Remove-Item $TempFiles -Force -ErrorAction SilentlyContinue
        }
        
        Pop-Location
        Invoke-IndividualTestCleanup -TestName $TestName -TestResult 'FAILED'
        Show-Results
        exit 1
    }
}

function Write-ColoredMessage {
    <#
    .SYNOPSIS
        Write formatted, colored messages to console and log file
    #>
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
        Write-Host "$colorCode[$Level]$($Colors.Reset) $Message"
    }
}

function Test-Template {
    <#
    .SYNOPSIS
        Test a single template configuration by generating, building, and validating
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$Name,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]$Parameters,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]$TestCategory
    )

    $script:TotalTests++

    $paramDisplay = if ($Parameters.Trim()) { "with params: $Parameters" } else { "(no parameters)" }
    Write-ColoredMessage -Level 'INFO' -Message "[$TestCategory] Testing: $Name $paramDisplay"

    $tempDir = Join-Path -Path $script:ExecutionTestDir -ChildPath $Name
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    Push-Location -Path $tempDir

    try {
        # Build template arguments
        $templateArgs = @('new', 'mmt', '-n', $Name, '--allow-scripts', 'yes')
        if ($Parameters.Trim()) {
            $templateArgs += ($Parameters -split '\s+' | Where-Object { $_ })
        }

        $tempOut = [System.IO.Path]::GetTempFileName()
        $tempErr = [System.IO.Path]::GetTempFileName()

        $process = Start-Process -FilePath 'dotnet' -ArgumentList $templateArgs `
            -NoNewWindow -Wait -PassThru `
            -RedirectStandardOutput $tempOut -RedirectStandardError $tempErr

        if ($process.ExitCode -ne 0) {
            $errorOutput = Get-Content $tempErr -Raw -ErrorAction SilentlyContinue
            $errorSummary = Get-ErrorSummary -ErrorOutput $errorOutput -MaxLines 3

            Write-ColoredMessage -Level 'ERROR' -Message "[$TestCategory] $Name`: Generation failed$errorSummary"
            $script:FailedTests++
            $script:TestResults[$Name] = 'FAILED'

            Remove-Item $tempOut, $tempErr -Force -ErrorAction SilentlyContinue
            return
        }

        # Clean up temp files for successful generation
        Remove-Item $tempOut, $tempErr -Force -ErrorAction SilentlyContinue

        if (-not (Test-Path -Path $Name -PathType Container)) {
            Write-ColoredMessage -Level 'ERROR' -Message "[$TestCategory] $Name`: Generation succeeded but directory not found"
            $script:FailedTests++
            $script:TestResults[$Name] = 'FAILED'

            return
        }

        Set-Location -Path $Name

        $projects = @(Get-ChildItem -Path . -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue)
        $projectCount = @($projects).Count

        $tempBuildOut = [System.IO.Path]::GetTempFileName()
        $tempBuildErr = [System.IO.Path]::GetTempFileName()

        # Attempt to build the generated project
        $buildProcess = Start-Process -FilePath 'dotnet' -ArgumentList @('build', '--verbosity', 'quiet') `
            -NoNewWindow -Wait -PassThru `
            -RedirectStandardOutput $tempBuildOut -RedirectStandardError $tempBuildErr

        if ($buildProcess.ExitCode -eq 0) {
            Write-ColoredMessage -Level 'SUCCESS' -Message "[$TestCategory] $Name`: Build succeeded ($projectCount projects)"

            # Run tests if they exist
            if (Test-Path -Path 'tests' -PathType Container) {
                $tempTestOut = [System.IO.Path]::GetTempFileName()
                $tempTestErr = [System.IO.Path]::GetTempFileName()

                $testProcess = Start-Process -FilePath 'dotnet' -ArgumentList @('test', '--verbosity', 'quiet') `
                    -NoNewWindow -Wait -PassThru `
                    -RedirectStandardOutput $tempTestOut -RedirectStandardError $tempTestErr

                if ($testProcess.ExitCode -eq 0) {
                    Write-ColoredMessage -Level 'SUCCESS' -Message "[$TestCategory] $Name`: Tests passed"
                }
                else {
                    $testOutput = Get-Content $tempTestErr -Raw -ErrorAction SilentlyContinue
                    $testErrorSummary = Get-ErrorSummary -ErrorOutput $testOutput -MaxLines 2 -Filter '(Failed|Error|Exception|Assert)'

                    Write-ColoredMessage -Level 'ERROR' -Message "[$TestCategory] $Name`: Tests failed$testErrorSummary"
                    $script:FailedTests++
                    $script:TestResults[$Name] = 'FAILED'

                    Remove-Item $tempTestOut, $tempTestErr -Force -ErrorAction SilentlyContinue

                    Test-MaxFailuresReached -TestName $Name
                    return
                }

                Remove-Item $tempTestOut, $tempTestErr -Force -ErrorAction SilentlyContinue
            }

            $script:PassedTests++
            $script:TestResults[$Name] = 'PASSED'
        }
        else {
            $buildOutput = Get-Content $tempBuildErr -Raw -ErrorAction SilentlyContinue
            $buildErrorSummary = Get-ErrorSummary -ErrorOutput $buildOutput -MaxLines 3 -Filter '(error|Error|CS[0-9]+|MSB[0-9]+)'

            Write-ColoredMessage -Level 'ERROR' -Message "[$TestCategory] $Name`: Build failed$buildErrorSummary"
            $script:FailedTests++
            $script:TestResults[$Name] = 'FAILED'

            Test-MaxFailuresReached -TestName $Name -TempFiles @($tempBuildOut, $tempBuildErr)
        }

        # Clean up build temp files
        Remove-Item $tempBuildOut, $tempBuildErr -Force -ErrorAction SilentlyContinue

    }
    catch {
        Write-ColoredMessage -Level 'ERROR' -Message "[$TestCategory] $Name`: Exception occurred - $($_.Exception.Message)"
        $script:FailedTests++
        $script:TestResults[$Name] = 'FAILED'

        Test-MaxFailuresReached -TestName $Name
    }
    finally {
        Pop-Location
        Invoke-IndividualTestCleanup -TestName $Name -TestResult $script:TestResults[$Name]
    }

    Write-Host '---'
}

function Invoke-IndividualTestCleanup {
    <#
    .SYNOPSIS
        Clean up individual test directory immediately after test completion
    #>
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

function Invoke-TestCategory {
    <#
    .SYNOPSIS
        Execute all tests within a specific category
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet(
            'component-isolation', 'database-config', 'port-config', 'org-names',
            'library-config', 'real-world-patterns', 'orleans-combinations', 'edge-cases'
        )]
        [string]$TestCategory
    )

    switch ($TestCategory) {
        'component-isolation' {
            Write-ColoredMessage -Level 'INFO' -Message "Running Category 1: Component Isolation Tests"

            $components = @(
                @{ Name = 'api'; TestName = 'TestApiOnly' },
                @{ Name = 'back-office'; TestName = 'TestBackOfficeOnly' },
                @{ Name = 'orleans'; TestName = 'TestOrleansOnly' },
                @{ Name = 'aspire'; TestName = 'TestAspireOnly' },
                @{ Name = 'docs'; TestName = 'TestDocsOnly' }
            )
            foreach ($component in $components) {
                $otherComponents = $components |
                    Where-Object { $_.Name -ne $component.Name } |
                    ForEach-Object { "--$($_.Name) false" }
                $params = "--$($component.Name) true " + ($otherComponents -join ' ')
                Test-Template -Name $component.TestName -Parameters $params -TestCategory 'Component Isolation'
            }

        }

        'database-config' {
            Write-ColoredMessage -Level 'INFO' -Message "Running Category 2: Database Configuration Tests"

            $databases = @(
                @{ Name = 'none'; TestName = 'TestDbNone' },
                @{ Name = 'npgsql'; TestName = 'TestDbNpgsql' },
                @{ Name = 'liquibase'; TestName = 'TestDbLiquibase' }
            )
            foreach ($db in $databases) {
                Test-Template -Name $db.TestName -Parameters "--db $($db.Name)" -TestCategory 'Database Config'
            }

            # Test database + infrastructure combinations
            Test-Template -Name 'TestNoInfra' -Parameters '--db none --kafka false' -TestCategory 'Database Config'
            Test-Template -Name 'TestDbKafka' -Parameters '--db liquibase --kafka true --orleans true' -TestCategory 'Database Config'
        }

        'port-config' {
            Write-ColoredMessage -Level 'INFO' -Message "Running Category 3: Port Configuration Tests"

            $ports = @(1024, 5000, 9000, 65000)
            foreach ($port in $ports) {
                Test-Template -Name "TestPort$port" -Parameters "--port $port" -TestCategory 'Port Config'
            }
        }

        'org-names' {
            Write-ColoredMessage -Level 'INFO' -Message "Running Category 4: Organization Name Tests"

            Test-Template -Name 'TestOrgSpecial' -Parameters '--org "Company-Name.Inc"' -TestCategory 'Organization Names'
            Test-Template -Name 'TestOrgNumbers' -Parameters '--org "123 Corp"' -TestCategory 'Organization Names'
            Test-Template -Name 'TestOrgAmpersand' -Parameters '--org "My Company & Partners"' -TestCategory 'Organization Names'
        }

        'library-config' {
            Write-ColoredMessage -Level 'INFO' -Message "Running Category 5: Library Configuration Tests"

            $libraries = @(
                @{ Name = 'defaults'; TestName = 'TestLibDefaults' },
                @{ Name = 'api'; TestName = 'TestLibApi' },
                @{ Name = 'ext'; TestName = 'TestLibExt' },
                @{ Name = 'kafka'; TestName = 'TestLibKafka' },
                @{ Name = 'generators'; TestName = 'TestLibGenerators' }
            )
            foreach ($lib in $libraries) {
                Test-Template -Name $lib.TestName -Parameters "--libs $($lib.Name)" -TestCategory 'Library Config'
            }

            # Test library combinations
            Test-Template -Name 'TestLibMulti' -Parameters '--libs defaults --libs api --libs kafka' -TestCategory 'Library Config'
            Test-Template -Name 'TestLibCustomName' -Parameters '--libs defaults --libs ext --lib-name CustomPlatform' -TestCategory 'Library Config'
        }

        'real-world-patterns' {
            Write-ColoredMessage -Level 'INFO' -Message "Running Category 6: Real-World Architecture Patterns"

            Test-Template -Name 'TestMicroservice' -Parameters '--api true --back-office false --kafka true --db npgsql' -TestCategory 'Real-World Patterns'
            Test-Template -Name 'TestEventProcessor' -Parameters '--api false --back-office true --kafka true --orleans true' -TestCategory 'Real-World Patterns'
            Test-Template -Name 'TestAPIGateway' -Parameters '--api true --no-sample --kafka false --db none' -TestCategory 'Real-World Patterns'
            Test-Template -Name 'TestFullStack' -Parameters '--orleans true --api true --back-office true --aspire true --libs defaults --libs api --libs kafka' -TestCategory 'Real-World Patterns'
        }

        'orleans-combinations' {
            Write-ColoredMessage -Level 'INFO' -Message "Running Category 7: Orleans Combinations"

            Test-Template -Name 'TestOrleansAPI' -Parameters '--orleans true --api true --aspire true' -TestCategory 'Orleans Combinations'
            Test-Template -Name 'TestOrleansFullStack' -Parameters '--orleans true --api true --back-office true --aspire true' -TestCategory 'Orleans Combinations'
            Test-Template -Name 'TestOrleansNoKafka' -Parameters '--orleans true --kafka false' -TestCategory 'Orleans Combinations'
            Test-Template -Name 'TestOrleansNoSample' -Parameters '--orleans true --no-sample' -TestCategory 'Orleans Combinations'
        }

        'edge-cases' {
            Write-ColoredMessage -Level 'INFO' -Message "Running Category 8: Edge Cases and Special Configurations"

            Test-Template -Name 'TestMinimal' -Parameters '--project-only --no-sample' -TestCategory 'Edge Cases'
            Test-Template -Name 'TestBareMin' -Parameters '--api false --back-office false --aspire false --docs false' -TestCategory 'Edge Cases'
            Test-Template -Name 'TestNoSample' -Parameters '--no-sample' -TestCategory 'Edge Cases'
            Test-Template -Name 'TestDebugSymbols' -Parameters '--debug-symbols' -TestCategory 'Edge Cases'
            $allFalseParams = '--api false --back-office false --orleans false --docs false --aspire false --kafka false'
            Test-Template -Name 'TestAllFalse' -Parameters $allFalseParams -TestCategory 'Edge Cases'
            
            $allTrueParams = '--api true --back-office true --orleans true --docs true --aspire true --kafka true'
            Test-Template -Name 'TestAllTrue' -Parameters $allTrueParams -TestCategory 'Edge Cases'
        }

        default {
            Write-ColoredMessage -Level 'ERROR' -Message "Unknown category: $TestCategory"
            exit 1
        }
    }
}

function Show-Categories {
    <#
    .SYNOPSIS
        Display all available test categories with descriptions
    #>
    [CmdletBinding()]
    param()

    Write-Host 'Available Test Categories:' -ForegroundColor Cyan
    Write-Host ''
    Write-Host 'Use -MaxFailures N to exit after N failures (default: 0 = run all)' -ForegroundColor Yellow
    Write-Host ''
    
    $categories = @(
        @{ Number = '1'; Name = 'component-isolation'; Description = 'Test each component in isolation' }
        @{ Number = '2'; Name = 'database-config'; Description = 'Validate all database setup options' }
        @{ Number = '3'; Name = 'port-config'; Description = 'Test port boundaries and common conflicts' }
        @{ Number = '4'; Name = 'org-names'; Description = 'Validate special characters handling' }
        @{ Number = '5'; Name = 'library-config'; Description = 'Test library reference combinations' }
        @{ Number = '6'; Name = 'real-world-patterns'; Description = 'Validate common deployment scenarios' }
        @{ Number = '7'; Name = 'orleans-combinations'; Description = 'Test stateful processing configurations' }
        @{ Number = '8'; Name = 'edge-cases'; Description = 'Test boundary conditions and special modes' }
    )
    
    foreach ($category in $categories) {
        $paddedName = $category.Name.PadRight(20)
        Write-Host "$($category.Number). $paddedName - $($category.Description)" -ForegroundColor White
    }
}

function Show-Results {
    <#
    .SYNOPSIS
        Display final test results and summary
    #>
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
    Write-ColoredMessage -Level 'INFO' -Message "Test execution directory: $script:ExecutionTestDir"
    Write-ColoredMessage -Level 'INFO' -Message "Full test results saved to: $LogFile"

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
    <#
    .SYNOPSIS
        Clean up test execution directory and artifacts
    #>
    [CmdletBinding()]
    param()

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
            Write-ColoredMessage -Level 'INFO' -Message "Cleaned up empty execution directory: $script:ExecutionTimestamp"
        }
        catch {
            try {
                Remove-Item -Path $script:ExecutionTestDir -Recurse -Force -ErrorAction Stop
                Write-ColoredMessage -Level 'INFO' -Message "Cleaned up execution directory (fallback): $script:ExecutionTimestamp"
            }
            catch {
                Write-ColoredMessage -Level 'WARN' -Message "Failed to clean up execution directory: $script:ExecutionTimestamp - $($_.Exception.Message)"
            }
        }
    }
}

function Invoke-Main {
    <#
    .SYNOPSIS
        Main execution function for the template test suite
    #>
    [CmdletBinding()]
    param()

    try {
        if ($ListCategories) {
            Show-Categories
            return
        }

        # Verify template is installed
        $templateList = & dotnet new list 2>$null
        if (-not ($templateList -match 'mmt')) {
            Write-ColoredMessage -Level 'ERROR' -Message 'Momentum template (mmt) not installed. Run: dotnet new install .'
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

        # Run specific category or all categories
        if ($Category) {
            Invoke-TestCategory -TestCategory $Category
        }
        else {
            $allCategories = @(
                'component-isolation',
                'database-config', 
                'port-config',
                'org-names',
                'library-config',
                'real-world-patterns',
                'orleans-combinations',
                'edge-cases'
            )

            foreach ($cat in $allCategories) {
                Invoke-TestCategory -TestCategory $cat
            }
        }

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

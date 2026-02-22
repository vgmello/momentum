# Parallel Template Tests Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add `-Parallel` switch to `Run-TemplateTests.ps1` that runs template tests 4-at-a-time via `ForEach-Object -Parallel`.

**Architecture:** Two-phase collect-then-execute. `Invoke-TestCategory` collects test definitions into an array. A new `Invoke-TestExecution` function either runs them sequentially (default) or pipes them to `ForEach-Object -Parallel -ThrottleLimit 4`. Parallel iterations return result hashtables; the main thread handles all logging and counter updates.

**Tech Stack:** PowerShell 7 `ForEach-Object -Parallel`, `Start-Process -WorkingDirectory`

---

### Task 1: Add `-Parallel` parameter and help text

**Files:**
- Modify: `scripts/Run-TemplateTests.ps1:49-105` (param block and help)

**Step 1: Add parameter documentation**

Add after the `.PARAMETER SkipRestoreTemplate` block (line 59):

```powershell
.PARAMETER Parallel
    Run tests in parallel using PowerShell 7 ForEach-Object -Parallel.
    Tests run 4-at-a-time. Console output is batched per test (not interleaved).
    Default: false (sequential execution).
```

Add example after the last `.EXAMPLE` block (line 75):

```powershell
.EXAMPLE
    ./Run-TemplateTests.ps1 -Parallel
    Run all tests with up to 4 tests executing concurrently
```

**Step 2: Add parameter declaration**

Add after `[switch]$SkipRestoreTemplate` (line 104), with a comma after `SkipRestoreTemplate`:

```powershell
    [Parameter(ParameterSetName = 'Run')]
    [switch]$Parallel
```

**Step 3: Verify script still parses**

Run: `pwsh -c '& ./scripts/Run-TemplateTests.ps1 -ListCategories' 2>&1 | head -5`
Expected: Shows category list without errors.

**Step 4: Commit**

```bash
git add scripts/Run-TemplateTests.ps1
git commit -m "feat: add -Parallel parameter to template test script"
```

---

### Task 2: Create `Invoke-SingleTest` function (thread-safe test runner)

This is the core test logic extracted from `Test-Template`, refactored to be self-contained and thread-safe. It uses `-WorkingDirectory` instead of `Push-Location`/`Set-Location`, takes all dependencies as parameters, and returns a result hashtable instead of mutating `$script:` variables.

**Files:**
- Modify: `scripts/Run-TemplateTests.ps1` (add new function before `Test-Template`)

**Step 1: Add `Invoke-SingleTest` function**

Add this function before `Test-Template` (before line 262). This function:
- Takes explicit parameters for everything it needs (no `$script:` access)
- Uses `-WorkingDirectory` on `Start-Process` instead of `Push-Location`/`Set-Location`
- Returns a result hashtable with messages array for deferred logging
- Does NOT call `Write-ColoredMessage`, `Test-MaxFailuresReached`, or `Invoke-IndividualTestCleanup`

```powershell
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
            $messages.Add(@{ Level = 'ERROR'; Message = "[$TestCategory] $Name`: Generation failed ($($genSw.Elapsed.TotalSeconds.ToString('F1'))s)$errorSummary" })
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
```

**Step 2: Verify script still parses**

Run: `pwsh -c '& ./scripts/Run-TemplateTests.ps1 -ListCategories' 2>&1 | head -5`
Expected: Shows category list without errors.

**Step 3: Commit**

```bash
git add scripts/Run-TemplateTests.ps1
git commit -m "feat: add Invoke-SingleTest function for thread-safe test execution"
```

---

### Task 3: Refactor `Invoke-TestCategory` to collect test definitions

Change `Invoke-TestCategory` from calling `Test-Template` directly to returning an array of test definition hashtables.

**Files:**
- Modify: `scripts/Run-TemplateTests.ps1:481-594` (`Invoke-TestCategory` function)

**Step 1: Rename and refactor `Invoke-TestCategory` to `Get-TestDefinitions`**

Replace the entire `Invoke-TestCategory` function. Instead of calling `Test-Template`, each branch builds and returns an array of `@{ Name; Parameters; TestCategory; ContentMustNotContain }` hashtables.

```powershell
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
```

**Step 2: Verify script still parses**

Run: `pwsh -c '& ./scripts/Run-TemplateTests.ps1 -ListCategories' 2>&1 | head -5`
Expected: Shows category list without errors.

**Step 3: Commit**

```bash
git add scripts/Run-TemplateTests.ps1
git commit -m "refactor: extract Get-TestDefinitions from Invoke-TestCategory"
```

---

### Task 4: Add `Invoke-TestExecution` and wire up `Invoke-Main`

Add the orchestrator function that handles both sequential and parallel execution, then update `Invoke-Main` to use the new collect-then-execute flow.

**Files:**
- Modify: `scripts/Run-TemplateTests.ps1` (add `Invoke-TestExecution`, update `Invoke-Main`)

**Step 1: Add `Invoke-TestExecution` function**

Add this function after `Get-TestDefinitions`. This handles:
- Sequential mode: calls `Invoke-SingleTest` in a loop, logs results in real-time, handles `MaxFailures`
- Parallel mode: pipes tests to `ForEach-Object -Parallel`, collects results, replays messages, handles `MaxFailures` between completions
- Both modes: updates `$script:` counters, calls `Invoke-IndividualTestCleanup`

```powershell
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
```

**Step 2: Update `Invoke-Main` to use collect-then-execute**

Replace the category execution block in `Invoke-Main` (the section starting from `# Run specific category or all categories` through the `foreach` loop). The new code:

```powershell
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
```

**Step 3: Remove old `Test-Template`, `Test-MaxFailuresReached`, and `Invoke-TestCategory`**

These functions are fully replaced by `Invoke-SingleTest`, `Invoke-TestExecution`, and `Get-TestDefinitions`. Delete them.

**Step 4: Verify script still parses**

Run: `pwsh -c '& ./scripts/Run-TemplateTests.ps1 -ListCategories' 2>&1 | head -5`
Expected: Shows category list without errors.

**Step 5: Commit**

```bash
git add scripts/Run-TemplateTests.ps1
git commit -m "feat: add parallel test execution with ForEach-Object -Parallel"
```

---

### Task 5: Test sequential mode (regression)

Run the script without `-Parallel` to verify the refactoring didn't break anything.

**Step 1: Run a quick single-category test**

Run: `pwsh -c '& ./scripts/Run-TemplateTests.ps1 -Category edge-cases -MaxFailures 1 -SkipRestoreTemplate'`

Expected: Tests execute sequentially with timing output. Generation, build, and test phases log as before. Passes and failures update correctly.

**Step 2: Verify output format**

Expected output lines should include timestamps, level, and timing:
```
[YYYY-MM-DD HH:mm:ss] [SUCCESS] [Edge Cases] TestMinimal: Generation succeeded (Xs)
[YYYY-MM-DD HH:mm:ss] [SUCCESS] [Edge Cases] TestMinimal: Build succeeded (N projects, Xs)
[YYYY-MM-DD HH:mm:ss] [INFO] [Edge Cases] TestMinimal: Total time: Xs (gen: Xs, build: Xs)
```

**Step 3: Commit if fixes were needed**

```bash
git add scripts/Run-TemplateTests.ps1
git commit -m "fix: adjust sequential execution after refactor"
```

---

### Task 6: Test parallel mode

Run the script with `-Parallel` to verify concurrent execution.

**Step 1: Run a parallel category test**

Run: `pwsh -c '& ./scripts/Run-TemplateTests.ps1 -Category component-isolation -Parallel -SkipRestoreTemplate'`

Expected: Output shows "Running 5 tests in parallel (4 concurrent)..." then batched results per test.

**Step 2: Verify results are correct**

- Total tests count matches number of test definitions
- Pass/fail counts are accurate
- Timing shows wall-clock benefit (total time less than sum of individual tests)

**Step 3: Commit if fixes were needed**

```bash
git add scripts/Run-TemplateTests.ps1
git commit -m "fix: adjust parallel execution after testing"
```

---

### Task 7: Update CI to use parallel flag

**Files:**
- Modify: `.github/workflows/ci.yml:181`

**Step 1: Add `-Parallel` flag to CI template test step**

Change line 181 from:
```yaml
          & ./scripts/Run-TemplateTests.ps1 -Category real-world-patterns -MaxFailures 3 -KeepResultsWithErrors:$false -SkipRestoreTemplate
```
to:
```yaml
          & ./scripts/Run-TemplateTests.ps1 -Category real-world-patterns -MaxFailures 3 -KeepResultsWithErrors:$false -SkipRestoreTemplate -Parallel
```

**Step 2: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: enable parallel template test execution"
```

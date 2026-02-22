# Parallel Template Test Execution

**Date**: 2026-02-22
**Status**: Approved

## Goal

Add a `-Parallel` switch to `Run-TemplateTests.ps1` that runs template tests 4-at-a-time using PowerShell 7's `ForEach-Object -Parallel`, for both local dev and CI.

## Design

### Parameter

- `[switch]$Parallel` — opt-in flag, default off (sequential as today)
- Throttle limit hardcoded to 4 concurrent tests

### Two-Phase Execution

1. **Collection phase**: `Invoke-TestCategory` collects test definitions into an array of hashtables instead of calling `Test-Template` directly:
   ```powershell
   @{ Name = 'TestApiOnly'; Parameters = '--api true ...'; TestCategory = 'Component Isolation'; ContentMustNotContain = @() }
   ```

2. **Execution phase**: Array is either piped to `ForEach-Object -Parallel` (parallel mode) or looped sequentially (default). Both use the same test logic.

### Refactoring for Thread Safety

- Replace `Push-Location`/`Set-Location` with explicit `-WorkingDirectory` on `Start-Process` calls — no shared CWD mutation
- Parallel block returns a result hashtable per test — main thread does all `$script:` counter updates and logging
- `dotnet build-server shutdown` runs once after all parallel jobs complete (not per-test)
- `MaxFailures` checked between job completions; can't cancel running jobs but won't launch new ones

### Result Object

Each parallel iteration returns:
```powershell
@{
    Name          = 'TestApiOnly'
    Category      = 'Component Isolation'
    Result        = 'PASSED'
    Messages      = @()          # Array of @{ Level; Message } for ordered replay
    GenDuration   = [TimeSpan]
    BuildDuration = [TimeSpan]
    TestDuration  = [TimeSpan]
    TotalDuration = [TimeSpan]
    ProjectCount  = 6
    ErrorSummary  = ''
}
```

### Console Output

- **Sequential mode**: No change — real-time streaming as today
- **Parallel mode**: Results logged as each job completes, batched per test (not interleaved). Detailed logs still go to per-test files.

### CI Integration

Update `.github/workflows/ci.yml` to pass `-Parallel` flag.

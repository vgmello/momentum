[CmdletBinding()]
param(
    [switch]$ShowDetails,
    [string]$TagPrefix = "test-"
)

$ErrorActionPreference = "Stop"

# Test script configuration
$ScriptPath = "./Calculate-Version.ps1"
$TestVersionFile = "test-version.txt"
$TestResults = @()
[int]$FailureCount = 0

# ANSI color codes for output
$Red = "`e[31m"
$Green = "`e[32m"
$Blue = "`e[34m"
$Reset = "`e[0m"

# Comprehensive test cases from VERSION_CALCULATION_TEST_CASES.md
$AllTestCases = @{
    # 1. First Release Scenarios
    "T1.1" = @{ Description = "First stable release"; FileVersion = "1.0.0"; ExistingTags = @(); Prerelease = $false; Expected = "1.0.0" }
    "T1.2" = @{ Description = "First prerelease"; FileVersion = "1.0.0"; ExistingTags = @(); Prerelease = $true; Expected = "1.0.0-preview.1" }
    "T1.3" = @{ Description = "First prerelease with custom label"; FileVersion = "1.0.0-alpha"; ExistingTags = @(); Prerelease = $true; Expected = "1.0.0-alpha.1" }

    # 2. Same Base Version Scenarios (Core Fix)
    "T2.1" = @{ Description = "Same base, increment existing prerelease"; FileVersion = "0.0.1"; ExistingTags = @("0.0.1-preview.1"); Prerelease = $true; Expected = "0.0.1-preview.2" }
    "T2.2" = @{ Description = "Same base, increment higher prerelease"; FileVersion = "0.0.1"; ExistingTags = @("0.0.1-preview.3"); Prerelease = $true; Expected = "0.0.1-preview.4" }
    "T2.3" = @{ Description = "Same base, different prerelease type"; FileVersion = "0.0.1-rc"; ExistingTags = @("0.0.1-preview.1"); Prerelease = $true; Expected = "0.0.1-rc.1" }
    "T2.4" = @{ Description = "Same base, same prerelease type"; FileVersion = "0.0.1-rc"; ExistingTags = @("0.0.1-rc.2"); Prerelease = $true; Expected = "0.0.1-rc.3" }
    "T2.5" = @{ Description = "Same base, earlier prerelease type"; FileVersion = "0.0.1-alpha"; ExistingTags = @("0.0.1-beta.1"); Prerelease = $true; Expected = "0.0.1-alpha.1" }
    "T2.6" = @{ Description = "Same base, latest is stable"; FileVersion = "0.0.1"; ExistingTags = @("0.0.1"); Prerelease = $true; Expected = "0.0.2-preview.1" }
    "T2.7" = @{ Description = "Same base, promote to stable"; FileVersion = "0.0.1"; ExistingTags = @("0.0.1-preview.1"); Prerelease = $false; Expected = "0.0.1" }

    # 3. File Version Greater Than Latest Release
    "T3.1" = @{ Description = "Higher patch, new prerelease"; FileVersion = "0.0.2"; ExistingTags = @("0.0.1"); Prerelease = $true; Expected = "0.0.2-preview.1" }
    "T3.2" = @{ Description = "Higher minor, new prerelease"; FileVersion = "0.1.0"; ExistingTags = @("0.0.5"); Prerelease = $true; Expected = "0.1.0-preview.1" }
    "T3.3" = @{ Description = "Higher major, new prerelease"; FileVersion = "1.0.0"; ExistingTags = @("0.9.9"); Prerelease = $true; Expected = "1.0.0-preview.1" }
    "T3.4" = @{ Description = "Higher base with custom label"; FileVersion = "0.0.2-beta"; ExistingTags = @("0.0.1-preview.1"); Prerelease = $true; Expected = "0.0.2-beta.1" }
    "T3.5" = @{ Description = "Higher patch, stable release"; FileVersion = "0.0.2"; ExistingTags = @("0.0.1-preview.1"); Prerelease = $false; Expected = "0.0.2" }

    # 4. File Version Less Than Latest Release
    "T4.1" = @{ Description = "Lower patch, increment latest prerelease"; FileVersion = "0.0.1"; ExistingTags = @("0.0.2-preview.1"); Prerelease = $true; Expected = "0.0.2-preview.2" }
    "T4.2" = @{ Description = "Lower patch, latest is stable"; FileVersion = "0.0.1"; ExistingTags = @("0.0.2"); Prerelease = $true; Expected = "0.0.3-preview.1" }
    "T4.3" = @{ Description = "Lower minor, increment latest prerelease"; FileVersion = "0.0.1"; ExistingTags = @("0.1.0-alpha.1"); Prerelease = $true; Expected = "0.1.0-alpha.2" }
    "T4.4" = @{ Description = "Lower version, stable increment"; FileVersion = "0.0.1"; ExistingTags = @("0.1.0"); Prerelease = $false; Expected = "0.1.1" }

    # 5. Prerelease Type Progression
    "T5.1" = @{ Description = "Start alpha cycle"; FileVersion = "1.0.0-alpha"; ExistingTags = @(); Prerelease = $true; Expected = "1.0.0-alpha.1" }
    "T5.2" = @{ Description = "Alpha → Beta"; FileVersion = "1.0.0-beta"; ExistingTags = @("1.0.0-alpha.3"); Prerelease = $true; Expected = "1.0.0-beta.1" }
    "T5.3" = @{ Description = "Beta → Preview"; FileVersion = "1.0.0-preview"; ExistingTags = @("1.0.0-beta.2"); Prerelease = $true; Expected = "1.0.0-preview.1" }
    "T5.4" = @{ Description = "Preview → RC"; FileVersion = "1.0.0-rc"; ExistingTags = @("1.0.0-preview.5"); Prerelease = $true; Expected = "1.0.0-rc.1" }
    "T5.5" = @{ Description = "RC → Stable"; FileVersion = "1.0.0"; ExistingTags = @("1.0.0-rc.2"); Prerelease = $false; Expected = "1.0.0" }

    # 6. Edge Cases
    "T6.1" = @{ Description = "File has number, should ignore"; FileVersion = "1.0.0-preview.5"; ExistingTags = @("1.0.0-preview.2"); Prerelease = $true; Expected = "1.0.0-preview.3" }
    "T6.2" = @{ Description = "Custom prerelease label"; FileVersion = "1.0.0-custom"; ExistingTags = @("1.0.0-preview.1"); Prerelease = $true; Expected = "1.0.0-custom.1" }
    "T6.3" = @{ Description = "Complex prerelease label"; FileVersion = "1.0.0-rc.candidate"; ExistingTags = @(); Prerelease = $true; Expected = "1.0.0-rc.1" }
    "T6.4" = @{ Description = "Numeric prerelease label"; FileVersion = "1.0.0-123"; ExistingTags = @(); Prerelease = $true; Expected = "1.0.0-123.1" }

    # 7. Multi-digit Version Numbers
    "T7.1" = @{ Description = "Large version numbers"; FileVersion = "12.34.56"; ExistingTags = @("12.34.55"); Prerelease = $true; Expected = "12.34.56-preview.1" }
    "T7.2" = @{ Description = "Double-digit prerelease number"; FileVersion = "12.34.56"; ExistingTags = @("12.34.56-preview.10"); Prerelease = $true; Expected = "12.34.56-preview.11" }
    "T7.3" = @{ Description = "Very high prerelease number"; FileVersion = "10.0.0"; ExistingTags = @("10.0.0-beta.99"); Prerelease = $true; Expected = "10.0.0-beta.100" }

    # 8. Cross-Major Version Scenarios
    "T8.1" = @{ Description = "Different major version, no matching tags"; FileVersion = "2.0.0"; ExistingTags = @("1.5.0", "1.4.0-preview.1"); Prerelease = $true; Expected = "2.0.0-preview.1" }
    "T8.2" = @{ Description = "Different major version, stable release"; FileVersion = "2.0.0"; ExistingTags = @("1.5.0", "1.4.0-preview.1"); Prerelease = $false; Expected = "2.0.0" }
    "T8.3" = @{ Description = "Higher major with existing major tags"; FileVersion = "3.0.0"; ExistingTags = @("2.1.0", "2.0.0-rc.1", "1.9.0"); Prerelease = $true; Expected = "3.0.0-preview.1" }

    # 9. Tag Prefix Variations
    "T9.1" = @{ Description = "Single character tag prefix"; FileVersion = "1.0.0"; ExistingTags = @("0.9.0"); Prerelease = $true; Expected = "1.0.0-preview.1"; CustomTagPrefix = "r" }
    "T9.2" = @{ Description = "Custom tag prefix"; FileVersion = "1.0.0"; ExistingTags = @("0.9.0"); Prerelease = $true; Expected = "1.0.0-preview.1"; CustomTagPrefix = "release-" }
    "T9.3" = @{ Description = "Special character prefix"; FileVersion = "1.0.0"; ExistingTags = @("0.9.0"); Prerelease = $true; Expected = "1.0.0-preview.1"; CustomTagPrefix = "v@" }

    # 10. Complex Prerelease Label Scenarios  
    "T10.1" = @{ Description = "Nested prerelease label"; FileVersion = "1.0.0-alpha.beta"; ExistingTags = @(); Prerelease = $true; Expected = "1.0.0-alpha.1" }
    "T10.2" = @{ Description = "Very long prerelease label"; FileVersion = "1.0.0-very-long-custom-label"; ExistingTags = @(); Prerelease = $true; Expected = "1.0.0-very-long-custom-label.1" }
    "T10.3" = @{ Description = "Prerelease with dashes"; FileVersion = "1.0.0-rc-candidate"; ExistingTags = @(); Prerelease = $true; Expected = "1.0.0-rc-candidate.1" }

    # 11. Git Repository Edge Cases
    "T11.1" = @{ Description = "No git tags in repository"; FileVersion = "1.0.0"; ExistingTags = @(); Prerelease = $false; Expected = "1.0.0" }
    "T11.2" = @{ Description = "No matching major version tags"; FileVersion = "5.0.0"; ExistingTags = @("1.0.0", "2.0.0", "3.0.0"); Prerelease = $true; Expected = "5.0.0-preview.1" }
    "T11.3" = @{ Description = "Mixed tag formats in repo"; FileVersion = "1.0.0"; ExistingTags = @("0.9.0", "not-a-version", "0.8.0-preview.1"); Prerelease = $true; Expected = "1.0.0-preview.1" }

    # 12. Boundary and Extremes
    "T12.1" = @{ Description = "Zero version numbers"; FileVersion = "0.0.0"; ExistingTags = @(); Prerelease = $true; Expected = "0.0.0-preview.1" }
    "T12.2" = @{ Description = "Single digit versions"; FileVersion = "1.0.0"; ExistingTags = @("1.0.0-preview.999"); Prerelease = $true; Expected = "1.0.0-preview.1000" }
    "T12.3" = @{ Description = "Same version files and tags"; FileVersion = "1.2.3-alpha"; ExistingTags = @("1.2.3-alpha.5"); Prerelease = $true; Expected = "1.2.3-alpha.6" }
}

function Write-ColorOutput {
    param([string]$Message, [string]$Color = $Reset)
    Write-Host "$Color$Message$Reset"
}

function Write-TestResult {
    param(
        [string]$TestCase,
        [string]$Description,
        [bool]$Passed,
        [string]$Expected,
        [string]$Actual
    )
    
    $status = if ($Passed) { "${Green}✅ PASS${Reset}" } else { "${Red}❌ FAIL${Reset}" }
    Write-Host "[$TestCase] $status - $Description"
    
    if (!$Passed) {
        Write-ColorOutput "  Expected: $Expected" $Red
        Write-ColorOutput "  Actual:   $Actual" $Red
    }
}

function Setup-TestEnvironment {
    Write-ColorOutput "`n🔧 Setting up test environment..." $Blue
    
    if (!(Test-Path $ScriptPath)) {
        throw "Version calculation script not found at: $ScriptPath"
    }
    
    Remove-Item $TestVersionFile -ErrorAction SilentlyContinue
    
    $existingTestTags = @(git tag -l "${TagPrefix}*" 2>$null)
    if ($existingTestTags.Count -gt 0) {
        $existingTestTags | ForEach-Object { git tag -d $_ 2>$null | Out-Null }
    }
}

function Run-TestCase {
    param([string]$TestId, [hashtable]$TestCase)
    
    try {
        # Determine tag prefix for this test case
        $testTagPrefix = if ($TestCase.ContainsKey("CustomTagPrefix")) { $TestCase.CustomTagPrefix } else { $TagPrefix }
        
        # Setup tags
        foreach ($tag in $TestCase.ExistingTags) {
            git tag "${testTagPrefix}${tag}" 2>$null | Out-Null
        }
        
        # Create version file
        $TestCase.FileVersion | Out-File -FilePath $TestVersionFile -Encoding utf8 -NoNewline
        
        # Build parameters
        $scriptParams = @{
            VersionFile = $TestVersionFile
            TagPrefix = $testTagPrefix
        }
        if ($TestCase.Prerelease) {
            $scriptParams.Prerelease = $true
        }
        
        # Run script and capture output
        $output = & $ScriptPath @scriptParams *>&1 | Out-String
        $cleanOutput = $output -replace '\e\[[0-9;]*m', ''
        
        if ($cleanOutput -match "Calculated version:\s*([^\r\n]+)") {
            $actualVersion = $Matches[1].Trim()
            $passed = $actualVersion -eq $TestCase.Expected
            
            Write-TestResult -TestCase $TestId -Description $TestCase.Description -Passed $passed -Expected $TestCase.Expected -Actual $actualVersion
            
            if (!$passed) {
                $script:FailureCount++
                if ($ShowDetails) {
                    Write-Host "Full output:"
                    Write-Host $output
                }
            }
        }
        else {
            Write-TestResult -TestCase $TestId -Description $TestCase.Description -Passed $false -Expected $TestCase.Expected -Actual "No version found"
            $script:FailureCount++
        }
    }
    catch {
        Write-TestResult -TestCase $TestId -Description $TestCase.Description -Passed $false -Expected $TestCase.Expected -Actual "ERROR: $($_.Exception.Message)"
        $script:FailureCount++
    }
    finally {
        # Clean up tags with correct prefix
        $testTagPrefix = if ($TestCase.ContainsKey("CustomTagPrefix")) { $TestCase.CustomTagPrefix } else { $TagPrefix }
        $TestCase.ExistingTags | ForEach-Object {
            git tag -d "${testTagPrefix}${_}" 2>$null | Out-Null
        }
    }
}

function Show-TestSummary {
    $totalTests = $AllTestCases.Count
    $passedTests = $totalTests - $FailureCount
    $successRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 }
    
    Write-ColorOutput "`n==================== TEST SUMMARY ====================" $Blue
    Write-ColorOutput "Total Tests: $totalTests" $Blue
    Write-ColorOutput "Passed: $passedTests" $Green
    Write-ColorOutput "Failed: $FailureCount" $(if ($FailureCount -eq 0) { $Green } else { $Red })
    Write-ColorOutput "Success Rate: ${successRate}%" $(if ($FailureCount -eq 0) { $Green } else { $Red })
}

function Cleanup-TestEnvironment {
    Write-ColorOutput "`n🧹 Cleaning up test environment..." $Blue
    Remove-Item $TestVersionFile -ErrorAction SilentlyContinue
    $testTags = @(git tag -l "${TagPrefix}*" 2>$null)
    if ($testTags.Count -gt 0) {
        $testTags | ForEach-Object { git tag -d $_ 2>$null | Out-Null }
    }
}

# Main execution
try {
    Write-ColorOutput "==================== VERSION CALCULATION TEST RUNNER ====================" $Blue
    
    Setup-TestEnvironment
    
    Write-ColorOutput "Running $($AllTestCases.Count) critical test cases..." $Blue
    Write-ColorOutput "Tag Prefix: $TagPrefix" $Blue
    
    # Run all tests
    foreach ($testId in $AllTestCases.Keys | Sort-Object) {
        Run-TestCase -TestId $testId -TestCase $AllTestCases[$testId]
    }
    
    Show-TestSummary
    
    if ($FailureCount -gt 0) {
        exit 1
    } else {
        exit 0
    }
}
catch {
    Write-ColorOutput "❌ Test execution failed: $($_.Exception.Message)" $Red
    exit 1
}
finally {
    Cleanup-TestEnvironment
}
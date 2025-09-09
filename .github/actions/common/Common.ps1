function Write-GitHubOutput {
    param([string]$Name, [string]$Value)
    if ($env:GITHUB_OUTPUT) {
        "$Name=$Value" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
    Write-Host "Output: $Name=$Value"
}

function Write-GitHubMultilineOutput {
    param([string]$Name, [string[]]$Values)
    if ($env:GITHUB_OUTPUT) {
        "$Name<<EOF" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
        $Values | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
        "EOF" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    }
}

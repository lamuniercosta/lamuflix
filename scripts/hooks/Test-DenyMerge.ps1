#!/usr/bin/env pwsh
# Self-test for deny-merge.ps1 (LamuFlix-local).
#
#   pwsh ./scripts/hooks/Test-DenyMerge.ps1
#
# Exit 0 when every case matches, 1 otherwise.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$hook = Join-Path $PSScriptRoot 'deny-merge.ps1'
$failures = 0

function Invoke-Hook {
    param([string]$Tool, [string]$Command, [string[]]$ScriptArgs = @())
    $json = @{ tool_name = $Tool; tool_input = @{ command = $Command } } | ConvertTo-Json -Compress
    $stdout = @($json | & pwsh -NoProfile -File $hook @ScriptArgs 2>$null)
    return [PSCustomObject]@{ ExitCode = $LASTEXITCODE; StdOut = ($stdout -join '').Trim() }
}

function Assert-Case {
    param([string]$Name, [bool]$Condition)
    if ($Condition) { Write-Host "PASS $Name"; return }
    Write-Host "FAIL $Name"
    $script:failures++
}

$blocked = @(
    @('Bash', 'gh pr merge 12 --squash'),
    @('Bash', 'gh -R owner/repo pr merge 12 --auto'),
    @('PowerShell', 'gh pr   merge'),
    @('Shell', 'gh api -X PUT repos/o/r/pulls/12/merge'),
    @('Bash', 'gh api graphql -f query="mutation { enablePullRequestAutoMerge(input:{}) { clientMutationId } }"'),
    @('run_terminal_cmd', 'gh api graphql -f query="mutation { mergePullRequest(input:{}) { clientMutationId } }"')
)
foreach ($case in $blocked) {
    $result = Invoke-Hook -Tool $case[0] -Command $case[1]
    Assert-Case "blocks [$($case[0])] $($case[1])" ($result.ExitCode -eq 2)
}

$allowed = @(
    @('Bash', 'gh pr view 12 --json mergeable,mergeStateStatus'),
    @('Bash', 'gh pr create --fill'),
    @('Bash', 'gh pr checks 12 --watch'),
    @('Bash', 'git merge origin/main'),
    @('Bash', 'gh pr view 12; echo merge'),
    @('Edit', 'gh pr merge')
)
foreach ($case in $allowed) {
    $result = Invoke-Hook -Tool $case[0] -Command $case[1]
    Assert-Case "allows [$($case[0])] $($case[1])" ($result.ExitCode -eq 0)
}

$deny = Invoke-Hook -Tool 'Bash' -Command 'gh pr merge 1' -ScriptArgs @('-OutputContract', 'Cursor')
$denyJson = $deny.StdOut | ConvertFrom-Json
Assert-Case 'Cursor deny emits permission=deny JSON' ($deny.ExitCode -eq 2 -and $denyJson.permission -eq 'deny')

$allow = Invoke-Hook -Tool 'Bash' -Command 'ls' -ScriptArgs @('-OutputContract', 'Cursor')
Assert-Case 'Cursor allow emits permission=allow JSON' ($allow.ExitCode -eq 0 -and $allow.StdOut -eq '{"permission":"allow"}')

$null = 'not json' | & pwsh -NoProfile -File $hook 2>$null
Assert-Case 'unparseable payload is allowed' ($LASTEXITCODE -eq 0)

if ($failures -gt 0) {
    Write-Host "$failures deny-merge check(s) failed."
    exit 1
}
Write-Host 'All deny-merge checks passed.'
exit 0

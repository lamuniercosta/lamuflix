#!/usr/bin/env pwsh
# Offline self-test for Set-YouTrackState.ps1. No network, no real credentials.
#
#   pwsh ./scripts/local/Test-SetYouTrackState.ps1

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'Set-YouTrackState.ps1'
$failures = 0
$checks = 0

function Assert-True {
    param([string]$Name, [bool]$Condition)
    $script:checks++
    if ($Condition) { Write-Host "  ok       $Name" }
    else { Write-Host "  FAIL     $Name" -ForegroundColor Red; $script:failures++ }
}

# Runs the script in a child pwsh so its `exit` codes are observable.
function Invoke-SetState {
    param([string]$Ticket, [string]$State, [string]$ReadBackState, [switch]$NoUrl, [switch]$Throw)

    $log = Join-Path ([IO.Path]::GetTempPath()) ('ytstate-' + [guid]::NewGuid().ToString('N') + '.log')
    $driver = @"
`$env:YT_TEST_LOG = '$log'
`$envReader = { param(`$Name, `$Target)
    if (`$Target -ne 'Process') { return `$null }
    if (`$Name -eq 'YOUTRACK_URL') { return $(if ($NoUrl) { '$null' } else { "'https://yt.example/'" }) }
    if (`$Name -eq 'YOUTRACK_TOKEN') { return 'perm-test' }
}
`$invoker = { param(`$Method, `$Uri, `$Headers, `$Body)
    Add-Content -LiteralPath `$env:YT_TEST_LOG -Value "`$Method `$Uri `$Body auth=`$(`$Headers.Authorization)"
    if ($(if ($Throw) { '$true' } else { '$false' })) { throw 'HTTP 500 with perm-test in the message' }
    if (`$Method -eq 'Get') {
        return [pscustomobject]@{ idReadable = '$Ticket'; customFields = @(
            [pscustomobject]@{ name = 'Type'; value = [pscustomobject]@{ name = 'Task' } },
            [pscustomobject]@{ name = 'State'; value = $(if ($ReadBackState) { "[pscustomobject]@{ name = '$ReadBackState' }" } else { '$null' }) }) }
    }
    return [pscustomobject]@{}
}
# A parameter-validation failure never reaches the script's own exit codes.
try { & '$script' -Ticket '$Ticket' -State '$State' -EnvironmentReader `$envReader -RestMethodInvoker `$invoker -DpapiFileReader { `$null } }
catch { Write-Output "binding: `$(`$_.Exception.Message)"; exit 3 }
exit `$LASTEXITCODE
"@
    $output = @(& pwsh -NoProfile -Command $driver 2>&1)
    $code = $LASTEXITCODE
    $calls = @(if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log })
    if (Test-Path -LiteralPath $log) { [IO.File]::Delete($log) }
    return [pscustomobject]@{ ExitCode = $code; Output = ($output -join "`n"); Calls = $calls }
}

Write-Host 'Set-YouTrackState'

$done = Invoke-SetState -Ticket 'DEV-289' -State 'Done' -ReadBackState 'Done'
Assert-True 'exit 0 when read-back matches' ($done.ExitCode -eq 0)
Assert-True 'reports verified state' ($done.Output -match 'DEV-289 State: Done \(verified\)')
Assert-True 'posts the State command' ($done.Calls[0] -match '^Post https://yt\.example/api/commands .*"query":"State \{Done\}"')
Assert-True 'reads the issue back' ($done.Calls[1] -match '^Get https://yt\.example/api/issues/DEV-289\?fields=')
Assert-True 'sends the token as Bearer' ($done.Calls[0] -match 'auth=Bearer perm-test$')

$multi = Invoke-SetState -Ticket 'DEV-290' -State 'In Progress' -ReadBackState 'In Progress'
Assert-True 'multi-word state is braced and verified' ($multi.ExitCode -eq 0 -and $multi.Calls[0] -match '"query":"State \{In Progress\}"')

$caseInsensitive = Invoke-SetState -Ticket 'DEV-1' -State 'done' -ReadBackState 'Done'
Assert-True 'state comparison ignores case' ($caseInsensitive.ExitCode -eq 0)

$refused = Invoke-SetState -Ticket 'DEV-289' -State 'Done' -ReadBackState 'In Review'
Assert-True 'exit 1 when read-back differs' ($refused.ExitCode -eq 1)
Assert-True 'names actual and expected state' ($refused.Output -match "is 'In Review' after the command, expected 'Done'")

$unset = Invoke-SetState -Ticket 'DEV-289' -State 'Done'
Assert-True 'exit 1 when State is unset' ($unset.ExitCode -eq 1 -and $unset.Output -match '<unset>')

$noUrl = Invoke-SetState -Ticket 'DEV-289' -State 'Done' -ReadBackState 'Done' -NoUrl
Assert-True 'exit 2 without YOUTRACK_URL' ($noUrl.ExitCode -eq 2 -and $noUrl.Calls.Count -eq 0)

$http = Invoke-SetState -Ticket 'DEV-289' -State 'Done' -Throw
Assert-True 'exit 2 on HTTP failure' ($http.ExitCode -eq 2)
Assert-True 'redacts the token from errors' ($http.Output -notmatch 'perm-test' -and $http.Output -match '<redacted>')

$badTicket = Invoke-SetState -Ticket 'dev 289' -State 'Done' -ReadBackState 'Done'
Assert-True 'rejects a malformed ticket id' ($badTicket.ExitCode -eq 3 -and $badTicket.Calls.Count -eq 0)

Write-Host ''
if ($failures -gt 0) {
    Write-Host "$failures of $checks check(s) failed." -ForegroundColor Red
    exit 1
}
Write-Host "All $checks checks passed."
exit 0

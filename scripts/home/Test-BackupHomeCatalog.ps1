#Requires -Version 7.0
# Companion test for Backup-HomeCatalog.ps1. kind, kubectl and docker are stubs; no cluster is touched.
# Exit 0 when every case passes, 1 otherwise.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-test-lib.ps1')

$target = Join-Path $PSScriptRoot 'Backup-HomeCatalog.ps1'
$root = New-TestRoot
$bin = New-StubBin -Dir (Join-Path $root 'bin')
$rules = Join-Path $root 'rules.json'

function New-BackupOps {
    param([string]$Name)
    $ops = Join-Path $root $Name
    $backups = Join-Path $ops 'backups'
    New-Item -ItemType Directory -Force -Path $backups | Out-Null
    $kube = Join-Path $ops 'kubeconfig'
    Write-TestText -Path $kube -Text ''
    $cfg = Write-TestConfig -Path (Join-Path $ops 'home.config.json') -LibraryPath $root -BackupPath $backups -KubeconfigPath $kube
    return [pscustomobject]@{ Config = $cfg; Backups = $backups }
}

function Get-FileHashText {
    param([string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Count-LogLines {
    param($Result, [string]$Pattern)
    return @(@($Result.Log) | Where-Object { $_ -match $Pattern }).Count
}

# Case 1: the CronJob is missing in the cluster; no Job is created
$o1 = New-BackupOps 'case-nocronjob'
Write-StubRules -Path $rules -Rules @(
    @{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' },
    @{ tool = 'kubectl'; match = 'get cronjob'; exit = 1 }
)
$r1 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o1.Config) -BinDir $bin -LogPath (Join-Path $root 'c1.log') -RulesPath $rules
Assert-Case 'no CronJob: exit 1' ($r1.ExitCode -eq 1) "exit $($r1.ExitCode)"
Assert-Case 'no CronJob: names the CronJob check' ($r1.Output -match 'postgres-backup CronJob')
Assert-Case 'no CronJob: no Job created' (-not (Test-LogHas $r1 'create job'))

# Case 2: -WhatIf creates nothing
$o2 = New-BackupOps 'case-whatif'
Write-StubRules -Path $rules -Rules @(
    @{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' },
    @{ tool = 'kubectl'; match = 'get cronjob'; stdout = 'cronjob.batch/postgres-backup' }
)
$r2 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o2.Config, '-WhatIf') -BinDir $bin -LogPath (Join-Path $root 'c2.log') -RulesPath $rules
Assert-Case 'WhatIf: exit 0' ($r2.ExitCode -eq 0) "exit $($r2.ExitCode): $($r2.Output)"
Assert-Case 'WhatIf: no create or wait or delete calls' (-not (Test-LogHas $r2 'create job|wait --for|delete'))

# Case 3: happy path. An old nightly dump is present and must not be picked; the new one written by the Job is verified.
$o3 = New-BackupOps 'case-happy'
$oldDump = Join-Path $o3.Backups 'lamuflix-home-nightly-old.dump'
Write-TestText -Path $oldDump -Text 'old dump'
(Get-Item -LiteralPath $oldDump).LastWriteTime = Get-Date '2020-01-01T00:00:00'
$oldHash = Get-FileHashText $oldDump
$newDump = Join-Path $o3.Backups 'lamuflix-home-nightly-new.dump'
$sentinel3 = Join-Path $o3.Backups 'keep-me.txt'
Write-TestText -Path $sentinel3 -Text 'sentinel'
$sentinelHash3 = Get-FileHashText $sentinel3
Write-StubRules -Path $rules -Rules @(
    @{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' },
    @{ tool = 'kubectl'; match = 'get cronjob'; stdout = 'cronjob.batch/postgres-backup' },
    @{ tool = 'kubectl'; match = 'create job'; exit = 0 },
    @{ tool = 'kubectl'; match = 'wait --for=condition=complete'; exit = 0; createFile = $newDump },
    @{ tool = 'docker'; match = ' -l /restore/'; exit = 0 }
)
$r3 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o3.Config) -BinDir $bin -LogPath (Join-Path $root 'c3.log') -RulesPath $rules
Assert-Case 'happy: exit 0' ($r3.ExitCode -eq 0) "exit $($r3.ExitCode): $($r3.Output)"
Assert-Case 'happy: reports the new dump, not the old one' (($r3.Output -match 'lamuflix-home-nightly-new\.dump') -and (-not ($r3.Output -match 'nightly-old')))
Assert-Case 'happy: Job created from the CronJob with a manual name' (Test-LogHas $r3 'create job --from=cronjob/postgres-backup lamuflix-home-manual-')
Assert-Case 'happy: waits for the Job to complete' (Test-LogHas $r3 'wait --for=condition=complete job/lamuflix-home-manual-')
Assert-Case 'happy: Job object deleted after success' (Test-LogHas $r3 'delete job lamuflix-home-manual-')
Assert-Case 'happy: the only delete is the Job object' ((Count-LogLines $r3 'delete') -eq (Count-LogLines $r3 'delete job lamuflix-home-manual-'))
Assert-Case 'happy: new dump exists, old dump and sentinel unchanged' ((Test-Path -LiteralPath $newDump) -and ((Get-FileHashText $oldDump) -eq $oldHash) -and ((Get-FileHashText $sentinel3) -eq $sentinelHash3))

# Case 4: the Job does not complete; the Job is kept and no file is touched
$o4 = New-BackupOps 'case-timeout'
$sentinel4 = Join-Path $o4.Backups 'keep-me.txt'
Write-TestText -Path $sentinel4 -Text 'sentinel'
$sentinelHash4 = Get-FileHashText $sentinel4
Write-StubRules -Path $rules -Rules @(
    @{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' },
    @{ tool = 'kubectl'; match = 'get cronjob'; stdout = 'cronjob.batch/postgres-backup' },
    @{ tool = 'kubectl'; match = 'create job'; exit = 0 },
    @{ tool = 'kubectl'; match = 'wait --for=condition=complete'; exit = 1 }
)
$r4 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o4.Config) -BinDir $bin -LogPath (Join-Path $root 'c4.log') -RulesPath $rules
Assert-Case 'not complete: exit 1' ($r4.ExitCode -eq 1) "exit $($r4.ExitCode)"
Assert-Case 'not complete: says the Job did not complete and is kept' ($r4.Output -match 'did not complete' -and $r4.Output -match 'kept')
Assert-Case 'not complete: Job not deleted' (-not (Test-LogHas $r4 'delete'))
Assert-Case 'not complete: sentinel unchanged' ((Get-FileHashText $sentinel4) -eq $sentinelHash4)

# Case 5: the Job writes a dump but pg_restore -l rejects it; the dump is kept and the Job is kept
$o5 = New-BackupOps 'case-badlist'
$badDump = Join-Path $o5.Backups 'lamuflix-home-nightly-bad.dump'
Write-StubRules -Path $rules -Rules @(
    @{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' },
    @{ tool = 'kubectl'; match = 'get cronjob'; stdout = 'cronjob.batch/postgres-backup' },
    @{ tool = 'kubectl'; match = 'create job'; exit = 0 },
    @{ tool = 'kubectl'; match = 'wait --for=condition=complete'; exit = 0; createFile = $badDump },
    @{ tool = 'docker'; match = ' -l /restore/'; exit = 1 }
)
$r5 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o5.Config) -BinDir $bin -LogPath (Join-Path $root 'c5.log') -RulesPath $rules
Assert-Case 'bad dump: exit 1' ($r5.ExitCode -eq 1) "exit $($r5.ExitCode)"
Assert-Case 'bad dump: reports the pg_restore -l failure' ($r5.Output -match 'pg_restore -l failed')
Assert-Case 'bad dump: Job kept, not deleted' (-not (Test-LogHas $r5 'delete'))
Assert-Case 'bad dump: dump kept' (Test-Path -LiteralPath $badDump)

Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
$failures = Get-CaseFailureCount
Write-Host ''
Write-Host "Backup companion: $failures failed"
if ($failures -gt 0) { exit 1 }
exit 0

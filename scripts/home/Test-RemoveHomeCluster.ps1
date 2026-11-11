#Requires -Version 7.0
# Companion test for Remove-HomeCluster.ps1. kind, kubectl and docker are stubs; no cluster is touched.
# Exit 0 when every case passes, 1 otherwise.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-test-lib.ps1')

$target = Join-Path $PSScriptRoot 'Remove-HomeCluster.ps1'
$root = New-TestRoot
$bin = New-StubBin -Dir (Join-Path $root 'bin')
$rules = Join-Path $root 'rules.json'
$library = Join-Path $root 'library'
New-Item -ItemType Directory -Force -Path $library | Out-Null

function New-RemoveOps {
    param([string]$Name)
    $ops = Join-Path $root $Name
    $backups = Join-Path $ops 'backups'
    New-Item -ItemType Directory -Force -Path $backups | Out-Null
    $kube = Join-Path $ops 'kubeconfig'
    Write-TestText -Path $kube -Text ''
    $cfg = Write-TestConfig -Path (Join-Path $ops 'home.config.json') -LibraryPath $library -BackupPath $backups -KubeconfigPath $kube
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

# The backup stage in the child script needs these rules: a CronJob, a Job that writes a dump, and a passing pg_restore -l.
function Get-BackupRules {
    param([string]$NewDump, [int]$CronExit = 0, [int]$ListExit = 0)
    return @(
        @{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' },
        @{ tool = 'kubectl'; match = 'get cronjob'; exit = $CronExit; stdout = 'cronjob.batch/postgres-backup' },
        @{ tool = 'kubectl'; match = 'create job'; exit = 0 },
        @{ tool = 'kubectl'; match = 'wait --for=condition=complete'; exit = 0; createFile = $NewDump },
        @{ tool = 'docker'; match = ' -l /restore/'; exit = $ListExit }
    )
}

# Case 1: no -Confirm; refused before any tool is called
$o1 = New-RemoveOps 'case-noconfirm'
Write-StubRules -Path $rules -Rules @()
$r1 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o1.Config) -BinDir $bin -LogPath (Join-Path $root 'c1.log') -RulesPath $rules
Assert-Case 'no -Confirm: exit 1' ($r1.ExitCode -eq 1) "exit $($r1.ExitCode)"
Assert-Case 'no -Confirm: refused with a message' ($r1.Output -match 'refusing')
Assert-Case 'no -Confirm: no tool was called' (@($r1.Log).Count -eq 0)

# Case 2: the named cluster is absent; nothing to remove, exit 0
$o2 = New-RemoveOps 'case-absent'
Write-StubRules -Path $rules -Rules @(@{ tool = 'kind'; match = '^get clusters'; stdout = '' })
$r2 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o2.Config, '-Confirm') -BinDir $bin -LogPath (Join-Path $root 'c2.log') -RulesPath $rules
Assert-Case 'cluster absent: exit 0' ($r2.ExitCode -eq 0) "exit $($r2.ExitCode): $($r2.Output)"
Assert-Case 'cluster absent: says nothing to remove' ($r2.Output -match 'nothing to remove')
Assert-Case 'cluster absent: no delete and no backup' (-not (Test-LogHas $r2 'delete|create job'))

# Case 3: the backup fails (CronJob missing); the cluster must not be deleted
$o3 = New-RemoveOps 'case-backupfails'
$sentinel3 = Join-Path $o3.Backups 'keep-me.txt'
Write-TestText -Path $sentinel3 -Text 'sentinel'
$hash3 = Get-FileHashText $sentinel3
Write-StubRules -Path $rules -Rules (Get-BackupRules -NewDump (Join-Path $o3.Backups 'lamuflix-home-nightly-x.dump') -CronExit 1)
$r3 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o3.Config, '-Confirm') -BinDir $bin -LogPath (Join-Path $root 'c3.log') -RulesPath $rules
Assert-Case 'backup fails: exit 1' ($r3.ExitCode -eq 1) "exit $($r3.ExitCode): $($r3.Output)"
Assert-Case 'backup fails: reports the cluster was not removed' ($r3.Output -match 'catalog backup failed; the cluster was not removed')
Assert-Case 'backup fails: kind delete never called' (-not (Test-LogHas $r3 '^kind delete'))
Assert-Case 'backup fails: sentinel unchanged' ((Get-FileHashText $sentinel3) -eq $hash3)

# Case 4: happy path. Exactly one kind delete for the named cluster; sentinels in library and backups survive.
$o4 = New-RemoveOps 'case-happy'
$newDump4 = Join-Path $o4.Backups 'lamuflix-home-nightly-removal.dump'
$sentinel4 = Join-Path $o4.Backups 'keep-me.txt'
Write-TestText -Path $sentinel4 -Text 'sentinel'
$hash4 = Get-FileHashText $sentinel4
$libSentinel = Join-Path $library 'library-sentinel.txt'
Write-TestText -Path $libSentinel -Text 'library sentinel'
$libHash = Get-FileHashText $libSentinel
Write-StubRules -Path $rules -Rules ((Get-BackupRules -NewDump $newDump4) + @(
    @{ tool = 'kubectl'; match = 'delete job'; exit = 0 },
    @{ tool = 'kind'; match = '^delete cluster'; exit = 0 }
))
$r4 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o4.Config, '-Confirm') -BinDir $bin -LogPath (Join-Path $root 'c4.log') -RulesPath $rules
Assert-Case 'happy: exit 0' ($r4.ExitCode -eq 0) "exit $($r4.ExitCode): $($r4.Output)"
Assert-Case 'happy: backup ran before the delete' ((Test-LogHas $r4 'create job --from=cronjob/postgres-backup') -and (Test-LogHas $r4 '^kind delete cluster'))
Assert-Case 'happy: exactly one kind delete, for the named cluster' ((Count-LogLines $r4 '^kind delete') -eq 1 -and (Test-LogHas $r4 '^kind delete cluster --name lamuflix-home --kubeconfig '))
Assert-Case 'happy: no kubectl delete other than the backup Job' ((Count-LogLines $r4 '^kubectl .* delete (?!job )') -eq 0)
Assert-Case 'happy: newest backup verified before removal' ($r4.Output -match 'newest backup verified: lamuflix-home-nightly-removal\.dump')
Assert-Case 'happy: backup dump, backups sentinel and library sentinel survive' ((Test-Path -LiteralPath $newDump4) -and ((Get-FileHashText $sentinel4) -eq $hash4) -and ((Get-FileHashText $libSentinel) -eq $libHash))
Assert-Case 'happy: host folders were not touched' ($r4.Output -match 'host folders, E:\\Filmes and the backups folder were not touched')

# Case 5: -WhatIf prints the plan and changes nothing
$o5 = New-RemoveOps 'case-whatif'
Write-StubRules -Path $rules -Rules @(@{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' })
$r5 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o5.Config, '-Confirm', '-WhatIf') -BinDir $bin -LogPath (Join-Path $root 'c5.log') -RulesPath $rules
Assert-Case 'WhatIf: exit 0' ($r5.ExitCode -eq 0) "exit $($r5.ExitCode): $($r5.Output)"
Assert-Case 'WhatIf: prints the plan' ($r5.Output -match 'WhatIf: would back up')
Assert-Case 'WhatIf: no backup, delete or create calls' (-not (Test-LogHas $r5 'delete cluster|create job|wait --for'))

# Case 6: a scratch cluster name that is absent; the home cluster in the list is not touched
$o6 = New-RemoveOps 'case-scratch'
Write-StubRules -Path $rules -Rules @(@{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' })
$r6 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o6.Config, '-ClusterName', 'lamuflix-scratch-absent', '-Confirm') -BinDir $bin -LogPath (Join-Path $root 'c6.log') -RulesPath $rules
Assert-Case 'scratch absent: exit 0' ($r6.ExitCode -eq 0) "exit $($r6.ExitCode): $($r6.Output)"
Assert-Case 'scratch absent: nothing to remove' ($r6.Output -match 'no kind cluster named lamuflix-scratch-absent; nothing to remove')
Assert-Case 'scratch absent: no delete calls' (-not (Test-LogHas $r6 'delete'))

Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
$failures = Get-CaseFailureCount
Write-Host ''
Write-Host "Remove companion: $failures failed"
if ($failures -gt 0) { exit 1 }
exit 0

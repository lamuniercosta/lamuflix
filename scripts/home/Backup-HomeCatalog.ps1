#Requires -Version 7.0
# Runs the postgres-backup CronJob once on demand and verifies the dump it writes.
# Creates one manual Job (lamuflix-home-manual-<timestamp>), waits for it to complete, checks that the
# newest nightly dump is newer than this run and passes pg_restore -l. On success only the manual Job
# object is deleted. On failure the Job is kept for inspection. No file is ever deleted or overwritten.
# Never prints secret values.
#
# Usage: pwsh -NoProfile -File scripts/home/Backup-HomeCatalog.ps1 [-ConfigPath <file>] [-ClusterName <name>] [-WhatIf]
# Exit 0 on success, 1 on failure.

[CmdletBinding()]
param(
    [string]$ConfigPath = 'F:\Dev\lamuflix-home\home.config.json',
    [string]$ClusterName,
    [string]$KubeconfigPath,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-common.ps1')

$AppNamespace = 'lamuflix-home'
$CronJobName = 'postgres-backup'
$DumpPattern = 'lamuflix-home-nightly-*.dump'

$exitCode = 0
try {
    $cfg = Read-HomeConfig -Path $ConfigPath -ClusterName $ClusterName -KubeconfigPath $KubeconfigPath
    if (-not (Test-ClusterExists -Cfg $cfg)) { throw "no kind cluster named $($cfg.ClusterName)" }
    Assert-Kube -Cfg $cfg -KubeArgs @('-n', $AppNamespace, 'get', 'cronjob', $CronJobName, '-o', 'name') `
        -What 'find the postgres-backup CronJob' | Out-Null

    $jobName = 'lamuflix-home-manual-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
    if ($WhatIf) {
        Write-HomeInfo "WhatIf: would create job/$jobName from cronjob/$CronJobName, wait up to $($cfg.WaitSeconds) s, verify the newest nightly dump in $($cfg.BackupPath) with pg_restore -l, then delete only the Job object."
        exit 0
    }

    $startedAt = Get-Date
    Assert-Kube -Cfg $cfg -KubeArgs @('-n', $AppNamespace, 'create', 'job', "--from=cronjob/$CronJobName", $jobName) `
        -What 'create the manual backup Job' | Out-Null
    Write-HomeInfo "created job/$jobName"

    $wait = Invoke-Kube -Cfg $cfg -KubeArgs @('-n', $AppNamespace, 'wait', '--for=condition=complete', "job/$jobName", "--timeout=$($cfg.WaitSeconds)s")
    if ($wait.ExitCode -ne 0) {
        throw "job/$jobName did not complete within $($cfg.WaitSeconds) s; the Job is kept for inspection"
    }

    $dump = Get-NewestDump -Directory $cfg.BackupPath -Pattern $DumpPattern
    if ($null -eq $dump -or $dump.LastWriteTime -lt $startedAt.AddSeconds(-2)) {
        throw "no new nightly dump found in $($cfg.BackupPath)"
    }
    if (-not (Test-DumpWithPgRestore -Cfg $cfg -DumpPath $dump.FullName)) {
        throw "pg_restore -l failed for $($dump.Name); the Job is kept for inspection"
    }
    Write-HomeInfo "verified $($dump.Name) with pg_restore -l"

    $del = Invoke-Kube -Cfg $cfg -KubeArgs @('-n', $AppNamespace, 'delete', 'job', $jobName, '--ignore-not-found')
    if ($del.ExitCode -eq 0) {
        Write-HomeInfo "deleted job/$jobName (Job object only)"
    }
    else {
        Write-HomeFail "could not delete job/$jobName; the dump itself is fine"
    }
    Write-HomeInfo "backup ready: $($dump.FullName)"
}
catch {
    Write-HomeFail $_.Exception.Message
    $exitCode = 1
}
exit $exitCode

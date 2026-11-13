#Requires -Version 7.0
# Restores a catalog dump into the lamuflix database of the home cluster.
# Order: verify the dump with pg_restore -l, pause Argo CD auto-sync, scale api to 0, restore,
# scale api back up, wait for readiness, then resume auto-sync (always, even after a failure).
# Refuses a non-empty catalog unless -Force. Never overwrites, moves or deletes the dump.
# Never prints secret values.
#
# Usage: pwsh -NoProfile -File scripts/home/Restore-HomeCatalog.ps1 -DumpPath <file.dump> [-Force] [-WhatIf]
# Exit 0 on success, 1 on failure.

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$DumpPath,
    [string]$ConfigPath = 'F:\Dev\lamuflix-home\home.config.json',
    [string]$ClusterName,
    [string]$KubeconfigPath,
    [switch]$Force,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-common.ps1')

$AppNamespace = 'lamuflix-home'
$ArgoNamespace = 'argocd'
$PostgresPod = 'postgres-0'
$DatabaseName = 'lamuflix'

function Set-ArgoAutoSync {
    param($Cfg, [bool]$Enabled)
    if ($Enabled) {
        $patch = '{"spec":{"syncPolicy":{"automated":{"prune":true,"selfHeal":true}}}}'
    }
    else {
        $patch = '{"spec":{"syncPolicy":{"automated":null}}}'
    }
    Assert-Kube -Cfg $Cfg -KubeArgs @('-n', $ArgoNamespace, 'patch', 'application', 'lamuflix-home', '--type', 'merge', '-p', $patch) `
        -What "set Argo CD auto-sync to $Enabled" | Out-Null
}

function Invoke-Postgres {
    param($Cfg, [string]$DbUser, [string[]]$Command)
    return Invoke-Kube -Cfg $Cfg -KubeArgs (@('-n', $AppNamespace, 'exec', $PostgresPod, '--') + $Command)
}

function Ensure-CatalogDatabase {
    param($Cfg, [string]$DbUser)
    $check = Invoke-Postgres -Cfg $Cfg -DbUser $DbUser -Command @('psql', '-U', $DbUser, '-d', 'postgres', '-tAc', "select 1 from pg_database where datname='$DatabaseName'")
    if ($check.ExitCode -ne 0) { throw 'could not reach postgres to check the database' }
    if ((@($check.Output) -join '').Trim() -ne '1') {
        Assert-Kube -Cfg $Cfg -KubeArgs @('-n', $AppNamespace, 'exec', $PostgresPod, '--', 'createdb', '-U', $DbUser, $DatabaseName) `
            -What 'create the lamuflix database' | Out-Null
        Write-HomeInfo "created empty database $DatabaseName"
    }
}

function Get-MovieCount {
    param($Cfg, [string]$DbUser)
    $query = 'select case when to_regclass(''public.movies'') is null then -1 else (select count(*) from movies) end'
    $result = Invoke-Postgres -Cfg $Cfg -DbUser $DbUser -Command @('psql', '-U', $DbUser, '-d', $DatabaseName, '-tAc', $query)
    if ($result.ExitCode -ne 0) { throw 'could not count movies in the lamuflix database' }
    $text = (@($result.Output) -join '').Trim()
    $count = 0
    if (-not [int]::TryParse($text, [ref]$count)) { throw 'movie count was not a number' }
    return $count
}

function Copy-AndRestore {
    param($Cfg, [string]$DbUser, [string]$DumpFile, [bool]$Clean)
    $dir = Split-Path -Parent $DumpFile
    $name = Split-Path -Leaf $DumpFile
    Push-Location -LiteralPath $dir
    try {
        Assert-Kube -Cfg $Cfg -KubeArgs @('-n', $AppNamespace, 'cp', $name, "${PostgresPod}:/tmp/restore.dump") -What 'copy the dump into postgres' | Out-Null
    }
    finally {
        Pop-Location
    }
    $restoreArgs = @('pg_restore', '-U', $DbUser, '-d', $DatabaseName, '--no-owner', '--no-acl')
    if ($Clean) { $restoreArgs += @('--clean', '--if-exists') }
    $restoreArgs += '/tmp/restore.dump'
    try {
        Assert-Kube -Cfg $Cfg -KubeArgs (@('-n', $AppNamespace, 'exec', $PostgresPod, '--') + $restoreArgs) -What 'pg_restore inside postgres' | Out-Null
    }
    finally {
        Invoke-Postgres -Cfg $Cfg -DbUser $DbUser -Command @('rm', '-f', '/tmp/restore.dump') | Out-Null
    }
}

$exitCode = 0
try {
    $cfg = Read-HomeConfig -Path $ConfigPath -ClusterName $ClusterName -KubeconfigPath $KubeconfigPath
    $dumpFile = [System.IO.Path]::GetFullPath($DumpPath)
    if (-not (Test-Path -LiteralPath $dumpFile -PathType Leaf)) { throw "dump not found: $dumpFile" }
    if ([System.IO.Path]::GetExtension($dumpFile) -ne '.dump') { throw 'the dump must have the .dump extension' }
    if (-not (Test-ClusterExists -Cfg $cfg)) { throw "no kind cluster named $($cfg.ClusterName)" }

    $dbUser = Read-EnvValue -Path (Join-Path (Join-Path $cfg.OpsRoot 'secrets') 'postgres.env') -Key 'POSTGRES_USER'
    if (-not $dbUser) { throw 'POSTGRES_USER not found in secrets\postgres.env; run New-HomeCluster.ps1 first' }

    if (-not (Test-DumpWithPgRestore -Cfg $cfg -DumpPath $dumpFile)) {
        throw 'pg_restore -l rejected the dump; nothing was changed'
    }
    Write-HomeInfo "dump listed by pg_restore -l: $(Split-Path -Leaf $dumpFile)"

    if ($WhatIf) {
        Write-HomeInfo 'WhatIf: no changes made. Would pause auto-sync, scale api to 0, restore into the lamuflix database, scale api to 1, wait for readiness, resume auto-sync and report the movie count.'
        exit 0
    }

    Set-ArgoAutoSync -Cfg $cfg -Enabled $false
    Write-HomeInfo 'Argo CD auto-sync paused'
    try {
        Ensure-CatalogDatabase -Cfg $cfg -DbUser $dbUser
        $before = Get-MovieCount -Cfg $cfg -DbUser $dbUser
        if ($before -gt 0 -and -not $Force) {
            throw "the catalog already has $before movies; pass -Force to restore over it"
        }

        Assert-Kube -Cfg $cfg -KubeArgs @('-n', $AppNamespace, 'scale', 'deployment/api', '--replicas=0') -What 'scale api to 0' | Out-Null
        Copy-AndRestore -Cfg $cfg -DbUser $dbUser -DumpFile $dumpFile -Clean ([bool]$Force)
        Write-HomeInfo 'dump restored'

        Assert-Kube -Cfg $cfg -KubeArgs @('-n', $AppNamespace, 'scale', 'deployment/api', '--replicas=1') -What 'scale api to 1' | Out-Null
        Assert-Kube -Cfg $cfg -KubeArgs @('-n', $AppNamespace, 'rollout', 'status', 'deployment/api', "--timeout=$($cfg.WaitSeconds)s") -What 'wait for api readiness' | Out-Null

        $after = Get-MovieCount -Cfg $cfg -DbUser $dbUser
        Write-HomeInfo "movies in catalog after restore: $after"
    }
    finally {
        try {
            Set-ArgoAutoSync -Cfg $cfg -Enabled $true
            Write-HomeInfo 'Argo CD auto-sync resumed'
        }
        catch {
            Write-HomeFail "could not resume auto-sync: $($_.Exception.Message)"
            $exitCode = 1
        }
    }
}
catch {
    Write-HomeFail $_.Exception.Message
    $exitCode = 1
}
exit $exitCode

#Requires -Version 7.0
# Restores a catalog dump into the Neon database (secrets\neon.env DATABASE_URL) used by the home cluster.
# Order: verify the dump with pg_restore -l, pause Argo CD auto-sync, scale api to 0, restore,
# scale api back up, wait for readiness, then resume auto-sync (always, even after a failure).
# Refuses a non-empty catalog unless -Force (-Force restores with --clean --if-exists).
# The dump is mounted read-only into the PostgreSQL 18 tools image; it is never copied, moved or deleted.
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

# Runs neon-tools.sh in the PostgreSQL 18 tools image. DATABASE_URL comes from the env file; the dump
# folder, when given, is mounted read-only at /restore.
function Invoke-NeonTool {
    param($Cfg, [string]$EnvFile, [string[]]$ToolArgs, [string]$DumpDir = '')
    $mounts = @('-v', "${PSScriptRoot}:/scripts:ro")
    if ($DumpDir) { $mounts += @('-v', "${DumpDir}:/restore:ro") }
    $arguments = @('run', '--rm', '--env-file', $EnvFile) + $mounts +
        @('--entrypoint', 'sh', $HomePostgresImage, '/scripts/neon-tools.sh') + $ToolArgs
    return Invoke-Tool -Exe $Cfg.Docker -Arguments $arguments
}

function Get-MovieCount {
    param($Cfg, [string]$EnvFile)
    $result = Invoke-NeonTool -Cfg $Cfg -EnvFile $EnvFile -ToolArgs @('count')
    if ($result.ExitCode -ne 0) { throw 'could not count movies in the Neon database' }
    $text = (@($result.Output) -join '').Trim()
    $count = 0
    if (-not [int]::TryParse($text, [ref]$count)) { throw 'movie count was not a number' }
    return $count
}

function Invoke-Restore {
    param($Cfg, [string]$EnvFile, [string]$DumpFile, [bool]$Clean)
    $mode = if ($Clean) { 'clean' } else { 'plain' }
    $name = Split-Path -Leaf $DumpFile
    $dir = Split-Path -Parent $DumpFile
    $result = Invoke-NeonTool -Cfg $Cfg -EnvFile $EnvFile -DumpDir $dir -ToolArgs @('restore', $mode, $name)
    if ($result.ExitCode -ne 0) { throw "pg_restore failed (exit $($result.ExitCode)); see the output above" }
}

$exitCode = 0
try {
    $cfg = Read-HomeConfig -Path $ConfigPath -ClusterName $ClusterName -KubeconfigPath $KubeconfigPath
    $dumpFile = [System.IO.Path]::GetFullPath($DumpPath)
    if (-not (Test-Path -LiteralPath $dumpFile -PathType Leaf)) { throw "dump not found: $dumpFile" }
    if ([System.IO.Path]::GetExtension($dumpFile) -ne '.dump') { throw 'the dump must have the .dump extension' }
    if (-not (Test-ClusterExists -Cfg $cfg)) { throw "no kind cluster named $($cfg.ClusterName)" }

    $secretsDir = Join-Path $cfg.OpsRoot 'secrets'
    $envFile = Write-CatalogDatabaseEnv -SecretsDir $secretsDir

    if (-not (Test-DumpWithPgRestore -Cfg $cfg -DumpPath $dumpFile)) {
        throw 'pg_restore -l rejected the dump; nothing was changed'
    }
    Write-HomeInfo "dump listed by pg_restore -l: $(Split-Path -Leaf $dumpFile)"

    if ($WhatIf) {
        Write-HomeInfo 'WhatIf: no changes made. Would pause auto-sync, scale api to 0, restore into the Neon database, scale api to 1, wait for readiness, resume auto-sync and report the movie count.'
        exit 0
    }

    Set-ArgoAutoSync -Cfg $cfg -Enabled $false
    Write-HomeInfo 'Argo CD auto-sync paused'
    try {
        $before = Get-MovieCount -Cfg $cfg -EnvFile $envFile
        if ($before -gt 0 -and -not $Force) {
            throw "the catalog already has $before movies; pass -Force to restore over it"
        }

        Assert-Kube -Cfg $cfg -KubeArgs @('-n', $AppNamespace, 'scale', 'deployment/api', '--replicas=0') -What 'scale api to 0' | Out-Null
        Invoke-Restore -Cfg $cfg -EnvFile $envFile -DumpFile $dumpFile -Clean ([bool]$Force)
        Write-HomeInfo 'dump restored'

        Assert-Kube -Cfg $cfg -KubeArgs @('-n', $AppNamespace, 'scale', 'deployment/api', '--replicas=1') -What 'scale api to 1' | Out-Null
        Assert-Kube -Cfg $cfg -KubeArgs @('-n', $AppNamespace, 'rollout', 'status', 'deployment/api', "--timeout=$($cfg.WaitSeconds)s") -What 'wait for api readiness' | Out-Null

        $after = Get-MovieCount -Cfg $cfg -EnvFile $envFile
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

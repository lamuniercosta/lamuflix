#Requires -Version 7.0
# Removes the home kind cluster and nothing else. Refuses to run without -Confirm.
# Order: take a catalog backup with Backup-HomeCatalog.ps1 (must succeed), verify the newest nightly
# dump with pg_restore -l, then delete only the named kind cluster.
# Host folders, E:\Filmes and the backups folder are never touched by this script.
# -WhatIf prints the plan and changes nothing.
#
# Usage: pwsh -NoProfile -File scripts/home/Remove-HomeCluster.ps1 -Confirm [-ClusterName <name>] [-WhatIf]
# Exit 0 when removed or when there is nothing to remove, 1 on refusal or failure.

[CmdletBinding()]
param(
    [string]$ConfigPath = 'F:\Dev\lamuflix-home\home.config.json',
    [string]$ClusterName,
    [string]$KubeconfigPath,
    [switch]$Confirm,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-common.ps1')

$DumpPattern = 'lamuflix-home-nightly-*.dump'

function Invoke-CatalogBackup {
    param($Cfg)
    $backupScript = Join-Path $PSScriptRoot 'Backup-HomeCatalog.ps1'
    & pwsh -NoProfile -File $backupScript -ConfigPath $Cfg.ConfigPath -ClusterName $Cfg.ClusterName -KubeconfigPath $Cfg.Kubeconfig
    if ($LASTEXITCODE -ne 0) { throw 'catalog backup failed; the cluster was not removed' }
}

function Test-LatestBackup {
    param($Cfg)
    $dump = Get-NewestDump -Directory $Cfg.BackupPath -Pattern $DumpPattern
    if ($null -eq $dump) { throw 'no nightly dump found after the backup; the cluster was not removed' }
    if (-not (Test-DumpWithPgRestore -Cfg $Cfg -DumpPath $dump.FullName)) {
        throw "pg_restore -l failed for $($dump.Name); the cluster was not removed"
    }
    Write-HomeInfo "newest backup verified: $($dump.Name)"
}

function Remove-KindCluster {
    param($Cfg)
    $result = Invoke-Tool -Exe $Cfg.Kind -Arguments @('delete', 'cluster', '--name', $Cfg.ClusterName, '--kubeconfig', $Cfg.Kubeconfig)
    if ($result.ExitCode -ne 0) { throw "kind delete cluster failed (exit $($result.ExitCode))" }
}

$exitCode = 0
try {
    if (-not $Confirm) { throw 'refusing to remove the cluster without -Confirm' }

    $cfg = Read-HomeConfig -Path $ConfigPath -ClusterName $ClusterName -KubeconfigPath $KubeconfigPath
    if (-not (Test-ClusterExists -Cfg $cfg)) {
        Write-HomeInfo "no kind cluster named $($cfg.ClusterName); nothing to remove"
        exit 0
    }

    if ($WhatIf) {
        Write-HomeInfo "WhatIf: would back up the catalog with Backup-HomeCatalog.ps1, verify the newest nightly dump in $($cfg.BackupPath), then run: kind delete cluster --name $($cfg.ClusterName) --kubeconfig <scratch kubeconfig>"
        Write-HomeInfo 'WhatIf: no change made. Host folders, E:\Filmes and the backups folder are never touched.'
        exit 0
    }

    Invoke-CatalogBackup -Cfg $cfg | Out-Host
    Test-LatestBackup -Cfg $cfg
    Remove-KindCluster -Cfg $cfg
    Write-HomeInfo "removed kind cluster $($cfg.ClusterName)"
    Write-HomeInfo 'host folders, E:\Filmes and the backups folder were not touched'
}
catch {
    Write-HomeFail $_.Exception.Message
    $exitCode = 1
}
exit $exitCode

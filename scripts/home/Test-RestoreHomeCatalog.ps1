#Requires -Version 7.0
# Companion test for Restore-HomeCatalog.ps1. kind, kubectl and docker are stubs; no cluster is touched.
# Exit 0 when every case passes, 1 otherwise.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-test-lib.ps1')

$target = Join-Path $PSScriptRoot 'Restore-HomeCatalog.ps1'
$root = New-TestRoot
$bin = New-StubBin -Dir (Join-Path $root 'bin')
$rules = Join-Path $root 'rules.json'

function New-RestoreOps {
    param([string]$Name)
    $ops = Join-Path $root $Name
    $secrets = Join-Path $ops 'secrets'
    $backups = Join-Path $ops 'backups'
    New-Item -ItemType Directory -Force -Path $secrets, $backups | Out-Null
    $fixtureValue = 'restore' + '-test-value'
    $pgLine = @('POSTGRES_PASSWORD', $fixtureValue) -join '='
    Write-TestText -Path (Join-Path $secrets 'postgres.env') -Text "POSTGRES_USER=lamuflix_test`n$pgLine`n"
    $kube = Join-Path $ops 'kubeconfig'
    Write-TestText -Path $kube -Text ''
    $cfg = Write-TestConfig -Path (Join-Path $ops 'home.config.json') -LibraryPath $root -BackupPath $backups -KubeconfigPath $kube
    return [pscustomobject]@{ Config = $cfg; Backups = $backups }
}

function New-FakeDump {
    param([string]$Path)
    Write-TestText -Path $Path -Text 'PGDMP-test-dump-content'
    return $Path
}

function Get-FileHashText {
    param([string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Count-LogLines {
    param($Result, [string]$Pattern)
    return @(@($Result.Log) | Where-Object { $_ -match $Pattern }).Count
}

# Rules shared by the cases that reach the catalog.
function Get-CatalogRules {
    param([string[]]$MovieCounts, [bool]$ListOk = $true)
    $listExit = 0
    if (-not $ListOk) { $listExit = 1 }
    return @(
        @{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' },
        @{ tool = 'kubectl'; match = 'to_regclass'; stdoutSequence = $MovieCounts },
        @{ tool = 'kubectl'; match = 'pg_database'; stdout = '1' },
        @{ tool = 'docker'; match = ' -l /restore/'; exit = $listExit }
    )
}

# Case 1: the dump path does not exist; nothing is called
$o1 = New-RestoreOps 'case-missing'
Write-StubRules -Path $rules -Rules @()
$r1 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o1.Config, '-DumpPath', (Join-Path $o1.Backups 'lamuflix-home-nightly-absent.dump')) -BinDir $bin -LogPath (Join-Path $root 'c1.log') -RulesPath $rules
Assert-Case 'missing dump: exit 1' ($r1.ExitCode -eq 1) "exit $($r1.ExitCode)"
Assert-Case 'missing dump: reports the missing file' ($r1.Output -match 'dump not found')
Assert-Case 'missing dump: no tool was called' (@($r1.Log).Count -eq 0)

# Case 2: pg_restore -l rejects the dump; the cluster is not changed and the dump is kept
$o2 = New-RestoreOps 'case-badlist'
$dump2 = New-FakeDump (Join-Path $o2.Backups 'lamuflix-home-nightly-bad.dump')
$hash2 = Get-FileHashText $dump2
$sentinel2 = Join-Path $o2.Backups 'keep-me.txt'
Write-TestText -Path $sentinel2 -Text 'sentinel'
Write-StubRules -Path $rules -Rules @(
    @{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' },
    @{ tool = 'docker'; match = ' -l /restore/'; exit = 1; stdout = 'pg_restore: error: not a custom-format dump' }
)
$r2 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o2.Config, '-DumpPath', $dump2) -BinDir $bin -LogPath (Join-Path $root 'c2.log') -RulesPath $rules
Assert-Case 'pg_restore -l fails: exit 1' ($r2.ExitCode -eq 1) "exit $($r2.ExitCode)"
Assert-Case 'pg_restore -l fails: reports the rejection' ($r2.Output -match 'pg_restore -l rejected')
Assert-Case 'pg_restore -l fails: no kubectl call' (-not (Test-LogHas $r2 '^kubectl'))
Assert-Case 'pg_restore -l fails: dump unchanged and kept' ((Test-Path -LiteralPath $dump2) -and ((Get-FileHashText $dump2) -eq $hash2))

# Case 3: catalog already has movies and -Force is not given; auto-sync is paused then resumed
$o3 = New-RestoreOps 'case-nonempty'
$dump3 = New-FakeDump (Join-Path $o3.Backups 'lamuflix-home-nightly-full.dump')
$hash3 = Get-FileHashText $dump3
Write-StubRules -Path $rules -Rules (Get-CatalogRules -MovieCounts @('7'))
$r3 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o3.Config, '-DumpPath', $dump3) -BinDir $bin -LogPath (Join-Path $root 'c3.log') -RulesPath $rules
Assert-Case 'non-empty without -Force: exit 1' ($r3.ExitCode -eq 1) "exit $($r3.ExitCode): $($r3.Output)"
Assert-Case 'non-empty without -Force: says how many movies and how to proceed' (($r3.Output -match 'already has 7 movies') -and ($r3.Output -match '-Force'))
Assert-Case 'non-empty without -Force: no copy and no restore' (-not ((Test-LogHas $r3 'cp ') -or (Test-LogHas $r3 'pg_restore -U')))
Assert-Case 'non-empty without -Force: api not scaled' (-not (Test-LogHas $r3 'scale deployment/api'))
Assert-Case 'non-empty without -Force: auto-sync paused and resumed' ((Count-LogLines $r3 'patch application') -eq 2)
Assert-Case 'non-empty without -Force: dump unchanged' ((Get-FileHashText $dump3) -eq $hash3)

# Case 4: happy path on an empty catalog
$o4 = New-RestoreOps 'case-happy'
$dump4 = New-FakeDump (Join-Path $o4.Backups 'lamuflix-home-nightly-good.dump')
$hash4 = Get-FileHashText $dump4
$sentinel4 = Join-Path $o4.Backups 'keep-me.txt'
Write-TestText -Path $sentinel4 -Text 'sentinel'
$sentinelHash4 = Get-FileHashText $sentinel4
Write-StubRules -Path $rules -Rules (Get-CatalogRules -MovieCounts @('-1', '7'))
$r4 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o4.Config, '-DumpPath', $dump4) -BinDir $bin -LogPath (Join-Path $root 'c4.log') -RulesPath $rules
Assert-Case 'happy: exit 0' ($r4.ExitCode -eq 0) "exit $($r4.ExitCode): $($r4.Output)"
Assert-Case 'happy: reports the movie count after restore' ($r4.Output -match 'movies in catalog after restore: 7')
Assert-Case 'happy: dump copied into postgres' (Test-LogHas $r4 '^kubectl .* cp lamuflix-home-nightly-good\.dump postgres-0:/tmp/restore\.dump')
Assert-Case 'happy: pg_restore run without --clean' ((Test-LogHas $r4 'pg_restore -U') -and (-not (Test-LogHas $r4 '--clean')))
Assert-Case 'happy: api scaled to 0 then back to 1' ((Test-LogHas $r4 'scale deployment/api --replicas=0') -and (Test-LogHas $r4 'scale deployment/api --replicas=1'))
Assert-Case 'happy: waits for api rollout' (Test-LogHas $r4 'rollout status deployment/api')
Assert-Case 'happy: auto-sync paused and resumed with prune and selfHeal' ((Count-LogLines $r4 'patch application') -eq 2 -and (Test-LogHas $r4 'prune'))
Assert-Case 'happy: temp dump removed inside postgres only' (Test-LogHas $r4 'rm -f /tmp/restore\.dump')
Assert-Case 'happy: no delete of any host file' (-not (Test-LogHas $r4 'delete'))
Assert-Case 'happy: dump unchanged and kept' ((Test-Path -LiteralPath $dump4) -and ((Get-FileHashText $dump4) -eq $hash4))
Assert-Case 'happy: sentinel file unchanged' ((Get-FileHashText $sentinel4) -eq $sentinelHash4)

# Case 5: -WhatIf validates the dump and changes nothing on the cluster
$o5 = New-RestoreOps 'case-whatif'
$dump5 = New-FakeDump (Join-Path $o5.Backups 'lamuflix-home-nightly-whatif.dump')
Write-StubRules -Path $rules -Rules (Get-CatalogRules -MovieCounts @('-1'))
$r5 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o5.Config, '-DumpPath', $dump5, '-WhatIf') -BinDir $bin -LogPath (Join-Path $root 'c5.log') -RulesPath $rules
Assert-Case 'WhatIf: exit 0' ($r5.ExitCode -eq 0) "exit $($r5.ExitCode): $($r5.Output)"
Assert-Case 'WhatIf: no patch, scale, copy or restore calls' (-not (Test-LogHas $r5 'patch application|scale deployment| cp |pg_restore -U'))
Assert-Case 'WhatIf: dump kept' (Test-Path -LiteralPath $dump5)

Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
$failures = Get-CaseFailureCount
Write-Host ''
Write-Host "Restore companion: $failures failed"
if ($failures -gt 0) { exit 1 }
exit 0

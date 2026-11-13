#Requires -Version 7.0
# Companion test for Get-HomeStatus.ps1. kind and kubectl are stubs; the web checks run against a
# loopback HTTP helper (_home-test-http.ps1) on ephemeral ports. No cluster is touched.
# Exit 0 when every case passes, 1 otherwise.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-test-lib.ps1')

$target = Join-Path $PSScriptRoot 'Get-HomeStatus.ps1'
$root = New-TestRoot
$bin = New-StubBin -Dir (Join-Path $root 'bin') -Tools @('kind', 'kubectl')
$rules = Join-Path $root 'rules.json'
$helper = Join-Path $PSScriptRoot '_home-test-http.ps1'
$servers = [System.Collections.Generic.List[object]]::new()

function Get-FreePort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    $port = $listener.LocalEndpoint.Port
    $listener.Stop()
    return $port
}

function Start-TestServer {
    param([int]$Port, [int]$GenresStatus = 200)
    $serverArgs = @('-NoProfile', '-File', $helper, '-Port', "$Port", '-GenresStatus', "$GenresStatus")
    $proc = Start-Process -FilePath 'pwsh' -ArgumentList $serverArgs -PassThru -WindowStyle Hidden
    $servers.Add($proc)
    for ($i = 0; $i -lt 80; $i++) {
        $probe = [System.Net.Sockets.TcpClient]::new()
        try {
            $probe.Connect('127.0.0.1', $Port)
            return $proc
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
        finally {
            $probe.Close()
        }
    }
    throw "test HTTP server on port $Port did not start"
}

function New-StatusOps {
    param([string]$Name, [int]$WebPort, [string]$ReleaseJson = '')
    $ops = Join-Path $root $Name
    $backups = Join-Path $ops 'backups'
    New-Item -ItemType Directory -Force -Path $backups | Out-Null
    $kube = Join-Path $ops 'kubeconfig'
    Write-TestText -Path $kube -Text ''
    $cfg = Write-TestConfig -Path (Join-Path $ops 'home.config.json') -LibraryPath $root -BackupPath $backups `
        -KubeconfigPath $kube -WebPort $WebPort -ReleaseJsonPath $ReleaseJson
    return [pscustomobject]@{ Config = $cfg; Backups = $backups }
}

function Get-KindRule {
    param([string]$Output = 'lamuflix-home')
    return @{ tool = 'kind'; match = '^get clusters'; stdout = $Output }
}

function Get-ArgoRules {
    param([string]$Sync = 'Synced', [string]$Health = 'Healthy')
    return @(
        @{ tool = 'kubectl'; match = 'status\.sync\.status'; stdout = $Sync },
        @{ tool = 'kubectl'; match = 'status\.health\.status'; stdout = $Health },
        @{ tool = 'kubectl'; match = 'status\.sync\.revision'; stdout = '0123abc' }
    )
}

function Count-LogLines {
    param($Result, [string]$Pattern)
    return @(@($Result.Log) | Where-Object { $_ -match $Pattern }).Count
}

try {
    $port = Get-FreePort
    $null = Start-TestServer -Port $port

    # Case 1: everything healthy, with a recorded release commit
    $releaseJson = Join-Path $root 'RELEASE.json'
    Write-TestText -Path $releaseJson -Text '{"commit":"abc1234"}'
    $o1 = New-StatusOps 'case-healthy' -WebPort $port -ReleaseJson $releaseJson
    Write-StubRules -Path $rules -Rules (@(Get-KindRule) + (Get-ArgoRules))
    $r1 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o1.Config) -BinDir $bin -LogPath (Join-Path $root 'c1.log') -RulesPath $rules
    Assert-Case 'healthy: exit 0' ($r1.ExitCode -eq 0) "exit $($r1.ExitCode): $($r1.Output)"
    Assert-Case 'healthy: STATUS healthy printed' ($r1.Output -match 'STATUS: healthy')
    Assert-Case 'healthy: reports the deployed commit from RELEASE.json' ($r1.Output -match 'abc1234')
    Assert-Case 'healthy: both web routes return 200' (($r1.Output -match 'web / : 200') -and ($r1.Output -match 'web /api/genres : 200'))
    Assert-Case 'healthy: read-only (no write verbs called)' (-not (Test-LogHas $r1 'patch|apply|delete|create|scale|exec'))

    # Case 2: Argo CD out of sync
    $o2 = New-StatusOps 'case-outofsync' -WebPort $port
    Write-StubRules -Path $rules -Rules (@(Get-KindRule) + (Get-ArgoRules -Sync 'OutOfSync'))
    $r2 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o2.Config) -BinDir $bin -LogPath (Join-Path $root 'c2.log') -RulesPath $rules
    Assert-Case 'OutOfSync: exit 1' ($r2.ExitCode -eq 1) "exit $($r2.ExitCode)"
    Assert-Case 'OutOfSync: reports the sync problem' ($r2.Output -match 'argo sync status is not Synced')
    Assert-Case 'OutOfSync: degraded line printed' ($r2.Output -match 'STATUS: degraded')

    # Case 5: /api/genres answers 500 while / answers 200
    $port5 = Get-FreePort
    $null = Start-TestServer -Port $port5 -GenresStatus 500
    $o5 = New-StatusOps 'case-genres500' -WebPort $port5
    Write-StubRules -Path $rules -Rules (@(Get-KindRule) + (Get-ArgoRules))
    $r5 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o5.Config) -BinDir $bin -LogPath (Join-Path $root 'c5.log') -RulesPath $rules
    Assert-Case 'genres 500: exit 1' ($r5.ExitCode -eq 1) "exit $($r5.ExitCode)"
    Assert-Case 'genres 500: reports the route' ($r5.Output -match 'web /api/genres returned 500')

    # Case 6: nothing listens on the web port
    $closedPort = Get-FreePort
    $o6 = New-StatusOps 'case-closed' -WebPort $closedPort
    Write-StubRules -Path $rules -Rules (@(Get-KindRule) + (Get-ArgoRules))
    $r6 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o6.Config) -BinDir $bin -LogPath (Join-Path $root 'c6.log') -RulesPath $rules
    Assert-Case 'closed web port: exit 1' ($r6.ExitCode -eq 1) "exit $($r6.ExitCode)"
    Assert-Case 'closed web port: reports unreachable' ($r6.Output -match 'web / returned unreachable')

    # Case 7: cluster absent; no Argo call is made and the problem is named
    $o7 = New-StatusOps 'case-nocluster' -WebPort $port
    Write-StubRules -Path $rules -Rules @(Get-KindRule -Output '')
    $r7 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o7.Config) -BinDir $bin -LogPath (Join-Path $root 'c7.log') -RulesPath $rules
    Assert-Case 'no cluster: exit 1' ($r7.ExitCode -eq 1) "exit $($r7.ExitCode)"
    Assert-Case 'no cluster: reports the missing cluster' ($r7.Output -match 'is not present')
    Assert-Case 'no cluster: no Argo query made' ((Count-LogLines $r7 'status.') -eq 0)

    # Case 8: releaseJsonPath set but the file is missing
    $o8 = New-StatusOps 'case-norelease' -WebPort $port -ReleaseJson (Join-Path $root 'missing-RELEASE.json')
    Write-StubRules -Path $rules -Rules (@(Get-KindRule) + (Get-ArgoRules))
    $r8 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $o8.Config) -BinDir $bin -LogPath (Join-Path $root 'c8.log') -RulesPath $rules
    Assert-Case 'missing RELEASE.json: exit 1' ($r8.ExitCode -eq 1) "exit $($r8.ExitCode)"
    Assert-Case 'missing RELEASE.json: names the file problem' ($r8.Output -match 'releaseJsonPath is set but the file is missing')
}
finally {
    foreach ($proc in $servers) {
        if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
    }
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}

$failures = Get-CaseFailureCount
Write-Host ''
Write-Host "Get-HomeStatus companion: $failures failed"
if ($failures -gt 0) { exit 1 }
exit 0

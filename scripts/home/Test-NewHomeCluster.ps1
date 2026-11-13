#Requires -Version 7.0
# Companion test for New-HomeCluster.ps1. kind, kubectl and docker are stubs; no cluster is created.
# Exit 0 when every case passes, 1 otherwise.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-test-lib.ps1')

$target = Join-Path $PSScriptRoot 'New-HomeCluster.ps1'
$root = New-TestRoot
$bin = New-StubBin -Dir (Join-Path $root 'bin')
$rules = Join-Path $root 'rules.json'
$log = Join-Path $root 'calls.log'
$argoManifest = Join-Path $root 'argocd-install.yaml'
Write-TestText -Path $argoManifest -Text "apiVersion: v1`nkind: List`n"

$library = Join-Path $root 'library'
$backups = Join-Path $root 'backups'
New-Item -ItemType Directory -Force -Path $library, $backups | Out-Null
$sourceEnv = Join-Path $root 'source.env'
Write-TestText -Path $sourceEnv -Text "OMDB_API_KEY=omdb-test-value`nTMDB_API_KEY=tmdb-test-value`nOPENSUBTITLES_API_KEY=opensubtitles-test-value`n"
$sourceEnvNoOmdb = Join-Path $root 'source-no-omdb.env'
Write-TestText -Path $sourceEnvNoOmdb -Text "TMDB_API_KEY=tmdb-test-value`n"

function New-Ops {
    param([string]$Name)
    $ops = Join-Path $root $Name
    New-Item -ItemType Directory -Force -Path $ops | Out-Null
    return $ops
}

$freshRules = @(
    @{ tool = 'kind'; match = '^get clusters'; stdout = '' },
    @{ tool = 'kubectl'; match = 'get namespace'; exit = 1 },
    @{ tool = 'kubectl'; match = 'create secret'; stdout = "apiVersion: v1`nkind: Secret`nmetadata:`n  name: stub`n" },
    @{ tool = 'kubectl'; match = 'status\.sync'; stdout = 'Synced' },
    @{ tool = 'kubectl'; match = 'status\.health'; stdout = 'Healthy' }
)
$presentRules = @(
    @{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' },
    @{ tool = 'kubectl'; match = 'create secret'; stdout = "apiVersion: v1`nkind: Secret`nmetadata:`n  name: stub`n" },
    @{ tool = 'kubectl'; match = 'status\.sync'; stdout = 'Synced' },
    @{ tool = 'kubectl'; match = 'status\.health'; stdout = 'Healthy' }
)

# Case 1: fresh cluster
$ops1 = New-Ops 'case-fresh'
$cfg1 = Write-TestConfig -Path (Join-Path $ops1 'home.config.json') -LibraryPath $library -BackupPath $backups `
    -KubeconfigPath (Join-Path $ops1 'kubeconfig') -SecretSourcePath $sourceEnv
Write-StubRules -Path $rules -Rules $freshRules
$r1 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $cfg1, '-ArgoManifestPath', $argoManifest) -BinDir $bin -LogPath $log -RulesPath $rules
Assert-Case 'fresh: exit 0' ($r1.ExitCode -eq 0) "exit $($r1.ExitCode): $($r1.Output)"
Assert-Case 'fresh: creates the kind cluster by name' (Test-LogHas $r1 '^kind create cluster --name lamuflix-home ')
Assert-Case 'fresh: creates both namespaces' ((Test-LogHas $r1 'create namespace lamuflix-home') -and (Test-LogHas $r1 'create namespace argocd'))
Assert-Case 'fresh: every kubectl call passes --kubeconfig explicitly' (-not (Test-LogHas $r1 '^kubectl (?!.*--kubeconfig)'))
Assert-Case 'fresh: applies the Application' (Test-LogHas $r1 'apply -f .*application\.yaml')

$secrets1 = Join-Path $ops1 'secrets'
$pgEnvPath = Join-Path $secrets1 'postgres.env'
Assert-Case 'fresh: credentials and api settings written to secrets' ((Test-Path (Join-Path $secrets1 'postgres.env')) -and (Test-Path (Join-Path $secrets1 'rabbitmq.env')) -and (Test-Path (Join-Path $secrets1 'api.settings.json')))
$settings = Get-Content -LiteralPath (Join-Path $secrets1 'api.settings.json') -Raw | ConvertFrom-Json
Assert-Case 'fresh: OTEL endpoint removed from api settings' (-not ($settings.PSObject.Properties.Name -contains 'OTEL_EXPORTER_OTLP_ENDPOINT'))
Assert-Case 'fresh: DefaultConnection points at postgres host' ([string]$settings.ConnectionStrings.DefaultConnection -like 'Host=postgres;*')
Assert-Case 'fresh: no leftover apply files' (-not (Get-ChildItem -LiteralPath $secrets1 -Filter '_apply-*' -ErrorAction SilentlyContinue))

$pgLine = Get-Content -LiteralPath $pgEnvPath | Where-Object { $_ -like 'POSTGRES_PASSWORD=*' }
$pgSecret = ($pgLine -split '=', 2)[1]
$omdbInSettings = ([string]$settings.Omdb.ApiKey -eq 'omdb-test-value')
Assert-Case 'fresh: OMDb key copied from source' $omdbInSettings
Assert-Case 'fresh: subtitle saving is enabled for the home library' ([bool]$settings.Subtitles.SaveEnabled -eq $true)
Assert-Case 'fresh: subtitle preferred languages are pt-BR then en' ((@($settings.Subtitles.PreferredLanguages) -join ',') -eq 'pt-BR,en')
$provider = $settings.Subtitles.PSObject.Properties['OpenSubtitles'] ? $settings.Subtitles.OpenSubtitles : $null
Assert-Case 'fresh: OpenSubtitles key copied from source' (($null -ne $provider) -and ([string]$provider.ApiKey -eq 'opensubtitles-test-value'))
Assert-Case 'fresh: OpenSubtitles endpoint is the production https API' (($null -ne $provider) -and ([string]$provider.BaseUrl -eq 'https://api.opensubtitles.com'))
Assert-Case 'fresh: OpenSubtitles allows the API and download hosts' (($null -ne $provider) -and ((@($provider.DownloadAllowedHosts) -join ',') -eq 'api.opensubtitles.com,dl.opensubtitles.org'))
Assert-Case 'fresh: OpenSubtitles keeps a download ceiling and timeout' (($null -ne $provider) -and ([long]$provider.MaxDownloadBytes -gt 0) -and ([string]$provider.RequestTimeout -ne ''))
Assert-Case 'fresh: secret values are not printed' ((-not $r1.Output.Contains($pgSecret)) -and (-not $r1.Output.Contains('omdb-test-value')) -and (-not $r1.Output.Contains('tmdb-test-value')) -and (-not $r1.Output.Contains('opensubtitles-test-value')))
Assert-Case 'fresh: secret values are not in the call log' ((-not (($r1.Log -join "`n").Contains($pgSecret))))

# Case 2: rerun on the same ops folder with the cluster present
Write-StubRules -Path $rules -Rules $presentRules
$passwordBefore = (Get-Content -LiteralPath $pgEnvPath -Raw)
$r2 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $cfg1, '-ArgoManifestPath', $argoManifest) -BinDir $bin -LogPath $log -RulesPath $rules
Assert-Case 'rerun: exit 0' ($r2.ExitCode -eq 0) "exit $($r2.ExitCode): $($r2.Output)"
Assert-Case 'rerun: no cluster create and no namespace create' ((-not (Test-LogHas $r2 'create cluster')) -and (-not (Test-LogHas $r2 'create namespace')))
Assert-Case 'rerun: existing credentials are kept' ((Get-Content -LiteralPath $pgEnvPath -Raw) -eq $passwordBefore)

# Case 3: fresh cluster but port 3100 already taken
$ops3 = New-Ops 'case-port'
$cfg3 = Write-TestConfig -Path (Join-Path $ops3 'home.config.json') -LibraryPath $library -BackupPath $backups `
    -KubeconfigPath (Join-Path $ops3 'kubeconfig') -SecretSourcePath $sourceEnv -CheckPorts $true
Write-StubRules -Path $rules -Rules @(@{ tool = 'kind'; match = '^get clusters'; stdout = '' })
$holder = $null
try { $holder = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 3100); $holder.Start() } catch { $holder = $null }
try {
    $r3 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $cfg3, '-ArgoManifestPath', $argoManifest) -BinDir $bin -LogPath $log -RulesPath $rules
}
finally {
    if ($holder) { $holder.Stop() }
}
Assert-Case 'port in use: exit 1' ($r3.ExitCode -eq 1) "exit $($r3.ExitCode)"
Assert-Case 'port in use: reports the port problem' ($r3.Output -match 'in use')
Assert-Case 'port in use: no cluster created' (-not (Test-LogHas $r3 'create cluster'))

# Case 4: WhatIf on a clean ops folder changes nothing
$ops4 = New-Ops 'case-whatif'
$cfg4 = Write-TestConfig -Path (Join-Path $ops4 'home.config.json') -LibraryPath $library -BackupPath $backups `
    -KubeconfigPath (Join-Path $ops4 'kubeconfig') -SecretSourcePath $sourceEnv
Write-StubRules -Path $rules -Rules @(@{ tool = 'kind'; match = '^get clusters'; stdout = '' })
$r4 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $cfg4, '-WhatIf') -BinDir $bin -LogPath $log -RulesPath $rules
Assert-Case 'WhatIf: exit 0' ($r4.ExitCode -eq 0) "exit $($r4.ExitCode): $($r4.Output)"
Assert-Case 'WhatIf: no create or apply calls' (-not ((Test-LogHas $r4 'create') -or (Test-LogHas $r4 'apply')))
Assert-Case 'WhatIf: no secrets folder written' (-not (Test-Path -LiteralPath (Join-Path $ops4 'secrets')))

# Case 5: the user kubeconfig is refused before any tool runs
$ops5 = New-Ops 'case-userkube'
$userKube = Join-Path (Join-Path $HOME '.kube') 'config'
$cfg5 = Write-TestConfig -Path (Join-Path $ops5 'home.config.json') -LibraryPath $library -BackupPath $backups `
    -KubeconfigPath $userKube -SecretSourcePath $sourceEnv
Write-StubRules -Path $rules -Rules @()
$r5 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $cfg5) -BinDir $bin -LogPath $log -RulesPath $rules
Assert-Case 'user kubeconfig: exit 1' ($r5.ExitCode -eq 1) "exit $($r5.ExitCode)"
Assert-Case 'user kubeconfig: refused with a message' ($r5.Output -match 'refusing')
Assert-Case 'user kubeconfig: no tool was called' ($r5.Log.Count -eq 0)

# Case 6: missing config file
$r6 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', (Join-Path $root 'does-not-exist.json')) -BinDir $bin -LogPath $log -RulesPath $rules
Assert-Case 'missing config: exit 1' ($r6.ExitCode -eq 1) "exit $($r6.ExitCode)"

# Case 7: OMDb key absent from the source; no cluster is created
$ops7 = New-Ops 'case-noomdb'
$cfg7 = Write-TestConfig -Path (Join-Path $ops7 'home.config.json') -LibraryPath $library -BackupPath $backups `
    -KubeconfigPath (Join-Path $ops7 'kubeconfig') -SecretSourcePath $sourceEnvNoOmdb
Write-StubRules -Path $rules -Rules @(@{ tool = 'kind'; match = '^get clusters'; stdout = '' })
$r7 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $cfg7, '-ArgoManifestPath', $argoManifest) -BinDir $bin -LogPath $log -RulesPath $rules
Assert-Case 'missing OMDb key: exit 1' ($r7.ExitCode -eq 1) "exit $($r7.ExitCode)"
Assert-Case 'missing OMDb key: names the key, not a value' ($r7.Output -match 'OMDB_API_KEY')
Assert-Case 'missing OMDb key: no cluster created' (-not (Test-LogHas $r7 'create cluster'))

# Case 9: OpenSubtitles key absent from the source; no cluster is created
$sourceEnvNoSubs = Join-Path $root 'source-no-subs.env'
Write-TestText -Path $sourceEnvNoSubs -Text "OMDB_API_KEY=omdb-test-value`nTMDB_API_KEY=tmdb-test-value`n"
$ops9 = New-Ops 'case-nosubs'
$cfg9 = Write-TestConfig -Path (Join-Path $ops9 'home.config.json') -LibraryPath $library -BackupPath $backups `
    -KubeconfigPath (Join-Path $ops9 'kubeconfig') -SecretSourcePath $sourceEnvNoSubs
Write-StubRules -Path $rules -Rules @(@{ tool = 'kind'; match = '^get clusters'; stdout = '' })
$r9 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $cfg9, '-ArgoManifestPath', $argoManifest) -BinDir $bin -LogPath $log -RulesPath $rules
Assert-Case 'missing OpenSubtitles key: exit 1' ($r9.ExitCode -eq 1) "exit $($r9.ExitCode)"
Assert-Case 'missing OpenSubtitles key: names the key, not a value' ($r9.Output -match 'OPENSUBTITLES_API_KEY')
Assert-Case 'missing OpenSubtitles key: no cluster created' (-not (Test-LogHas $r9 'create cluster'))

Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
$failures = Get-CaseFailureCount
Write-Host ''
Write-Host "New-HomeCluster companion: $failures failed"
if ($failures -gt 0) { exit 1 }
exit 0

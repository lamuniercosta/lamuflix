#Requires -Version 7.0
# Companion test for New-HomeCluster.ps1. kind, kubectl and docker are stubs; no cluster is created.
# Exit 0 when every case passes, 1 otherwise.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-test-lib.ps1')
. (Join-Path $PSScriptRoot '_home-common.ps1')

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
Write-TestText -Path $sourceEnv -Text "OMDB_API_KEY=omdb-test-value`nTMDB_API_KEY=tmdb-test-value`n"
$sourceEnvNoOmdb = Join-Path $root 'source-no-omdb.env'
Write-TestText -Path $sourceEnvNoOmdb -Text "TMDB_API_KEY=tmdb-test-value`n"

# Fixture Neon URL. The password is generated per run, so no credential-shaped literal lives in this file.
# It must never reach the output or the call log.
$neonPass = 'neon-test-' + [guid]::NewGuid().ToString('N')
$neonUrl = 'postgresql://neondb_owner:' + $neonPass + '@ep-test-1.neon.tech/neondb?sslmode=require&channel_binding=require'

function New-Ops {
    param([string]$Name, [switch]$WithoutNeon)
    $ops = Join-Path $root $Name
    $secrets = Join-Path $ops 'secrets'
    New-Item -ItemType Directory -Force -Path $secrets | Out-Null
    if (-not $WithoutNeon) {
        $line = 'DATABASE_URL="' + $neonUrl + '"'
        Write-TestText -Path (Join-Path $secrets 'neon.env') -Text ($line + "`n")
    }
    return $ops
}

# Unit checks for the Neon helpers (no tools run). The password is percent-encoded from a generated value.
$decodedPass = $neonPass + '@'
$encodedPass = [System.Uri]::EscapeDataString($decodedPass)
$npg = ConvertTo-NpgsqlConnectionString -Url ('postgresql://neondb_owner:' + $encodedPass + '@ep-x.neon.tech/neondb?sslmode=require')
Assert-Case 'npgsql: decodes the password, default port 5432, Require SSL' ($npg -eq ('Host=ep-x.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=' + $decodedPass + ';SSL Mode=Require'))
$semiRefused = $false
try { ConvertTo-NpgsqlConnectionString -Url ('postgresql://u:' + $neonPass + ';x@h.neon.tech/db') | Out-Null } catch { $semiRefused = $true }
Assert-Case 'npgsql: refuses a ";" inside a value' $semiRefused

$freshRules = @(
    @{ tool = 'kind'; match = '^get clusters'; stdout = '' },
    @{ tool = 'kubectl'; match = 'get namespace'; exit = 1 },
    @{ tool = 'kubectl'; match = 'create secret'; stdout = "apiVersion: v1`nkind: Secret`nmetadata:`n  name: stub`n" },
    @{ tool = 'kubectl'; match = 'delete secret'; stdout = '' },
    @{ tool = 'kubectl'; match = 'status\.sync'; stdout = 'Synced' },
    @{ tool = 'kubectl'; match = 'status\.health'; stdout = 'Healthy' }
)
$presentRules = @(
    @{ tool = 'kind'; match = '^get clusters'; stdout = 'lamuflix-home' },
    @{ tool = 'kubectl'; match = 'create secret'; stdout = "apiVersion: v1`nkind: Secret`nmetadata:`n  name: stub`n" },
    @{ tool = 'kubectl'; match = 'delete secret'; stdout = '' },
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
Assert-Case 'fresh: catalog-database, rabbitmq and api settings written to secrets' ((Test-Path (Join-Path $secrets1 'catalog-database.env')) -and (Test-Path (Join-Path $secrets1 'rabbitmq.env')) -and (Test-Path (Join-Path $secrets1 'api.settings.json')))
Assert-Case 'fresh: no postgres credentials file is written' (-not (Test-Path (Join-Path $secrets1 'postgres.env')))
$catalogLine = (Get-Content -LiteralPath (Join-Path $secrets1 'catalog-database.env') -Raw)
Assert-Case 'fresh: catalog-database env holds an unquoted DATABASE_URL' ($catalogLine.StartsWith('DATABASE_URL=postgresql://'))
$settings = Get-Content -LiteralPath (Join-Path $secrets1 'api.settings.json') -Raw | ConvertFrom-Json
Assert-Case 'fresh: OTEL endpoint removed from api settings' (-not ($settings.PSObject.Properties.Name -contains 'OTEL_EXPORTER_OTLP_ENDPOINT'))
Assert-Case 'fresh: DefaultConnection is the Neon Npgsql string' ([string]$settings.ConnectionStrings.DefaultConnection -eq "Host=ep-test-1.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=$neonPass;SSL Mode=Require")
Assert-Case 'fresh: DefaultConnection does not point at an in-cluster host' ([string]$settings.ConnectionStrings.DefaultConnection -notlike 'Host=postgres*')
Assert-Case 'fresh: publishes the catalog-database secret' (Test-LogHas $r1 'create secret generic catalog-database')
Assert-Case 'fresh: no postgres-credentials secret is published' (-not (Test-LogHas $r1 'create secret generic postgres-credentials'))
Assert-Case 'fresh: legacy postgres-credentials secret is removed if present' (Test-LogHas $r1 'delete secret postgres-credentials --ignore-not-found')
Assert-Case 'fresh: no leftover apply files' (-not (Get-ChildItem -LiteralPath $secrets1 -Filter '_apply-*' -ErrorAction SilentlyContinue))
$omdbInSettings = ([string]$settings.Omdb.ApiKey -eq 'omdb-test-value')
Assert-Case 'fresh: OMDb key copied from source' $omdbInSettings
Assert-Case 'fresh: secret values are not printed' ((-not $r1.Output.Contains($neonPass)) -and (-not $r1.Output.Contains('omdb-test-value')) -and (-not $r1.Output.Contains('tmdb-test-value')))
Assert-Case 'fresh: secret values are not in the call log' ((-not (($r1.Log -join "`n").Contains($neonPass))))

# Case 2: rerun on the same ops folder with the cluster present
Write-StubRules -Path $rules -Rules $presentRules
$rmqPath = Join-Path $secrets1 'rabbitmq.env'
$rmqBefore = (Get-Content -LiteralPath $rmqPath -Raw)
$r2 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $cfg1, '-ArgoManifestPath', $argoManifest) -BinDir $bin -LogPath $log -RulesPath $rules
Assert-Case 'rerun: exit 0' ($r2.ExitCode -eq 0) "exit $($r2.ExitCode): $($r2.Output)"
Assert-Case 'rerun: no cluster create and no namespace create' ((-not (Test-LogHas $r2 'create cluster')) -and (-not (Test-LogHas $r2 'create namespace')))
Assert-Case 'rerun: existing rabbitmq credentials are kept' ((Get-Content -LiteralPath $rmqPath -Raw) -eq $rmqBefore)

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
Assert-Case 'WhatIf: no generated secret files written' (-not ((Test-Path -LiteralPath (Join-Path $ops4 'secrets\rabbitmq.env')) -or (Test-Path -LiteralPath (Join-Path $ops4 'secrets\catalog-database.env')) -or (Test-Path -LiteralPath (Join-Path $ops4 'secrets\api.settings.json'))))

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

# Case 8: neon.env absent; the script names DATABASE_URL and creates no cluster
$ops8 = New-Ops 'case-noneon' -WithoutNeon
$cfg8 = Write-TestConfig -Path (Join-Path $ops8 'home.config.json') -LibraryPath $library -BackupPath $backups `
    -KubeconfigPath (Join-Path $ops8 'kubeconfig') -SecretSourcePath $sourceEnv
Write-StubRules -Path $rules -Rules @(@{ tool = 'kind'; match = '^get clusters'; stdout = '' })
$r8 = Invoke-HomeCase -Script $target -Arguments @('-ConfigPath', $cfg8, '-ArgoManifestPath', $argoManifest) -BinDir $bin -LogPath $log -RulesPath $rules
Assert-Case 'missing neon.env: exit 1' ($r8.ExitCode -eq 1) "exit $($r8.ExitCode)"
Assert-Case 'missing neon.env: names DATABASE_URL' ($r8.Output -match 'DATABASE_URL')
Assert-Case 'missing neon.env: no cluster created' (-not (Test-LogHas $r8 'create cluster'))

Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
$failures = Get-CaseFailureCount
Write-Host ''
Write-Host "New-HomeCluster companion: $failures failed"
if ($failures -gt 0) { exit 1 }
exit 0

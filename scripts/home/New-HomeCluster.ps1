#Requires -Version 7.0
# Creates or repairs the home kind cluster, its Secrets, Argo CD and the lamuflix-home Application.
# Safe to run again: existing cluster, namespaces and Argo CD are left in place and re-applied.
# Never prints secret values; never reads or writes ~\.kube\config.
#
# Usage: pwsh -NoProfile -File scripts/home/New-HomeCluster.ps1 [-ConfigPath <file>] [-ClusterName <name>] [-WhatIf]
# Exit 0 on success, 1 on failure.

[CmdletBinding()]
param(
    [string]$ConfigPath = 'F:\Dev\lamuflix-home\home.config.json',
    [string]$ClusterName,
    [string]$KubeconfigPath,
    [string]$ArgoManifestPath,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-common.ps1')

$ArgoInstallUrl = 'https://raw.githubusercontent.com/argoproj/argo-cd/v3.6.1/manifests/install.yaml'
$ArgoInstallSha256 = '7be23ca7d9ca3d3074dee8332f1ca92a933b990cee7c9cf22cf3843ee8c2913e'
$AppNamespace = 'lamuflix-home'
$ArgoNamespace = 'argocd'

function Ensure-Namespace {
    param($Cfg, [string]$Name)
    if ((Invoke-Kube -Cfg $Cfg -KubeArgs @('get', 'namespace', $Name)).ExitCode -eq 0) { return }
    Assert-Kube -Cfg $Cfg -KubeArgs @('create', 'namespace', $Name) -What "create namespace $Name" | Out-Null
    Write-HomeInfo "created namespace $Name"
}

function Get-FreshCredentials {
    param([string]$SecretsDir)
    $pgEnv = Join-Path $SecretsDir 'postgres.env'
    if (-not (Test-Path -LiteralPath $pgEnv)) {
        Write-EnvFile -Path $pgEnv -Values ([ordered]@{ POSTGRES_USER = 'lamuflix'; 'POSTGRES_PASSWORD' = (New-RandomSecret -Length 40) })
        Write-HomeInfo 'generated postgres credentials (values not shown)'
    }
    $rmqEnv = Join-Path $SecretsDir 'rabbitmq.env'
    if (-not (Test-Path -LiteralPath $rmqEnv)) {
        Write-EnvFile -Path $rmqEnv -Values ([ordered]@{ RABBITMQ_DEFAULT_USER = 'lamuflix'; RABBITMQ_DEFAULT_PASS = (New-RandomSecret -Length 40) })
        Write-HomeInfo 'generated rabbitmq credentials (values not shown)'
    }
}

function New-ApiSettingsFile {
    param([string]$RepoRoot, [string]$SecretsDir, $Cfg)
    $example = Join-Path $RepoRoot 'docker\api.settings.example.json'
    if (-not (Test-Path -LiteralPath $example -PathType Leaf)) { throw "missing $example" }
    $pg = Read-EnvFile (Join-Path $SecretsDir 'postgres.env')
    $rmq = Read-EnvFile (Join-Path $SecretsDir 'rabbitmq.env')

    $omdb = Read-EnvValue -Path $Cfg.SecretSourcePath -Key 'OMDB_API_KEY'
    if (-not $omdb) { throw 'OMDB_API_KEY not found in secretSourcePath' }

    $settings = Get-Content -LiteralPath $example -Raw | ConvertFrom-Json
    $settings.PSObject.Properties.Remove('OTEL_EXPORTER_OTLP_ENDPOINT')
    $settings.ConnectionStrings.DefaultConnection = "Host=postgres;Database=lamuflix;Username=$($pg['POSTGRES_USER']);Password=$($pg['POSTGRES_PASSWORD'])"
    $settings.RabbitMq.UserName = $rmq['RABBITMQ_DEFAULT_USER']
    $settings.RabbitMq.Password = $rmq['RABBITMQ_DEFAULT_PASS']
    $settings.Omdb.ApiKey = $omdb

    Write-TextFileUtf8 -Path (Join-Path $SecretsDir 'api.settings.json') -Text ($settings | ConvertTo-Json -Depth 20)
}

function Read-EnvFile {
    param([string]$Path)
    $map = [ordered]@{}
    foreach ($line in (Get-Content -LiteralPath $Path)) {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') { $map[$matches[1]] = $matches[2] }
    }
    return $map
}

function Get-TmdbKeyFile {
    param($Cfg, [string]$SecretsDir)
    $tmdb = Read-EnvValue -Path $Cfg.SecretSourcePath -Key 'TMDB_API_KEY'
    $path = Join-Path $SecretsDir 'tmdb.env'
    if ($tmdb) {
        Write-EnvFile -Path $path -Values ([ordered]@{ apiKey = $tmdb })
        return $path
    }
    Write-HomeInfo 'TMDB_API_KEY not found; tmdb-credentials is optional and will be skipped'
    return $null
}

function Publish-Secret {
    param($Cfg, [string]$Name, [string[]]$CreateArgs, [string]$SecretsDir)
    $yaml = Invoke-Kube -Cfg $Cfg -KubeArgs (@('-n', $AppNamespace, 'create', 'secret', 'generic', $Name) + $CreateArgs + @('--dry-run=client', '-o', 'yaml'))
    if ($yaml.ExitCode -ne 0) { throw "could not render secret $Name (kubectl exit $($yaml.ExitCode))" }
    $tmp = Join-Path $SecretsDir "_apply-$Name.yaml"
    try {
        Write-TextFileUtf8 -Path $tmp -Text (($yaml.Output -join "`n") + "`n")
        Assert-Kube -Cfg $Cfg -KubeArgs @('apply', '-f', $tmp) -What "apply secret $Name" | Out-Null
    }
    finally {
        Remove-Item -LiteralPath $tmp -ErrorAction SilentlyContinue
    }
    Write-HomeInfo "secret $Name applied (values not shown)"
}

function New-KindCluster {
    param($Cfg, [string]$RepoRoot, [string]$SecretsDir)
    $template = Join-Path $RepoRoot 'deploy\kind\cluster.template.yaml'
    if (-not (Test-Path -LiteralPath $template -PathType Leaf)) { throw "missing $template" }
    foreach ($p in @($Cfg.LibraryPath, $Cfg.BackupPath)) {
        if ($p.Contains("'")) { throw "paths with a single quote are not supported in the kind config: $p" }
    }
    $text = (Get-Content -LiteralPath $template -Raw).Replace('{{LIBRARY_PATH}}', $Cfg.LibraryPath).Replace('{{BACKUP_PATH}}', $Cfg.BackupPath)
    $configFile = Join-Path $SecretsDir 'kind-cluster.yaml'
    Write-TextFileUtf8 -Path $configFile -Text $text
    $create = Invoke-Tool -Exe $Cfg.Kind -Arguments @('create', 'cluster', '--name', $Cfg.ClusterName, '--config', $configFile, '--kubeconfig', $Cfg.Kubeconfig)
    if ($create.ExitCode -ne 0) { throw "kind create cluster failed (exit $($create.ExitCode))" }
    Write-HomeInfo "created kind cluster $($Cfg.ClusterName)"
}

function Install-ArgoCd {
    param($Cfg, [string]$RepoRoot, [string]$ScratchDir)
    if ($ArgoManifestPath) {
        Write-HomeInfo 'using -ArgoManifestPath; upstream checksum not verified (offline copy)'
        $manifest = $ArgoManifestPath
    }
    else {
        $manifest = Join-Path $ScratchDir 'argocd-install.yaml'
        Invoke-WebRequest -Uri $ArgoInstallUrl -OutFile $manifest -UseBasicParsing -TimeoutSec 120
        $actual = (Get-FileHash -LiteralPath $manifest -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($actual -ne $ArgoInstallSha256) { throw "Argo CD install.yaml checksum mismatch (got $actual)" }
        Write-HomeInfo 'Argo CD v3.6.1 install.yaml checksum verified'
    }
    Assert-Kube -Cfg $Cfg -KubeArgs @('apply', '--server-side', '--force-conflicts', '-n', $ArgoNamespace, '-f', $manifest) -What 'apply Argo CD install' | Out-Null
    Assert-Kube -Cfg $Cfg -KubeArgs @('apply', '--server-side', '--force-conflicts', '-k', (Join-Path $RepoRoot 'deploy\argocd')) -What 'apply Argo CD patches' | Out-Null
    Assert-Kube -Cfg $Cfg -KubeArgs @('-n', $ArgoNamespace, 'rollout', 'status', 'deployment/argocd-server', "--timeout=$($Cfg.WaitSeconds)s") -What 'wait for argocd-server' | Out-Null
    Write-HomeInfo 'Argo CD is up'
}

function Wait-Application {
    param($Cfg)
    $deadline = (Get-Date).AddSeconds($Cfg.WaitSeconds)
    $sync = ''
    $health = ''
    while ($true) {
        $s = Invoke-Kube -Cfg $Cfg -KubeArgs @('-n', $ArgoNamespace, 'get', 'application', 'lamuflix-home', '-o', 'jsonpath={.status.sync.status}')
        $h = Invoke-Kube -Cfg $Cfg -KubeArgs @('-n', $ArgoNamespace, 'get', 'application', 'lamuflix-home', '-o', 'jsonpath={.status.health.status}')
        $sync = (@($s.Output) -join '').Trim()
        $health = (@($h.Output) -join '').Trim()
        if ($sync -eq 'Synced' -and $health -eq 'Healthy') { return }
        if ((Get-Date) -ge $deadline) { throw "application lamuflix-home not Synced and Healthy within $($Cfg.WaitSeconds)s (sync=$sync, health=$health)" }
        Start-Sleep -Seconds 5
    }
}

$exitCode = 0
$scratch = $null
try {
    $repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $cfg = Read-HomeConfig -Path $ConfigPath -ClusterName $ClusterName -KubeconfigPath $KubeconfigPath
    Write-HomeInfo "cluster $($cfg.ClusterName), kubeconfig $($cfg.Kubeconfig)"

    if ($cfg.WebPort -ne 3100 -or $cfg.ArgoPort -ne 3180) {
        throw 'webPort and argoPort must be 3100 and 3180: deploy/kind/cluster.template.yaml fixes the host ports'
    }

    $docker = Invoke-Tool -Exe $cfg.Docker -Arguments @('info', '--format', '{{.ServerVersion}}')
    if ($docker.ExitCode -ne 0) { throw 'Docker is not reachable; start Docker Desktop and retry' }

    foreach ($p in @($cfg.LibraryPath, $cfg.BackupPath)) {
        if (-not (Test-Path -LiteralPath $p -PathType Container)) { throw "folder does not exist: $p" }
    }
    if ($cfg.SecretSourcePath -and -not (Test-Path -LiteralPath $cfg.SecretSourcePath -PathType Leaf)) {
        throw "secretSourcePath does not exist: $($cfg.SecretSourcePath)"
    }

    $clusterExists = Test-ClusterExists -Cfg $cfg
    if (-not $clusterExists -and $cfg.CheckPorts) {
        foreach ($p in @($cfg.WebPort, $cfg.ArgoPort)) {
            if (-not (Test-PortFree -Port $p)) { throw "port $p on 127.0.0.1 is in use" }
        }
    }
    Write-HomeInfo "preflight passed (cluster exists: $clusterExists)"

    if ($WhatIf) {
        Write-HomeInfo 'WhatIf: no changes made. Would create the ops folders, generate missing credentials, build api.settings.json, create the cluster if absent, create namespaces and Secrets, install Argo CD, apply the Application and wait for Synced and Healthy.'
        exit 0
    }

    $secretsDir = Join-Path $cfg.OpsRoot 'secrets'
    New-Item -ItemType Directory -Force -Path $secretsDir | Out-Null
    $scratch = Join-Path $cfg.OpsRoot 'scratch-new-home'
    New-Item -ItemType Directory -Force -Path $scratch | Out-Null

    Get-FreshCredentials -SecretsDir $secretsDir
    New-ApiSettingsFile -RepoRoot $repoRoot -SecretsDir $secretsDir -Cfg $cfg
    $tmdbFile = Get-TmdbKeyFile -Cfg $cfg -SecretsDir $secretsDir

    if (-not $clusterExists) { New-KindCluster -Cfg $cfg -RepoRoot $repoRoot -SecretsDir $secretsDir }

    Ensure-Namespace -Cfg $cfg -Name $AppNamespace
    Ensure-Namespace -Cfg $cfg -Name $ArgoNamespace

    Publish-Secret -Cfg $cfg -Name 'postgres-credentials' -SecretsDir $secretsDir -CreateArgs @("--from-env-file=$(Join-Path $secretsDir 'postgres.env')")
    Publish-Secret -Cfg $cfg -Name 'rabbitmq-credentials' -SecretsDir $secretsDir -CreateArgs @("--from-env-file=$(Join-Path $secretsDir 'rabbitmq.env')")
    Publish-Secret -Cfg $cfg -Name 'api-settings' -SecretsDir $secretsDir -CreateArgs @("--from-file=appsettings.Production.json=$(Join-Path $secretsDir 'api.settings.json')")
    if ($tmdbFile) {
        Publish-Secret -Cfg $cfg -Name 'tmdb-credentials' -SecretsDir $secretsDir -CreateArgs @("--from-env-file=$tmdbFile")
    }

    Install-ArgoCd -Cfg $cfg -RepoRoot $repoRoot -ScratchDir $scratch
    Assert-Kube -Cfg $cfg -KubeArgs @('apply', '-f', (Join-Path $repoRoot 'deploy\argocd\application.yaml')) -What 'apply Application' | Out-Null
    Wait-Application -Cfg $cfg

    Write-HomeInfo "web:  $($cfg.SiteBaseUrl)/"
    Write-HomeInfo "argo: $($cfg.ArgoUrl)/  (admin password is in the argocd-initial-admin-secret; not printed)"
}
catch {
    Write-HomeFail $_.Exception.Message
    $exitCode = 1
}
finally {
    if ($scratch -and (Test-Path -LiteralPath $scratch)) {
        Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
    }
}
exit $exitCode

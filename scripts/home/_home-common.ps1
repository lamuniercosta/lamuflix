# Shared helpers for the operator scripts in scripts/home. Dot-source; no work happens on load.
#
# Config file (default F:\Dev\lamuflix-home\home.config.json). Keys follow deploy/home/home.config.example.json:
#   clusterName, libraryPath, backupPath, webPort (3100), argoPort (3180)
# Optional keys:
#   kubeconfigPath   default <opsRoot>\kubeconfig. Must not be the user's ~\.kube\config.
#   secretSourcePath env-style file holding TMDB_API_KEY and OMDB_API_KEY (values are never printed)
#   releaseJsonPath  RELEASE.json written by promote-home, read by Get-HomeStatus
#   kind, kubectl, docker   executables, default from PATH
#   waitSeconds      default 900
#   checkPorts       default true; false skips the port-free preflight (tests only)

Set-StrictMode -Version Latest

function Write-HomeInfo {
    param([string]$Message)
    Write-Host "[home] $Message"
}

function Write-HomeFail {
    param([string]$Message)
    Write-Host "[home] FAIL: $Message" -ForegroundColor Red
}

function Get-OptionalProperty {
    param($Object, [string]$Name, $Default = $null)
    if ($null -ne $Object -and $Object.PSObject.Properties.Name -contains $Name -and $null -ne $Object.$Name) {
        return $Object.$Name
    }
    return $Default
}

function Assert-ScratchKubeconfig {
    param([string]$Path)
    $full = [System.IO.Path]::GetFullPath($Path)
    $userConfig = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $HOME '.kube') 'config'))
    if ([string]::Equals($full, $userConfig, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "refusing to use the user kubeconfig ($userConfig); set kubeconfigPath to a scratch file"
    }
}

function Read-HomeConfig {
    param(
        [Parameter(Mandatory)][string]$Path,
        [string]$ClusterName,
        [string]$KubeconfigPath
    )
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "config not found: $Path (pass -ConfigPath or create it from deploy/home/home.config.example.json)"
    }
    $raw = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $opsRoot = Split-Path -Parent ([System.IO.Path]::GetFullPath($Path))

    foreach ($required in @('libraryPath', 'backupPath')) {
        if (-not (Get-OptionalProperty $raw $required)) { throw "config is missing '$required'" }
    }

    $name = if ($ClusterName) { $ClusterName } else { [string](Get-OptionalProperty $raw 'clusterName' 'lamuflix-home') }
    if ($name -notmatch '^[a-z0-9][a-z0-9-]{0,40}$') { throw "invalid cluster name: $name" }

    $kube = if ($KubeconfigPath) { $KubeconfigPath } else { [string](Get-OptionalProperty $raw 'kubeconfigPath' (Join-Path $opsRoot 'kubeconfig')) }
    Assert-ScratchKubeconfig $kube

    $webPort = [int](Get-OptionalProperty $raw 'webPort' 3100)
    $argoPort = [int](Get-OptionalProperty $raw 'argoPort' 3180)

    return [pscustomobject]@{
        ConfigPath       = [System.IO.Path]::GetFullPath($Path)
        OpsRoot          = $opsRoot
        ClusterName      = $name
        Kubeconfig       = [System.IO.Path]::GetFullPath($kube)
        LibraryPath      = [string]$raw.libraryPath
        BackupPath       = [string]$raw.backupPath
        WebPort          = $webPort
        ArgoPort         = $argoPort
        SiteBaseUrl      = "http://localhost:$webPort"
        ArgoUrl          = "http://localhost:$argoPort"
        SecretSourcePath = [string](Get-OptionalProperty $raw 'secretSourcePath' '')
        ReleaseJsonPath  = [string](Get-OptionalProperty $raw 'releaseJsonPath' '')
        Kind             = [string](Get-OptionalProperty $raw 'kind' 'kind')
        Kubectl          = [string](Get-OptionalProperty $raw 'kubectl' 'kubectl')
        Docker           = [string](Get-OptionalProperty $raw 'docker' 'docker')
        WaitSeconds      = [int](Get-OptionalProperty $raw 'waitSeconds' 900)
        CheckPorts       = [bool](Get-OptionalProperty $raw 'checkPorts' $true)
    }
}

function Invoke-Tool {
    param(
        [Parameter(Mandatory)][string]$Exe,
        [string[]]$Arguments = @()
    )
    try {
        $output = @(& $Exe @Arguments 2>&1 | ForEach-Object { [string]$_ })
        $code = $LASTEXITCODE
    }
    catch {
        return [pscustomobject]@{ ExitCode = 127; Output = @($_.Exception.Message) }
    }
    return [pscustomobject]@{ ExitCode = $code; Output = $output }
}

function Invoke-Kube {
    param([Parameter(Mandatory)]$Cfg, [string[]]$KubeArgs = @())
    return Invoke-Tool -Exe $Cfg.Kubectl -Arguments (@('--kubeconfig', $Cfg.Kubeconfig) + $KubeArgs)
}

function Assert-Kube {
    param([Parameter(Mandatory)]$Cfg, [string[]]$KubeArgs, [string]$What)
    $result = Invoke-Kube -Cfg $Cfg -KubeArgs $KubeArgs
    if ($result.ExitCode -ne 0) { throw "$What failed (kubectl exit $($result.ExitCode))" }
    return $result
}

function Get-KindClusters {
    param([Parameter(Mandatory)]$Cfg)
    $result = Invoke-Tool -Exe $Cfg.Kind -Arguments @('get', 'clusters')
    if ($result.ExitCode -ne 0) { throw "kind get clusters failed (exit $($result.ExitCode))" }
    return @($result.Output | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })
}

function Test-ClusterExists {
    param([Parameter(Mandatory)]$Cfg, [string]$Name = $Cfg.ClusterName)
    return ((Get-KindClusters -Cfg $Cfg) -contains $Name)
}

function Test-PortFree {
    param([int]$Port)
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
        $listener.Start()
        $listener.Stop()
        return $true
    }
    catch {
        return $false
    }
}

function New-RandomSecret {
    param([int]$Length = 40)
    $alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789'
    $chars = [char[]]::new($Length)
    for ($i = 0; $i -lt $Length; $i++) {
        $chars[$i] = $alphabet[[System.Security.Cryptography.RandomNumberGenerator]::GetInt32($alphabet.Length)]
    }
    return -join $chars
}

function Write-TextFileUtf8 {
    param([string]$Path, [string]$Text)
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function Write-EnvFile {
    param([string]$Path, [System.Collections.IDictionary]$Values)
    $lines = foreach ($key in $Values.Keys) { "$key=$($Values[$key])" }
    Write-TextFileUtf8 -Path $Path -Text (($lines -join "`n") + "`n")
}

function Read-EnvValue {
    param([string]$Path, [string]$Key)
    if (-not $Path -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
    foreach ($line in (Get-Content -LiteralPath $Path)) {
        if ($line -match "^\s*$([regex]::Escape($Key))\s*=\s*(.*?)\s*$") {
            return $matches[1].Trim('"', "'")
        }
    }
    return $null
}

function Get-NewestDump {
    param([string]$Directory, [string]$Pattern = 'lamuflix-home-*.dump')
    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) { return $null }
    return Get-ChildItem -LiteralPath $Directory -Filter $Pattern -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

# Same digest as deploy/k8s/base (postgres) and phase2-decisions. Used only to run pg_restore -l on the host.
$HomePostgresImage = 'pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d'

function Test-DumpWithPgRestore {
    param([Parameter(Mandatory)]$Cfg, [Parameter(Mandatory)][string]$DumpPath)
    $full = [System.IO.Path]::GetFullPath($DumpPath)
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { throw "dump not found: $full" }
    $dir = Split-Path -Parent $full
    $name = Split-Path -Leaf $full
    $list = Invoke-Tool -Exe $Cfg.Docker -Arguments @('run', '--rm', '-v', "${dir}:/restore:ro", '--entrypoint', 'pg_restore', $HomePostgresImage, '-l', "/restore/$name")
    return ($list.ExitCode -eq 0)
}

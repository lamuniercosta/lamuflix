# Helpers for the Test-*.ps1 companions in scripts/home. Dot-source only.
# Companions run the real operator scripts as child processes with kind, kubectl and docker
# replaced by stubs on PATH (see _home-stub.ps1). No Docker, cluster or user kubeconfig is touched.

Set-StrictMode -Version Latest

$script:CaseFailures = 0

function Write-TestText {
    param([string]$Path, [string]$Text)
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function New-TestRoot {
    $root = Join-Path ([System.IO.Path]::GetTempPath()) ('lamuflix-home-test-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    return $root
}

function New-StubBin {
    param([string]$Dir, [string[]]$Tools = @('kind', 'kubectl', 'docker'))
    New-Item -ItemType Directory -Force -Path $Dir | Out-Null
    $main = Join-Path $PSScriptRoot '_home-stub.ps1'
    foreach ($tool in $Tools) {
        if ($IsWindows) {
            $body = '@echo off' + "`r`n" + 'pwsh -NoProfile -File "' + $main + '" ' + $tool + ' %*' + "`r`n" + 'exit /b %ERRORLEVEL%' + "`r`n"
            Write-TestText -Path (Join-Path $Dir "$tool.cmd") -Text $body
        }
        else {
            $launcher = Join-Path $Dir $tool
            $body = "#!/bin/sh`n" + "exec pwsh -NoProfile -File '" + $main + "' " + $tool + " `"`$@`"`n"
            Write-TestText -Path $launcher -Text $body
            & chmod +x $launcher
        }
    }
    return $Dir
}

function Write-StubRules {
    param([string]$Path, [object[]]$Rules = @())
    $json = ConvertTo-Json -InputObject @($Rules) -Depth 6
    if (@($Rules).Count -eq 0) { $json = '[]' }
    Write-TestText -Path $Path -Text $json
}

function Write-TestConfig {
    param(
        [string]$Path,
        [string]$LibraryPath,
        [string]$BackupPath,
        [string]$KubeconfigPath,
        [string]$SecretSourcePath = '',
        [string]$ReleaseJsonPath = '',
        [int]$WebPort = 3100,
        [int]$ArgoPort = 3180,
        [bool]$CheckPorts = $false,
        [string]$ClusterName = 'lamuflix-home'
    )
    $config = [ordered]@{
        clusterName      = $ClusterName
        libraryPath      = $LibraryPath
        backupPath       = $BackupPath
        webPort          = $WebPort
        argoPort         = $ArgoPort
        kubeconfigPath   = $KubeconfigPath
        secretSourcePath = $SecretSourcePath
        releaseJsonPath  = $ReleaseJsonPath
        kind             = 'kind'
        kubectl          = 'kubectl'
        docker           = 'docker'
        waitSeconds      = 30
        checkPorts       = $CheckPorts
    }
    Write-TestText -Path $Path -Text (ConvertTo-Json -InputObject $config -Depth 4)
    return $Path
}

function Invoke-HomeCase {
    param(
        [Parameter(Mandatory)][string]$Script,
        [string[]]$Arguments = @(),
        [Parameter(Mandatory)][string]$BinDir,
        [Parameter(Mandatory)][string]$LogPath,
        [Parameter(Mandatory)][string]$RulesPath
    )
    $savedPath = $env:PATH
    $savedLog = $env:LAMUFLIX_STUB_LOG
    $savedRules = $env:LAMUFLIX_STUB_RULES
    try {
        $env:PATH = $BinDir + [System.IO.Path]::PathSeparator + $savedPath
        $env:LAMUFLIX_STUB_LOG = $LogPath
        $env:LAMUFLIX_STUB_RULES = $RulesPath
        if (Test-Path -LiteralPath $LogPath) { Remove-Item -LiteralPath $LogPath -Force }
        $output = @(& pwsh -NoProfile -File $Script @Arguments 2>&1 | ForEach-Object { [string]$_ })
        $code = $LASTEXITCODE
    }
    finally {
        $env:PATH = $savedPath
        $env:LAMUFLIX_STUB_LOG = $savedLog
        $env:LAMUFLIX_STUB_RULES = $savedRules
    }
    $log = @()
    if (Test-Path -LiteralPath $LogPath) { $log = @(Get-Content -LiteralPath $LogPath) }
    return [pscustomobject]@{ ExitCode = $code; Output = ($output -join "`n"); Log = $log }
}

function Assert-Case {
    param([string]$Name, [bool]$Ok, [string]$Detail = '')
    if ($Ok) {
        Write-Host "PASS  $Name"
    }
    else {
        $script:CaseFailures++
        Write-Host "FAIL  $Name $Detail" -ForegroundColor Red
    }
}

function Test-LogHas {
    param($Result, [string]$Pattern)
    $hits = @(@($Result.Log) | Where-Object { $_ -match $Pattern })
    return ($hits.Count -gt 0)
}

function Get-CaseFailureCount {
    return $script:CaseFailures
}

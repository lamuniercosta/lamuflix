#Requires -Version 7.0
# Read-only status for the home cluster: cluster present, Argo CD sync and health, deployed commit
# and the two web routes. One line per check; "STATUS: healthy" only when
# every check passes. Exit 0 when healthy, 1 otherwise. Changes nothing on the cluster or the host.
#
# Usage: pwsh -NoProfile -File scripts/home/Get-HomeStatus.ps1 [-ConfigPath <file>] [-ClusterName <name>]

[CmdletBinding()]
param(
    [string]$ConfigPath = 'F:\Dev\lamuflix-home\home.config.json',
    [string]$ClusterName,
    [string]$KubeconfigPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_home-common.ps1')

$ArgoNamespace = 'argocd'
$ArgoApp = 'lamuflix-home'
$WebRoutes = @('/', '/api/genres')

$script:Problems = [System.Collections.Generic.List[string]]::new()

function Add-Problem {
    param([string]$Text)
    $script:Problems.Add($Text)
    Write-HomeFail $Text
}

function Get-ArgoField {
    param($Cfg, [string]$JsonPath)
    $result = Invoke-Kube -Cfg $Cfg -KubeArgs @('-n', $ArgoNamespace, 'get', 'application', $ArgoApp, '-o', "jsonpath={$JsonPath}")
    if ($result.ExitCode -ne 0) { return $null }
    return (@($result.Output) -join '').Trim()
}

function Get-HttpStatus {
    param([string]$Url)
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 10 -SkipHttpErrorCheck
        return [string][int]$response.StatusCode
    }
    catch {
        return 'unreachable'
    }
}

function Test-ArgoStatus {
    param($Cfg)
    $sync = Get-ArgoField -Cfg $Cfg -JsonPath '.status.sync.status'
    $health = Get-ArgoField -Cfg $Cfg -JsonPath '.status.health.status'
    $revision = Get-ArgoField -Cfg $Cfg -JsonPath '.status.sync.revision'
    Write-HomeInfo "argo sync: $sync; health: $health; revision: $revision"
    if ($sync -ne 'Synced') { Add-Problem 'argo sync status is not Synced' }
    if ($health -ne 'Healthy') { Add-Problem 'argo health is not Healthy' }
}

function Test-DeployedCommit {
    param($Cfg)
    $release = [string]$Cfg.ReleaseJsonPath
    if (-not $release) {
        Write-HomeInfo 'deployed commit: releaseJsonPath not configured'
        return
    }
    if (-not (Test-Path -LiteralPath $release -PathType Leaf)) {
        Add-Problem "releaseJsonPath is set but the file is missing: $release"
        return
    }
    $json = Get-Content -LiteralPath $release -Raw | ConvertFrom-Json
    $commit = Get-OptionalProperty $json 'commit' 'not recorded'
    Write-HomeInfo "deployed commit (RELEASE.json): $commit"
}

function Test-WebRoutes {
    param($Cfg)
    $base = $Cfg.SiteBaseUrl.TrimEnd('/')
    foreach ($route in $WebRoutes) {
        $code = Get-HttpStatus -Url ($base + $route)
        Write-HomeInfo "web $route : $code"
        if ($code -ne '200') { Add-Problem "web $route returned $code" }
    }
}

$exitCode = 0
try {
    $cfg = Read-HomeConfig -Path $ConfigPath -ClusterName $ClusterName -KubeconfigPath $KubeconfigPath
    if (Test-ClusterExists -Cfg $cfg) {
        Write-HomeInfo "cluster: $($cfg.ClusterName) present"
        Test-ArgoStatus -Cfg $cfg
    }
    else {
        Add-Problem "cluster $($cfg.ClusterName) is not present"
    }
    Test-DeployedCommit -Cfg $cfg
    Test-WebRoutes -Cfg $cfg

    if ($script:Problems.Count -eq 0) {
        Write-HomeInfo 'STATUS: healthy'
    }
    else {
        Write-HomeFail "STATUS: degraded ($($script:Problems.Count) problem(s))"
        $exitCode = 1
    }
}
catch {
    Write-HomeFail $_.Exception.Message
    $exitCode = 1
}
exit $exitCode

#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Preflight for the DEV-540 plot embedding evaluation.

.DESCRIPTION
    Checks the live prerequisites for the three-variant measured run without
    sending any embedding workload: the configured local Ollama endpoint must
    serve both all-minilm and nomic-embed-text:v1.5, and the scratch
    evaluation store must be reachable. Writes a JSON report under
    artifacts/plot-embedding/ (git-ignored). Missing prerequisites are
    reported as Could not run lines; the script itself exits 0 when the
    report is written.
#>
[CmdletBinding()]
param(
    [string]$Endpoint = '',
    [string]$ReportPath = '',
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$baselineModel = 'all-minilm'
$alternateModel = 'nomic-embed-text:v1.5'
$defaultEndpoint = 'http://localhost:11434/'
$connectionVariable = 'ConnectionStrings__DefaultConnection'

function Write-Usage {
    Write-Output @"
Usage: pwsh -NoProfile -File scripts/run-plot-embedding-preflight.ps1 [OPTIONS]

Checks DEV-540 live prerequisites and writes a JSON report.

OPTIONS:
  -Endpoint <url>     Ollama endpoint. Default: `$env:Ollama__Endpoint or http://localhost:11434/
  -ReportPath <path>  Report path. Default: artifacts/plot-embedding/preflight.json
  -Help               Show this help.
"@
}

if ($Help) {
    Write-Usage
    exit 0
}

$callerLocation = (Get-Location).ProviderPath
$repoRoot = Split-Path -Parent $PSScriptRoot

$endpoint = if ([string]::IsNullOrWhiteSpace($Endpoint)) {
    if ([string]::IsNullOrWhiteSpace($env:Ollama__Endpoint)) { $defaultEndpoint } else { $env:Ollama__Endpoint }
} else {
    $Endpoint
}

$reportFullPath = if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    Join-Path $repoRoot 'artifacts/plot-embedding/preflight.json'
} else {
    [System.IO.Path]::GetFullPath($ReportPath, $callerLocation)
}

function Test-LocalEndpoint {
    param([string]$Target)

    $uri = $null
    if (-not [System.Uri]::TryCreate($Target, [System.UriKind]::Absolute, [ref]$uri)) {
        return $false
    }

    if ($uri.Scheme -ne 'http' -and $uri.Scheme -ne 'https') {
        return $false
    }

    $hostName = $uri.Host
    return $hostName -eq 'localhost' -or $hostName -eq '127.0.0.1' -or $hostName -eq '[::1]' -or $hostName -eq '::1'
}

function Test-ScratchConnectionString {
    param([string]$Connection)

    if ([string]::IsNullOrWhiteSpace($Connection)) {
        return $false
    }

    return $Connection.Contains('scratch', [System.StringComparison]::OrdinalIgnoreCase) `
        -or $Connection.Contains('evaluation', [System.StringComparison]::OrdinalIgnoreCase) `
        -or $Connection.Contains('memory', [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-ModelServed {
    param([string]$BaseEndpoint, [string]$Model)

    try {
        $tags = Invoke-RestMethod -Uri ($BaseEndpoint.TrimEnd('/') + '/api/tags') -TimeoutSec 10 -MaximumRedirection 0
    } catch {
        return $false
    }

    foreach ($entry in @($tags.models)) {
        $name = [string]$entry.name
        if ($name -eq $Model -or $name.StartsWith($Model + ':', [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Get-GitRevision {
    param([string]$Arguments, [string]$WorkingDirectory)

    try {
        $output = & git -C $WorkingDirectory $Arguments.Split(' ') 2>$null
        $text = [string]$output
        if ([string]::IsNullOrWhiteSpace($text)) {
            return 'unknown'
        }
        return $text.Trim()
    } catch {
        return 'unknown'
    }
}

$endpointIsLocal = Test-LocalEndpoint -Target $endpoint

$baselineServed = $false
$alternateServed = $false
if ($endpointIsLocal) {
    $baselineServed = Test-ModelServed -BaseEndpoint $endpoint -Model $baselineModel
    $alternateServed = Test-ModelServed -BaseEndpoint $endpoint -Model $alternateModel
}

$storeEntry = Get-Item -Path "Env:$connectionVariable" -ErrorAction SilentlyContinue
$storeConnection = if ($null -eq $storeEntry) { '' } else { [string]$storeEntry.Value }
$storeIsolated = Test-ScratchConnectionString -Connection $storeConnection
$storeReachable = -not [string]::IsNullOrWhiteSpace($storeConnection)

$missing = @()
if (-not $endpointIsLocal) {
    $missing += 'Configured Ollama endpoint is not local; refusing remote model probes.'
} else {
    if (-not $baselineServed) { $missing += "Ollama model '$baselineModel' is not served at the configured endpoint." }
    if (-not $alternateServed) { $missing += "Ollama model '$alternateModel' is not served at the configured endpoint." }
}
if (-not $storeIsolated) {
    $missing += 'Scratch evaluation store identity is unverified; refusing readiness without a scratch lane marker.'
} elseif (-not $storeReachable) {
    $missing += 'Scratch evaluation store is unreachable.'
}

$report = [ordered]@{
    endpoint = $endpoint
    endpointIsLocal = $endpointIsLocal
    baselineModel = $baselineModel
    baselineModelServed = $baselineServed
    alternateModel = $alternateModel
    alternateModelServed = $alternateServed
    scratchStoreIsolated = $storeIsolated
    scratchStoreReachable = $storeReachable
    canRun = $missing.Count -eq 0
    missingPrerequisites = $missing
    evaluatedRevision = Get-GitRevision -Arguments 'rev-parse HEAD' -WorkingDirectory $repoRoot
    baseRevision = Get-GitRevision -Arguments 'merge-base HEAD origin/main' -WorkingDirectory $repoRoot
}

$directory = Split-Path -Parent $reportFullPath
if (-not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}
$report | ConvertTo-Json -Depth 4 | Set-Content -Path $reportFullPath -Encoding utf8

if ($missing.Count -eq 0) {
    Write-Output "Preflight PASS: all DEV-540 live prerequisites are available. Report: $reportFullPath"
} else {
    foreach ($line in $missing) {
        Write-Output "Could not run: $line"
    }
    Write-Output "Report: $reportFullPath"
}

exit 0

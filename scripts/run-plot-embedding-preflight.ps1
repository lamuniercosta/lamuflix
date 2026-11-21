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

function Test-ModelServed {
    param([string]$BaseEndpoint, [string]$Model)

    try {
        $tags = Invoke-RestMethod -Uri ($BaseEndpoint.TrimEnd('/') + '/api/tags') -TimeoutSec 10
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

$baselineServed = Test-ModelServed -BaseEndpoint $endpoint -Model $baselineModel
$alternateServed = Test-ModelServed -BaseEndpoint $endpoint -Model $alternateModel
$storeEntry = Get-Item -Path "Env:$connectionVariable" -ErrorAction SilentlyContinue
$storeReachable = $null -ne $storeEntry -and -not [string]::IsNullOrWhiteSpace($storeEntry.Value)

$missing = @()
if (-not $baselineServed) { $missing += "Ollama model '$baselineModel' is not served at the configured endpoint." }
if (-not $alternateServed) { $missing += "Ollama model '$alternateModel' is not served at the configured endpoint." }
if (-not $storeReachable) { $missing += 'Scratch evaluation store is unreachable.' }

$report = [ordered]@{
    endpoint = $endpoint
    baselineModel = $baselineModel
    baselineModelServed = $baselineServed
    alternateModel = $alternateModel
    alternateModelServed = $alternateServed
    scratchStoreReachable = $storeReachable
    canRun = $missing.Count -eq 0
    missingPrerequisites = $missing
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

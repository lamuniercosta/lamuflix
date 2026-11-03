#!/usr/bin/env pwsh
# Runs the Compose browser smoke (npm run e2e in web/).
# Exits 0 on PASS, 1 on FAIL, 2 (SKIPPED) when Docker is not available.

[CmdletBinding()]
param([switch]$Help)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_gate-common.ps1')

if ($Help) {
    Write-Output @"
Usage: run-e2e-smoke.ps1 [-Help]

Runs npm run e2e in web/. Exit 0 = PASS, 1 = FAIL, 2 = SKIPPED
(Docker not available). SKIPPED is never PASS: the PR's e2e-smoke
check still has to pass.

  -Help    Show this help
"@
    exit 0
}

$repoRoot = Get-RepoRoot

$dockerOk = $false
try {
    docker info *> $null
    if ($LASTEXITCODE -eq 0) { $dockerOk = $true }
}
catch {
    # docker command missing
}

if (-not $dockerOk) {
    Write-Host "SKIPPED: Docker is not running. The PR's e2e-smoke check still has to pass."
    exit 2
}

# Not web/e2e/.runtime: run.mjs deletes that folder at start, and the open log blocks it on Windows.
$logDir = Join-Path $repoRoot 'artifacts/e2e-smoke'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$logFile = Join-Path $logDir 'run-e2e-smoke.log'
"$(Get-Date -Format o) run-e2e-smoke starting" | Tee-Object -FilePath $logFile -Append

Push-Location (Join-Path $repoRoot 'web')
try {
    npm run e2e 2>&1 | Tee-Object -FilePath $logFile -Append
    $npmExit = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($npmExit -eq 0) {
    Write-Host 'PASS:'
    exit 0
}

Write-Host "FAIL: npm run e2e exited $npmExit"
exit 1

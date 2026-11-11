#!/usr/bin/env pwsh
# Companion test for scripts/Test-DeployParity.ps1.
# Each directory under scripts/fixtures/deploy-parity is one case with rendered.yaml and
# expect.json. A case may carry its own compose.yml; otherwise the shared one is used.
# A case passes when the parity check's exit code equals expect.json exitCode and its
# output contains every mustContain line and none of the mustNotContain lines.
# Exit 0 when all cases pass, 1 otherwise.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$fixtureRoot = Join-Path $PSScriptRoot 'fixtures/deploy-parity'
$parity = Join-Path $PSScriptRoot 'Test-DeployParity.ps1'
$sharedCompose = Join-Path $fixtureRoot 'compose.yml'

$cases = @(Get-ChildItem -LiteralPath $fixtureRoot -Directory | Sort-Object Name)
if ($cases.Count -eq 0) {
    Write-Host "FAIL  no fixture cases under $fixtureRoot"
    exit 1
}

$failedCases = 0
foreach ($case in $cases) {
    $expectPath = Join-Path $case.FullName 'expect.json'
    $renderedPath = Join-Path $case.FullName 'rendered.yaml'
    $composePath = Join-Path $case.FullName 'compose.yml'
    if (-not (Test-Path -LiteralPath $composePath)) { $composePath = $sharedCompose }

    if (-not (Test-Path -LiteralPath $expectPath) -or -not (Test-Path -LiteralPath $renderedPath)) {
        Write-Host "FAIL  $($case.Name): missing expect.json or rendered.yaml"
        $failedCases++
        continue
    }
    $expect = Get-Content -LiteralPath $expectPath -Raw | ConvertFrom-Json

    $output = & pwsh -NoProfile -File $parity -ComposePath $composePath -RenderedPath $renderedPath 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | ForEach-Object { [string]$_ }) -join "`n"

    $problems = [System.Collections.Generic.List[string]]::new()
    if ($exitCode -ne [int]$expect.exitCode) { $problems.Add("exit $exitCode, expected $($expect.exitCode)") }
    foreach ($line in @($expect.mustContain)) {
        if (-not $text.Contains([string]$line)) { $problems.Add("output lacks '$line'") }
    }
    foreach ($line in @($expect.mustNotContain)) {
        if ($text.Contains([string]$line)) { $problems.Add("output contains forbidden '$line'") }
    }

    if ($problems.Count -eq 0) {
        Write-Host "PASS  $($case.Name) (exit $exitCode)"
    }
    else {
        $failedCases++
        Write-Host "FAIL  $($case.Name): $($problems -join '; ')" -ForegroundColor Red
        Write-Host $text
    }
}

Write-Host ''
Write-Host "$($cases.Count) cases, $failedCases failed"
if ($failedCases -gt 0) { exit 1 }
exit 0

#!/usr/bin/env pwsh
# Companion test for scripts/Resolve-ReleaseGate.ps1.
# Each directory under scripts/fixtures/release-gate is one case with
# runs.json (ci.yml workflow runs), jobs.json (run id -> stryker-core-gate
# conclusion), compares.json (head sha -> changed filenames vs the promoted
# sha), args.json (sha and tag passed to the script) and expect.json.
# A case passes when the gate script's exit code equals expect.json exitCode
# and its output contains every mustContain line and none of the
# mustNotContain lines. The run also fails when the script's docs-only paths
# differ from the ci.yml push paths-ignore list.
# Exit 0 when all cases pass, 1 otherwise.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$gate = Join-Path $PSScriptRoot 'Resolve-ReleaseGate.ps1'
$fixtureRoot = Join-Path $PSScriptRoot 'fixtures/release-gate'
$ciPath = Join-Path $repoRoot '.github/workflows/ci.yml'

$failures = [System.Collections.Generic.List[string]]::new()

$scriptText = Get-Content -LiteralPath $gate -Raw
$listMatch = [regex]::Match($scriptText, '\$DocsOnlyPaths\s*=\s*@\(([^)]*)\)')
$scriptPaths = @()
if ($listMatch.Success) {
    foreach ($m in [regex]::Matches($listMatch.Groups[1].Value, "'([^']+)'")) {
        $scriptPaths += $m.Groups[1].Value
    }
}

$ciText = Get-Content -LiteralPath $ciPath -Raw
$pushBlock = [regex]::Match($ciText, '(?s)push:\s*\r?\n(?:.*\r?\n)*?.*?paths-ignore:\s*\r?\n((?:\s*-\s*''[^'']+''\s*\r?\n?)+)')
$ciPaths = @()
if ($pushBlock.Success) {
    foreach ($m in [regex]::Matches($pushBlock.Groups[1].Value, "'([^']+)'")) {
        $ciPaths += $m.Groups[1].Value
    }
}

$missing = @($ciPaths | Where-Object { $scriptPaths -notcontains $_ })
$extra = @($scriptPaths | Where-Object { $ciPaths -notcontains $_ })
if ($scriptPaths.Count -eq 0) {
    $failures.Add('parity: no docs-only paths found in Resolve-ReleaseGate.ps1')
}
foreach ($p in $missing) { $failures.Add("parity: ci.yml push paths-ignore entry '$p' missing from the script") }
foreach ($p in $extra) { $failures.Add("parity: script entry '$p' missing from ci.yml push paths-ignore") }
if ($missing.Count -eq 0 -and $extra.Count -eq 0 -and $scriptPaths.Count -gt 0) {
    Write-Host "PASS  parity docs-only paths match ci.yml push paths-ignore ($($scriptPaths.Count) entries)"
}
else {
    Write-Host 'FAIL  parity docs-only paths differ from ci.yml push paths-ignore' -ForegroundColor Red
}

$cases = @(Get-ChildItem -LiteralPath $fixtureRoot -Directory | Sort-Object Name)
if ($cases.Count -eq 0) {
    Write-Host "FAIL  no fixture cases under $fixtureRoot"
    exit 1
}

$failedCases = 0
foreach ($case in $cases) {
    $expectPath = Join-Path $case.FullName 'expect.json'
    $runsPath = Join-Path $case.FullName 'runs.json'
    $jobsPath = Join-Path $case.FullName 'jobs.json'
    $comparesPath = Join-Path $case.FullName 'compares.json'
    $argsPath = Join-Path $case.FullName 'args.json'
    foreach ($p in @($expectPath, $runsPath, $jobsPath, $comparesPath, $argsPath)) {
        if (-not (Test-Path -LiteralPath $p)) {
            Write-Host "FAIL  $($case.Name): missing $($p | Split-Path -Leaf)"
            $failedCases++
            continue
        }
    }
    if ($failedCases -gt 0 -and -not (Test-Path -LiteralPath $expectPath)) { continue }
    $expect = Get-Content -LiteralPath $expectPath -Raw | ConvertFrom-Json
    $args = Get-Content -LiteralPath $argsPath -Raw | ConvertFrom-Json

    $output = & pwsh -NoProfile -File $gate -Sha $args.sha -Tag $args.tag -RunsFile $runsPath -JobsFile $jobsPath -ComparesFile $comparesPath 2>&1
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
if ($failedCases -gt 0 -or $failures.Count -gt 0) {
    foreach ($f in $failures) { Write-Host "FAIL  $f" -ForegroundColor Red }
    exit 1
}
exit 0

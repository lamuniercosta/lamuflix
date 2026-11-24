#!/usr/bin/env pwsh
# Release gate for home-cluster release tags (DEV-546).
#
# Decides whether the promoted commit may be released by reading the newest
# eligible ci.yml run on main whose head is the promoted SHA itself or a later
# main commit whose diff from it is confined to the docs-only paths below, and
# requiring that run's stryker-core-gate job to conclude success.
# Eligible trigger events are schedule (nightly) and workflow_dispatch (manual).
# A push-triggered run never satisfies the gate: docs-only pushes skip CI, so a
# skipped stryker-core-gate job cannot satisfy the gate.
#
# Live mode (used by promote-home.yml) queries the GitHub API through the gh
# CLI. Offline mode (used by fixtures) reads the same shapes from JSON files.
#
# Exit 0 when the gate passes. Every refusal exits nonzero and fails the
# workflow step before any env/home change.

[CmdletBinding()]
param(
    [string]$Sha,
    [string]$Repo,
    [string]$Tag = '',
    [string]$RunsFile = '',
    [string]$JobsFile = '',
    [string]$ComparesFile = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Docs-only paths. This list must stay in sync with the push paths-ignore list
# in .github/workflows/ci.yml (named here as the sync source).
$DocsOnlyPaths = @('specs/**', 'docs/**', '.specify/**', 'CONTEXT.md', '**/*.md')

function Test-IsDocsOnlyFile {
    param([string]$Name)
    $n = [string]$Name
    if ($n.StartsWith('specs/')) { return $true }
    if ($n.StartsWith('docs/')) { return $true }
    if ($n.StartsWith('.specify/')) { return $true }
    if ($n -eq 'CONTEXT.md' -or $n.EndsWith('/CONTEXT.md')) { return $true }
    if ($n.EndsWith('.md')) { return $true }
    return $false
}

function Get-DocsOnlyPaths {
    return @($DocsOnlyPaths)
}

function Select-EligibleReleaseRun {
    param($Runs, [string]$PromotedSha, $Compares)
    $eligible = [System.Collections.Generic.List[object]]::new()
    foreach ($run in @($Runs)) {
        $event = [string]$run.event
        if ($event -ne 'schedule' -and $event -ne 'workflow_dispatch') { continue }
        if ([string]$run.path -ne '.github/workflows/ci.yml') { continue }
        if ([string]$run.head_branch -ne 'main') { continue }
        $head = [string]$run.head_sha
        if ($head -eq $PromotedSha) {
            $eligible.Add($run)
            continue
        }
        $files = $null
        if ($null -ne $Compares) {
            if ($Compares -is [System.Collections.IDictionary] -and $Compares.Contains($head)) {
                $files = @($Compares[$head])
            }
        }
        if ($null -eq $files) { continue }
        $allDocsOnly = $true
        foreach ($f in $files) {
            if (-not (Test-IsDocsOnlyFile ([string]$f))) { $allDocsOnly = $false; break }
        }
        if ($allDocsOnly) { $eligible.Add($run) }
    }
    $sorted = @($eligible | Sort-Object { [string]$_.created_at })
    if ($sorted.Count -eq 0) { return $null }
    return $sorted[$sorted.Count - 1]
}

function Get-LiveRuns {
    param([string]$RepoName)
    $json = & gh api "repos/$RepoName/actions/workflows/ci.yml/runs?branch=main&per_page=100" --jq '[.workflow_runs[] | {id, event, path, head_branch, head_sha, created_at, status, conclusion}]' 2>&1
    if ($LASTEXITCODE -ne 0) { throw "gh api workflow runs failed: $json" }
    $parsed = $json | ConvertFrom-Json
    return @($parsed)
}

function Get-LiveCompareFiles {
    param([string]$RepoName, [string]$Base, [string]$Head)
    $json = & gh api "repos/$RepoName/compare/$Base...$Head" --jq '[.files[].filename]' 2>&1
    if ($LASTEXITCODE -ne 0) { throw "gh api compare failed for $Base...$Head`: $json" }
    $parsed = $json | ConvertFrom-Json
    return @($parsed)
}

function Get-LiveGateConclusion {
    param([string]$RepoName, $RunId)
    $json = & gh api "repos/$RepoName/actions/runs/$RunId/jobs" --paginate --jq '.jobs[] | select(.name == "stryker-core-gate") | (.conclusion // .status)' 2>&1
    if ($LASTEXITCODE -ne 0) { throw "gh api jobs failed for run $RunId`: $json" }
    if ($null -eq $json) { return '' }
    return ((@($json) | ForEach-Object { [string]$_ }) -join "`n").Trim()
}

try {
    if (-not $Sha -or -not $Repo) {
        if (-not $RunsFile -or -not $JobsFile -or -not $ComparesFile) {
            Write-Host '::error::Resolve-ReleaseGate needs -Sha and -Repo (live), or -RunsFile, -JobsFile and -ComparesFile (offline).'
            exit 1
        }
    }
    if ($Tag -ne '' -and $Tag -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+$') {
        Write-Host "::error::Release tags look like v1.2.3, got $Tag."
        exit 1
    }

    if ($RunsFile -ne '') {
        $runs = @(Get-Content -LiteralPath $RunsFile -Raw | ConvertFrom-Json)
        $jobsMap = Get-Content -LiteralPath $JobsFile -Raw | ConvertFrom-Json -AsHashtable
        $comparesMap = Get-Content -LiteralPath $ComparesFile -Raw | ConvertFrom-Json -AsHashtable
        $gateOf = { param($id) return ([string]$jobsMap[[string]$id]).Trim() }
        $filesOf = { param($head) if ($comparesMap.Contains([string]$head)) { return @($comparesMap[[string]$head]) } return $null }
        $comparesForSelect = @{}
        foreach ($k in $comparesMap.Keys) { $comparesForSelect[[string]$k] = @($comparesMap[$k]) }
        $selected = Select-EligibleReleaseRun $runs $Sha $comparesForSelect
        if ($null -eq $selected) {
            $sawPushOnly = [bool]@($runs | Where-Object { [string]$_.event -eq 'push' }).Count
            if ($runs.Count -eq 0) {
                Write-Host '::error::No qualifying CI run found. Wait for the nightly run or dispatch ci.yml when the commit to release is at the tip of main. For a hotfix that cannot wait for the gate, promote by sha with workflow_dispatch (no release recorded).'
                exit 1
            }
            if ($sawPushOnly) {
                Write-Host '::error::Only push-triggered CI history found for this commit. A skipped stryker-core-gate job cannot satisfy the gate: use the nightly schedule or a manual workflow_dispatch run instead.'
                exit 1
            }
            $heads = @($runs | ForEach-Object { [string]$_.head_sha } | Select-Object -Unique)
            $codeHeads = @()
            foreach ($h in $heads) {
                if ($h -eq $Sha) { continue }
                $files = & $filesOf $h
                if ($null -eq $files) { continue }
                foreach ($f in $files) {
                    if (-not (Test-IsDocsOnlyFile ([string]$f))) { $codeHeads += $h; break }
                }
            }
            if ($codeHeads.Count -gt 0) {
                Write-Host "::error::Newer main commits change code beyond the docs-only paths, so no later run can judge $Sha. Release $Sha itself once its nightly or manual gate passes."
                exit 1
            }
            Write-Host '::error::No qualifying CI run found. Wait for the nightly run or dispatch ci.yml when the commit to release is at the tip of main. For a hotfix that cannot wait for the gate, promote by sha with workflow_dispatch (no release recorded).'
            exit 1
        }
        $gate = & $gateOf $selected.id
        if ($gate -ne 'success') {
            $state = if ($gate) { $gate } else { 'missing' }
            Write-Host "::error::stryker-core-gate for run $($selected.id) (head $($selected.head_sha)) is '$state'. Wait for it to pass and re-run this workflow, or promote by sha with workflow_dispatch for a hotfix."
            exit 1
        }
        Write-Host "Release gate passed for $Sha (CI run $($selected.id), head $($selected.head_sha), event $($selected.event))."
        exit 0
    }

    $liveRuns = @(Get-LiveRuns $Repo)
    $compares = @{}
    $eligible = [System.Collections.Generic.List[object]]::new()
    foreach ($run in $liveRuns) {
        $event = [string]$run.event
        if ($event -ne 'schedule' -and $event -ne 'workflow_dispatch') { continue }
        if ([string]$run.path -ne '.github/workflows/ci.yml') { continue }
        if ([string]$run.head_branch -ne 'main') { continue }
        $head = [string]$run.head_sha
        if ($head -eq $Sha) { $eligible.Add($run); continue }
        $files = @(Get-LiveCompareFiles $Repo $Sha $head)
        $compares[$head] = $files
        $docsOnly = $true
        foreach ($f in $files) {
            if (-not (Test-IsDocsOnlyFile ([string]$f))) { $docsOnly = $false; break }
        }
        if ($docsOnly) { $eligible.Add($run) }
    }
    $sorted = @($eligible | Sort-Object { [string]$_.created_at })
    if ($sorted.Count -eq 0) {
        Write-Host '::error::No qualifying CI run found. Wait for the nightly run or dispatch ci.yml when the commit to release is at the tip of main. For a hotfix that cannot wait for the gate, promote by sha with workflow_dispatch (no release recorded).'
        exit 1
    }
    $newest = $sorted[$sorted.Count - 1]
    $conclusion = Get-LiveGateConclusion $Repo $newest.id
    if ($conclusion -ne 'success') {
        $state = if ($conclusion) { $conclusion } else { 'missing' }
        Write-Host "::error::stryker-core-gate for run $($newest.id) (head $($newest.head_sha)) is '$state'. Wait for it to pass and re-run this workflow, or promote by sha with workflow_dispatch for a hotfix."
        exit 1
    }
    Write-Host "Release gate passed for $Sha (CI run $($newest.id), head $($newest.head_sha), event $($newest.event))."
    exit 0
}
catch {
    Write-Host "::error::$($_.Exception.Message)"
    exit 1
}

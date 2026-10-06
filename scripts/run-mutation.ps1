#!/usr/bin/env pwsh
# Runs Stryker.NET mutation testing on changed production C# files under src/.
# Exits 0 on PASS, 1 on FAIL, 2 on SKIPPED (no production files changed) or NOT
# APPLICABLE (every changed project listed in gates.mutation.exclusions). -Help
# prints the exit-code table, and AGENTS.md carries the same table.
#
# Worktree-safe replacement for `dotnet stryker --since`:
# In Stryker 4.16.0 the built-in `since` filter resolved linked git worktrees to the
# main checkout and measured nothing; it has not been re-verified on 5.0.0, so it stays
# disabled. This script discovers changed production files via git merge-base, maps them
# to project-relative mutate globs, generates temporary configs in the system temp
# directory with since disabled, and executes Stryker sequentially per project.
#
# Test runner (DEV-382, docs/adr/0016-stryker-mtp-runner.md): the xUnit v3 test projects
# are executables, so under VSTest the tests run in a child of testhost that never sees
# the active mutant and every mutant survives. The tracked config therefore selects the
# MTP runner with coverage off, and every generated config carries both settings plus an
# explicit test-projects list that excludes *.ArchitectureTests and *.IntegrationTests:
# the first fails on the Stryker.* types injected into every mutated assembly, which is
# an artifact kill, and the second holds the real-database tests this run does not drive.
#
# Usage:
#   ./scripts/run-mutation.ps1
#   ./scripts/run-mutation.ps1 -BaseRef origin/main
#   ./scripts/run-mutation.ps1 -DryRun
#   ./scripts/run-mutation.ps1 -Project LamuFlix.Api -OutputRoot <dir outside the repo>
#   ./scripts/run-mutation.ps1 -EvaluateReport <path/to/mutation-report.json>

[CmdletBinding()]
param(
    [string]$BaseRef = '',
    [switch]$DryRun,
    [string]$EvaluateReport = '',
    [string[]]$Project = @(),
    [string]$OutputRoot = '',
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_gate-common.ps1')

# The exit-code contract of this gate. -Help prints this table and AGENTS.md carries a
# copy of it, so the three texts stay identical line for line after the leading
# whitespace on each line is trimmed.
<#
EXIT CODES:
| Exit | Verdict | Meaning | Blocking |
|---|---|---|---|
| 0 | `PASSED` | At least one eligible changed project was mutated, every eligible result is at or above `gates.mutation.threshold`, each configured exclusion is printed as NOT APPLICABLE with its reason, and no unlisted ineligible project exists. | — |
| 1 | `FAILED` | A score below threshold, a Stryker failure, invalid configuration, or any changed project with no eligible test project that is not in `gates.mutation.exclusions`. | Yes, on every tier |
| 2 | `SKIPPED` | No production C# under `src/` changed (scope-empty). | No; reported as `SKIPPED (scope-empty)`, never PASS |
| 2 | `NOT APPLICABLE` | Every changed project is explicitly listed in `gates.mutation.exclusions`; nothing was mutated. | No; reported as N/A, never PASS |

The table describes a real run. -DryRun starts no Stryker process, so its exit 0 means
classification passed and is never the `PASSED` row above.
#>

if ($Help) {
    Write-Output @'
Usage: run-mutation.ps1 [OPTIONS]

Runs Stryker.NET mutation testing on changed production C# files under src/.
The threshold is read from harness.yml at gates.mutation.threshold (default: 80).

OPTIONS:
  -BaseRef <ref>          Git ref to diff against (default: origin/<baseBranch> from harness.yml)
  -DryRun                 Classify changed projects and generate temp configs without running Stryker; a clean exit is a classification verdict, never mutation proof
  -EvaluateReport <path>  Evaluate an existing Stryker mutation report without running Stryker (skips changed-file containment checks when run standalone)
  -Project <name[]>       Only run these changed projects (e.g. LamuFlix.Api or LamuFlix.Api.csproj); a name with no changed files is an error
  -OutputRoot <dir>       Where Stryker output goes, one <Project> folder each (default: <system temp>/LamuFlix-stryker/<UTC yyyyMMdd-HHmmss>-<6 hex>; must be outside the repo)
  -Help                   Show this help

CONFIGURATION:
  harness.yml gates.mutation.exclusions maps a changed project name (no .csproj
  suffix, matched ordinal-ignore-case) to the reason printed with its NOT APPLICABLE
  classification. A blank reason and a .csproj suffix are configuration errors that
  exit 1 before any Stryker run. A reason must not contain " #": the harness.yml
  subset treats a # that starts a token as a comment, so the rest of the line would
  be dropped. That limitation is documented here and not validated.

EXAMPLES:
  ./scripts/run-mutation.ps1
  ./scripts/run-mutation.ps1 -BaseRef origin/main
  ./scripts/run-mutation.ps1 -DryRun
  ./scripts/run-mutation.ps1 -Project LamuFlix.Infrastructure,LamuFlix.Api
  ./scripts/run-mutation.ps1 -EvaluateReport <output root>/LamuFlix.Api/reports/mutation-report.json

EXIT CODES:
  A real run reports these outcomes; -DryRun starts no Stryker process, so its exit 0
  means classification passed and is never the PASSED row.

| Exit | Verdict | Meaning | Blocking |
|---|---|---|---|
| 0 | `PASSED` | At least one eligible changed project was mutated, every eligible result is at or above `gates.mutation.threshold`, each configured exclusion is printed as NOT APPLICABLE with its reason, and no unlisted ineligible project exists. | — |
| 1 | `FAILED` | A score below threshold, a Stryker failure, invalid configuration, or any changed project with no eligible test project that is not in `gates.mutation.exclusions`. | Yes, on every tier |
| 2 | `SKIPPED` | No production C# under `src/` changed (scope-empty). | No; reported as `SKIPPED (scope-empty)`, never PASS |
| 2 | `NOT APPLICABLE` | Every changed project is explicitly listed in `gates.mutation.exclusions`; nothing was mutated. | No; reported as N/A, never PASS |
'@
    exit 0
}

function Get-ReportTestIndex {
    # Maps each test id in a schema-2 report's testFiles to its name and file, so
    # killedBy ids can be attributed. Reports without testFiles yield an empty map.
    param($Report)

    $index = @{}
    if ($Report.PSObject.Properties.Match('testFiles').Count -eq 0 -or -not $Report.testFiles) {
        return $index
    }
    foreach ($tf in $Report.testFiles.PSObject.Properties) {
        if ($tf.Value.PSObject.Properties.Match('tests').Count -eq 0) { continue }
        foreach ($t in @($tf.Value.tests)) {
            $index[[string]$t.id] = [PSCustomObject]@{ Name = [string]$t.name; File = [string]$tf.Name }
        }
    }
    return $index
}

function Test-ArchitectureTestKiller {
    param($Test)

    return ($Test.File -match '[\\/]([^\\/]*\.)?ArchitectureTests[\\/]') -or
           ($Test.Name -match '(^|\.)ArchitectureTests\.')
}

function Get-EligibleTestProjects {
    # Test projects that directly reference the mutated project, minus ArchitectureTests and IntegrationTests.
    # Stryker's project mode needs a direct reference; listing them explicitly keeps
    # ArchitectureTests (artifact kills) and IntegrationTests (real-database tests) out of the run.
    param(
        [string[]]$TestProjects,
        [string]$MutatedProjectPath
    )

    $target = [System.IO.Path]::GetFullPath($MutatedProjectPath)
    $eligible = foreach ($tp in $TestProjects) {
        if ([System.IO.Path]::GetFileNameWithoutExtension($tp) -match '(^|\.)(ArchitectureTests|IntegrationTests)$') { continue }
        $tpDir = Split-Path $tp -Parent
        $xml = [xml](Get-Content -LiteralPath $tp -Raw)
        $refs = @($xml.SelectNodes('//*[local-name()="ProjectReference"]/@Include') | ForEach-Object { $_.Value })
        foreach ($ref in $refs) {
            $refFull = [System.IO.Path]::GetFullPath((Join-Path $tpDir ($ref -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar)))
            if ($refFull.Equals($target, [System.StringComparison]::OrdinalIgnoreCase)) {
                $tp
                break
            }
        }
    }
    return @($eligible | Sort-Object)
}

function Evaluate-MutationReport {
    param(
        [string]$ReportPath,
        [int]$NativeExitCode,
        [string]$ProjectName,
        [string[]]$ChangedFiles,
        [int]$Threshold,
        [string]$RepoRoot
    )

    $failureReasons = [System.Collections.Generic.List[string]]::new()
    $survivingMutants = [System.Collections.Generic.List[PSCustomObject]]::new()
    $mutatedFiles = [System.Collections.Generic.List[string]]::new()
    $outsideFiles = [System.Collections.Generic.List[string]]::new()
    $filesWithMutantsNoneTested = [System.Collections.Generic.List[string]]::new()
    $artifactKillers = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    $artifactKills = 0
    $unattributedKills = 0

    if ($NativeExitCode -ne 0) {
        $failureReasons.Add("Native exit code was non-zero ($NativeExitCode).")
    }

    if (-not $ReportPath -or -not (Test-Path -LiteralPath $ReportPath)) {
        $failureReasons.Add("Mutation report not found or was not generated.")
        return [PSCustomObject]@{
            ProjectName                = $ProjectName
            ReportPath                 = $ReportPath
            NativeExitCode             = $NativeExitCode
            Killed                     = 0
            Survived                   = 0
            Timeout                    = 0
            NoCoverage                 = 0
            Ignored                    = 0
            CompileError               = 0
            TotalMutants               = 0
            Tested                     = 0
            Score                      = $null
            SinceFilterIgnoredCount    = 0
            ArtifactKills              = 0
            UnattributedKills          = 0
            MutatedFiles               = @()
            OutsideMutatedFiles        = @()
            FilesWithMutantsNoneTested = @()
            SurvivingMutants           = @()
            FailureReasons             = @($failureReasons)
            Passed                     = $false
        }
    }

    $report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
    $testIndex = Get-ReportTestIndex -Report $report

    $killed = 0
    $survived = 0
    $timeout = 0
    $noCoverage = 0
    $ignored = 0
    $compileError = 0
    $sinceFilterIgnoredCount = 0

    if ($report.PSObject.Properties.Match('files').Count -gt 0 -and $report.files) {
        foreach ($fileProp in $report.files.PSObject.Properties) {
            $fileKey = $fileProp.Name
            $fileData = $fileProp.Value

            $fileRel = if ([System.IO.Path]::IsPathRooted($fileKey)) {
                [System.IO.Path]::GetRelativePath($RepoRoot, $fileKey).Replace('\', '/')
            }
            else {
                $fileKey.Replace('\', '/')
            }

            $mutants = @($fileData.mutants)
            $mutantCount = $mutants.Count

            if ($mutantCount -gt 0) {
                $mutatedFiles.Add($fileRel)

                $isChanged = $false
                foreach ($cf in $ChangedFiles) {
                    if ($cf.Equals($fileRel, [System.StringComparison]::OrdinalIgnoreCase)) {
                        $isChanged = $true
                        break
                    }
                }

                if (-not $isChanged -and $ChangedFiles.Count -gt 0) {
                    $outsideFiles.Add($fileRel)
                }

                $fileTested = 0
                foreach ($m in $mutants) {
                    $status = [string]$m.status
                    $hasReason = ($m.PSObject.Properties.Match('statusReason').Count -gt 0)
                    $reason = if ($hasReason) { [string]$m.statusReason } else { '' }

                    if ($reason -match 'Removed by since filter') {
                        $sinceFilterIgnoredCount++
                    }

                    switch ($status) {
                        'Killed' {
                            $killed++
                            $fileTested++
                            # Every kill must name a resolvable test, and none may come from
                            # ArchitectureTests (artifact kill on the injected Stryker.* types).
                            $killerIds = @(if ($m.PSObject.Properties.Match('killedBy').Count -gt 0) { $m.killedBy | Where-Object { $_ } })
                            $unresolved = ($killerIds.Count -eq 0)
                            $isArtifact = $false
                            foreach ($kid in $killerIds) {
                                $killer = $testIndex[[string]$kid]
                                if ($null -eq $killer) { $unresolved = $true; continue }
                                if (Test-ArchitectureTestKiller -Test $killer) {
                                    $isArtifact = $true
                                    [void]$artifactKillers.Add($killer.Name)
                                }
                            }
                            if ($unresolved) { $unattributedKills++ }
                            if ($isArtifact) { $artifactKills++ }
                        }
                        'Survived' {
                            $survived++
                            $fileTested++
                            $hasLocation = ($m.PSObject.Properties.Match('location').Count -gt 0)
                            $line = if ($hasLocation -and $m.location -and $m.location.PSObject.Properties.Match('start').Count -gt 0 -and $m.location.start) { [int]$m.location.start.line } else { 0 }
                            $hasMutator = ($m.PSObject.Properties.Match('mutatorName').Count -gt 0)
                            $mutator = if ($hasMutator -and $m.mutatorName) { [string]$m.mutatorName } else { 'Unknown' }
                            $survivingMutants.Add([PSCustomObject]@{
                                File    = $fileRel
                                Line    = $line
                                Mutator = $mutator
                            })
                        }
                        'Timeout' {
                            $timeout++
                            $fileTested++
                        }
                        'NoCoverage' {
                            $noCoverage++
                            $fileTested++
                        }
                        'Ignored' {
                            $ignored++
                        }
                        'CompileError' {
                            $compileError++
                        }
                    }
                }

                if ($isChanged -and $fileTested -eq 0) {
                    $filesWithMutantsNoneTested.Add("$fileRel ($mutantCount mutant(s), 0 tested)")
                }
            }
        }
    }

    $tested = $killed + $timeout + $survived + $noCoverage
    $totalMutants = $killed + $survived + $timeout + $noCoverage + $ignored + $compileError

    $score = if ($tested -gt 0) {
        [double]($killed + $timeout) / [double]$tested * 100.0
    }
    else {
        $null
    }

    if ($sinceFilterIgnoredCount -gt 0) {
        $failureReasons.Add("$sinceFilterIgnoredCount mutant(s) ignored with 'Removed by since filter' (since filter leaked back in).")
    }

    if ($outsideFiles.Count -gt 0) {
        $failureReasons.Add("Mutated file(s) outside changed files: $($outsideFiles -join ', ').")
    }

    if ($filesWithMutantsNoneTested.Count -gt 0) {
        $failureReasons.Add("Changed file(s) have mutants but none were tested: $($filesWithMutantsNoneTested -join ', ').")
    }

    if ($totalMutants -gt 0 -and $tested -eq 0) {
        $failureReasons.Add("Invocation has $totalMutants mutant(s) but 0 tested (no score).")
    }

    if ($tested -gt 0 -and $killed -eq 0) {
        $failureReasons.Add("Killed 0 of $tested tested mutant(s) ($timeout timeout) - this indicates a mutant-to-test linkage or measurement failure, not a test-quality verdict.")
    }

    if ($artifactKills -gt 0) {
        $shown = @($artifactKillers | Select-Object -First 5)
        $more = if ($artifactKillers.Count -gt $shown.Count) { ", +$($artifactKillers.Count - $shown.Count) more" } else { '' }
        $failureReasons.Add("Artifact kill: $artifactKills killed mutant(s) attributed to ArchitectureTests test(s) ($($shown -join ', ')$more); these fail on Stryker's injected types, not on the mutation.")
    }

    if ($unattributedKills -gt 0) {
        $failureReasons.Add("$unattributedKills killed mutant(s) have no killedBy test names resolvable through testFiles; artifact kills cannot be ruled out.")
    }

    if ($score -ne $null -and $score -lt $Threshold) {
        $scoreStr = "{0:N2}%" -f $score
        $failureReasons.Add("Score $scoreStr is below threshold $Threshold%.")
    }

    $passed = ($failureReasons.Count -eq 0)

    return [PSCustomObject]@{
        ProjectName                = $ProjectName
        ReportPath                 = $ReportPath
        NativeExitCode             = $NativeExitCode
        Killed                     = $killed
        Survived                   = $survived
        Timeout                    = $timeout
        NoCoverage                 = $noCoverage
        Ignored                    = $ignored
        CompileError               = $compileError
        TotalMutants               = $totalMutants
        Tested                     = $tested
        Score                      = $score
        SinceFilterIgnoredCount    = $sinceFilterIgnoredCount
        ArtifactKills              = $artifactKills
        UnattributedKills          = $unattributedKills
        MutatedFiles               = @($mutatedFiles)
        OutsideMutatedFiles        = @($outsideFiles)
        FilesWithMutantsNoneTested = @($filesWithMutantsNoneTested)
        SurvivingMutants           = @($survivingMutants)
        FailureReasons             = @($failureReasons)
        Passed                     = $passed
    }
}

$repoRoot = if ($env:HARNESS_REPO_ROOT) {
    (Resolve-Path -LiteralPath $env:HARNESS_REPO_ROOT).Path
}
else {
    $currentGitRoot = git rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($currentGitRoot)) {
        (Resolve-Path -LiteralPath $currentGitRoot.Trim()).Path
    }
    else {
        Get-RepoRoot
    }
}

# Precondition check: dotnet-tools manifest
$manifestPath = Join-Path $repoRoot '.config/dotnet-tools.json'
if (-not (Test-Path -LiteralPath $manifestPath)) {
    Write-Host 'GATE NOT WIRED: .config/dotnet-tools.json is missing.'
    exit 1
}

$manifestContent = Get-Content -LiteralPath $manifestPath -Raw
if ($manifestContent -notmatch '"dotnet-stryker"') {
    Write-Host 'GATE NOT WIRED: dotnet-stryker is not present in .config/dotnet-tools.json.'
    exit 1
}

# Read threshold from harness.yml
$threshold = Get-HarnessValue -Key 'gates.mutation.threshold' -RepoRoot $repoRoot
if ($threshold -eq $null) {
    $threshold = 80
}

# Exclusion policy, keyed the way -Project names projects and matched ordinal-ignore-case.
# Read and validated here, above the scope-empty check, so an entry with a blank reason or
# a .csproj name is a configuration error even when the diff carries no production C#.
try {
    $exclusionReasons = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @(Get-HarnessMap -Key 'gates.mutation.exclusions' -RepoRoot $repoRoot)) {
        $exclusionReasons[[string]$entry.Project] = [string]$entry.Reason
    }
}
catch {
    Write-Host "Mutation testing: FAILED - invalid mutation configuration in harness.yml gates.mutation.exclusions: $($_.Exception.Message)"
    exit 1
}

# Direct report evaluation mode (e.g. fail-safe verification / regression check)
if (-not [string]::IsNullOrWhiteSpace($EvaluateReport)) {
    $evalPath = if ([System.IO.Path]::IsPathRooted($EvaluateReport)) {
        $EvaluateReport
    }
    else {
        Join-Path (Get-Location).Path $EvaluateReport
    }

    if (-not (Test-Path -LiteralPath $evalPath)) {
        Write-Host "Mutation testing: FAILED - report file not found at '$evalPath'."
        exit 1
    }

    Write-Host "Evaluating Stryker mutation report: $evalPath"
    $eval = Evaluate-MutationReport `
        -ReportPath $evalPath `
        -NativeExitCode 0 `
        -ProjectName (Split-Path $evalPath -Leaf) `
        -ChangedFiles @() `
        -Threshold $threshold `
        -RepoRoot $repoRoot

    Write-Host "`nProject: $($eval.ProjectName)"
    Write-Host "  Report: $($eval.ReportPath)"
    Write-Host ("  Mutants: {0} | Killed: {1} | Survived: {2} | Timeout: {3} | NoCoverage: {4} | Ignored: {5} | CompileError: {6}" -f `
        $eval.TotalMutants, $eval.Killed, $eval.Survived, $eval.Timeout, $eval.NoCoverage, $eval.Ignored, $eval.CompileError)
    $scoreText = if ($eval.Score -ne $null) { ("{0:N2}%" -f $eval.Score) } else { 'N/A (0 tested)' }
    Write-Host ("  Tested:  {0} | Score: {1} (Threshold: {2}%, {3} timeout)" -f $eval.Tested, $scoreText, $threshold, $eval.Timeout)

    if ($eval.SurvivingMutants.Count -gt 0) {
        Write-Host "`nSurviving mutants ($($eval.SurvivingMutants.Count)):"
        foreach ($sm in $eval.SurvivingMutants) {
            Write-Host "  $($sm.File):$($sm.Line) [$($sm.Mutator)]"
        }
    }

    if ($eval.Passed) {
        Write-Host "`nMutation testing: PASSED (score >= $threshold% on tested mutants)."
        exit 0
    }
    else {
        Write-Host "`nMutation testing: FAILED"
        foreach ($reason in $eval.FailureReasons) {
            Write-Host "  - $reason"
        }
        exit 1
    }
}

# Resolve base ref and merge-base
$baseBranch = Get-HarnessValue -Key 'baseBranch' -RepoRoot $repoRoot
if ([string]::IsNullOrWhiteSpace($baseBranch)) {
    $baseBranch = 'main'
}

$targetRef = if (-not [string]::IsNullOrWhiteSpace($BaseRef)) {
    $BaseRef
}
else {
    "origin/$baseBranch"
}

$mergeBase = git -C $repoRoot merge-base HEAD $targetRef 2>$null

if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($mergeBase)) {
    Write-Host "Mutation testing: FAILED - could not resolve git merge-base between HEAD and '$targetRef'."
    if ([string]::IsNullOrWhiteSpace($BaseRef)) {
        Write-Host "  Fetch the remote branch with 'git fetch origin' or pass -BaseRef explicitly."
    }
    exit 1
}

$mergeBase = $mergeBase.Trim()

# Discover changed production C# files under src/
$changedFiles = @(
    git -C $repoRoot diff --name-only --diff-filter=AMR $mergeBase HEAD -- 'src/*.cs' 'src/**/*.cs' |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_.Trim().Replace('\', '/') } |
        Where-Object { $_ -notmatch 'Persistence[\\/]Migrations[\\/]' } |
        Select-Object -Unique
)

if ($changedFiles.Count -eq 0) {
    Write-Host 'Mutation testing: SKIPPED - no production C# files modified under src/.'
    exit 2
}

# Group changed files by nearest ancestor *.csproj
$projectGroups = [ordered]@{}
$projectDirs = @{}
$projectPaths = @{}

foreach ($file in $changedFiles) {
    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $file))
    $dir = Split-Path $fullPath -Parent
    $foundCsproj = $null

    while ($dir.Length -ge $repoRoot.Length) {
        $csProjs = @(Get-ChildItem -LiteralPath $dir -Filter '*.csproj' -File)
        if ($csProjs.Count -eq 1) {
            $foundCsproj = $csProjs[0]
            break
        }
        elseif ($csProjs.Count -gt 1) {
            throw "Multiple .csproj files found in $dir"
        }
        $parentDir = Split-Path $dir -Parent
        if ($parentDir -eq $dir) { break }
        $dir = $parentDir
    }

    if (-not $foundCsproj) {
        throw "Could not find ancestor .csproj for changed file '$file'."
    }

    $projName = $foundCsproj.Name
    $projDir = $foundCsproj.DirectoryName
    $projectDirs[$projName] = $projDir
    $projectPaths[$projName] = $foundCsproj.FullName

    $relToProj = [System.IO.Path]::GetRelativePath($projDir, $fullPath).Replace('\', '/')
    if (-not $projectGroups.Contains($projName)) {
        $projectGroups[$projName] = [System.Collections.Generic.List[string]]::new()
    }
    $projectGroups[$projName].Add($relToProj)
}

# Read tracked stryker-config.json
$strykerConfigPath = Join-Path $repoRoot 'stryker-config.json'
if (-not (Test-Path -LiteralPath $strykerConfigPath)) {
    Write-Host "GATE NOT WIRED: stryker-config.json is missing at '$strykerConfigPath'."
    exit 1
}

$baseConfigJson = Get-Content -LiteralPath $strykerConfigPath -Raw | ConvertFrom-Json
$baseStryker = $baseConfigJson.'stryker-config'

# Stryker must run on MTP with coverage off: under VSTest (or with coverage analysis on)
# no mutant reaches the xUnit v3 test executables and every mutant survives (DEV-382).
# Require the exact values, not just non-blank ones.
$requiredSettings = [ordered]@{ 'test-runner' = 'mtp'; 'coverage-analysis' = 'off' }
foreach ($requiredKey in $requiredSettings.Keys) {
    $expectedValue = $requiredSettings[$requiredKey]
    if ($baseStryker.PSObject.Properties.Match($requiredKey).Count -eq 0 -or [string]::IsNullOrWhiteSpace([string]$baseStryker.$requiredKey)) {
        Write-Host "GATE NOT WIRED: stryker-config.json does not set '$requiredKey' (expected test-runner=mtp, coverage-analysis=off; see docs/adr/0016-stryker-mtp-runner.md)."
        exit 1
    }
    $actualValue = ([string]$baseStryker.$requiredKey).Trim()
    if ($actualValue -ne $expectedValue) {
        Write-Host "GATE NOT WIRED: stryker-config.json sets '$requiredKey' to '$actualValue' (expected test-runner=mtp, coverage-analysis=off; see docs/adr/0016-stryker-mtp-runner.md)."
        exit 1
    }
}

$sortedProjects = @($projectGroups.Keys | Sort-Object)

if ($Project.Count -gt 0) {
    $requested = @($Project | ForEach-Object { $_ -split ',' } | Where-Object { $_.Trim() } |
        ForEach-Object { $n = $_.Trim(); if ($n -notmatch '\.csproj$') { "$n.csproj" } else { $n } })
    $unknown = @($requested | Where-Object { $sortedProjects -notcontains $_ })
    if ($unknown.Count -gt 0) {
        Write-Host "Mutation testing: FAILED - -Project names with no changed production files: $($unknown -join ', ')."
        Write-Host "  Changed projects: $($sortedProjects -join ', ')"
        exit 1
    }
    $sortedProjects = @($sortedProjects | Where-Object { $requested -contains $_ })
}

# Classify every changed project before any Stryker run, so a project that is unmeasured
# never stops a project that can be measured. Eligibility is computed for all of them so
# the lines below say which policy applies before the verdict names what it failed on.
$allTestProjects = @(Get-TestProjects -RepoRoot $repoRoot)
$testProjectsByProject = @{}
$eligibleProjects = [System.Collections.Generic.List[string]]::new()
$unlistedIneligibleProjects = [System.Collections.Generic.List[string]]::new()
$classifications = [ordered]@{}

foreach ($proj in $sortedProjects) {
    $eligible = @(Get-EligibleTestProjects -TestProjects $allTestProjects -MutatedProjectPath $projectPaths[$proj])
    $projShortName = $proj -replace '\.csproj$', ''
    $lines = [System.Collections.Generic.List[string]]::new()

    if ($exclusionReasons.ContainsKey($projShortName)) {
        $lines.Add("Classification for ${proj}: NOT APPLICABLE - listed in gates.mutation.exclusions: $($exclusionReasons[$projShortName])")
        if ($eligible.Count -gt 0) {
            $staleShown = ($eligible | ForEach-Object { [System.IO.Path]::GetRelativePath($repoRoot, $_).Replace('\', '/') }) -join ', '
            $lines.Add("Classification for ${proj}: WARNING - the exclusion may be stale: eligible test project(s) exist ($staleShown), and the project stays excluded.")
        }
    }
    elseif ($eligible.Count -gt 0) {
        $shown = ($eligible | ForEach-Object { [System.IO.Path]::GetRelativePath($repoRoot, $_).Replace('\', '/') }) -join ', '
        $lines.Add("Test projects for ${proj}: $shown")
        $testProjectsByProject[$proj] = $eligible
        $eligibleProjects.Add($proj)
    }
    else {
        $lines.Add("Classification for ${proj}: no eligible test project, not in policy")
        $unlistedIneligibleProjects.Add($proj)
    }

    $classifications[$proj] = $lines
}

# Every changed project is now either listed, measurable, or reported; the flag only says
# which of those three left nothing at all to measure. A configured exclusion that matches
# no changed project never reaches this point, because it is only ever looked up above.
$everyChangedProjectListed = $eligibleProjects.Count -eq 0 -and $unlistedIneligibleProjects.Count -eq 0

# A DryRun prints these classifications inside its own report below, so a real run, and the
# all-listed return that happens before that report exists, are what print them here.
if (-not $DryRun -or $everyChangedProjectListed) {
    foreach ($proj in $sortedProjects) {
        foreach ($line in $classifications[$proj]) {
            Write-Host $line
        }
    }
}

# Every changed project listed leaves nothing to measure, so no config is written, no
# output directory is created, and no Stryker is started.
if ($everyChangedProjectListed) {
    Write-Host "`nMutation testing: NOT APPLICABLE - every changed project is listed in gates.mutation.exclusions; nothing was mutated."
    exit 2
}

# Output root lives outside the repo so StrykerOutput never lands in the working tree.
$outputRootPath = if (-not [string]::IsNullOrWhiteSpace($OutputRoot)) {
    [System.IO.Path]::GetFullPath($OutputRoot)
}
else {
    # The GUID suffix keeps two runs started in the same second out of one folder.
    Join-Path ([System.IO.Path]::GetTempPath()) ("LamuFlix-stryker/" + [System.DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6))
}
$outputRootPath = [System.IO.Path]::GetFullPath($outputRootPath)
$repoRootWithSep = $repoRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
if ($outputRootPath.Equals($repoRoot.TrimEnd('\', '/'), [System.StringComparison]::OrdinalIgnoreCase) -or
    $outputRootPath.StartsWith($repoRootWithSep, [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Host "Mutation testing: FAILED - -OutputRoot '$outputRootPath' is inside the repository; use a directory outside '$repoRoot'."
    exit 1
}
Write-Host "Output root: $outputRootPath"

# Write one temp config per project into the system temp directory
$tempConfigs = [ordered]@{}
$tempFilesToClean = [System.Collections.Generic.List[string]]::new()

try {
    foreach ($proj in $eligibleProjects) {
        $projShortName = $proj -replace '\.csproj$', ''
        $tempConfigPath = Join-Path ([System.IO.Path]::GetTempPath()) "stryker-$projShortName-$([System.Guid]::NewGuid().ToString('N').Substring(0, 8)).json"
        $tempFilesToClean.Add($tempConfigPath)

        $high = if ($baseStryker.thresholds.high -ne $null) { [int]$baseStryker.thresholds.high } else { 90 }
        $low = if ($baseStryker.thresholds.low -ne $null) { [int]$baseStryker.thresholds.low } else { [int]$threshold }

        $strykerObj = [ordered]@{
            'stryker-config' = [ordered]@{
                'mutation-level'    = $baseStryker.'mutation-level'
                'test-runner'       = $baseStryker.'test-runner'
                'coverage-analysis' = $baseStryker.'coverage-analysis'
                'test-projects'     = @($testProjectsByProject[$proj])
                'since'             = [ordered]@{ 'enabled' = $false }
                'mutate'         = @($projectGroups[$proj])
                'thresholds'     = [ordered]@{
                    'high'  = $high
                    'low'   = $low
                    'break' = [int]$threshold
                }
                'reporters'      = @($baseStryker.reporters)
            }
        }

        $configJson = $strykerObj | ConvertTo-Json -Depth 6
        [System.IO.File]::WriteAllText($tempConfigPath, $configJson, [System.Text.UTF8Encoding]::new($false))

        $hash = (Get-FileHash -LiteralPath $tempConfigPath -Algorithm SHA256).Hash
        Write-Host "Stryker config for ${proj}: $tempConfigPath"
        Write-Host "  SHA256: $hash"

        $tempConfigs[$proj] = @{
            Path = $tempConfigPath
            Hash = $hash
            Json = $configJson
        }
    }

    if ($DryRun) {
        Write-Host "`nMutation testing: DRY RUN - project groups:"
        foreach ($proj in $sortedProjects) {
            foreach ($line in $classifications[$proj]) {
                Write-Host "`n  $line"
            }
            if (-not $testProjectsByProject.ContainsKey($proj)) { continue }

            $filesList = @($projectGroups[$proj])
            Write-Host "`n  Project: $proj ($($filesList.Count) file(s))"
            foreach ($f in $filesList) {
                Write-Host "    - $f"
            }
            Write-Host "`n  Generated config ($($tempConfigs[$proj].Path)):"
            Write-Host $tempConfigs[$proj].Json
        }

        # A classification run measures no mutant, so exit 0 below is a classification
        # verdict and never mutation proof. An unlisted project with no eligible test
        # project is the one classification failure, and it fails here exactly as the
        # same project fails a real run, after the eligible ones have been reported.
        if ($unlistedIneligibleProjects.Count -gt 0) {
            Write-Host "`nMutation testing: FAILED - changed project(s) with no eligible test project (a test project that directly references it, excluding *.ArchitectureTests and *.IntegrationTests) that are not in gates.mutation.exclusions: $($unlistedIneligibleProjects -join ', ')."
            exit 1
        }

        Write-Host "`nMutation testing: Dry run complete (0 tests executed)."
        exit 0
    }

    # Run Stryker sequentially per eligible project, from the project directory, into <output root>/<project>
    $results = [System.Collections.Generic.List[PSCustomObject]]::new()

    foreach ($proj in $eligibleProjects) {
        Write-Host "`n================================================================================"
        Write-Host "Running Stryker for $proj..."
        Write-Host "================================================================================"

        $cfgPath = $tempConfigs[$proj].Path
        $projOut = Join-Path $outputRootPath ($proj -replace '\.csproj$', '')
        New-Item -ItemType Directory -Path $projOut -Force | Out-Null
        Copy-Item -LiteralPath $cfgPath -Destination (Join-Path $projOut 'stryker-config.json') -Force

        Push-Location $projectDirs[$proj]
        try {
            & dotnet stryker -f $cfgPath -p $proj -O $projOut
            $nativeExit = $LASTEXITCODE
        }
        finally {
            Pop-Location
        }
        Write-Host "Stryker native exit for ${proj}: $nativeExit"

        $candidateReport = Join-Path $projOut 'reports/mutation-report.json'
        $reportPath = if (Test-Path -LiteralPath $candidateReport) { (Resolve-Path -LiteralPath $candidateReport).Path } else { $null }

        $projEval = Evaluate-MutationReport `
            -ReportPath $reportPath `
            -NativeExitCode $nativeExit `
            -ProjectName $proj `
            -ChangedFiles $changedFiles `
            -Threshold $threshold `
            -RepoRoot $repoRoot

        $results.Add($projEval)
    }

    # Summary and evaluation
    Write-Host "`n================================================================================"
    Write-Host 'Mutation Testing Summary'
    Write-Host '================================================================================'

    $allSurvivingMutants = [System.Collections.Generic.List[PSCustomObject]]::new()
    $runFailureReasons = [System.Collections.Generic.List[string]]::new()
    $totalTestedOverall = 0

    foreach ($r in $results) {
        $totalTestedOverall += $r.Tested
        foreach ($sm in $r.SurvivingMutants) {
            $allSurvivingMutants.Add($sm)
        }

        Write-Host "`nProject: $($r.ProjectName)"
        Write-Host "  Report: $($r.ReportPath)"
        Write-Host ("  Mutants: {0} | Killed: {1} | Survived: {2} | Timeout: {3} | NoCoverage: {4} | Ignored: {5} | CompileError: {6}" -f `
            $r.TotalMutants, $r.Killed, $r.Survived, $r.Timeout, $r.NoCoverage, $r.Ignored, $r.CompileError)
        $scoreText = if ($r.Score -ne $null) { ("{0:N2}%" -f $r.Score) } else { 'N/A (0 tested)' }
        $statusText = if ($r.Passed) { 'PASS' } else { 'FAIL' }
        Write-Host ("  Tested:  {0} | Score: {1} (Threshold: {2}%, {3} timeout) -> {4}" -f $r.Tested, $scoreText, $threshold, $r.Timeout, $statusText)

        if (-not $r.Passed) {
            foreach ($fr in $r.FailureReasons) {
                $runFailureReasons.Add("$($r.ProjectName): $fr")
            }
        }
    }

    if ($allSurvivingMutants.Count -gt 0) {
        Write-Host "`nSurviving mutants ($($allSurvivingMutants.Count)):"
        foreach ($sm in $allSurvivingMutants) {
            Write-Host "  $($sm.File):$($sm.Line) [$($sm.Mutator)]"
        }
    }

    if ($unlistedIneligibleProjects.Count -gt 0) {
        $runFailureReasons.Add("Changed project(s) with no eligible test project (a test project that directly references it, excluding *.ArchitectureTests and *.IntegrationTests) that are not in gates.mutation.exclusions: $($unlistedIneligibleProjects -join ', ').")
    }

    if ($totalTestedOverall -eq 0) {
        $runFailureReasons.Add('Total number of tested mutants across all invocations is 0.')
    }

    Write-Host "`nOutput root: $outputRootPath"
    Write-Host "`n--------------------------------------------------------------------------------"
    if ($runFailureReasons.Count -eq 0) {
        Write-Host "Mutation testing: PASSED (all projects scored at or above threshold $threshold%, total tested: $totalTestedOverall)."
        exit 0
    }
    else {
        Write-Host 'Mutation testing: FAILED'
        foreach ($fr in $runFailureReasons) {
            Write-Host "  - $fr"
        }
        exit 1
    }
}
finally {
    # Temp configs may be deleted at the end, but only after the report has been located
    foreach ($tmp in $tempFilesToClean) {
        if (Test-Path -LiteralPath $tmp) {
            Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
        }
    }
}

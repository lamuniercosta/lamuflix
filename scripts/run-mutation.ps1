#!/usr/bin/env pwsh
# Runs Stryker.NET mutation testing on changed production C# files under src/.
# Exits 0 on PASS, 1 on FAIL, 2 on SKIPPED (no production files changed).
#
# Worktree-safe replacement for `dotnet stryker --since`:
# In Stryker 4.16.0, the built-in `since` filter resolves linked git worktrees
# to the main checkout and measures nothing. This script discovers changed production
# files via git merge-base, maps them to project-relative mutate globs, generates
# temporary configs in $env:TEMP with since disabled, and executes Stryker sequentially
# per project.
#
# Usage:
#   ./scripts/run-mutation.ps1
#   ./scripts/run-mutation.ps1 -BaseRef origin/main
#   ./scripts/run-mutation.ps1 -DryRun
#   ./scripts/run-mutation.ps1 -EvaluateReport <path/to/mutation-report.json>

[CmdletBinding()]
param(
    [string]$BaseRef = '',
    [switch]$DryRun,
    [string]$EvaluateReport = '',
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_gate-common.ps1')

if ($Help) {
    Write-Output @"
Usage: run-mutation.ps1 [OPTIONS]

Runs Stryker.NET mutation testing on changed production C# files under src/.
The threshold is read from harness.yml at gates.mutation.threshold (default: 80).

OPTIONS:
  -BaseRef <ref>          Git ref to diff against (default: origin/<baseBranch> from harness.yml)
  -DryRun                 Discover changed files and generate temp configs without running Stryker
  -EvaluateReport <path>  Evaluate an existing Stryker mutation report without running Stryker (skips changed-file containment checks when run standalone)
  -Help                   Show this help

EXAMPLES:
  ./scripts/run-mutation.ps1
  ./scripts/run-mutation.ps1 -BaseRef origin/main
  ./scripts/run-mutation.ps1 -DryRun
  ./scripts/run-mutation.ps1 -EvaluateReport ./StrykerOutput/2026-09-28.03-18-47/reports/mutation-report.json
"@
    exit 0
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
            MutatedFiles               = @()
            OutsideMutatedFiles        = @()
            FilesWithMutantsNoneTested = @()
            SurvivingMutants           = @()
            FailureReasons             = @($failureReasons)
            Passed                     = $false
        }
    }

    $report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json

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
        $failureReasons.Add("Killed 0 of $tested tested mutant(s) ($timeout timeout).")
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
        Select-Object -Unique
)

if ($changedFiles.Count -eq 0) {
    Write-Host 'Mutation testing: SKIPPED - no production C# files modified under src/.'
    exit 2
}

# Group changed files by nearest ancestor *.csproj
$projectGroups = [ordered]@{}
$projectDirs = @{}

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

# Write one temp config per project into $env:TEMP
$tempConfigs = [ordered]@{}
$tempFilesToClean = [System.Collections.Generic.List[string]]::new()

try {
    $sortedProjects = @($projectGroups.Keys | Sort-Object)

    foreach ($proj in $sortedProjects) {
        $projShortName = $proj -replace '\.csproj$', ''
        $tempConfigPath = Join-Path $env:TEMP "stryker-$projShortName-$([System.Guid]::NewGuid().ToString('N').Substring(0, 8)).json"
        $tempFilesToClean.Add($tempConfigPath)

        $high = if ($baseStryker.thresholds.high -ne $null) { [int]$baseStryker.thresholds.high } else { 90 }
        $low = if ($baseStryker.thresholds.low -ne $null) { [int]$baseStryker.thresholds.low } else { [int]$threshold }

        $strykerObj = [ordered]@{
            'stryker-config' = [ordered]@{
                'mutation-level' = $baseStryker.'mutation-level'
                'since'          = [ordered]@{ 'enabled' = $false }
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
            $filesList = @($projectGroups[$proj])
            Write-Host "`n  Project: $proj ($($filesList.Count) file(s))"
            foreach ($f in $filesList) {
                Write-Host "    - $f"
            }
            Write-Host "`n  Generated config ($($tempConfigs[$proj].Path)):"
            Write-Host $tempConfigs[$proj].Json
        }
        Write-Host "`nMutation testing: Dry run complete (0 tests executed)."
        exit 0
    }

    # Run Stryker sequentially per project
    $results = [System.Collections.Generic.List[PSCustomObject]]::new()
    $strykerOutputDir = Join-Path $repoRoot 'StrykerOutput'

    Push-Location $repoRoot
    try {
        foreach ($proj in $sortedProjects) {
            Write-Host "`n================================================================================"
            Write-Host "Running Stryker for $proj..."
            Write-Host "================================================================================"

            $cfgPath = $tempConfigs[$proj].Path
            $runStartTime = [System.DateTime]::UtcNow

            # Record directories in StrykerOutput before run
            $preDirs = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
            if (Test-Path -LiteralPath $strykerOutputDir) {
                Get-ChildItem -Path $strykerOutputDir -Directory -ErrorAction SilentlyContinue |
                    ForEach-Object { [void]$preDirs.Add($_.FullName) }
            }

            & dotnet stryker -f $cfgPath -p $proj
            $nativeExit = $LASTEXITCODE

            # Locate the newest report produced by this run
            $reportPath = $null
            if (Test-Path -LiteralPath $strykerOutputDir) {
                $candidateDirs = @(
                    Get-ChildItem -Path $strykerOutputDir -Directory -ErrorAction SilentlyContinue |
                        Where-Object { -not $preDirs.Contains($_.FullName) } |
                        Sort-Object LastWriteTimeUtc -Descending
                )

                if ($candidateDirs.Count -gt 0) {
                    $candidateReport = Join-Path $candidateDirs[0].FullName 'reports\mutation-report.json'
                    if (Test-Path -LiteralPath $candidateReport) {
                        $reportPath = (Resolve-Path -LiteralPath $candidateReport).Path
                    }
                }

                if (-not $reportPath) {
                    $fallbackReports = @(
                        Get-ChildItem -Path $strykerOutputDir -Filter 'mutation-report.json' -Recurse -File -ErrorAction SilentlyContinue |
                            Where-Object { $_.LastWriteTimeUtc -ge $runStartTime.AddSeconds(-5) } |
                            Sort-Object LastWriteTimeUtc -Descending
                    )
                    if ($fallbackReports.Count -gt 0) {
                        $reportPath = $fallbackReports[0].FullName
                    }
                }
            }

            $projEval = Evaluate-MutationReport `
                -ReportPath $reportPath `
                -NativeExitCode $nativeExit `
                -ProjectName $proj `
                -ChangedFiles $changedFiles `
                -Threshold $threshold `
                -RepoRoot $repoRoot

            $results.Add($projEval)
        }
    }
    finally {
        Pop-Location
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

    if ($totalTestedOverall -eq 0) {
        $runFailureReasons.Add('Total number of tested mutants across all invocations is 0.')
    }

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

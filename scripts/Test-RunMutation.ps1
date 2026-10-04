#!/usr/bin/env pwsh
# Behaviour fixture for run-mutation.ps1 (DEV-397).
#
# run-mutation.ps1 sorts changed projects into eligible, configured-excluded,
# and unlisted-ineligible, and those three share exit codes: two of them can
# return 1 or 2 depending on the diff around them. The exit code alone therefore
# cannot tell a blank reason, a GATE NOT WIRED precondition, and an unlisted
# ineligible project apart, so every case here asserts verdict or classification
# text as well as the exit code.
#
# Each case builds a throwaway git worktree, overlays the gate files under test
# with the tool manifest and stryker-config.json, commits a diff touching
# exactly the projects that case is about, and drives run-mutation.ps1 through
# HARNESS_REPO_ROOT. The worktree carries the solution and the test projects, so
# eligibility is decided by the real project references rather than by a mock.
# Without that overlay every case exits 1 GATE NOT WIRED for the wrong reason,
# so the wiring is asserted before any case runs.
#
# Exits 0 when every case holds, 1 when any case fails.
#
# Usage:
#   pwsh -NoProfile -File ./scripts/Test-RunMutation.ps1

[CmdletBinding()]
param(
    [switch]$KeepWorktree
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# What a case needs to reach a classification at all. The two project files are
# not overlaid: the worktree already carries the solution and every csproj.
$overlayFiles = @(
    '.config/dotnet-tools.json',
    'stryker-config.json',
    'scripts/run-mutation.ps1',
    'scripts/_gate-common.ps1',
    'scripts/_harness-config.ps1'
)

$gateTimeoutMs = 300000

# One committed marker per selected project: the runner groups changed files by
# nearest ancestor csproj, so one added file puts exactly one project in the
# diff without disturbing any existing source.
$apiMarker = 'src/LamuFlix.Api/MutationFixtureMarker.cs'
$infraMarker = 'src/LamuFlix.Infrastructure/MutationFixtureMarker.cs'
$scriptsMarker = 'scripts/MutationFixtureMarker.txt'

$apiReason = 'host proof lives in IntegrationTests per Constitution IX'
$infraReason = 'listed only to prove a stale exclusion still warns'

$checks = 0
$failures = 0
$failed = $false

function Invoke-FixtureGit {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $output = & git -C $script:checkout @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit $LASTEXITCODE"
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Reset-Fixture {
    Invoke-FixtureGit @('reset', '--hard', $script:fixtureBase) | Out-Null
    Invoke-FixtureGit @('clean', '-fdq') | Out-Null
}

function Set-FixtureHarnessConfig {
    <#
      Writes the harness.yml the case is about. $Exclusions of $null leaves the
      key out entirely, which must read as an empty map, not as a missing gate.
    #>
    param([AllowNull()][hashtable]$Exclusions)

    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in @('harnessVersion: 0.7.1', 'pack: dotnet', 'baseBranch: main', 'tracker: youtrack',
            'solution: null', '', 'gates:', '  mutation:', '    threshold: 80')) {
        $lines.Add($line)
    }
    if ($null -ne $Exclusions) {
        $lines.Add('    exclusions:')
        foreach ($name in @($Exclusions.Keys | Sort-Object)) {
            $lines.Add("      ${name}: `"$($Exclusions[$name])`"")
        }
    }

    $path = Join-Path $script:checkout 'harness.yml'
    [System.IO.File]::WriteAllText($path, ($lines -join "`n") + "`n", [System.Text.UTF8Encoding]::new($false))
}

function Add-ChangedPaths {
    param([Parameter(Mandatory)][string[]]$Paths)

    foreach ($rel in $Paths) {
        $full = Join-Path $script:checkout $rel
        New-Item -ItemType Directory -Path (Split-Path $full -Parent) -Force | Out-Null
        # Comment-only, so a marker can never break a build that a later case
        # decides to compile.
        Set-Content -LiteralPath $full -Value "// Mutation fixture marker: selects $rel for the diff under test."
    }
    Invoke-FixtureGit (@('add', '--') + $Paths) | Out-Null
    Invoke-FixtureGit @('-c', 'user.name=mutation fixture', '-c', 'user.email=fixture@lamuflix.invalid',
        '-c', 'commit.gpgsign=false', 'commit', '--quiet', '-m', "fixture diff: $($Paths -join ', ')") | Out-Null
}

function Invoke-MutationGate {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = (Get-Command pwsh).Source
    foreach ($arg in (@('-NoProfile', '-File', (Join-Path $script:checkout 'scripts/run-mutation.ps1')) + $Arguments)) {
        [void]$psi.ArgumentList.Add($arg)
    }
    $psi.WorkingDirectory = $script:checkout
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.Environment['HARNESS_REPO_ROOT'] = $script:checkout

    $proc = [System.Diagnostics.Process]::Start($psi)
    # Both streams are drained concurrently: a full pipe buffer on either one
    # would block the child before it could exit.
    $stdout = $proc.StandardOutput.ReadToEndAsync()
    $stderr = $proc.StandardError.ReadToEndAsync()
    if (-not $proc.WaitForExit($script:gateTimeoutMs)) {
        $proc.Kill($true)
        throw "run-mutation.ps1 $($Arguments -join ' ') exceeded $script:gateTimeoutMs ms and was killed"
    }

    return [PSCustomObject]@{
        Exit   = $proc.ExitCode
        Output = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
    }
}

function Assert-MutationCase {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Setup,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][int]$ExpectedExit,
        [string[]]$MustContain = @(),
        [string[]]$MustNotContain = @()
    )

    $script:checks++
    Reset-Fixture
    & $Setup

    $run = Invoke-MutationGate -Arguments $Arguments
    $problems = [System.Collections.Generic.List[string]]::new()

    if ($run.Exit -ne $ExpectedExit) {
        $problems.Add("exit $($run.Exit), expected $ExpectedExit")
    }
    foreach ($text in $MustContain) {
        if ($run.Output.IndexOf($text, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            $problems.Add("output is missing '$text'")
        }
    }
    foreach ($text in $MustNotContain) {
        if ($run.Output.IndexOf($text, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $problems.Add("output must not contain '$text'")
        }
    }

    if ($problems.Count -eq 0) {
        Write-Host "  ok       $Name (exit $($run.Exit))"
        return
    }

    $script:failures++
    Write-Host "  FAIL     $Name" -ForegroundColor Red
    foreach ($problem in $problems) {
        Write-Host "           - $problem" -ForegroundColor Red
    }
    foreach ($line in @($run.Output -split "`r?`n" | Where-Object { $_.Trim() })) {
        Write-Host "           | $line" -ForegroundColor Red
    }
}

function Assert-FixtureWiring {
    $manifest = Join-Path $script:checkout '.config/dotnet-tools.json'
    if (-not (Test-Path -LiteralPath $manifest)) { throw "overlay missing: $manifest" }
    if ((Get-Content -LiteralPath $manifest -Raw) -notmatch '"dotnet-stryker"') {
        throw 'overlay .config/dotnet-tools.json does not name dotnet-stryker'
    }

    $strykerConfig = Join-Path $script:checkout 'stryker-config.json'
    if (-not (Test-Path -LiteralPath $strykerConfig)) { throw "overlay missing: $strykerConfig" }
    $baseStryker = (Get-Content -LiteralPath $strykerConfig -Raw | ConvertFrom-Json).'stryker-config'
    foreach ($required in @(@('test-runner', 'mtp'), @('coverage-analysis', 'off'))) {
        if ([string]$baseStryker.($required[0]) -ne $required[1]) {
            throw "stryker-config.json does not set $($required[0])=$($required[1])"
        }
    }

    if (-not (Test-Path -LiteralPath (Join-Path $script:checkout 'LamuFlix.sln'))) {
        throw 'the fixture checkout carries no LamuFlix.sln, so no test project can be resolved'
    }

    # Every classification in every case follows from these two references, so a
    # silent change to them must fail here rather than read as a gate bug.
    $unitTests = Join-Path $script:checkout 'tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj'
    if (-not (Test-Path -LiteralPath $unitTests)) { throw "the fixture checkout carries no $unitTests" }
    $unitTestsText = Get-Content -LiteralPath $unitTests -Raw
    if ($unitTestsText -notmatch 'LamuFlix\.Infrastructure\.csproj') {
        throw 'LamuFlix.UnitTests no longer references LamuFlix.Infrastructure, so no case has an eligible project'
    }
    if ($unitTestsText -match 'LamuFlix\.Api\.csproj') {
        throw 'LamuFlix.UnitTests now references LamuFlix.Api, so Api would be eligible and every Api case would be wrong'
    }
}

$repoRoot = [System.IO.Path]::GetFullPath((& git rev-parse --show-toplevel).Trim())
$headSha = ((& git -C $repoRoot rev-parse HEAD).Trim())

$script:work = Join-Path ([System.IO.Path]::GetTempPath()) `
    ('LamuFlix-mutation-fixture/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6))
$script:checkout = Join-Path $script:work 'checkout'
$worktreeAdded = $false

try {
    New-Item -ItemType Directory -Path $script:work -Force | Out-Null

    & git -C $repoRoot worktree add --detach $script:checkout $headSha | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "git worktree add failed with exit $LASTEXITCODE" }
    $worktreeAdded = $true

    foreach ($file in $overlayFiles) {
        $source = Join-Path $repoRoot $file
        if (-not (Test-Path -LiteralPath $source)) { throw "overlay source missing: $source" }
        Copy-Item -LiteralPath $source -Destination (Join-Path $script:checkout $file) -Force
    }
    Invoke-FixtureGit (@('add', '--') + $overlayFiles) | Out-Null
    # The overlay is empty whenever the gate files under test are already committed, which
    # is the case whenever this runs from a clean checkout, so the commit must tolerate it:
    # the diff base has to exist either way.
    Invoke-FixtureGit @('-c', 'user.name=mutation fixture', '-c', 'user.email=fixture@lamuflix.invalid',
        '-c', 'commit.gpgsign=false', 'commit', '--quiet', '--allow-empty', '-m', 'fixture tooling overlay') | Out-Null
    $script:fixtureBase = (@(Invoke-FixtureGit @('rev-parse', 'HEAD'))[0]).Trim()

    Assert-FixtureWiring

    Write-Host 'Fixture checkout: ' $script:checkout
    Write-Host "Diff base: $script:fixtureBase (overlay of the gate files under test)"
    Write-Host ''
    Write-Host 'Classification cases:'

    Assert-MutationCase -Name '1 listed Api plus eligible Infrastructure classifies and exits 0' -Setup {
        Set-FixtureHarnessConfig -Exclusions @{ 'LamuFlix.Api' = $apiReason }
        Add-ChangedPaths -Paths @($apiMarker, $infraMarker)
    } -Arguments @('-DryRun', '-BaseRef', $script:fixtureBase) -ExpectedExit 0 -MustContain @(
        'NOT APPLICABLE',
        $apiReason,
        'LamuFlix.Infrastructure.csproj',
        'LamuFlix.UnitTests/LamuFlix.UnitTests.csproj'
    )

    $listedApiOnly = {
        Set-FixtureHarnessConfig -Exclusions @{ 'LamuFlix.Api' = $apiReason }
        Add-ChangedPaths -Paths @($apiMarker)
    }
    Assert-MutationCase -Name '2 listed Api only, -DryRun, exits 2 NOT APPLICABLE' -Setup $listedApiOnly `
        -Arguments @('-DryRun', '-BaseRef', $script:fixtureBase) -ExpectedExit 2 `
        -MustContain @('NOT APPLICABLE', $apiReason) -MustNotContain @('LamuFlix.UnitTests')

    # Nothing here is eligible, so the real run has no Stryker invocation to make
    # and is the check on the real-run NOT APPLICABLE branch.
    Assert-MutationCase -Name '2 listed Api only, real run, exits 2 NOT APPLICABLE' -Setup $listedApiOnly `
        -Arguments @('-BaseRef', $script:fixtureBase) -ExpectedExit 2 `
        -MustContain @('NOT APPLICABLE', $apiReason) -MustNotContain @('Stryker native exit')

    Assert-MutationCase -Name '3 unlisted Api plus eligible Infrastructure exits 1 with both classifications' -Setup {
        Set-FixtureHarnessConfig -Exclusions $null
        Add-ChangedPaths -Paths @($apiMarker, $infraMarker)
    } -Arguments @('-DryRun', '-BaseRef', $script:fixtureBase) -ExpectedExit 1 -MustContain @(
        'LamuFlix.Api.csproj',
        'no eligible test project, not in policy',
        'LamuFlix.Infrastructure.csproj',
        'LamuFlix.UnitTests/LamuFlix.UnitTests.csproj'
    )

    # The diff touches no production C#, so an invalid policy has to be caught
    # before the scope-empty check instead of hiding behind SKIPPED.
    Assert-MutationCase -Name '4 blank reason exits 1 as a configuration error, ahead of scope-empty' -Setup {
        Set-FixtureHarnessConfig -Exclusions @{ 'LamuFlix.Api' = '   ' }
        Add-ChangedPaths -Paths @($scriptsMarker)
    } -Arguments @('-DryRun', '-BaseRef', $script:fixtureBase) -ExpectedExit 1 `
        -MustContain @('configuration') -MustNotContain @('SKIPPED')

    Assert-MutationCase -Name '5 listed project that has eligible tests warns and stays excluded' -Setup {
        Set-FixtureHarnessConfig -Exclusions @{ 'LamuFlix.Infrastructure' = $infraReason }
        Add-ChangedPaths -Paths @($infraMarker)
    } -Arguments @('-DryRun', '-BaseRef', $script:fixtureBase) -ExpectedExit 2 `
        -MustContain @('WARNING', 'NOT APPLICABLE', $infraReason)
}
catch {
    Write-Host "Mutation fixture: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    $failed = $true
}
finally {
    if ($worktreeAdded -and -not $KeepWorktree) {
        & git -C $repoRoot worktree remove --force $script:checkout *> $null
    }
    if ($KeepWorktree) {
        Write-Host "Fixture checkout kept: $script:checkout"
    }
    else {
        Remove-Item -Recurse -Force $script:work -ErrorAction SilentlyContinue
    }
}

Write-Host ''
if ($failed -or $failures -gt 0) {
    Write-Host "$failures of $checks case invocations FAILED." -ForegroundColor Red
    exit 1
}
Write-Host "All $checks case invocations passed." -ForegroundColor Green
exit 0
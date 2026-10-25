#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates an evaluation fixture, replays it through the real DEV-416 final
    ranking and publishes the deterministic metrics summary for DEV-417.

.DESCRIPTION
    The single intentional publication path (DEV-356 Q3/Q7, FR-002). It builds the
    LamuFlix.Evaluation executable, selects the dedicated evaluation entry with an
    exact runner filter, transports the fixture path, output path, evaluation seed
    and an explicit evaluation-mode marker through child-scoped environment
    variables, then restores the caller environment. The child performs fixture
    validation, replay and the complete summary write.

    Ordinary `dotnet test` execution of the evaluation project never publishes:
    publication requires the mode marker. Zero selected entry tests fail the
    command, as do a build failure, a native child nonzero exit, a failed summary
    write and a script error. Exit 0 is returned only after the summary exists at
    the requested path.
#>
[CmdletBinding()]
param(
    [string]$FixturePath = '',
    [string]$OutputPath = '',
    [int]$Seed = 356,
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$defaultFixtureRelative = 'tests/LamuFlix.Evaluation/Fixtures/baseline.json'
$defaultOutputRelative = 'artifacts/evaluation/metrics-summary.json'
$defaultSeed = 356
$evaluationProjectRelative = 'tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj'
$buildOutputRelative = 'artifacts/evaluation/bin'
$entryClass = 'LamuFlix.Evaluation.Entry.EvaluationEntryTests'
$modeVariable = 'LAMUFLIX_EVALUATION_MODE'
$modeValue = 'publish'
$fixtureVariable = 'LAMUFLIX_EVALUATION_FIXTURE_PATH'
$outputVariable = 'LAMUFLIX_EVALUATION_OUTPUT_PATH'
$seedVariable = 'LAMUFLIX_EVALUATION_SEED'

function Write-Usage {
    Write-Output @"
Usage: pwsh -NoProfile -File scripts/run-evaluation.ps1 [OPTIONS]

Validates the fixture, replays it through the real DEV-416 final ranking and
publishes the deterministic metrics summary.

OPTIONS:
  -FixturePath <path>   Evaluation fixture JSON. Default, resolved from the
                        repository root: $defaultFixtureRelative
  -OutputPath <path>    Published summary path. Default, resolved from the
                        repository root: $defaultOutputRelative
  -Seed <int>           Evaluation seed. Default: $defaultSeed
  -Help                 Show this help.

Relative paths are resolved against the caller's current location. Exit code 0
means validation, replay and the complete summary write all succeeded.
"@
}

if ($Help) {
    Write-Usage
    exit 0
}

$callerLocation = (Get-Location).ProviderPath
$repoRoot = Split-Path -Parent $PSScriptRoot

if ($PSBoundParameters.ContainsKey('FixturePath') -and [string]::IsNullOrWhiteSpace($FixturePath)) {
    throw 'FixturePath must not be blank.'
}

if ($PSBoundParameters.ContainsKey('OutputPath') -and [string]::IsNullOrWhiteSpace($OutputPath)) {
    throw 'OutputPath must not be blank.'
}

if ($Seed -lt 0) {
    throw "Seed must be a non-negative integer; got $Seed."
}

function Resolve-CommandPath {
    param(
        [string]$Value,
        [string]$DefaultRelative
    )

    $candidate = if ([string]::IsNullOrWhiteSpace($Value)) {
        Join-Path $repoRoot $DefaultRelative
    }
    else {
        $Value
    }

    return [System.IO.Path]::GetFullPath($candidate, $callerLocation)
}

$fixtureFullPath = Resolve-CommandPath -Value $FixturePath -DefaultRelative $defaultFixtureRelative
$outputFullPath = Resolve-CommandPath -Value $OutputPath -DefaultRelative $defaultOutputRelative

if (-not (Test-Path -LiteralPath $fixtureFullPath -PathType Leaf)) {
    throw "Evaluation fixture not found: $fixtureFullPath"
}

$projectFullPath = Join-Path $repoRoot $evaluationProjectRelative
$buildOutputFullPath = Join-Path $repoRoot $buildOutputRelative

Write-Output 'Building the evaluation executable...'
& dotnet build $projectFullPath --nologo --verbosity minimal --configuration Debug --output $buildOutputFullPath
if ($LASTEXITCODE -ne 0) {
    throw "The evaluation executable build failed with exit code $LASTEXITCODE."
}

$evaluationExecutable = Join-Path $buildOutputFullPath 'LamuFlix.Evaluation.dll'
if (-not (Test-Path -LiteralPath $evaluationExecutable -PathType Leaf)) {
    throw "The built evaluation executable was not found: $evaluationExecutable"
}

$selected = & dotnet $evaluationExecutable -class $entryClass -list tests -noLogo
if ($LASTEXITCODE -ne 0) {
    throw "Selecting the evaluation entry tests failed with exit code $LASTEXITCODE."
}

$selectedTests = @($selected | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
if ($selectedTests.Count -ne 1) {
    throw "Expected exactly one selected evaluation entry test; found $($selectedTests.Count)."
}

if (-not ([string]$selectedTests[0]).StartsWith($entryClass + '.', [System.StringComparison]::Ordinal)) {
    throw "The selected test '$($selectedTests[0])' is not the dedicated evaluation entry."
}

$childEnvironment = [ordered]@{
    $modeVariable = $modeValue
    $fixtureVariable = $fixtureFullPath
    $outputVariable = $outputFullPath
    $seedVariable = [string]$Seed
}

$previousEnvironment = @{}
foreach ($name in $childEnvironment.Keys) {
    $previousEnvironment[$name] = [System.Environment]::GetEnvironmentVariable($name, 'Process')
}

$childExitCode = 1
try {
    foreach ($name in $childEnvironment.Keys) {
        [System.Environment]::SetEnvironmentVariable($name, $childEnvironment[$name], 'Process')
    }

    & dotnet $evaluationExecutable -class $entryClass -noLogo -noColor
    $childExitCode = $LASTEXITCODE
}
finally {
    foreach ($name in $previousEnvironment.Keys) {
        [System.Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
    }
}

if ($childExitCode -ne 0) {
    throw "The evaluation child exited with code $childExitCode; no summary was published."
}

if (-not (Test-Path -LiteralPath $outputFullPath -PathType Leaf)) {
    throw "The evaluation child reported success but published no summary at $outputFullPath."
}

$published = Get-Item -LiteralPath $outputFullPath
if ($published.Length -le 0) {
    throw "The published summary is empty: $outputFullPath"
}

Write-Output "Metrics summary published: $outputFullPath"
exit 0

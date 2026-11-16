#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [switch]$Help,
    [switch]$RegressionProof,
    [switch]$FailurePathProof,
    [string]$CandidateRef = 'HEAD'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

if ($Help) {
    Write-Output @"
Usage: run-compose-smoke.ps1 [-Help] [-RegressionProof] [-FailurePathProof] [-CandidateRef <git-ref>]

Required Compose smoke for DEV-471. It runs two separately isolated positive
phases against disposable candidate source copies OUTSIDE the PR checkout.

Phase 1 (plain startup) generates an ephemeral docker/api.settings.json inside
its copy only (Features:LocalPlay false, no committed secrets), then starts the
shipped docker-compose.yml with no e2e Ollama endpoint override. It requires
healthy Postgres and RabbitMQ, real Ollama model readiness, and API health.

Phase 2 (stubbed full flow) starts the shipped base plus the existing
web/e2e/compose.e2e.yml overlay under a second randomized project, running
web/e2e/run.mjs with E2E_SMOKE_MODE=dev471. That entrypoint selects only
web/e2e/dev471-compose-smoke.e2e.ts and asserts, for one fixture-correlated
movie: the /hubs/ connection through shipped nginx, import through the real
stack, library listing with deterministic stub enrichment, a decodable
thumbnail, a genuine server-originated hub update, advancing in-page browser
playback with real media metadata, and an active process-play POST through
shipped nginx that must answer 403. Only external providers are stubbed.

  -RegressionProof   DEV-440 controlled-revert mode. Runs the phase-1 candidate
                     assertion plus an identical assertion on a second
                     disposable candidate copy whose only change is the reverse
                     of production commit 4646476f. That copy must never reach
                     API health for the attributable localhost/embedding
                     startup cause. A patch mismatch fails closed. The PR
                     checkout is never mutated and the historical parent is
                     never used as the candidate.
  -FailurePathProof  Runs the DEV-471 failure-path proofs instead of the smoke.
                     Startup timeout, unavailable Docker, assertion failure and
                     teardown failure each must answer exit 1 with diagnostics
                     and a teardown attempt; planted secrets must never reach
                     stdout or evidence; cancellation must leave the always-run
                     CI cleanup a registered project to reclaim without touching
                     unrelated projects; and the required smoke must not inherit
                     the local wrapper's SKIPPED exit 2. These proofs drive a
                     deterministic orchestration double in place of the docker
                     CLI, so they never start a container and they do NOT
                     replace the real positive Compose phases or the
                     controlled-revert proof.
  -CandidateRef      Commit-ish to stage. Resolved with git rev-parse.
                     Default: HEAD.
  -Help              Show this help

Exit codes (strict smoke semantics - there is no SKIPPED result):
  0  PASS  every required assertion held and every project teardown succeeded
  1  FAIL  missing Docker, build or settings failure, unhealthy dependency,
           model readiness failure, timeout, assertion failure, patch
           mismatch, job-cap exhaustion, or teardown failure

Budgets come from web/e2e/fixtures/dev471/budgets.json. Sanitized evidence is
written under artifacts/compose-smoke/<run>/ and nowhere else. This script
uploads nothing.
"@
    exit 0
}

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Docker = if ($env:DOCKER) { $env:DOCKER } else { 'docker' }
$BudgetPath = Join-Path $RepoRoot 'web/e2e/fixtures/dev471/budgets.json'
$Dev440Commit = '4646476fb1f8afb8539a07c8f404c911a5fc174e'
$Dev440Path = 'docker-compose.yml'
$Dev440ExpectedAdded = @('    environment:', '      Ollama__Endpoint: http://ollama:11434/')
$Dev440BindingValue = 'http://ollama:11434/'
$LocalhostEndpointPattern = 'http://localhost:11434'
$RunStarted = Get-Date
$RunStamp = $RunStarted.ToString('yyyyMMddTHHmmssZ')
$EvidenceBase = 'artifacts/compose-smoke'
if ($env:DEV471_FAILURE_PATH_CHILD -eq '1') {
    $RunStamp = '{0}-{1}' -f $RunStamp, ([System.Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(3)).ToLowerInvariant())
    $EvidenceBase = 'artifacts/compose-smoke-failure-paths'
}

$script:Secrets = [System.Collections.Generic.List[string]]::new()
$script:Log = [System.Collections.Generic.List[string]]::new()

function Write-Step {
    param([string]$Message)
    $line = '[compose-smoke] {0}' -f $Message
    $script:Log.Add($line)
    [Console]::Out.WriteLine($line)
}

function Write-StepWarning {
    param([string]$Message)
    $line = '[compose-smoke] WARN {0}' -f $Message
    $script:Log.Add($line)
    [Console]::Out.WriteLine($line)
}

function Register-Secret {
    param([string]$Value)
    if ($Value) { $script:Secrets.Add($Value) }
    return $Value
}

function Join-Output {
    param($StandardOut, $StandardError)
    return (@($StandardOut, $StandardError) -join "`n").Trim()
}

function Protect-Text {
    param($Text)
    $result = (@($Text) -join "`n")
    foreach ($secret in $script:Secrets) {
        if ($secret -and $secret.Length -ge 4) { $result = $result.Replace($secret, '[redacted]') }
    }
    $result = [regex]::Replace(
        $result,
        '(?im)(?<=^|[^a-z0-9])(password|passwd|pwd|apikey|api_key|secret|token)(\s*[=:]\s*)("[^"]*"|''[^'']*''|[^\s;"'']+)',
        '$1$2[redacted]')
    $result = [regex]::Replace($result, '(?i)\b(username)\b(\s*[=:]\s*)[^;"''\s]+', '$1$2[redacted]')
    $result = [regex]::Replace($result, '(?i)(Host=[^;"''\s]+;Database=[^;"''\s]+;Username=)[^;"''\s]+', '$1[redacted]')
    return $result
}

function Invoke-Native {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @(),
        [int]$TimeoutSeconds = 0,
        [string]$WorkingDirectory = $RepoRoot
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false
    foreach ($argument in $Arguments) { $startInfo.ArgumentList.Add($argument) }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $timedOut = $false

    if ($TimeoutSeconds -gt 0) {
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            $timedOut = $true
            try { $process.Kill($true) } catch { }
            $process.WaitForExit()
        }
    }
    else {
        $process.WaitForExit()
    }

    return [pscustomobject]@{
        ExitCode = if ($timedOut) { 124 } else { $process.ExitCode }
        TimedOut = $timedOut
        StdOut   = $stdoutTask.GetAwaiter().GetResult()
        StdErr   = $stderrTask.GetAwaiter().GetResult()
    }
}

function Invoke-Checked {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @(),
        [int]$TimeoutSeconds = 0,
        [string]$What = 'command'
    )

    $result = Invoke-Native -FilePath $FilePath -Arguments $Arguments -TimeoutSeconds $TimeoutSeconds
    if ($result.TimedOut) {
        throw ('{0} timed out after {1}s' -f $What, $TimeoutSeconds)
    }
    if ($result.ExitCode -ne 0) {
        throw ('{0} exited {1}: {2}' -f $What, $result.ExitCode, (Protect-Text (Join-Output $result.StdOut $result.StdErr)))
    }
    return $result
}

function Read-FailureBudgetOverride {
    $rawJson = $env:DEV471_FAILURE_PATH_BUDGETS
    if (-not $rawJson) { return $null }
    if ($env:DEV471_FAILURE_PATH_CHILD -ne '1') {
        throw 'DEV471_FAILURE_PATH_BUDGETS is only honoured inside a -FailurePathProof child run'
    }

    $raw = $rawJson | ConvertFrom-Json
    $bounds = [ordered]@{
        stackStartupSeconds       = @(3, 60)
        asyncAssertionSeconds     = @(3, 60)
        teardownPerProjectSeconds = @(3, 60)
        jobCapSeconds             = @(20, 600)
    }

    foreach ($key in $bounds.Keys) {
        $value = $raw.$key
        if ($null -eq $value -or -not ($value -is [int]) -and -not ($value -is [long])) {
            throw ("failure-path budget '{0}' must be an integer" -f $key)
        }
        if ($value -lt $bounds[$key][0] -or $value -gt $bounds[$key][1]) {
            throw ("failure-path budget '{0}' must be within {1}..{2}, found {3}" -f $key, $bounds[$key][0], $bounds[$key][1], $value)
        }
    }
    if ($null -eq $raw.wholeTestRetries -or $raw.wholeTestRetries -ne 0) {
        throw 'failure-path budget wholeTestRetries must be 0'
    }

    return [pscustomobject]@{
        StackStartupSeconds       = [int]$raw.stackStartupSeconds
        AsyncAssertionSeconds     = [int]$raw.asyncAssertionSeconds
        TeardownPerProjectSeconds = [int]$raw.teardownPerProjectSeconds
        JobCapSeconds             = [int]$raw.jobCapSeconds
    }
}

function Read-Budgets {
    $override = Read-FailureBudgetOverride
    if ($override) {
        Write-Step ('failure-path proof child: bounded simulation budgets startup {0}s, assertion {1}s, teardown {2}s, job cap {3}s (validated config untouched)' -f $override.StackStartupSeconds, $override.AsyncAssertionSeconds, $override.TeardownPerProjectSeconds, $override.JobCapSeconds)
        return $override
    }

    if (-not (Test-Path -LiteralPath $BudgetPath)) {
        throw ('budget config not found: {0}' -f $BudgetPath)
    }

    $raw = Get-Content -LiteralPath $BudgetPath -Raw | ConvertFrom-Json
    $bounds = [ordered]@{
        stackStartupSeconds       = @(60, 3600)
        asyncAssertionSeconds     = @(10, 600)
        teardownPerProjectSeconds = @(30, 900)
        jobCapSeconds             = @(600, 14400)
        setupReserveSeconds       = @(60, 3600)
    }

    foreach ($key in $bounds.Keys) {
        $value = $raw.$key
        if ($null -eq $value -or -not ($value -is [int]) -and -not ($value -is [long])) {
            throw ("budget config '{0}' must be an integer" -f $key)
        }
        $minimum = $bounds[$key][0]
        $maximum = $bounds[$key][1]
        if ($value -lt $minimum -or $value -gt $maximum) {
            throw ("budget config '{0}' must be within {1}..{2}, found {3}" -f $key, $minimum, $maximum, $value)
        }
    }

    if ($null -eq $raw.wholeTestRetries -or $raw.wholeTestRetries -ne 0) {
        throw 'budget config wholeTestRetries must be 0'
    }

    return [pscustomobject]@{
        StackStartupSeconds       = [int]$raw.stackStartupSeconds
        AsyncAssertionSeconds     = [int]$raw.asyncAssertionSeconds
        TeardownPerProjectSeconds = [int]$raw.teardownPerProjectSeconds
        JobCapSeconds             = [int]$raw.jobCapSeconds
        SetupReserveSeconds       = [int]$raw.setupReserveSeconds
    }
}

function New-RandomToken {
    param([int]$Bytes = 18)
    return [System.Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes($Bytes)).ToLowerInvariant()
}

function Get-TextSha256 {
    param($Text)
    if ($null -eq $Text) { return $null }
    $bytes = [System.Text.Encoding]::UTF8.GetBytes((@($Text) -join "`n"))
    return [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Get-StepTimeout {
    param([int]$Requested, [object]$Budgets)

    $remaining = [int][Math]::Floor(($script:Deadline - (Get-Date)).TotalSeconds) - $Budgets.TeardownPerProjectSeconds
    if ($remaining -lt 1) {
        throw ('job cap exhausted; {0}s usable time remains after reserving {1}s for teardown' -f $remaining, $Budgets.TeardownPerProjectSeconds)
    }
    return [int][Math]::Min($Requested, $remaining)
}

function New-StagingCopy {
    param([string]$Commit, [string]$Destination)

    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    $archive = Join-Path $Destination 'source.tar'
    Invoke-Checked -FilePath 'git' -Arguments @('-C', $RepoRoot, 'archive', '--format=tar', $Commit, '-o', $archive) -What 'git archive (disposable source)' | Out-Null
    Invoke-Checked -FilePath 'tar' -Arguments @('-xf', $archive, '-C', $Destination) -What 'tar extract (disposable source)' | Out-Null
    Remove-Item -LiteralPath $archive -Force
    return $Destination
}

function New-SmokeContext {
    param([string]$SourceRoot, [string]$PhaseName)

    $postgresUser = Register-Secret ('lamuflix_{0}' -f (New-RandomToken 4))
    $postgresCredential = Register-Secret (New-RandomToken 18)
    $rabbitUser = Register-Secret ('lamuflix_{0}' -f (New-RandomToken 4))
    $rabbitCredential = Register-Secret (New-RandomToken 18)
    $omdbApiKey = Register-Secret (New-RandomToken 12)

    $settings = [ordered]@{
        Features          = [ordered]@{ LocalPlay = $false }
        Database          = [ordered]@{ ApplyMigrationsOnStartup = $true }
        ConnectionStrings = [ordered]@{
            DefaultConnection = ('Host=postgres;Database=lamuflix;Username={0};Password={1}' -f $postgresUser, $postgresCredential)
        }
        RabbitMq          = [ordered]@{
            HostName   = 'rabbitmq'
            UserName   = $rabbitUser
            'Password' = $rabbitCredential
            RetryDelay = '00:00:30'
        }
        Omdb              = [ordered]@{ ApiKey = $omdbApiKey }
        Enrichment        = [ordered]@{ SweepInterval = '00:01:00'; ClaimLease = '00:00:10' }
        Library           = [ordered]@{ RootPath = '/media/library' }
    }

    $settingsPath = Join-Path $SourceRoot 'docker/api.settings.json'
    ($settings | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $settingsPath -Encoding utf8NoBOM

    $envPath = Join-Path $SourceRoot 'compose-smoke.env'
    @(
        ('POSTGRES_USER={0}' -f $postgresUser)
        ('POSTGRES_PASSWORD={0}' -f $postgresCredential)
        ('RABBITMQ_USER={0}' -f $rabbitUser)
        ('RABBITMQ_PASSWORD={0}' -f $rabbitCredential)
    ) | Set-Content -LiteralPath $envPath -Encoding utf8NoBOM

    Write-Step ('[{0}] generated ephemeral docker/api.settings.json (Features:LocalPlay false) inside the disposable copy' -f $PhaseName)

    return [pscustomobject]@{
        Phase     = $PhaseName
        Root      = $SourceRoot
        Settings  = $settingsPath
        EnvFile   = $envPath
        Compose   = (Join-Path $SourceRoot 'docker-compose.yml')
        LocalPlay = $false
    }
}

function Get-ComposeArguments {
    param([object]$Context, [string]$Project, [string[]]$Command)
    return @(
        'compose', '-p', $Project,
        '-f', $Context.Compose,
        '--env-file', $Context.EnvFile,
        '--project-directory', $Context.Root
    ) + $Command
}

function Register-Project {
    param([string]$Project, [string]$RegistryPath)
    Add-Content -LiteralPath $RegistryPath -Value $Project -Encoding utf8NoBOM
    Write-Step ('registered compose project {0} before startup' -f $Project)
}

function Get-ServiceState {
    param([object]$Context, [string]$Project, [string]$Service, [int]$TimeoutSeconds)

    $located = Invoke-Native -FilePath $Docker -Arguments (Get-ComposeArguments $Context $Project @('ps', '-a', '-q', $Service)) -TimeoutSeconds $TimeoutSeconds
    if ($located.ExitCode -ne 0) {
        return [pscustomobject]@{ Status = 'unknown'; Health = 'none'; ExitCode = $null; Container = $null }
    }
    $container = ($located.StdOut -split "`n" | Where-Object { $_.Trim() } | Select-Object -First 1)
    if (-not $container) {
        return [pscustomobject]@{ Status = 'missing'; Health = 'none'; ExitCode = $null; Container = $null }
    }

    $inspect = Invoke-Native -FilePath $Docker -Arguments @(
        'inspect', '--format', '{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}|{{.State.ExitCode}}', $container.Trim()
    ) -TimeoutSeconds $TimeoutSeconds
    if ($inspect.ExitCode -ne 0) {
        return [pscustomobject]@{ Status = 'unknown'; Health = 'none'; ExitCode = $null; Container = $container.Trim() }
    }

    $parts = ($inspect.StdOut.Trim() -split '\|')
    return [pscustomobject]@{
        Status    = $parts[0]
        Health    = if ($parts.Count -gt 1) { $parts[1] } else { 'none' }
        ExitCode  = if ($parts.Count -gt 2) { $parts[2] } else { $null }
        Container = $container.Trim()
    }
}

function Wait-ServiceHealthy {
    param([object]$Context, [string]$Project, [string]$Service, [int]$TimeoutSeconds)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $state = Get-ServiceState -Context $Context -Project $Project -Service $Service -TimeoutSeconds 30
        if ($state.Status -eq 'running' -and $state.Health -eq 'healthy') { return $true }
        if ($state.Status -eq 'exited' -or $state.Status -eq 'dead') { return $false }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Wait-ServiceOutcome {
    param([object]$Context, [string]$Project, [string]$Service, [int]$TimeoutSeconds)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $last = [pscustomobject]@{ Status = 'unknown'; Health = 'none'; ExitCode = $null; Container = $null }
    while ((Get-Date) -lt $deadline) {
        $state = Get-ServiceState -Context $Context -Project $Project -Service $Service -TimeoutSeconds 30
        $last = $state
        if ($state.Status -eq 'running' -and $state.Health -eq 'healthy') { return [pscustomobject]@{ Outcome = 'healthy'; State = $state } }
        if ($state.Status -eq 'exited' -or $state.Status -eq 'dead') { return [pscustomobject]@{ Outcome = 'exited'; State = $state } }
        Start-Sleep -Seconds 2
    }
    return [pscustomobject]@{ Outcome = 'timeout'; State = $last }
}

function Save-Diagnostics {
    param([object]$Context, [string]$Project, [string]$EvidencePath, [object]$Budgets)

    New-Item -ItemType Directory -Force -Path $EvidencePath | Out-Null
    $readTimeout = [Math]::Max($Budgets.AsyncAssertionSeconds, 60)

    $status = Invoke-Native -FilePath $Docker -Arguments (Get-ComposeArguments $Context $Project @('ps', '--all', '--format', 'json')) -TimeoutSeconds $readTimeout
    $statusText = Protect-Text (Join-Output $status.StdOut $status.StdErr)
    Set-Content -LiteralPath (Join-Path $EvidencePath 'compose-ps.json') -Value $statusText -Encoding utf8NoBOM

    $logs = Invoke-Native -FilePath $Docker -Arguments (Get-ComposeArguments $Context $Project @('logs', '--no-color', '--timestamps')) -TimeoutSeconds $readTimeout
    $logText = Protect-Text (Join-Output $logs.StdOut $logs.StdErr)
    Set-Content -LiteralPath (Join-Path $EvidencePath 'compose.log') -Value $logText -Encoding utf8NoBOM

    return [pscustomobject]@{ Logs = $logText; PsJson = $statusText }
}

function Remove-Project {
    param([object]$Context, [string]$Project, [object]$Budgets)

    $result = Invoke-Native -FilePath $Docker -Arguments (Get-ComposeArguments $Context $Project @('down', '--volumes', '--remove-orphans')) -TimeoutSeconds $Budgets.TeardownPerProjectSeconds
    if ($result.TimedOut) {
        throw ('teardown of project {0} timed out after {1}s' -f $Project, $Budgets.TeardownPerProjectSeconds)
    }
    if ($result.ExitCode -ne 0) {
        throw ('teardown of project {0} exited {1}: {2}' -f $Project, $result.ExitCode, (Protect-Text (Join-Output $result.StdOut $result.StdErr)))
    }
    Write-Step ('tore down project-scoped stack {0}' -f $Project)
}

function Get-ApiServiceMatch {
    param([string]$ComposePath)

    $lines = @(Get-Content -LiteralPath $ComposePath)
    $start = -1
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match '^  api:\s*$') { $start = $index; break }
    }
    if ($start -lt 0) { throw 'the candidate docker-compose.yml has no api service' }

    $end = $lines.Count
    for ($index = $start + 1; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match '^  \S') { $end = $index; break }
    }

    $bindings = @()
    for ($index = $start; $index -lt $end; $index++) {
        $lineMatch = [regex]::Match($lines[$index], '^\s+Ollama__Endpoint:\s*(\S+)\s*$')
        if ($lineMatch.Success) {
            $bindings += [pscustomobject]@{ Index = $index; Value = $lineMatch.Groups[1].Value }
        }
    }

    return [pscustomobject]@{ Lines = $lines; Bindings = $bindings }
}

function Test-Dev440Binding {
    param([string]$ComposePath)

    $match = Get-ApiServiceMatch -ComposePath $ComposePath
    if ($match.Bindings.Count -ne 1) {
        return $false
    }
    return $match.Bindings[0].Value -eq $Dev440BindingValue
}

function Remove-Dev440Binding {
    param([string]$ComposePath)

    $diff = Invoke-Native -FilePath 'git' -Arguments @('-C', $RepoRoot, 'diff', '--unified=0', ('{0}^' -f $Dev440Commit), $Dev440Commit, '--', $Dev440Path)
    if ($diff.ExitCode -ne 0) {
        throw ('DEV-440 patch mismatch: could not read the commit diff: {0}' -f (Protect-Text (Join-Output $diff.StdOut $diff.StdErr)))
    }

    $added = @(
        $diff.StdOut -split "`n" |
            Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' } |
            ForEach-Object { $_.Substring(1).TrimEnd("`r") }
    )
    if ($added.Count -ne $Dev440ExpectedAdded.Count) {
        throw ('DEV-440 patch mismatch: commit adds {0} line(s), expected {1}' -f $added.Count, $Dev440ExpectedAdded.Count)
    }
    for ($index = 0; $index -lt $Dev440ExpectedAdded.Count; $index++) {
        if ($added[$index] -ne $Dev440ExpectedAdded[$index]) {
            throw ('DEV-440 patch mismatch: added line {0} is {1}, expected {2}' -f ($index + 1), $added[$index].Trim(), $Dev440ExpectedAdded[$index].Trim())
        }
    }

    $match = Get-ApiServiceMatch -ComposePath $ComposePath
    if ($match.Bindings.Count -ne 1 -or $match.Bindings[0].Value -ne $Dev440BindingValue) {
        throw ('DEV-440 patch mismatch: the candidate api service does not carry exactly one {0} binding' -f $Dev440BindingValue)
    }

    $original = Get-Content -LiteralPath $ComposePath -Raw
    $reversed = [System.Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $match.Lines.Count; $index++) {
        if ($index -eq $match.Bindings[0].Index) { continue }
        $reversed.Add($match.Lines[$index])
    }
    $reversedText = ($reversed -join "`n") + "`n"
    Set-Content -LiteralPath $ComposePath -Value $reversedText -Encoding utf8NoBOM

    if (Test-Dev440Binding -ComposePath $ComposePath) {
        throw 'DEV-440 patch mismatch: the Ollama__Endpoint binding survived the reversal'
    }

    return [pscustomobject]@{
        commit      = $Dev440Commit
        patchSha256 = Get-TextSha256 $diff.StdOut
        removed     = $Dev440ExpectedAdded[1].Trim()
        preSha256   = Get-TextSha256 $original
        postSha256  = Get-TextSha256 $reversedText
    }
}

function Invoke-PlainStartup {
    param([object]$Context, [string]$Project, [string]$EvidencePath, [object]$Budgets, [string]$PhaseName)

    Invoke-Checked -FilePath $Docker -Arguments (Get-ComposeArguments $Context $Project @('build', 'api')) -TimeoutSeconds (Get-StepTimeout $Budgets.StackStartupSeconds $Budgets) -What ('[{0}] docker compose build api' -f $PhaseName) | Out-Null
    Write-Step ('[{0}] api image built from the disposable copy' -f $PhaseName)

    Invoke-Checked -FilePath $Docker -Arguments (Get-ComposeArguments $Context $Project @('up', '-d', '--no-build', 'postgres', 'rabbitmq', 'ollama')) -TimeoutSeconds (Get-StepTimeout $Budgets.StackStartupSeconds $Budgets) -What ('[{0}] docker compose up dependencies' -f $PhaseName) | Out-Null

    foreach ($service in @('postgres', 'rabbitmq')) {
        if (-not (Wait-ServiceHealthy -Context $Context -Project $Project -Service $service -TimeoutSeconds (Get-StepTimeout $Budgets.StackStartupSeconds $Budgets))) {
            Save-Diagnostics -Context $Context -Project $Project -EvidencePath $EvidencePath -Budgets $Budgets | Out-Null
            throw ('{0} never became healthy within the startup budget' -f $service)
        }
        Write-Step ('[{0}] {1} healthy' -f $PhaseName, $service)
    }

    Write-Step ('[{0}] provisioning real Ollama model readiness' -f $PhaseName)
    Invoke-Checked -FilePath $Docker -Arguments (Get-ComposeArguments $Context $Project @('up', '-d', '--no-build', 'ollama-model-init')) -TimeoutSeconds (Get-StepTimeout $Budgets.StackStartupSeconds $Budgets) -What ('[{0}] docker compose up ollama-model-init' -f $PhaseName) | Out-Null

    $modelDeadline = (Get-Date).AddSeconds((Get-StepTimeout $Budgets.StackStartupSeconds $Budgets))
    $modelReady = $false
    while ((Get-Date) -lt $modelDeadline) {
        $state = Get-ServiceState -Context $Context -Project $Project -Service 'ollama-model-init' -TimeoutSeconds 30
        if ($state.Status -eq 'exited') {
            if ($state.ExitCode -ne '0') {
                Save-Diagnostics -Context $Context -Project $Project -EvidencePath $EvidencePath -Budgets $Budgets | Out-Null
                throw ('ollama model provisioning exited {0}; model readiness was not established' -f $state.ExitCode)
            }
            $modelReady = $true
            break
        }
        Start-Sleep -Seconds 2
    }
    if (-not $modelReady) {
        Save-Diagnostics -Context $Context -Project $Project -EvidencePath $EvidencePath -Budgets $Budgets | Out-Null
        throw 'ollama model readiness was not established within the startup budget'
    }
    Write-Step ('[{0}] real Ollama model readiness established' -f $PhaseName)

    Write-Step ('[{0}] starting api on the plain shipped stack' -f $PhaseName)
    Invoke-Checked -FilePath $Docker -Arguments (Get-ComposeArguments $Context $Project @('up', '-d', '--no-build', 'api')) -TimeoutSeconds (Get-StepTimeout $Budgets.StackStartupSeconds $Budgets) -What ('[{0}] docker compose up api' -f $PhaseName) | Out-Null

    $outcome = Wait-ServiceOutcome -Context $Context -Project $Project -Service 'api' -TimeoutSeconds (Get-StepTimeout $Budgets.StackStartupSeconds $Budgets)
    $diagnostics = Save-Diagnostics -Context $Context -Project $Project -EvidencePath $EvidencePath -Budgets $Budgets

    return [pscustomobject]@{
        Outcome  = $outcome.Outcome
        ApiState = $outcome.State
        Logs     = $diagnostics.Logs
        PsJson   = $diagnostics.PsJson
    }
}

function Invoke-PlainPhase {
    param(
        [ValidateSet('candidate', 'regression')][string]$Mode,
        [object]$Budgets,
        [string]$StagingRoot,
        [string]$EvidenceRoot,
        [string]$Commit
    )

    $phaseName = if ($Mode -eq 'candidate') { 'Candidate' } else { 'Regression' }
    $evidencePath = Join-Path $EvidenceRoot ('plain-startup-{0}' -f $Mode)
    New-Item -ItemType Directory -Force -Path $evidencePath | Out-Null

    $source = Join-Path $StagingRoot ('source-{0}' -f $Mode)
    Write-Step ('[{0}] staging a disposable copy of {1} outside the PR checkout' -f $phaseName, $Commit)
    New-StagingCopy -Commit $Commit -Destination $source | Out-Null

    $context = New-SmokeContext -SourceRoot $source -PhaseName $phaseName

    $patch = $null
    if ($Mode -eq 'candidate') {
        if (-not (Test-Dev440Binding -ComposePath $context.Compose)) {
            throw ('the candidate stack does not carry the DEV-440 production Ollama endpoint binding {0}' -f $Dev440BindingValue)
        }
        Write-Step '[Candidate] shipped Compose keeps its production Ollama__Endpoint binding; no e2e endpoint override is applied'
    }
    else {
        $patch = Remove-Dev440Binding -ComposePath $context.Compose
        Write-Step ('[Regression] reversed only {0} in the disposable copy; compose {1} -> {2}' -f $Dev440Commit.Substring(0, 8), $patch.preSha256.Substring(0, 12), $patch.postSha256.Substring(0, 12))
    }

    $project = New-RandomToken 4
    $project = if ($Mode -eq 'candidate') { 'lamuflix-smoke-plain-' + $project } else { 'lamuflix-smoke-regress-' + $project }
    Register-Project -Project $project -RegistryPath (Join-Path $EvidenceRoot 'projects.txt')

    $receipt = [ordered]@{
        phase            = 'plain-startup'
        mode             = $Mode
        project          = $project
        source           = [ordered]@{
            commit     = $Commit
            copy       = $source
            resolution = 'disposable copy staged outside the PR checkout; historical parent never used'
        }
        localPlay        = $false
        endpointOverride = 'none (plain shipped docker-compose.yml)'
        patch            = if ($patch) {
            [ordered]@{
                commit      = $patch.commit
                patchSha256 = $patch.patchSha256
                removed     = $patch.removed
                preSha256   = $patch.preSha256
                postSha256  = $patch.postSha256
                scope       = 'reversed in the disposable copy only; PR checkout untouched'
            }
        } else {
            [ordered]@{ applied = $false; note = 'candidate copy is unmodified; no reverse patch applied' }
        }
        prerequisites    = $null
        health           = $null
        exit             = $null
        cause            = $null
        verdict          = 'FAIL'
    }

    $failure = $null
    $teardownFailure = $null
    try {
        $result = Invoke-PlainStartup -Context $context -Project $project -EvidencePath $evidencePath -Budgets $Budgets -PhaseName $phaseName

        $receipt.prerequisites = [ordered]@{
            docker      = 'available'
            build       = 'succeeded'
            settings    = 'generated inside the disposable copy'
            postgres    = 'healthy'
            rabbitmq    = 'healthy'
            ollamaModel = 'ready'
        }
        $receipt.exit = [ordered]@{ apiContainerExitCode = $result.ApiState.ExitCode; smokeExitCode = $null }
        $receipt.health = ('api {0} (status={1}, health={2})' -f $result.Outcome, $result.ApiState.Status, $result.ApiState.Health)

        if ($Mode -eq 'candidate') {
            if ($result.Outcome -ne 'healthy') {
                throw ('candidate plain startup never reached API health ({0})' -f $result.Outcome)
            }
            $receipt.exit.smokeExitCode = 0
            $receipt.verdict = 'PASS'
            Write-Step '[Candidate] PASS: plain shipped startup reached API health after dependency and model readiness'
        }
        else {
            if ($result.Outcome -eq 'healthy') {
                throw 'the reversed source still reached API health; the DEV-440 regression is not reproducible'
            }

            $attributable = @($result.Logs -split "`n" | Where-Object { $_ -match [regex]::Escape($LocalhostEndpointPattern) })
            if ($attributable.Count -eq 0) {
                throw ('api was never healthy ({0}) but no localhost/embedding startup cause was attributable' -f $result.Outcome)
            }

            $requestLine = ($attributable | Where-Object { $_ -match 'POST ' + [regex]::Escape($LocalhostEndpointPattern) } | Select-Object -First 1)
            $failureLine = ($attributable | Where-Object { $_ -match 'localhost:11434|Connection refused|No connection could be made' } | Select-Object -Last 1)
            $causeParts = @()
            if ($requestLine) {
                $endpointMatch = [regex]::Match($requestLine, '(?i)https?://localhost:11434/[^\s"''\\]*')
                if ($endpointMatch.Success) { $causeParts += ('embedding request to {0} through the default localhost endpoint' -f $endpointMatch.Value) }
            }
            if ($failureLine) {
                $reasonMatch = [regex]::Match($failureLine, '(?i)(connection refused|No connection could be made[^"\\]*)')
                if ($reasonMatch.Success) { $causeParts += ('startup probe failed: {0}' -f $reasonMatch.Value.Trim()) }
            }
            if ($causeParts.Count -eq 0) {
                $causeParts += ('api log references {0}' -f $LocalhostEndpointPattern)
            }
            $causeParts += ('api container exited {0}' -f $(if ($result.ApiState.ExitCode) { $result.ApiState.ExitCode } else { 'unknown' }))

            $receipt.cause = Protect-Text ($causeParts -join '; ')
            $receipt.exit.smokeExitCode = 0
            $receipt.verdict = 'PASS'
            Write-Step '[Regression] PASS: the identical health assertion failed for the attributable localhost/embedding startup cause'
        }
    }
    catch {
        $failure = $_.Exception.Message
        if (-not $receipt.cause) { $receipt.cause = Protect-Text $failure }
    }
    finally {
        try {
            Remove-Project -Context $context -Project $project -Budgets $Budgets
        }
        catch {
            $teardownFailure = $_.Exception.Message
            $receipt.verdict = 'FAIL'
            $receipt.exit = [ordered]@{ apiContainerExitCode = $null; smokeExitCode = 1 }
            $receipt.cause = Protect-Text ('teardown failure: {0}' -f $teardownFailure)
        }
    }

    ($receipt | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath (Join-Path $evidencePath 'receipt.json') -Encoding utf8NoBOM

    if ($teardownFailure) {
        throw ('teardown failed for project {0}: {1}' -f $project, $teardownFailure)
    }
    if ($failure) {
        throw $failure
    }

    return $receipt
}

function Get-CheckoutBaseline {
    $status = Invoke-Native -FilePath 'git' -Arguments @('-C', $RepoRoot, 'status', '--porcelain=v1', '--untracked-files=all')
    if ($status.ExitCode -ne 0) {
        throw 'could not read the PR checkout baseline'
    }
    return Get-TextSha256 $status.StdOut
}

function Connect-ToolchainModules {
    param([string]$SourceRoot)

    $source = Join-Path $RepoRoot 'web/node_modules'
    if (-not (Test-Path -LiteralPath $source)) {
        throw ('web/node_modules is missing in the PR checkout; install the locked web toolchain before running the smoke')
    }

    $destination = Join-Path $SourceRoot 'web/node_modules'
    if (Test-Path -LiteralPath $destination) {
        return
    }

    New-Item -ItemType Directory -Force -Path (Join-Path $SourceRoot 'web') | Out-Null
    $linkType = if ($IsWindows) { 'Junction' } else { 'SymbolicLink' }
    New-Item -ItemType $linkType -Path $destination -Target $source | Out-Null
    Write-Step '[Stubbed flow] linked the locked web toolchain into the disposable copy (toolchain only, no source)'
}

function Invoke-StubbedFlowPhase {
    param(
        [object]$Budgets,
        [string]$StagingRoot,
        [string]$EvidenceRoot,
        [string]$Commit
    )

    $phaseName = 'Stubbed flow'
    $evidencePath = Join-Path $EvidenceRoot 'stubbed-flow'
    New-Item -ItemType Directory -Force -Path $evidencePath | Out-Null

    $source = Join-Path $StagingRoot 'source-flow'
    Write-Step ('[{0}] staging a disposable copy of {1} outside the PR checkout' -f $phaseName, $Commit)
    New-StagingCopy -Commit $Commit -Destination $source | Out-Null
    Connect-ToolchainModules -SourceRoot $source

    $project = 'lamuflix-smoke-flow-' + (New-RandomToken 4)
    Register-Project -Project $project -RegistryPath (Join-Path $EvidenceRoot 'projects.txt')

    $runner = Join-Path $source 'web/e2e/run.mjs'
    if (-not (Test-Path -LiteralPath $runner)) {
        throw ('the candidate source has no web/e2e/run.mjs; the stubbed full-flow phase cannot run')
    }

    $receipt = [ordered]@{
        phase         = 'stubbed-flow'
        project       = $project
        source        = [ordered]@{
            commit     = $Commit
            copy       = $source
            entrypoint = 'web/e2e/run.mjs via E2E_SMOKE_MODE=dev471'
        }
        localPlay     = $false
        fixture       = 'web/e2e/fixtures/dev471/smoke-fixture.json'
        stubbing      = 'external providers only (existing web/e2e/compose.e2e.yml overlay, unchanged)'
        exit          = $null
        verdict       = 'FAIL'
        failure       = $null
        log           = $null
    }

    $previousSmokeMode = $env:E2E_SMOKE_MODE
    $previousProject = $env:E2E_PROJECT_NAME
    $teardownFailure = $null
    $failure = $null
    try {
        $env:E2E_SMOKE_MODE = 'dev471'
        $env:E2E_PROJECT_NAME = $project
        $flowRunner = if ($env:DEV471_FAILURE_PATH_CHILD -eq '1' -and $env:DEV471_FLOW_RUNNER) { $env:DEV471_FLOW_RUNNER } else { 'node' }
        $run = Invoke-Native -FilePath $flowRunner -Arguments @($runner) -TimeoutSeconds (Get-StepTimeout ($Budgets.StackStartupSeconds * 2) $Budgets) -WorkingDirectory $source
        $log = Protect-Text (Join-Output $run.StdOut $run.StdErr)
        Set-Content -LiteralPath (Join-Path $evidencePath 'stubbed-flow.log') -Value $log -Encoding utf8NoBOM
        $receipt.log = Join-Path $evidencePath 'stubbed-flow.log'
        $receipt.exit = [ordered]@{ smokeExitCode = $run.ExitCode }

        if ($run.TimedOut) {
            throw ('the stubbed full-flow phase timed out after {0}s' -f ($Budgets.StackStartupSeconds * 2))
        }
        if ($run.ExitCode -ne 0) {
            throw ('the stubbed full-flow phase exited {0}' -f $run.ExitCode)
        }

        $receipt.verdict = 'PASS'
        Write-Step ('[{0}] PASS: fixture-correlated import, stub enrichment, listing, thumbnail, hub update, advancing browser play and process-play 403' -f $phaseName)
    }
    catch {
        $failure = $_.Exception.Message
        $receipt.failure = Protect-Text $failure
        if ($null -eq $receipt.log) {
            Set-Content -LiteralPath (Join-Path $evidencePath 'stubbed-flow.log') -Value (Protect-Text $failure) -Encoding utf8NoBOM
            $receipt.log = Join-Path $evidencePath 'stubbed-flow.log'
        }
    }
    finally {
        $env:E2E_SMOKE_MODE = $previousSmokeMode
        $env:E2E_PROJECT_NAME = $previousProject
        try {
            $runtimeEnv = Join-Path $source 'web/e2e/.runtime/compose.env'
            $downArgs = @('compose', '-p', $project, '-f', (Join-Path $source 'docker-compose.yml'), '-f', (Join-Path $source 'web/e2e/compose.e2e.yml'))
            if (Test-Path -LiteralPath $runtimeEnv) {
                $downArgs += @('--env-file', $runtimeEnv)
            }
            $downArgs += @('down', '--volumes', '--remove-orphans')

            $remaining = Invoke-Native -FilePath $Docker -Arguments @('ps', '--all', '--quiet', '--filter', ('label=com.docker.compose.project={0}' -f $project)) -TimeoutSeconds $Budgets.TeardownPerProjectSeconds
            if ($remaining.ExitCode -ne 0) {
                throw ('could not read the remaining containers for project {0}' -f $project)
            }
            if (($remaining.StdOut -split "`n" | Where-Object { $_.Trim() }).Count -eq 0) {
                Write-Step ('project {0} already has no containers; teardown satisfied' -f $project)
            }
            else {
                $down = Invoke-Native -FilePath $Docker -Arguments $downArgs -TimeoutSeconds $Budgets.TeardownPerProjectSeconds
                if ($down.TimedOut) {
                    throw ('teardown of project {0} timed out after {1}s' -f $project, $Budgets.TeardownPerProjectSeconds)
                }
                if ($down.ExitCode -ne 0) {
                    throw ('teardown of project {0} exited {1}: {2}' -f $project, $down.ExitCode, (Protect-Text (Join-Output $down.StdOut $down.StdErr)))
                }
                Write-Step ('tore down project-scoped stack {0}' -f $project)
            }
        }
        catch {
            $teardownFailure = $_.Exception.Message
            $receipt.verdict = 'FAIL'
            $receipt.exit = [ordered]@{ smokeExitCode = 1 }
            $receipt.failure = Protect-Text ('teardown failure: {0}' -f $teardownFailure)
        }
    }

    ($receipt | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath (Join-Path $evidencePath 'receipt.json') -Encoding utf8NoBOM

    if ($teardownFailure) {
        throw ('teardown failed for project {0}: {1}' -f $project, $teardownFailure)
    }
    if ($failure) {
        throw $failure
    }

    return $receipt
}

function New-FakeDockerShim {
    param([string]$Directory)

    New-Item -ItemType Directory -Force -Path $Directory | Out-Null
    $shimScript = Join-Path $Directory 'fake-docker.ps1'
    Copy-Item -LiteralPath (Join-Path $RepoRoot 'web/e2e/fixtures/dev471/fake-docker.ps1') -Destination $shimScript -Force
    $wrapper = Join-Path $Directory 'docker.cmd'
    @(
        '@echo off'
        'pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0fake-docker.ps1" %*'
        'exit /b %ERRORLEVEL%'
    ) | Set-Content -LiteralPath $wrapper -Encoding ascii
    return $Directory
}

function Get-EvidenceRuns {
    param([string]$Base = $EvidenceBase)
    $root = Join-Path $RepoRoot $Base
    if (-not (Test-Path -LiteralPath $root)) { return @() }
    return @(Get-ChildItem -LiteralPath $root -Directory | ForEach-Object { $_.Name })
}

function Invoke-FailureChild {
    param(
        [Parameter(Mandatory)][string]$Scenario,
        [Parameter(Mandatory)][hashtable]$Budgets,
        [int]$TimeoutSeconds = 180,
        [hashtable]$Environment = @{}
    )

    $before = Get-EvidenceRuns -Base 'artifacts/compose-smoke-failure-paths'
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'pwsh'
    foreach ($argument in @('-NoProfile', '-File', (Join-Path $PSScriptRoot 'run-compose-smoke.ps1'))) {
        $startInfo.ArgumentList.Add($argument)
    }
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false
    $startInfo.WorkingDirectory = $RepoRoot
    $startInfo.Environment['DEV471_FAILURE_PATH_CHILD'] = '1'
    $startInfo.Environment['DEV471_FAILURE_PATH_BUDGETS'] = (($Budgets + @{ wholeTestRetries = 0 }) | ConvertTo-Json -Compress)
    $startInfo.Environment['DEV471_FAKE_DOCKER_SCENARIO'] = $Scenario
    foreach ($key in $Environment.Keys) { $startInfo.Environment[$key] = [string]$Environment[$key] }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $timedOut = -not $process.WaitForExit($TimeoutSeconds * 1000)
    if ($timedOut) {
        try { $process.Kill($true) } catch { }
        $process.WaitForExit()
    }

    $after = Get-EvidenceRuns -Base 'artifacts/compose-smoke-failure-paths'
    $new = @($after | Where-Object { $before -notcontains $_ })
    $evidenceDir = if ($new.Count -gt 0) { Join-Path $RepoRoot ('artifacts/compose-smoke-failure-paths/{0}' -f $new[0]) } else { $null }

    return [pscustomobject]@{
        Scenario = $Scenario
        ExitCode = if ($timedOut) { 124 } else { $process.ExitCode }
        TimedOut = $timedOut
        StdOut   = $stdoutTask.GetAwaiter().GetResult()
        StdErr   = $stderrTask.GetAwaiter().GetResult()
        Evidence = $evidenceDir
    }
}

function Read-JsonFile {
    param([string]$Path)
    if (-not $Path -or -not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json)
}

function Get-DockerCalls {
    param([string]$LogPath)
    if (-not (Test-Path -LiteralPath $LogPath)) { return @() }
    return @(Get-Content -LiteralPath $LogPath | Where-Object { $_.Trim() })
}

function Add-FailureCase {
    param([string]$Name, [bool]$Pass, [string]$Detail)

    $script:FailureCases.Add([pscustomobject]@{ case = $Name; verdict = if ($Pass) { 'PASS' } else { 'FAIL' }; detail = $Detail })
    $line = '{0} {1}: {2}' -f $(if ($Pass) { 'PASS' } else { 'FAIL' }), $Name, $Detail
    Write-Step ($line)
}

function Test-FailureStdoutClean {
    param([object]$Child, [string[]]$PlantedValues)

    $text = (@($Child.StdOut, $Child.StdErr) -join "`n")
    foreach ($value in $PlantedValues) {
        if ($text.Contains($value)) { return [pscustomobject]@{ Clean = $false; Detail = 'runner stdout echoed a planted value' } }
    }
    return [pscustomobject]@{ Clean = $true; Detail = 'runner stdout carried no planted value' }
}

function Test-EvidenceClean {
    param([string]$EvidencePath, [string[]]$PlantedValues, [string[]]$RedactionSites = @())

    if (-not $EvidencePath -or -not (Test-Path -LiteralPath $EvidencePath)) {
        return [pscustomobject]@{ Clean = $true; Detail = 'no evidence was written, so nothing could leak' }
    }
    $leaks = @()
    $missingMarkers = @()
    foreach ($site in $RedactionSites) {
        $sitePath = Join-Path $EvidencePath $site
        if (-not (Test-Path -LiteralPath $sitePath)) {
            $missingMarkers += ('{0} (absent)' -f $site)
            continue
        }
        $siteText = Get-Content -LiteralPath $sitePath -Raw
        if ($siteText -notmatch '\[redacted\]') { $missingMarkers += $site }
    }
    foreach ($file in @(Get-ChildItem -LiteralPath $EvidencePath -Recurse -File)) {
        $text = Get-Content -LiteralPath $file.FullName -Raw
        if ($null -eq $text) { continue }
        foreach ($value in $PlantedValues) {
            if ($text.Contains($value)) { $leaks += [System.IO.Path]::GetFileName($file.FullName) }
        }
    }
    $pass = $true
    $detail = @()
    if ($leaks.Count -gt 0) {
        $pass = $false
        $detail += ('evidence leaks a planted value in: {0}' -f (($leaks | Sort-Object -Unique) -join ', '))
    }
    else {
        $detail += 'no evidence file carried a planted value'
    }
    if ($missingMarkers.Count -gt 0) {
        $pass = $false
        $detail += ('captured logs lack the [redacted] marker in: {0}' -f (($missingMarkers | Sort-Object -Unique) -join ', '))
    }
    elseif ($RedactionSites.Count -gt 0) {
        $detail += ('captured logs carry [redacted] in {0}' -f ($RedactionSites -join ', '))
    }
    return [pscustomobject]@{ Clean = $pass; Detail = ($detail -join '; ') }
}

function New-FailurePathWorkRoot {
    $root = Join-Path ([System.IO.Path]::GetTempPath()) ('dev471-failure-path-{0}' -f $RunStamp)
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    return $root
}

function New-FlowRunnerStub {
    param([string]$Directory)

    New-Item -ItemType Directory -Force -Path $Directory | Out-Null
    $script = Join-Path $Directory 'node-stub.ps1'
    @(
        'Write-Output " 1) dev471 compose smoke"'
        'Write-Output "   x fixture-correlated enrichment assertion failed: enrichmentStatus never reached Enriched"'
        'Write-Output "1 failed"'
        'exit 1'
    ) | Set-Content -LiteralPath $script -Encoding utf8NoBOM
    $wrapper = Join-Path $Directory 'node.cmd'
    @(
        '@echo off'
        'pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0node-stub.ps1" %*'
        'exit /b %ERRORLEVEL%'
    ) | Set-Content -LiteralPath $wrapper -Encoding ascii
    return $wrapper
}

function Test-FailureRun {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Scenario,
        [Parameter(Mandatory)][hashtable]$Budgets,
        [long]$ExpectedExit = 1,
        [switch]$ExpectDiagnostics,
        [switch]$ExpectTeardownAttempt,
        [switch]$ExpectNoTeardown,
        [switch]$ExpectPlantedSecretClean,
        [string[]]$PlantedValues = @(),
        [string[]]$RedactionSites = @(),
        [hashtable]$ChildEnvironment = @{},
        [hashtable]$RunnerEnvironment = @{}
    )

    foreach ($key in $RunnerEnvironment.Keys) { Set-Item -Path ('env:{0}' -f $key) -Value $RunnerEnvironment[$key] }

    $callsFile = Join-Path $script:WorkRoot ('{0}.calls.log' -f $Name)
    $stateDir = Join-Path $script:WorkRoot ('{0}.state' -f $Name)
    New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
    $environment = @{
        DOCKER                          = $script:ShimDocker
        DEV471_FAKE_DOCKER_LOG          = $callsFile
        DEV471_FAKE_DOCKER_STATE        = $stateDir
        DEV471_FAKE_DOCKER_SCENARIO     = $Scenario
    }
    foreach ($key in $ChildEnvironment.Keys) { $environment[$key] = $ChildEnvironment[$key] }

    $child = Invoke-FailureChild -Scenario $Scenario -Budgets $Budgets -Environment $environment
    $calls = Get-DockerCalls -LogPath $callsFile
    $detail = @()
    $pass = $true

    if ($child.TimedOut) { $pass = $false; $detail += 'child timed out' }
    if ($child.ExitCode -ne $ExpectedExit) {
        $pass = $false
        $detail += ('exit {0}, expected {1}' -f $child.ExitCode, $ExpectedExit)
    }
    else {
        $detail += ('exit {0} (never green/SKIPPED, expected {1})' -f $child.ExitCode, $ExpectedExit)
    }

    if ($child.ExitCode -eq 2 -or $child.ExitCode -eq 0) {
        $pass = $false
        $detail += 'required smoke answered 0 or 2, which would mask the failure'
    }

    $logsCalled = @($calls | Where-Object { $_ -match 'compose .*\blogs\b' }).Count -gt 0
    $downCalled = @($calls | Where-Object { $_ -match 'compose .*\bdown\b' }).Count -gt 0
    if ($ExpectDiagnostics) {
        if ($logsCalled) { $detail += 'diagnostics captured (compose logs) before teardown' } else { $pass = $false; $detail += 'no compose logs capture was attempted' }
    }
    if ($ExpectTeardownAttempt) {
        if ($downCalled) { $detail += 'teardown attempt recorded' } else { $pass = $false; $detail += 'no project teardown attempt was recorded' }
    }
    if ($ExpectNoTeardown -and $downCalled) {
        $pass = $false
        $detail += 'teardown was attempted although nothing was ever registered'
    }
    if ($logsCalled -and $downCalled) {
        $lastLogs = ($calls | Select-String -Pattern 'compose .*\blogs\b' | Select-Object -Last 1).LineNumber
        $firstDown = ($calls | Select-String -Pattern 'compose .*\bdown\b' | Select-Object -First 1).LineNumber
        if ($lastLogs -gt $firstDown) { $pass = $false; $detail += 'diagnostics were captured after teardown began' }
    }

    $receipt = if ($child.Evidence) { Read-JsonFile -Path (Join-Path $child.Evidence 'receipt.json') } else { $null }
    if ($child.Evidence) {
        $detail += ('evidence written under {0}' -f (Split-Path -Leaf $child.Evidence))
        if ($receipt) {
            if ($receipt.verdict -ne 'FAIL') { $pass = $false; $detail += ('receipt verdict {0}, expected FAIL' -f $receipt.verdict) }
            if (-not $receipt.failure) { $pass = $false; $detail += 'receipt recorded no failure detail' }
            else { $detail += ('receipt cause: {0}' -f $receipt.failure) }
        }
        else {
            $pass = $false
            $detail += 'evidence directory carries no receipt.json'
        }
    }
    elseif ($ExpectedExit -eq 1 -and $Scenario -ne 'unavailable') {
        $pass = $false
        $detail += 'a failing run wrote no evidence directory at all'
    }

    if ($ExpectPlantedSecretClean) {
        $stdout = Test-FailureStdoutClean -Child $child -PlantedValues $PlantedValues
        if (-not $stdout.Clean) { $pass = $false }
        $detail += $stdout.Detail

        $evidence = Test-EvidenceClean -EvidencePath $child.Evidence -PlantedValues $PlantedValues -RedactionSites $RedactionSites
        if (-not $evidence.Clean) { $pass = $false }
        $detail += $evidence.Detail
    }

    foreach ($key in $RunnerEnvironment.Keys) { Remove-Item -Path ('env:{0}' -f $key) -ErrorAction SilentlyContinue }

    $script:FailureRuns.Add([pscustomobject]@{ scenario = $Name; childExit = $child.ExitCode; evidence = $child.Evidence })
    Add-FailureCase -Name $Name -Pass $pass -Detail ($detail -join '; ')
    return $child
}

function Get-CiStepScript {
    param([Parameter(Mandatory)][string]$StepName)

    $workflowPath = Join-Path $RepoRoot '.github/workflows/compose-smoke.yml'
    if (-not (Test-Path -LiteralPath $workflowPath)) { throw ('dedicated workflow not found: {0}' -f $workflowPath) }
    $lines = @(Get-Content -LiteralPath $workflowPath)
    $start = -1
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match ('^\s+- name:\s*{0}\s*$' -f [regex]::Escape($StepName))) { $start = $index; break }
    }
    if ($start -lt 0) { throw ('the dedicated workflow has no step named {0}' -f $StepName) }
    $indent = ([regex]::Match($lines[$start], '^(\s*)')).Groups[1].Value.Length
    $body = [System.Collections.Generic.List[string]]::new()
    for ($index = $start + 1; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        $currentIndent = ([regex]::Match($line, '^(\s*)')).Groups[1].Value.Length
        if ($line.Trim() -and $currentIndent -le $indent) { break }
        $body.Add($line)
    }
    $runIndex = -1
    for ($index = 0; $index -lt $body.Count; $index++) {
        if ($body[$index] -match '^\s+run:\s*\|\s*$') { $runIndex = $index; break }
    }
    if ($runIndex -lt 0) { throw ('the {0} step has no run block' -f $StepName) }
    $runIndent = ([regex]::Match($body[$runIndex], '^(\s*)')).Groups[1].Value.Length
    $script = [System.Collections.Generic.List[string]]::new()
    for ($index = $runIndex + 1; $index -lt $body.Count; $index++) {
        $line = $body[$index]
        if ($line.Trim()) {
            $currentIndent = ([regex]::Match($line, '^(\s*)')).Groups[1].Value.Length
            if ($currentIndent -le $runIndent) { break }
            $script.Add($line.Substring([Math]::Min($runIndent + 2, $line.Length)))
        }
        else {
            $script.Add('')
        }
    }
    return ($script -join "`n")
}

function Get-CiCleanupScript {
    return (Get-CiStepScript -StepName 'Clean up compose-smoke projects')
}

function Test-CiCleanupReclaim {
    $cleanupScript = Get-CiCleanupScript
    $detail = @()
    $pass = $true
    $results = @()

    foreach ($case in @(
            @{ name = 'registered smoke project only'; containerScenario = 'healthy' }
            @{ name = 'unrelated project present'; containerScenario = 'healthy' }
            @{ name = 'cleanup failure stays failure'; containerScenario = 'teardown-failure' }
        )) {

        $work = Join-Path $script:WorkRoot ('cleanup-{0}' -f ($case.name -replace '[^a-z]+', '-'))
        New-Item -ItemType Directory -Force -Path (Join-Path $work 'artifacts/compose-smoke/run') | Out-Null
        $projects = @(
            'lamuflix-smoke-plain-0a1b2c3d'
            'lamuflix-smoke-flow-4d5e6f70'
            'lamuflix-e2e-aaaabbbb'
            'someone-else-stack'
        )
        if ($case.name -ne 'registered smoke project only') { $projects += 'unrelated-live-project' }
        $projects | Set-Content -LiteralPath (Join-Path $work 'artifacts/compose-smoke/run/projects.txt') -Encoding utf8NoBOM

        $callsFile = Join-Path $work 'docker.calls.log'
        $stateDir = Join-Path $work 'docker.state'
        New-Item -ItemType Directory -Force -Path $stateDir | Out-Null

        $cleanupPath = Join-Path $work 'cleanup.ps1'
        Set-Content -LiteralPath $cleanupPath -Value $cleanupScript -Encoding utf8NoBOM
        $cleanupShim = Join-Path $work 'shim'
        New-FakeDockerShim -Directory $cleanupShim | Out-Null
        $previousPath = $env:PATH
        $previousScenario = $env:DEV471_FAKE_DOCKER_SCENARIO
        $previousLog = $env:DEV471_FAKE_DOCKER_LOG
        $previousState = $env:DEV471_FAKE_DOCKER_STATE
        try {
            $env:PATH = '{0}{1}{2}' -f $cleanupShim, [System.IO.Path]::PathSeparator, $previousPath
            $env:DEV471_FAKE_DOCKER_SCENARIO = $case.containerScenario
            $env:DEV471_FAKE_DOCKER_LOG = $callsFile
            $env:DEV471_FAKE_DOCKER_STATE = $stateDir
            pushd $work
            & pwsh -NoProfile -File $cleanupPath *> (Join-Path $work 'cleanup.log')
            $exit = $LASTEXITCODE
            popd
        }
        finally {
            $env:PATH = $previousPath
            $env:DEV471_FAKE_DOCKER_SCENARIO = $previousScenario
            $env:DEV471_FAKE_DOCKER_LOG = $previousLog
            $env:DEV471_FAKE_DOCKER_STATE = $previousState
        }

        $calls = Get-DockerCalls -LogPath $callsFile
        $removedProjects = @($calls | ForEach-Object { if ($_ -match 'label=com\.docker\.compose\.project=(\S+)') { $Matches[1] } } | Sort-Object -Unique)
        $touchedUnrelated = @($removedProjects | Where-Object { $_ -notmatch '^lamuflix-smoke-(plain|regress|flow)-[0-9a-f]{8}$' })
        $expectedExit = if ($case.containerScenario -eq 'teardown-failure') { 1 } else { 0 }

        if ($exit -ne $expectedExit) { $pass = $false; $detail += ('{0}: exit {1}, expected {2}' -f $case.name, $exit, $expectedExit) }
        if ($touchedUnrelated.Count -gt 0) { $pass = $false; $detail += ('{0}: touched non-smoke project(s) {1}' -f $case.name, ($touchedUnrelated -join ', ')) }
        if ($exit -eq 0 -and $removedProjects.Count -lt 2) { $pass = $false; $detail += ('{0}: only {1} registered smoke project(s) were reclaimed' -f $case.name, $removedProjects.Count) }
        if ($case.containerScenario -eq 'teardown-failure' -and $exit -ne 0) { $detail += ('{0}: cleanup failure was reported as failure' -f $case.name) }
        $results += [pscustomobject]@{ case = $case.name; exit = $exit; projects = $removedProjects }
    }

    Add-FailureCase -Name 'ci-always-run-cleanup' -Pass $pass -Detail ($detail -join '; ')
    $script:CleanupResults = $results
}

function Test-ExistingRunnerNonregression {
    $detail = @()
    $pass = $true

    $nonregressionRoot = Join-Path $script:WorkRoot 'existing-runner'
    New-Item -ItemType Directory -Force -Path $nonregressionRoot | Out-Null
    $shimDir = Join-Path $nonregressionRoot 'shim'
    New-FakeDockerShim -Directory $shimDir | Out-Null

    $previousPath = $env:PATH
    $previousScenario = $env:DEV471_FAKE_DOCKER_SCENARIO
    $previousLog = $env:DEV471_FAKE_DOCKER_LOG
    $previousState = $env:DEV471_FAKE_DOCKER_STATE
    try {
        $env:PATH = '{0}{1}{2}' -f $shimDir, [System.IO.Path]::PathSeparator, $previousPath
        $env:DEV471_FAKE_DOCKER_SCENARIO = 'unavailable'
        $env:DEV471_FAKE_DOCKER_LOG = Join-Path $nonregressionRoot 'docker.calls.log'
        $env:DEV471_FAKE_DOCKER_STATE = Join-Path $nonregressionRoot 'docker.state'

        $local = Invoke-Native -FilePath 'pwsh' -Arguments @('-NoProfile', '-File', (Join-Path $PSScriptRoot 'run-e2e-smoke.ps1'))
        $localExit = $local.ExitCode
        $localText = Join-Output $local.StdOut $local.StdErr
    }
    finally {
        $env:PATH = $previousPath
        $env:DEV471_FAKE_DOCKER_SCENARIO = $previousScenario
        $env:DEV471_FAKE_DOCKER_LOG = $previousLog
        $env:DEV471_FAKE_DOCKER_STATE = $previousState
    }

    if ($localExit -eq 2) { $detail += 'existing local wrapper keeps its SKIPPED semantics (exit 2) when Docker is unavailable' }
    else { $pass = $false; $detail += ('existing local wrapper exited {0}, expected its own SKIPPED exit 2' -f $localExit) }
    if ($localText -notmatch 'SKIPPED') { $pass = $false; $detail += 'existing local wrapper did not report SKIPPED' }

    $required = Test-FailureRun -Name 'required-smoke-no-docker' -Scenario 'unavailable' -Budgets @{
        stackStartupSeconds       = 5
        asyncAssertionSeconds     = 5
        teardownPerProjectSeconds = 5
        jobCapSeconds             = 60
    } -ExpectedExit 1 -ExpectNoTeardown

    if ($required.ExitCode -eq 2) { $pass = $false; $detail += 'required smoke inherited the local SKIPPED exit 2' }
    else { $detail += ('required smoke refused to inherit SKIPPED: exit {0} while the local wrapper exited {1}' -f $required.ExitCode, $localExit) }

    Add-FailureCase -Name 'existing-runner-nonregression' -Pass $pass -Detail ($detail -join '; ')
}

function Test-CancellationCleanup {
    $detail = @()
    $pass = $true

    $work = Join-Path $script:WorkRoot 'cancellation'
    $stateDir = Join-Path $work 'docker.state'
    New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
    $callsFile = Join-Path $work 'docker.calls.log'

    $budgets = @{
        stackStartupSeconds       = 30
        asyncAssertionSeconds     = 10
        teardownPerProjectSeconds = 5
        jobCapSeconds             = 240
    }
    $before = Get-EvidenceRuns -Base 'artifacts/compose-smoke-failure-paths'

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'pwsh'
    foreach ($argument in @('-NoProfile', '-File', (Join-Path $PSScriptRoot 'run-compose-smoke.ps1'))) {
        $startInfo.ArgumentList.Add($argument)
    }
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false
    $startInfo.WorkingDirectory = $RepoRoot
    $startInfo.Environment['DEV471_FAILURE_PATH_CHILD'] = '1'
    $startInfo.Environment['DEV471_FAILURE_PATH_BUDGETS'] = (($budgets + @{ wholeTestRetries = 0 }) | ConvertTo-Json -Compress)
    $startInfo.Environment['DOCKER'] = $script:ShimDocker
    $startInfo.Environment['DEV471_FAKE_DOCKER_SCENARIO'] = 'startup-timeout'
    $startInfo.Environment['DEV471_FAKE_DOCKER_LOG'] = $callsFile
    $startInfo.Environment['DEV471_FAKE_DOCKER_STATE'] = $stateDir
    $startInfo.Environment['DEV471_FAKE_DOCKER_STALL'] = '60'

    $process = [System.Diagnostics.Process]::Start($startInfo)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()

    $registered = $null
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Date) -lt $deadline -and -not $registered) {
        $new = @(Get-EvidenceRuns -Base 'artifacts/compose-smoke-failure-paths' | Where-Object { $before -notcontains $_ })
        foreach ($run in $new) {
            $path = Join-Path $RepoRoot ('artifacts/compose-smoke-failure-paths/{0}/projects.txt' -f $run)
            if (Test-Path -LiteralPath $path) {
                $name = @(Get-Content -LiteralPath $path | ForEach-Object { $_.Trim() } | Where-Object { $_ })
                if ($name.Count -gt 0) {
                    $registered = [pscustomobject]@{ run = $run; project = $name[0] }
                    break
                }
            }
        }
        if (-not $registered) { Start-Sleep -Milliseconds 500 }
    }

    if (-not $registered) {
        $pass = $false
        $detail += 'the runner never registered a project before it was cancelled'
        try { $process.Kill($true) } catch { }
        $process.WaitForExit()
    }
    else {
        $detail += ('cancellation observed after the runner registered {0} and before any phase completed' -f $registered.project)
        try { $process.Kill($true) } catch { }
        $process.WaitForExit()
        $null = $stdoutTask
        if (-not $process.HasExited) { $pass = $false; $detail += 'the cancelled child could not be terminated' }

        $runDir = Join-Path $RepoRoot ('artifacts/compose-smoke-failure-paths/{0}' -f $registered.run)
        $downCalledByRunner = @(Get-DockerCalls -LogPath $callsFile | Where-Object { $_ -match 'compose .*\bdown\b' }).Count -gt 0
        if ($downCalledByRunner) { $detail += 'the runner attempted its own teardown before cancellation' }
        else { $detail += 'a hard cancellation leaves the registered project behind, which is why the always-run CI cleanup exists' }

        $cleanupRoot = Join-Path $work 'ci'
        New-Item -ItemType Directory -Force -Path (Join-Path $cleanupRoot ('artifacts/compose-smoke/{0}' -f $registered.run)) | Out-Null
        Copy-Item -LiteralPath (Join-Path $runDir 'projects.txt') -Destination (Join-Path $cleanupRoot ('artifacts/compose-smoke/{0}/projects.txt' -f $registered.run))
        $cleanupPath = Join-Path $cleanupRoot 'cleanup.ps1'
        Set-Content -LiteralPath $cleanupPath -Value (Get-CiCleanupScript) -Encoding utf8NoBOM

        $cleanupShim = Join-Path $cleanupRoot 'shim'
        New-FakeDockerShim -Directory $cleanupShim | Out-Null
        $afterCalls = Join-Path $cleanupRoot 'docker.calls.log'
        $previousPath = $env:PATH
        $previousScenario = $env:DEV471_FAKE_DOCKER_SCENARIO
        $previousLog = $env:DEV471_FAKE_DOCKER_LOG
        $previousState = $env:DEV471_FAKE_DOCKER_STATE
        $previousStall = $env:DEV471_FAKE_DOCKER_STALL
        try {
            $env:PATH = '{0}{1}{2}' -f $cleanupShim, [System.IO.Path]::PathSeparator, $previousPath
            $env:DEV471_FAKE_DOCKER_SCENARIO = 'teardown-failure'
            $env:DEV471_FAKE_DOCKER_LOG = $afterCalls
            $env:DEV471_FAKE_DOCKER_STATE = $stateDir
            Remove-Item -Path 'env:DEV471_FAKE_DOCKER_STALL' -ErrorAction SilentlyContinue
            pushd $cleanupRoot
            & pwsh -NoProfile -File $cleanupPath *> (Join-Path $cleanupRoot 'cleanup.log')
            $cleanupExit = $LASTEXITCODE
            popd
        }
        finally {
            $env:PATH = $previousPath
            $env:DEV471_FAKE_DOCKER_SCENARIO = $previousScenario
            $env:DEV471_FAKE_DOCKER_LOG = $previousLog
            $env:DEV471_FAKE_DOCKER_STATE = $previousState
            if ($null -ne $previousStall) { $env:DEV471_FAKE_DOCKER_STALL = $previousStall }
        }

        $reclaimed = @(Get-DockerCalls -LogPath $afterCalls | ForEach-Object { if ($_ -match 'com\.docker\.compose\.project=(\S+)') { $Matches[1] } } | Sort-Object -Unique)
        if ($reclaimed -contains $registered.project) { $detail += ('the always-run CI cleanup targeted the left-behind project {0}' -f $registered.project) }
        else { $pass = $false; $detail += 'the always-run CI cleanup did not target the left-behind project' }
        if ($cleanupExit -eq 1) { $detail += 'the simulated removal failure was reported as cleanup failure, not PASS' }
    }

    Add-FailureCase -Name 'cancellation-cleanup' -Pass $pass -Detail ($detail -join '; ')
}

function Test-UploadGuardPlantedSecret {
    $detail = @()
    $pass = $true
    $guardScript = Get-CiStepScript -StepName 'Guard compose-smoke evidence'

    $work = Join-Path $script:WorkRoot 'upload-guard'
    $runDir = Join-Path $work 'artifacts/compose-smoke/planted'
    foreach ($sub in @('plain-startup-candidate', 'stubbed-flow')) {
        New-Item -ItemType Directory -Force -Path (Join-Path $runDir $sub) | Out-Null
    }
    New-Item -ItemType Directory -Force -Path $work | Out-Null

    $plantedValue = 'Zq7PlantedUploadSecret0099'
    $seed = {
        param([string]$Value)
        Set-Content -LiteralPath (Join-Path $runDir 'receipt.json') -Value '{"verdict":"PASS"}' -Encoding utf8NoBOM
        Set-Content -LiteralPath (Join-Path $runDir 'run.log') -Value '[compose-smoke] verdict: PASS' -Encoding utf8NoBOM
        Set-Content -LiteralPath (Join-Path $runDir 'projects.txt') -Value 'lamuflix-smoke-plain-0a1b2c3d' -Encoding utf8NoBOM
        New-Item -ItemType Directory -Force -Path (Join-Path $runDir 'plain-startup-regression') | Out-Null
        foreach ($file in @('receipt.json', 'compose-ps.json')) {
            Set-Content -LiteralPath (Join-Path $runDir ('plain-startup-candidate/{0}' -f $file)) -Value '{}' -Encoding utf8NoBOM
            Set-Content -LiteralPath (Join-Path $runDir ('plain-startup-regression/{0}' -f $file)) -Value '{}' -Encoding utf8NoBOM
        }
        Set-Content -LiteralPath (Join-Path $runDir 'stubbed-flow/receipt.json') -Value '{}' -Encoding utf8NoBOM
        Set-Content -LiteralPath (Join-Path $runDir 'stubbed-flow/stubbed-flow.log') -Value 'ok' -Encoding utf8NoBOM
        Set-Content -LiteralPath (Join-Path $runDir 'plain-startup-candidate/compose.log') -Value ('api | debug: env POSTGRES_PASSWORD={0}' -f $Value) -Encoding utf8NoBOM
        Set-Content -LiteralPath (Join-Path $runDir 'plain-startup-regression/compose.log') -Value 'api | info: running' -Encoding utf8NoBOM
    }

    $guardPath = Join-Path $work 'guard.ps1'
    Set-Content -LiteralPath $guardPath -Value $guardScript -Encoding utf8NoBOM

    $cases = @(
        @{ name = 'guard rejects a planted credential'; value = $plantedValue; expected = 1 }
        @{ name = 'guard accepts the redacted form'; value = '[redacted]'; expected = 0 }
    )
    foreach ($case in $cases) {
        Remove-Item -LiteralPath (Join-Path $work 'artifacts') -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force -Path $runDir | Out-Null
        foreach ($sub in @('plain-startup-candidate', 'stubbed-flow', 'plain-startup-regression')) {
            New-Item -ItemType Directory -Force -Path (Join-Path $runDir $sub) | Out-Null
        }
        & $seed $case.value

        $previousOutcome = $env:SMOKE_OUTCOME
        $previousProof = $env:PROOF_REQUIRED
        try {
            $env:SMOKE_OUTCOME = 'success'
            $env:PROOF_REQUIRED = 'true'
            pushd $work
            & pwsh -NoProfile -File $guardPath *> (Join-Path $work 'guard.log')
            $exit = $LASTEXITCODE
            popd
        }
        finally {
            $env:SMOKE_OUTCOME = $previousOutcome
            $env:PROOF_REQUIRED = $previousProof
        }

        $staged = Test-Path -LiteralPath (Join-Path $work 'artifacts/compose-smoke-upload')
        if ($exit -ne $case.expected) {
            $pass = $false
            $detail += ('{0}: exit {1}, expected {2}' -f $case.name, $exit, $case.expected)
        }
        if ($case.expected -eq 1) {
            if ($staged) { $pass = $false; $detail += ('{0}: staged an upload anyway' -f $case.name) }
            else { $detail += ('{0}: exit 1 and nothing was staged for upload' -f $case.name) }
            if ((Get-Content -LiteralPath (Join-Path $work 'guard.log') -Raw) -notmatch 'unsafe') {
                $pass = $false
                $detail += ('{0}: the guard did not report the unsafe evidence' -f $case.name)
            }
        }
        else {
            if ($staged) { $detail += ('{0}: staged the allowlisted evidence' -f $case.name) }
            else { $pass = $false; $detail += ('{0}: a safe payload was not staged' -f $case.name) }
        }
    }

    Add-FailureCase -Name 'planted-secret-upload-guard' -Pass $pass -Detail ($detail -join '; ')
}

function Invoke-FailurePathProofs {
    $budgets = Read-Budgets
    $script:Deadline = $RunStarted.AddSeconds(600)
    $script:WorkRoot = New-FailurePathWorkRoot
    $script:FailureCases = [System.Collections.Generic.List[object]]::new()
    $script:FailureRuns = [System.Collections.Generic.List[object]]::new()
    $script:CleanupResults = @()

    $shimDir = Join-Path $script:WorkRoot 'shim'
    New-FakeDockerShim -Directory $shimDir | Out-Null
    $script:ShimDocker = Join-Path $shimDir 'docker.cmd'
    $flowStub = New-FlowRunnerStub -Directory (Join-Path $script:WorkRoot 'flow-runner')

    $simulationBudgets = @{
        stackStartupSeconds       = 4
        asyncAssertionSeconds     = 4
        teardownPerProjectSeconds = 5
        jobCapSeconds             = 240
    }

    Write-Step 'failure-path proofs: deterministic orchestration double standing in for the docker CLI only'
    Write-Step 'failure-path proofs: these do not replace the real positive Compose phases or the controlled-revert proof'

    Test-FailureRun -Name 'startup-timeout' -Scenario 'startup-timeout' -Budgets $simulationBudgets -ExpectedExit 1 -ExpectDiagnostics -ExpectTeardownAttempt | Out-Null
    Test-FailureRun -Name 'assertion-failure' -Scenario 'assertion-failure' -Budgets $simulationBudgets -ExpectedExit 1 -ExpectDiagnostics -ExpectTeardownAttempt -ChildEnvironment @{ DEV471_FLOW_RUNNER = $flowStub } | Out-Null
    Test-FailureRun -Name 'teardown-failure' -Scenario 'teardown-failure' -Budgets $simulationBudgets -ExpectedExit 1 -ExpectDiagnostics -ExpectTeardownAttempt | Out-Null
    $planted = 'Zq7PlantedSecretValue0042'
    Test-FailureRun -Name 'planted-secret' -Scenario 'planted-secret' -Budgets $simulationBudgets -ExpectedExit 1 -ExpectDiagnostics -ExpectTeardownAttempt -ExpectPlantedSecretClean -PlantedValues @($planted) -RedactionSites @('plain-startup-candidate/compose.log') -RunnerEnvironment @{ DEV471_PLANTED_SECRET = $planted } | Out-Null

    Test-CiCleanupReclaim
    Test-CancellationCleanup
    Test-UploadGuardPlantedSecret
    Test-ExistingRunnerNonregression

    $cases = @($script:FailureCases)
    $failed = @($cases | Where-Object { $_.verdict -ne 'PASS' })
    $summary = [ordered]@{
        dev471              = $true
        mode               = 'failure-path-proofs'
        generatedAt        = (Get-Date).ToUniversalTime().ToString('o')
        simulation         = 'deterministic orchestration double for the docker CLI only; no container was started'
        notAcceptance      = 'real positive Compose phases and the controlled-revert proof remain mandatory'
        validatedBudgets   = 'web/e2e/fixtures/dev471/budgets.json untouched; children received bounded simulation budgets'
        cases              = $cases
        runs               = @($script:FailureRuns)
        ciCleanup          = @($script:CleanupResults)
        verdict            = if ($failed.Count -gt 0) { 'FAIL' } else { 'PASS' }
    }

    $evidenceRoot = Join-Path $RepoRoot ('artifacts/compose-smoke-failure-paths/{0}' -f $RunStamp)
    New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null
    ($summary | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath (Join-Path $evidenceRoot 'failure-path-receipt.json') -Encoding utf8NoBOM
    Set-Content -LiteralPath (Join-Path $evidenceRoot 'failure-path.log') -Value ($script:Log -join "`n") -Encoding utf8NoBOM

    Write-Step ('failure-path evidence written under {0}' -f $evidenceRoot)
    if ($failed.Count -gt 0) {
        Write-Step ('verdict: FAIL ({0} of {1} proofs failed)' -f $failed.Count, $cases.Count)
        return 1
    }
    Write-Step ('verdict: PASS ({0} of {0} proofs held)' -f $cases.Count)
    return 0
}


function Invoke-Smoke {
    $budgets = Read-Budgets
    $script:Deadline = $RunStarted.AddSeconds($budgets.JobCapSeconds)
    Write-Step ('budgets: startup {0}s, assertion {1}s, teardown {2}s, job cap {3}s, whole-test retries 0' -f $budgets.StackStartupSeconds, $budgets.AsyncAssertionSeconds, $budgets.TeardownPerProjectSeconds, $budgets.JobCapSeconds)

    $baseline = Get-CheckoutBaseline
    $evidenceRoot = Join-Path $RepoRoot ('{0}/{1}' -f $EvidenceBase, $RunStamp)
    New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

    $stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('lamuflix-compose-smoke-{0}' -f $RunStamp)
    New-Item -ItemType Directory -Force -Path $stagingRoot | Out-Null
    Write-Step ('disposable staging root {0}' -f $stagingRoot)

    $receipts = [System.Collections.Generic.List[object]]::new()
    $failure = $null
    $commit = $null
    $dockerPrerequisite = [ordered]@{ available = $false; server = $null; detail = $null }
    try {
        $dockerInfo = Invoke-Native -FilePath $Docker -Arguments @('info', '--format', '{{.ServerVersion}}') -TimeoutSeconds 60
        if ($dockerInfo.ExitCode -ne 0) {
            throw ('Docker is not available: {0}' -f (Protect-Text (Join-Output $dockerInfo.StdOut $dockerInfo.StdErr)))
        }
        $dockerPrerequisite.available = $true
        $dockerPrerequisite.server = $dockerInfo.StdOut.Trim()
        Write-Step ('docker server {0}' -f $dockerInfo.StdOut.Trim())

        $commit = (Invoke-Checked -FilePath 'git' -Arguments @('-C', $RepoRoot, 'rev-parse', '--verify', ('{0}^{{commit}}' -f $CandidateRef)) -What 'git rev-parse').StdOut.Trim()
        Write-Step ('candidate source commit {0}' -f $commit)

        $receipts.Add((Invoke-PlainPhase -Mode 'candidate' -Budgets $budgets -StagingRoot $stagingRoot -EvidenceRoot $evidenceRoot -Commit $commit))
        $receipts.Add((Invoke-StubbedFlowPhase -Budgets $budgets -StagingRoot $stagingRoot -EvidenceRoot $evidenceRoot -Commit $commit))
        if ($RegressionProof) {
            $receipts.Add((Invoke-PlainPhase -Mode 'regression' -Budgets $budgets -StagingRoot $stagingRoot -EvidenceRoot $evidenceRoot -Commit $commit))
        }
    }
    catch {
        $failure = $_.Exception.Message
        $dockerPrerequisite.detail = Protect-Text $failure
        Write-Step ('FAIL: {0}' -f (Protect-Text $failure))
    }

    $afterBaseline = $baseline
    try { $afterBaseline = Get-CheckoutBaseline }
    catch { Write-StepWarning 'could not re-read the PR checkout baseline after the run' }
    $checkoutUnchanged = ($afterBaseline -eq $baseline)
    if (-not $checkoutUnchanged -and -not $failure) {
        $failure = 'the PR checkout baseline changed during the run'
    }

    $summary = [ordered]@{
        dev471             = $true
        generatedAt        = (Get-Date).ToUniversalTime().ToString('o')
        candidateCommit    = $commit
        budgets            = $budgets
        regressionProof    = [bool]$RegressionProof
        docker             = $dockerPrerequisite
        checkoutBaseline   = [ordered]@{ before = $baseline; after = $afterBaseline; unchanged = $checkoutUnchanged }
        phases             = $receipts
        verdict            = if ($failure) { 'FAIL' } else { 'PASS' }
        failure            = if ($failure) { Protect-Text $failure } else { $null }
    }
    ($summary | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath (Join-Path $evidenceRoot 'receipt.json') -Encoding utf8NoBOM
    Set-Content -LiteralPath (Join-Path $evidenceRoot 'run.log') -Value ($script:Log -join "`n") -Encoding utf8NoBOM

    if (Test-Path -LiteralPath $stagingRoot) {
        try { Remove-Item -LiteralPath $stagingRoot -Recurse -Force }
        catch { Write-StepWarning ('could not remove the disposable staging root: {0}' -f (Protect-Text $_.Exception.Message)) }
    }

    Write-Step ('evidence written under {0}' -f (Protect-Text $evidenceRoot))
    if ($failure) {
        Write-Step 'verdict: FAIL'
        return 1
    }
    Write-Step 'verdict: PASS'
    return 0
}

try {
    if ($FailurePathProof) {
        $exitCode = Invoke-FailurePathProofs
    }
    else {
        $exitCode = Invoke-Smoke
    }
}
catch {
    Write-Output ('[compose-smoke] FAIL: {0}' -f (Protect-Text $_.Exception.Message))
    $exitCode = 1
}

exit $exitCode

#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [switch]$Help,
    [switch]$RegressionProof,
    [string]$CandidateRef = 'HEAD'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

if ($Help) {
    Write-Output @"
Usage: run-compose-smoke.ps1 [-Help] [-RegressionProof] [-CandidateRef <git-ref>]

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
        '(?im)\b(password|pwd|apikey|api_key|secret|token)\b(\s*[=:]\s*)[^\s"'';]+',
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

function Read-Budgets {
    if (-not (Test-Path -LiteralPath $BudgetPath)) {
        throw ('budget config not found: {0}' -f $BudgetPath)
    }

    $raw = Get-Content -LiteralPath $BudgetPath -Raw | ConvertFrom-Json
    $bounds = [ordered]@{
        stackStartupSeconds       = @(60, 3600)
        asyncAssertionSeconds     = @(10, 600)
        teardownPerProjectSeconds = @(30, 900)
        jobCapSeconds             = @(600, 14400)
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
        $run = Invoke-Native -FilePath 'node' -Arguments @($runner) -TimeoutSeconds (Get-StepTimeout ($Budgets.StackStartupSeconds * 2) $Budgets) -WorkingDirectory $source
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

function Invoke-Smoke {
    $budgets = Read-Budgets
    $script:Deadline = $RunStarted.AddSeconds($budgets.JobCapSeconds)
    Write-Step ('budgets: startup {0}s, assertion {1}s, teardown {2}s, job cap {3}s, whole-test retries 0' -f $budgets.StackStartupSeconds, $budgets.AsyncAssertionSeconds, $budgets.TeardownPerProjectSeconds, $budgets.JobCapSeconds)

    $dockerInfo = Invoke-Native -FilePath $Docker -Arguments @('info', '--format', '{{.ServerVersion}}') -TimeoutSeconds 60
    if ($dockerInfo.ExitCode -ne 0) {
        throw ('Docker is not available: {0}' -f (Protect-Text (Join-Output $dockerInfo.StdOut $dockerInfo.StdErr)))
    }
    Write-Step ('docker server {0}' -f $dockerInfo.StdOut.Trim())

    $commit = (Invoke-Checked -FilePath 'git' -Arguments @('-C', $RepoRoot, 'rev-parse', '--verify', ('{0}^{{commit}}' -f $CandidateRef)) -What 'git rev-parse').StdOut.Trim()
    Write-Step ('candidate source commit {0}' -f $commit)

    $baseline = Get-CheckoutBaseline
    $evidenceRoot = Join-Path $RepoRoot ('artifacts/compose-smoke/{0}' -f $RunStamp)
    New-Item -ItemType Directory -Force -Path $evidenceRoot | Out-Null

    $stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('lamuflix-compose-smoke-{0}' -f $RunStamp)
    New-Item -ItemType Directory -Force -Path $stagingRoot | Out-Null
    Write-Step ('disposable staging root {0}' -f $stagingRoot)

    $receipts = [System.Collections.Generic.List[object]]::new()
    $failure = $null
    try {
        $receipts.Add((Invoke-PlainPhase -Mode 'candidate' -Budgets $budgets -StagingRoot $stagingRoot -EvidenceRoot $evidenceRoot -Commit $commit))
        $receipts.Add((Invoke-StubbedFlowPhase -Budgets $budgets -StagingRoot $stagingRoot -EvidenceRoot $evidenceRoot -Commit $commit))
        if ($RegressionProof) {
            $receipts.Add((Invoke-PlainPhase -Mode 'regression' -Budgets $budgets -StagingRoot $stagingRoot -EvidenceRoot $evidenceRoot -Commit $commit))
        }
    }
    catch {
        $failure = $_.Exception.Message
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
    $exitCode = Invoke-Smoke
}
catch {
    Write-Output ('[compose-smoke] FAIL: {0}' -f (Protect-Text $_.Exception.Message))
    $exitCode = 1
}

exit $exitCode

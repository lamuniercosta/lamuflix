#!/usr/bin/env pwsh
# Deterministic orchestration double for the DEV-471 runner failure-path proofs.
# It stands in for the docker CLI only. It never contacts a Docker daemon and never
# starts a container, so it exercises the runner's failure handling cheaply and
# repeatably. It is NOT acceptance evidence: the real positive Compose phases and the
# controlled-revert proof stay mandatory (brief.md Approach step 5).
#
# Scenario: DEV471_FAKE_DOCKER_SCENARIO
#   unavailable       - docker info fails, as when no daemon is running
#   startup-timeout   - dependencies are healthy but the api never becomes healthy
#   assertion-failure - the api is healthy, so the full-flow assertion is what fails
#   teardown-failure  - the phase reaches teardown and the project cannot be removed
#   planted-secret    - the api never becomes healthy and its logs carry planted secrets
# Every invocation is appended to DEV471_FAKE_DOCKER_LOG when that variable is set.

# $args carries the docker command line verbatim. No param block: docker flags
# such as -p must not be bound as PowerShell parameters.

$ErrorActionPreference = 'Stop'
$Arguments = @($args | ForEach-Object { [string]$_ })
$scenario = if ($env:DEV471_FAKE_DOCKER_SCENARIO) { $env:DEV471_FAKE_DOCKER_SCENARIO } else { 'unavailable' }
$stateDir = if ($env:DEV471_FAKE_DOCKER_STATE) { $env:DEV471_FAKE_DOCKER_STATE } else { Join-Path ([System.IO.Path]::GetTempPath()) 'dev471-fake-docker' }
New-Item -ItemType Directory -Force -Path $stateDir | Out-Null

if ($env:DEV471_FAKE_DOCKER_LOG) {
    Add-Content -LiteralPath $env:DEV471_FAKE_DOCKER_LOG -Value ($Arguments -join ' ') -Encoding utf8NoBOM
}

function Write-Out { param([string]$Text) [Console]::Out.WriteLine($Text) }
function Write-Err { param([string]$Text) [Console]::Error.WriteLine($Text) }

function Get-ComposeProject {
    for ($index = 0; $index -lt $Arguments.Count; $index++) {
        if ($Arguments[$index] -eq '-p' -and ($index + 1) -lt $Arguments.Count) { return $Arguments[$index + 1] }
    }
    return 'dev471-fake-project'
}

function Get-ComposeSubcommand {
    $index = 1
    while ($index -lt $Arguments.Count) {
        $value = $Arguments[$index]
        if ($value -in @('-p', '-f', '--env-file', '--project-directory')) { $index += 2; continue }
        if ($value.StartsWith('-')) { $index++; continue }
        return $value
    }
    return $null
}

function Get-TrailingService {
    for ($index = $Arguments.Count - 1; $index -ge 0; $index--) {
        $value = $Arguments[$index]
        if ($value.StartsWith('-')) { break }
        if ($value -in @('ps', 'up', 'logs', 'down', 'build', 'inspect')) { break }
        return $value
    }
    return $null
}

function Get-ApiHealth {
    switch ($scenario) {
        'unavailable' { return 'running|starting|0' }
        'startup-timeout' { return 'running|starting|0' }
        'planted-secret' { return 'running|starting|0' }
        default { return 'running|healthy|0' }
    }
}

function Get-InspectLine {
    param([string]$Container)

    if ($Container -match 'ollama-model-init') { return 'exited|none|0' }
    if ($Container -match 'postgres|rabbitmq|ollama') { return 'running|healthy|0' }
    if ($Container -match 'api') { return (Get-ApiHealth) }
    return 'running|none|0'
}

if ($scenario -eq 'unavailable') {
    Write-Err 'Cannot connect to the Docker daemon at unix:///var/run/docker.sock. Is the docker daemon running?'
    exit 1
}

function Get-FilterProject {
    foreach ($argument in $Arguments) {
        if ($argument -match '^label=com\.docker\.compose\.project=(.+)$') { return $Matches[1] }
    }
    return $null
}

$primary = if ($Arguments.Count -gt 0) { $Arguments[0] } else { '' }
$project = Get-ComposeProject

switch ($primary) {
    'info' {
        Write-Out '29.9.3-fake'
        exit 0
    }
    'ps' {
        $filtered = Get-FilterProject
        if (-not $filtered) { Write-Out ''; exit 0 }
        Write-Out ('{0}-api-1' -f $filtered)
        exit 0
    }
    'inspect' {
        Write-Out (Get-InspectLine -Container $Arguments[$Arguments.Count - 1])
        exit 0
    }
    'rm' {
        if ($scenario -eq 'teardown-failure') {
            Write-Err 'Error response from daemon: removal of container failed: device or resource busy'
            exit 1
        }
        exit 0
    }
    'network' {
        $filtered = Get-FilterProject
        if ($filtered -and ($Arguments -contains 'ls')) {
            if ($scenario -eq 'teardown-failure') { Write-Out ('{0}-default' -f $filtered); exit 0 }
        }
        Write-Out ''
        exit 0
    }
    'volume' {
        $filtered = Get-FilterProject
        if ($filtered -and ($Arguments -contains 'ls')) {
            if ($scenario -eq 'teardown-failure') { Write-Out ('{0}-ollama-models' -f $filtered); exit 0 }
        }
        Write-Out ''
        exit 0
    }
    'compose' { }
    default {
        Write-Out ''
        exit 0
    }
}

$subcommand = Get-ComposeSubcommand
$service = Get-TrailingService

switch ($subcommand) {
    'build' {
        if ($env:DEV471_FAKE_DOCKER_STALL) { Start-Sleep -Seconds ([int]$env:DEV471_FAKE_DOCKER_STALL) }
        Write-Out '#1 [internal] load build definition'
        Write-Out 'naming to docker.io/library/dev471-fake-api:latest done'
        exit 0
    }
    'up' {
        Write-Out '[+] Running 4/4'
        Write-Out (' - Container {0}-postgres-1  Started' -f $project)
        Write-Out (' - Container {0}-rabbitmq-1  Started' -f $project)
        Write-Out (' - Container {0}-ollama-1    Started' -f $project)
        Write-Out (' - Container {0}-api-1       Started' -f $project)
        exit 0
    }
    'ps' {
        if ($service) { Write-Out ('{0}-{1}-1' -f $project, $service) }
        else { Write-Out ('{0}-api-1' -f $project) }
        exit 0
    }
    'logs' {
        $planted = if ($env:DEV471_PLANTED_SECRET) { $env:DEV471_PLANTED_SECRET } else { 'unset-planted-value' }
        switch ($scenario) {
            { $_ -in @('startup-timeout', 'planted-secret') } {
                Write-Out ('{0}-api-1  | info: awaiting dependency readiness' -f $project)
                Write-Out ('{0}-api-1  | warn: startup probe still pending' -f $project)
                if ($scenario -eq 'planted-secret') {
                    Write-Out ('{0}-api-1  | debug: env POSTGRES_PASSWORD={1}' -f $project, $planted)
                    Write-Out ('{0}-api-1  | debug: env RABBITMQ_PASSWORD={1}' -f $project, $planted)
                    Write-Out ('{0}-api-1  | debug: env Omdb__ApiKey={1}' -f $project, $planted)
                    Write-Out ('{0}-api-1  | debug: env ConnectionStrings__DefaultConnection=Host=postgres;Database=lamuflix;Username=lamuflix_{1};Password={1}' -f $project, $planted)
                }
            }
            'assertion-failure' {
                Write-Out ('{0}-api-1  | info: startup complete' -f $project)
                Write-Out ('{0}-api-1  | info: serving requests' -f $project)
            }
            default {
                Write-Out ('{0}-api-1  | info: running' -f $project)
            }
        }
        exit 0
    }
    'down' {
        if ($scenario -eq 'teardown-failure') {
            Write-Err 'Error response from daemon: removal of container dev471-fake failed: device or resource busy'
            exit 1
        }
        Write-Out '[+] Running 4/4'
        Write-Out (' - Container {0}-api-1  Removed' -f $project)
        exit 0
    }
    default { exit 0 }
}

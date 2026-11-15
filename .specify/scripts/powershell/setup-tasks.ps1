#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [switch]$Json,
    [switch]$Help
)

$ErrorActionPreference = 'Stop'

if ($Help) {
    Write-Output "Usage: setup-tasks.ps1 [-Json] [-Help]"
    exit 0
}

# Source common functions
. "$PSScriptRoot/common.ps1"

# Get feature paths and validate branch
$paths = Get-FeaturePathsEnv

# If feature.json pins an existing feature directory, branch naming is not required.
if (-not (Test-FeatureJsonMatchesFeatureDir -RepoRoot $paths.REPO_ROOT -ActiveFeatureDir $paths.FEATURE_DIR)) {
    if (-not (Test-FeatureBranch -Branch $paths.CURRENT_BRANCH -HasGit $paths.HAS_GIT)) {
        exit 1
    }
}

$ticketId = Get-ArtifactTicketIdFromName -Branch $paths.CURRENT_BRANCH -FeatureDir $paths.FEATURE_DIR
$privateTicketDir = $null
$planPath = $paths.IMPL_PLAN
$tasksPath = $paths.TASKS
if (-not [string]::IsNullOrWhiteSpace($ticketId)) {
    try {
        $artifactPaths = Get-ArtifactPathsEnv -TicketId $ticketId
        $privateTicketDir = $artifactPaths.PRIVATE_TICKET_DIR
        if ($artifactPaths.PRIVATE_AVAILABLE) {
            if (Test-Path -LiteralPath $artifactPaths.PRIVATE_PLAN -PathType Leaf) { $planPath = $artifactPaths.PRIVATE_PLAN }
            if (Test-Path -LiteralPath $artifactPaths.PRIVATE_TASKS -PathType Leaf) { $tasksPath = $artifactPaths.PRIVATE_TASKS }
        } elseif (-not (Test-Path -LiteralPath $paths.IMPL_PLAN -PathType Leaf)) {
            $privateTicketDir = New-ArtifactTicketWorkspace -TicketId $ticketId -Subdirectories @()
            $planPath = Join-Path $privateTicketDir 'plan.md'
            $tasksPath = Join-Path $privateTicketDir 'tasks.md'
        }
    } catch {
        [Console]::Error.WriteLine("ERROR: $($_.Exception.Message)")
        exit 1
    }
}

if (-not (Test-Path -LiteralPath $planPath -PathType Leaf)) {
    [Console]::Error.WriteLine("ERROR: plan.md not found in $($paths.FEATURE_DIR) or the private ticket directory")
    [Console]::Error.WriteLine("Run /speckit.plan first to create the implementation plan.")
    exit 1
}

if (-not (Test-Path -LiteralPath $paths.FEATURE_SPEC -PathType Leaf)) {
    [Console]::Error.WriteLine("ERROR: spec.md not found in $($paths.FEATURE_DIR)")
    [Console]::Error.WriteLine("Run /speckit.specify first to create the feature structure.")
    exit 1
}

# Build available docs list
$docs = @()
if (Test-Path $paths.RESEARCH) { $docs += 'research.md' }
if (Test-Path $paths.DATA_MODEL) { $docs += 'data-model.md' }
if ((Test-Path $paths.CONTRACTS_DIR) -and (Get-ChildItem -Path $paths.CONTRACTS_DIR -ErrorAction SilentlyContinue | Select-Object -First 1)) {
    $docs += 'contracts/'
}
if (Test-Path $paths.QUICKSTART) { $docs += 'quickstart.md' }

# Resolve tasks template through override stack
$tasksTemplate = Resolve-Template -TemplateName 'tasks-template' -RepoRoot $paths.REPO_ROOT
if (-not $tasksTemplate -or -not (Test-Path -LiteralPath $tasksTemplate -PathType Leaf)) {
    $expectedCoreTemplate = Join-Path $paths.REPO_ROOT '.specify/templates/tasks-template.md'
    [Console]::Error.WriteLine("ERROR: Tasks template not found for repository root: $($paths.REPO_ROOT)`nTemplate resolution order: overrides -> presets -> extensions -> core.`nExpected shared/core template location: $expectedCoreTemplate`nTo continue, verify whether 'tasks-template.md' is available in '.specify/templates/overrides/', preset templates, extension templates, or restore the shared/core templates (for example by re-running 'specify init') so that '.specify/templates/tasks-template.md' exists.")
    exit 1
}
$tasksTemplate = (Resolve-Path -LiteralPath $tasksTemplate).Path

# Output results
if ($Json) {
    [PSCustomObject]@{
        FEATURE_DIR    = $paths.FEATURE_DIR
        AVAILABLE_DOCS = $docs
        TASKS_TEMPLATE = $tasksTemplate
        TICKET_ID      = $ticketId
        PRIVATE_TICKET_DIR = $privateTicketDir
        IMPL_PLAN      = $planPath
        TASKS_PATH     = $tasksPath
    } | ConvertTo-Json -Compress
} else {
    Write-Output "FEATURE_DIR: $($paths.FEATURE_DIR)"
    Write-Output "TASKS_TEMPLATE: $(if ($tasksTemplate) { $tasksTemplate } else { 'not found' })"
    Write-Output "IMPL_PLAN: $planPath"
    Write-Output "TASKS_PATH: $tasksPath"
    if ($privateTicketDir) { Write-Output "PRIVATE_TICKET_DIR: $privateTicketDir" }
    Write-Output "AVAILABLE_DOCS:"
    Test-FileExists -Path $paths.RESEARCH -Description 'research.md' | Out-Null
    Test-FileExists -Path $paths.DATA_MODEL -Description 'data-model.md' | Out-Null
    Test-DirHasFiles -Path $paths.CONTRACTS_DIR -Description 'contracts/' | Out-Null
    Test-FileExists -Path $paths.QUICKSTART -Description 'quickstart.md' | Out-Null
}

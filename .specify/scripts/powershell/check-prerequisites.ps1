#!/usr/bin/env pwsh

# Consolidated prerequisite checking script (PowerShell)
#
# This script provides unified prerequisite checking for Spec-Driven Development workflow.
# It replaces the functionality previously spread across multiple scripts.
#
# Usage: ./check-prerequisites.ps1 [OPTIONS]
#
# OPTIONS:
#   -Json               Output in JSON format
#   -RequireTasks       Require tasks.md to exist (for implementation phase)
#   -IncludeTasks       Include tasks.md in AVAILABLE_DOCS list
#   -PathsOnly          Only output path variables (no validation)
#   -RequirePrivate     Require the ticket's private artifact storage and bootstrap seed
#   -Help, -h           Show help message

[CmdletBinding()]
param(
    [switch]$Json,
    [switch]$RequireTasks,
    [switch]$IncludeTasks,
    [switch]$PathsOnly,
    [switch]$RequirePrivate,
    [switch]$Help
)

$ErrorActionPreference = 'Stop'

# Show help if requested
if ($Help) {
    Write-Output @"
Usage: check-prerequisites.ps1 [OPTIONS]

Consolidated prerequisite checking for Spec-Driven Development workflow.

OPTIONS:
  -Json               Output in JSON format
  -RequireTasks       Require tasks.md to exist (for implementation phase)
  -IncludeTasks       Include tasks.md in AVAILABLE_DOCS list
  -PathsOnly          Only output path variables (no prerequisite validation)
  -RequirePrivate     Require the ticket private artifact storage and verified bootstrap seed
  -Help, -h           Show this help message

EXAMPLES:
  # Check task prerequisites (plan.md required)
  .\check-prerequisites.ps1 -Json
  
  # Check implementation prerequisites (plan.md + tasks.md required)
  .\check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks
  
  # Get feature paths only (no validation)
  .\check-prerequisites.ps1 -PathsOnly

"@
    exit 0
}

# Source common functions
. "$PSScriptRoot/common.ps1"

# Get feature paths and validate branch
$paths = Get-FeaturePathsEnv

if (-not (Test-FeatureBranch -Branch $paths.CURRENT_BRANCH -HasGit:$paths.HAS_GIT)) { 
    exit 1 
}

# Public/private artifact resolution. Discovery is read-only: it never creates the private root
# and never migrates an operational record. Missing private storage only matters to stages that
# declare they need private inputs.
$artifactPaths = $null
$artifactProblem = $null
try {
    $artifactPaths = Get-ArtifactPathsEnv -TicketId (Get-ArtifactTicketIdFromName -Branch $paths.CURRENT_BRANCH -FeatureDir $paths.FEATURE_DIR)
} catch {
    $artifactProblem = $_.Exception.Message
}

# A stage that requires private inputs fails closed here rather than reading a public copy.
if ($RequirePrivate) {
    if ($artifactProblem) {
        Write-Output "ERROR: $artifactProblem"
        exit 1
    }
    try {
        $artifactPaths = Get-ArtifactPathsEnv -TicketId $artifactPaths.TICKET_ID -RequirePrivate
    } catch {
        Write-Output "ERROR: $($_.Exception.Message)"
        exit 1
    }
}

# If paths-only mode, output paths and exit (support combined -Json -PathsOnly)
if ($PathsOnly) {
    if ($Json) {
        [PSCustomObject]@{
            REPO_ROOT    = $paths.REPO_ROOT
            BRANCH       = $paths.CURRENT_BRANCH
            FEATURE_DIR  = $paths.FEATURE_DIR
            FEATURE_SPEC = $paths.FEATURE_SPEC
            IMPL_PLAN    = $paths.IMPL_PLAN
            TASKS        = $paths.TASKS
            TICKET_ID    = if ($artifactPaths) { $artifactPaths.TICKET_ID } else { $null }
            PRIVATE_ROOT = if ($artifactPaths) { $artifactPaths.PRIVATE_ROOT } else { $null }
            PRIVATE_TICKET_DIR = if ($artifactPaths) { $artifactPaths.PRIVATE_TICKET_DIR } else { $null }
            PRIVATE_AVAILABLE  = if ($artifactPaths) { $artifactPaths.PRIVATE_AVAILABLE } else { $false }
            PRIVATE_SEED_VERIFIED = if ($artifactPaths) { $artifactPaths.PRIVATE_SEED_VERIFIED } else { $false }
        } | ConvertTo-Json -Compress
    } else {
        Write-Output "REPO_ROOT: $($paths.REPO_ROOT)"
        Write-Output "BRANCH: $($paths.CURRENT_BRANCH)"
        Write-Output "FEATURE_DIR: $($paths.FEATURE_DIR)"
        Write-Output "FEATURE_SPEC: $($paths.FEATURE_SPEC)"
        Write-Output "IMPL_PLAN: $($paths.IMPL_PLAN)"
        Write-Output "TASKS: $($paths.TASKS)"
        if ($artifactPaths) {
            Write-Output "TICKET_ID: $($artifactPaths.TICKET_ID)"
            Write-Output "PRIVATE_ROOT: $($artifactPaths.PRIVATE_ROOT)"
            Write-Output "PRIVATE_TICKET_DIR: $($artifactPaths.PRIVATE_TICKET_DIR)"
            Write-Output "PRIVATE_AVAILABLE: $($artifactPaths.PRIVATE_AVAILABLE)"
            Write-Output "PRIVATE_SEED_VERIFIED: $($artifactPaths.PRIVATE_SEED_VERIFIED)"
        }
    }
    exit 0
}

# Validate required directories and files. Operational records (plan.md, tasks.md) resolve to the
# private ticket directory when private storage holds them; the public path stays valid only for a
# ticket that has not been cut over yet. Neither route creates or migrates anything.
$privatePlan = if ($artifactPaths -and $artifactPaths.PRIVATE_AVAILABLE) { $artifactPaths.PRIVATE_PLAN } else { $null }
$privateTasks = if ($artifactPaths -and $artifactPaths.PRIVATE_AVAILABLE) { $artifactPaths.PRIVATE_TASKS } else { $null }
$planPath = if ($privatePlan -and (Test-Path -LiteralPath $privatePlan -PathType Leaf)) { $privatePlan } else { $paths.IMPL_PLAN }
$tasksPath = if ($privateTasks -and (Test-Path -LiteralPath $privateTasks -PathType Leaf)) { $privateTasks } else { $paths.TASKS }

if (-not (Test-Path $paths.FEATURE_DIR -PathType Container)) {
    Write-Output "ERROR: Feature directory not found: $($paths.FEATURE_DIR)"
    Write-Output "Run /speckit.specify first to create the feature structure."
    exit 1
}

if (-not (Test-Path -LiteralPath $planPath -PathType Leaf)) {
    Write-Output "ERROR: plan.md not found in $($paths.FEATURE_DIR) or the private ticket directory."
    Write-Output "Run /speckit.plan first to create the implementation plan."
    exit 1
}

# Check for tasks.md if required
if ($RequireTasks -and -not (Test-Path -LiteralPath $tasksPath -PathType Leaf)) {
    Write-Output "ERROR: tasks.md not found in $($paths.FEATURE_DIR) or the private ticket directory."
    Write-Output "Run /speckit.tasks first to create the task list."
    exit 1
}

# Build list of available documents
$docs = @()

# Always check these optional docs
if (Test-Path $paths.RESEARCH) { $docs += 'research.md' }
if (Test-Path $paths.DATA_MODEL) { $docs += 'data-model.md' }

# Check contracts directory (only if it exists and has files)
if ((Test-Path $paths.CONTRACTS_DIR) -and (Get-ChildItem -Path $paths.CONTRACTS_DIR -ErrorAction SilentlyContinue | Select-Object -First 1)) { 
    $docs += 'contracts/' 
}

if (Test-Path $paths.QUICKSTART) { $docs += 'quickstart.md' }

# Include tasks.md if requested and it exists
if ($IncludeTasks -and (Test-Path -LiteralPath $tasksPath)) { 
    $docs += 'tasks.md' 
}

# Output results
if ($Json) {
    # JSON output
    [PSCustomObject]@{ 
        FEATURE_DIR = $paths.FEATURE_DIR
        AVAILABLE_DOCS = $docs
        IMPL_PLAN = $planPath
        TASKS = $tasksPath
        TICKET_ID = if ($artifactPaths) { $artifactPaths.TICKET_ID } else { $null }
        PRIVATE_TICKET_DIR = if ($artifactPaths) { $artifactPaths.PRIVATE_TICKET_DIR } else { $null }
    } | ConvertTo-Json -Compress
} else {
    # Text output
    Write-Output "FEATURE_DIR:$($paths.FEATURE_DIR)"
    Write-Output "AVAILABLE_DOCS:"
    
    # Show status of each potential document
    Test-FileExists -Path $paths.RESEARCH -Description 'research.md' | Out-Null
    Test-FileExists -Path $paths.DATA_MODEL -Description 'data-model.md' | Out-Null
    Test-DirHasFiles -Path $paths.CONTRACTS_DIR -Description 'contracts/' | Out-Null
    Test-FileExists -Path $paths.QUICKSTART -Description 'quickstart.md' | Out-Null
    
    if ($IncludeTasks) {
        Test-FileExists -Path $tasksPath -Description 'tasks.md' | Out-Null
    }
}

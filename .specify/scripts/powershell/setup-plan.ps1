#!/usr/bin/env pwsh
# Setup implementation plan for a feature

[CmdletBinding()]
param(
    [switch]$Json,
    [switch]$Help
)

$ErrorActionPreference = 'Stop'

# Show help if requested
if ($Help) {
    Write-Output "Usage: ./setup-plan.ps1 [-Json] [-Help]"
    Write-Output "  -Json     Output results in JSON format"
    Write-Output "  -Help     Show this help message"
    exit 0
}

# Load common functions
. "$PSScriptRoot/common.ps1"

# Get all paths and variables from common functions
$paths = Get-FeaturePathsEnv

# If feature.json pins an existing feature directory, branch naming is not required.
if (-not (Test-FeatureJsonMatchesFeatureDir -RepoRoot $paths.REPO_ROOT -ActiveFeatureDir $paths.FEATURE_DIR)) {
    if (-not (Test-FeatureBranch -Branch $paths.CURRENT_BRANCH -HasGit $paths.HAS_GIT)) {
        exit 1
    }
}

# plan.md is an operational record: it lands in the private ticket directory once the ticket has
# private storage, and a brand-new ticket gets its workspace created here. A ticket that has not
# been cut over still keeps its plan under the public specs/ tree. Neither route falls back to the
# public tree when private storage is missing or unusable: that fails closed with guidance.
$planPath = $paths.IMPL_PLAN
$ticketId = Get-ArtifactTicketIdFromName -Branch $paths.CURRENT_BRANCH -FeatureDir $paths.FEATURE_DIR
$privateTicketDir = $null
if (-not [string]::IsNullOrWhiteSpace($ticketId)) {
    try {
        $artifactPaths = Get-ArtifactPathsEnv -TicketId $ticketId
        $privateTicketDir = $artifactPaths.PRIVATE_TICKET_DIR
        if ($artifactPaths.PRIVATE_AVAILABLE) {
            $planPath = $artifactPaths.PRIVATE_PLAN
        } elseif (-not (Test-Path -LiteralPath $paths.IMPL_PLAN -PathType Leaf)) {
            $privateTicketDir = New-ArtifactTicketWorkspace -TicketId $ticketId -Subdirectories @()
            $planPath = Join-Path $privateTicketDir 'plan.md'
        }
    } catch {
        [Console]::Error.WriteLine("ERROR: $($_.Exception.Message)")
        exit 1
    }
}

# Ensure the owning directory exists
$planParent = [System.IO.Path]::GetDirectoryName($planPath)
if (-not [string]::IsNullOrWhiteSpace($planParent)) {
    New-Item -ItemType Directory -Path $planParent -Force | Out-Null
}

# Copy plan template if it exists, otherwise note it or create empty file
$template = Resolve-Template -TemplateName 'plan-template' -RepoRoot $paths.REPO_ROOT
$planText = $null
if ($template -and (Test-Path $template)) {
    # Read the template content and write it to the implementation plan file with UTF-8 encoding without BOM
    $planText = [System.IO.File]::ReadAllText($template)
} else {
    Write-Warning "Plan template not found"
}

$planIsPrivate = $privateTicketDir -and (-not [string]::IsNullOrWhiteSpace($privateTicketDir)) -and
    (Test-ArtifactPathInside -Path $planPath -Container $privateTicketDir)
if ($planIsPrivate) {
    $planText = if ($null -ne $planText) { $planText } else { '' }
    $worktreeId = (& git -C $paths.REPO_ROOT rev-parse --show-toplevel 2>$null | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($worktreeId)) { $worktreeId = $paths.REPO_ROOT }
    $commitSha = (& git -C $paths.REPO_ROOT rev-parse HEAD 2>$null | Out-String).Trim()
    if ($commitSha -notmatch '^[0-9a-fA-F]{40}$') { $commitSha = $null }
    try {
        Invoke-ArtifactTicketWrite -TicketDir $privateTicketDir -Owner 'setup-plan' -WorktreeId $worktreeId -CommitSha $commitSha -Action {
            Write-ArtifactRecord -TicketDir $privateTicketDir -RelativePath 'plan.md' -Text $planText
        } | Out-Null
    } catch {
        [Console]::Error.WriteLine("ERROR: $($_.Exception.Message)")
        exit 1
    }
} elseif ($null -ne $planText) {
    [System.IO.File]::WriteAllText($planPath, $planText, (New-Object System.Text.UTF8Encoding($false)))
} else {
    # Create a basic plan file if template doesn't exist
    New-Item -ItemType File -Path $planPath -Force | Out-Null
}

# Output results
if ($Json) {
    $result = [PSCustomObject]@{ 
        FEATURE_SPEC = $paths.FEATURE_SPEC
        IMPL_PLAN = $planPath
        SPECS_DIR = $paths.FEATURE_DIR
        BRANCH = $paths.CURRENT_BRANCH
        HAS_GIT = $paths.HAS_GIT
        TICKET_ID = $ticketId
        PRIVATE_TICKET_DIR = $privateTicketDir
    }
    $result | ConvertTo-Json -Compress
} else {
    Write-Output "FEATURE_SPEC: $($paths.FEATURE_SPEC)"
    Write-Output "IMPL_PLAN: $planPath"
    Write-Output "SPECS_DIR: $($paths.FEATURE_DIR)"
    Write-Output "BRANCH: $($paths.CURRENT_BRANCH)"
    Write-Output "HAS_GIT: $($paths.HAS_GIT)"
    if ($privateTicketDir) { Write-Output "PRIVATE_TICKET_DIR: $privateTicketDir" }
}

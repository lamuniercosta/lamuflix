#!/usr/bin/env pwsh
# Common PowerShell functions analogous to common.sh

# Find repository root by searching upward for .specify directory
# This is the primary marker for spec-kit projects
function Find-SpecifyRoot {
    param([string]$StartDir = (Get-Location).Path)

    # Normalize to absolute path to prevent issues with relative paths
    # Use -LiteralPath to handle paths with wildcard characters ([, ], *, ?)
    $resolved = Resolve-Path -LiteralPath $StartDir -ErrorAction SilentlyContinue
    $current = if ($resolved) { $resolved.Path } else { $null }
    if (-not $current) { return $null }

    while ($true) {
        if (Test-Path -LiteralPath (Join-Path $current ".specify") -PathType Container) {
            return $current
        }
        $parent = Split-Path $current -Parent
        if ([string]::IsNullOrEmpty($parent) -or $parent -eq $current) {
            return $null
        }
        $current = $parent
    }
}

# Get repository root, prioritizing .specify directory over git
# This prevents using a parent git repo when spec-kit is initialized in a subdirectory
function Get-RepoRoot {
    # First, look for .specify directory (spec-kit's own marker)
    $specifyRoot = Find-SpecifyRoot
    if ($specifyRoot) {
        return $specifyRoot
    }

    # Fallback to git if no .specify found
    try {
        $result = git rev-parse --show-toplevel 2>$null
        if ($LASTEXITCODE -eq 0) {
            return $result
        }
    } catch {
        # Git command failed
    }

    # Final fallback to script location for non-git repos
    # Use -LiteralPath to handle paths with wildcard characters
    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "../../..")).Path
}

function Get-CurrentBranch {
    # First check if SPECIFY_FEATURE environment variable is set
    if ($env:SPECIFY_FEATURE) {
        return $env:SPECIFY_FEATURE
    }

    # Then check git if available at the spec-kit root (not parent)
    $repoRoot = Get-RepoRoot
    if (Test-HasGit) {
        try {
            $result = git -C $repoRoot rev-parse --abbrev-ref HEAD 2>$null
            if ($LASTEXITCODE -eq 0) {
                return $result
            }
        } catch {
            # Git command failed
        }
    }

    # For non-git repos, try to find the latest feature directory
    $specsDir = Join-Path $repoRoot "specs"
    
    if (Test-Path $specsDir) {
        $latestFeature = ""
        $highest = 0
        $latestTimestamp = ""

        Get-ChildItem -Path $specsDir -Directory | ForEach-Object {
            if ($_.Name -match '^(\d{8}-\d{6})-') {
                # Timestamp-based branch: compare lexicographically
                $ts = $matches[1]
                if ($ts -gt $latestTimestamp) {
                    $latestTimestamp = $ts
                    $latestFeature = $_.Name
                }
            } elseif ($_.Name -match '^(\d{3,})-') {
                $num = [long]$matches[1]
                if ($num -gt $highest) {
                    $highest = $num
                    # Only update if no timestamp branch found yet
                    if (-not $latestTimestamp) {
                        $latestFeature = $_.Name
                    }
                }
            }
        }

        if ($latestFeature) {
            return $latestFeature
        }
    }
    
    # Final fallback
    return "main"
}

# Check if we have git available at the spec-kit root level
# Returns true only if git is installed and the repo root is inside a git work tree
# Handles both regular repos (.git directory) and worktrees/submodules (.git file)
function Test-HasGit {
    # First check if git command is available (before calling Get-RepoRoot which may use git)
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        return $false
    }
    $repoRoot = Get-RepoRoot
    # Check if .git exists (directory or file for worktrees/submodules)
    # Use -LiteralPath to handle paths with wildcard characters
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot ".git"))) {
        return $false
    }
    # Verify it's actually a valid git work tree
    try {
        $null = git -C $repoRoot rev-parse --is-inside-work-tree 2>$null
        return ($LASTEXITCODE -eq 0)
    } catch {
        return $false
    }
}

# Strip a single optional path segment (e.g. gitflow "feat/004-name" -> "004-name").
# Only when the full name is exactly two slash-free segments; otherwise returns the raw name.
function Get-SpecKitEffectiveBranchName {
    param([string]$Branch)
    if ($Branch -match '^([^/]+)/([^/]+)$') {
        return $Matches[2]
    }
    return $Branch
}

function Test-FeatureBranch {
    param(
        [string]$Branch,
        [bool]$HasGit = $true
    )
    
    # For non-git repos, we can't enforce branch naming but still provide output
    if (-not $HasGit) {
        Write-Warning "[specify] Warning: Git repository not detected; skipped branch validation"
        return $true
    }

    $raw = $Branch
    $Branch = Get-SpecKitEffectiveBranchName $raw
    
    # Accept sequential prefix (3+ digits) but exclude malformed timestamps
    # Malformed: 7-or-8 digit date + 6-digit time with no trailing slug (e.g. "2026031-143022" or "20260319-143022")
    $hasMalformedTimestamp = ($Branch -match '^[0-9]{7}-[0-9]{6}-') -or ($Branch -match '^(?:\d{7}|\d{8})-\d{6}$')
    $isSequential = ($Branch -match '^[0-9]{3,}-') -and (-not $hasMalformedTimestamp)
    if (-not $isSequential -and $Branch -notmatch '^\d{8}-\d{6}-') {
        [Console]::Error.WriteLine("ERROR: Not on a feature branch. Current branch: $raw")
        [Console]::Error.WriteLine("Feature branches should be named like: 001-feature-name, 1234-feature-name, or 20260319-143022-feature-name")
        return $false
    }
    return $true
}

# True when .specify/feature.json pins an existing feature directory that matches the
# active FEATURE_DIR from Get-FeaturePathsEnv (so /speckit.plan can skip git branch pattern checks).
function Test-FeatureJsonMatchesFeatureDir {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$ActiveFeatureDir
    )

    $featureJson = Join-Path (Join-Path $RepoRoot '.specify') 'feature.json'
    if (-not (Test-Path -LiteralPath $featureJson -PathType Leaf)) {
        return $false
    }

    try {
        $raw = Get-Content -LiteralPath $featureJson -Raw
        $cfg = $raw | ConvertFrom-Json
    } catch {
        return $false
    }

    $fd = $cfg.feature_directory
    if ([string]::IsNullOrWhiteSpace([string]$fd)) {
        return $false
    }

    if (-not [System.IO.Path]::IsPathRooted($fd)) {
        $fd = Join-Path $RepoRoot $fd
    }

    if (-not (Test-Path -LiteralPath $fd -PathType Container)) {
        return $false
    }

    # Resolve both paths to canonical absolute form. Prefer Resolve-Path (follows
    # symlinks and is the canonical PS way); fall back to [Path]::GetFullPath when
    # Resolve-Path can't produce a value. Mirrors the pattern used by Find-SpecifyRoot.
    $resolvedJson = Resolve-Path -LiteralPath $fd -ErrorAction SilentlyContinue
    if ($resolvedJson) {
        $normJson = $resolvedJson.Path
    } else {
        $normJson = [System.IO.Path]::GetFullPath($fd)
    }

    $resolvedActive = Resolve-Path -LiteralPath $ActiveFeatureDir -ErrorAction SilentlyContinue
    if ($resolvedActive) {
        $normActive = $resolvedActive.Path
    } else {
        $normActive = [System.IO.Path]::GetFullPath($ActiveFeatureDir)
    }

    # Use case-insensitive compare only on Windows; POSIX filesystems are case-sensitive.
    # PowerShell 5.1 is Windows-only and does not define $IsWindows, so treat its
    # absence as "we're on Windows".
    if ($null -ne $IsWindows) {
        $onWindows = $IsWindows
    } else {
        $onWindows = $true
    }

    if ($onWindows) {
        $comparison = [System.StringComparison]::OrdinalIgnoreCase
    } else {
        $comparison = [System.StringComparison]::Ordinal
    }

    return [string]::Equals($normJson, $normActive, $comparison)
}

# Resolve specs/<feature-dir> by numeric/timestamp prefix (mirrors scripts/bash/common.sh find_feature_dir_by_prefix).
function Find-FeatureDirByPrefix {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$Branch
    )
    $specsDir = Join-Path $RepoRoot 'specs'
    $branchName = Get-SpecKitEffectiveBranchName $Branch

    $prefix = $null
    if ($branchName -match '^(\d{8}-\d{6})-') {
        $prefix = $Matches[1]
    } elseif ($branchName -match '^(\d{3,})-') {
        $prefix = $Matches[1]
    } else {
        return (Join-Path $specsDir $branchName)
    }

    $dirMatches = @()
    if (Test-Path -LiteralPath $specsDir -PathType Container) {
        $dirMatches = @(Get-ChildItem -LiteralPath $specsDir -Filter "$prefix-*" -Directory -ErrorAction SilentlyContinue)
    }

    if ($dirMatches.Count -eq 0) {
        return (Join-Path $specsDir $branchName)
    }
    if ($dirMatches.Count -eq 1) {
        return $dirMatches[0].FullName
    }
    $names = ($dirMatches | ForEach-Object { $_.Name }) -join ' '
    [Console]::Error.WriteLine("ERROR: Multiple spec directories found with prefix '$prefix': $names")
    [Console]::Error.WriteLine('Please ensure only one spec directory exists per prefix.')
    return $null
}

# Branch-based prefix resolution; mirrors bash get_feature_paths failure (stderr + exit 1).
function Get-FeatureDirFromBranchPrefixOrExit {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$CurrentBranch
    )
    $resolved = Find-FeatureDirByPrefix -RepoRoot $RepoRoot -Branch $CurrentBranch
    if ($null -eq $resolved) {
        [Console]::Error.WriteLine('ERROR: Failed to resolve feature directory')
        exit 1
    }
    return $resolved
}

function Get-FeaturePathsEnv {
    $repoRoot = Get-RepoRoot
    $currentBranch = Get-CurrentBranch
    $hasGit = Test-HasGit

    # Resolve feature directory.  Priority:
    #   1. SPECIFY_FEATURE_DIRECTORY env var (explicit override)
    #   2. .specify/feature.json "feature_directory" key (persisted by /speckit.specify)
    #   3. Branch-name-based prefix lookup (same as scripts/bash/common.sh)
    $featureJson = Join-Path $repoRoot '.specify/feature.json'
    if ($env:SPECIFY_FEATURE_DIRECTORY) {
        $featureDir = $env:SPECIFY_FEATURE_DIRECTORY
        # Normalize relative paths to absolute under repo root
        if (-not [System.IO.Path]::IsPathRooted($featureDir)) {
            $featureDir = Join-Path $repoRoot $featureDir
        }
    } elseif (Test-Path $featureJson) {
        $featureJsonRaw = Get-Content -LiteralPath $featureJson -Raw
        try {
            $featureConfig = $featureJsonRaw | ConvertFrom-Json
        } catch {
            [Console]::Error.WriteLine("ERROR: Failed to parse .specify/feature.json: $_")
            exit 1
        }
        if ($featureConfig.feature_directory) {
            $featureDir = $featureConfig.feature_directory
            # Normalize relative paths to absolute under repo root
            if (-not [System.IO.Path]::IsPathRooted($featureDir)) {
                $featureDir = Join-Path $repoRoot $featureDir
            }
        } else {
            $featureDir = Get-FeatureDirFromBranchPrefixOrExit -RepoRoot $repoRoot -CurrentBranch $currentBranch
        }
    } else {
        $featureDir = Get-FeatureDirFromBranchPrefixOrExit -RepoRoot $repoRoot -CurrentBranch $currentBranch
    }
    
    [PSCustomObject]@{
        REPO_ROOT     = $repoRoot
        CURRENT_BRANCH = $currentBranch
        HAS_GIT       = $hasGit
        FEATURE_DIR   = $featureDir
        FEATURE_SPEC  = Join-Path $featureDir 'spec.md'
        IMPL_PLAN     = Join-Path $featureDir 'plan.md'
        TASKS         = Join-Path $featureDir 'tasks.md'
        RESEARCH      = Join-Path $featureDir 'research.md'
        DATA_MODEL    = Join-Path $featureDir 'data-model.md'
        QUICKSTART    = Join-Path $featureDir 'quickstart.md'
        CONTRACTS_DIR = Join-Path $featureDir 'contracts'
        PUBLIC_FEATURE_DIR = $featureDir
        PUBLIC_SPEC        = Join-Path $featureDir 'spec.md'
        PUBLIC_CONCLUSIONS = Join-Path $featureDir 'CONCLUSIONS.md'
        PUBLIC_ASSUMPTIONS = Join-Path $featureDir 'ASSUMPTIONS.md'
    }
}

function Test-FileExists {
    param([string]$Path, [string]$Description)
    if (Test-Path -Path $Path -PathType Leaf) {
        Write-Output "  ✓ $Description"
        return $true
    } else {
        Write-Output "  ✗ $Description"
        return $false
    }
}

function Test-DirHasFiles {
    param([string]$Path, [string]$Description)
    if ((Test-Path -Path $Path -PathType Container) -and (Get-ChildItem -Path $Path -ErrorAction SilentlyContinue | Where-Object { -not $_.PSIsContainer } | Select-Object -First 1)) {
        Write-Output "  ✓ $Description"
        return $true
    } else {
        Write-Output "  ✗ $Description"
        return $false
    }
}

# Find a usable Python 3 executable (python3, python, or py -3).
# Returns the command/arguments as an array, or $null if none found.
function Get-Python3Command {
    if (Get-Command python3 -ErrorAction SilentlyContinue) { return @('python3') }
    if (Get-Command python -ErrorAction SilentlyContinue) {
        $ver = & python --version 2>&1
        if ($ver -match 'Python 3') { return @('python') }
    }
    if (Get-Command py -ErrorAction SilentlyContinue) {
        $ver = & py -3 --version 2>&1
        if ($ver -match 'Python 3') { return @('py', '-3') }
    }
    return $null
}

# Resolve a template name to a file path using the priority stack:
#   1. .specify/templates/overrides/
#   2. .specify/presets/<preset-id>/templates/ (sorted by priority from .registry)
#   3. .specify/extensions/<ext-id>/templates/
#   4. .specify/templates/ (core)
function Resolve-Template {
    param(
        [Parameter(Mandatory=$true)][string]$TemplateName,
        [Parameter(Mandatory=$true)][string]$RepoRoot
    )

    $base = Join-Path $RepoRoot '.specify/templates'

    # Priority 1: Project overrides
    $override = Join-Path $base "overrides/$TemplateName.md"
    if (Test-Path $override) { return $override }

    # Priority 2: Installed presets (sorted by priority from .registry)
    $presetsDir = Join-Path $RepoRoot '.specify/presets'
    if (Test-Path $presetsDir) {
        $registryFile = Join-Path $presetsDir '.registry'
        $sortedPresets = @()
        if (Test-Path $registryFile) {
            try {
                $registryData = Get-Content $registryFile -Raw | ConvertFrom-Json
                $presets = $registryData.presets
                if ($presets) {
                    $sortedPresets = $presets.PSObject.Properties |
                        Where-Object { $null -eq $_.Value.enabled -or $_.Value.enabled -ne $false } |
                        Sort-Object { if ($null -ne $_.Value.priority) { $_.Value.priority } else { 10 } } |
                        ForEach-Object { $_.Name }
                }
            } catch {
                # Fallback: alphabetical directory order
                $sortedPresets = @()
            }
        }

        if ($sortedPresets.Count -gt 0) {
            foreach ($presetId in $sortedPresets) {
                $candidate = Join-Path $presetsDir "$presetId/templates/$TemplateName.md"
                if (Test-Path $candidate) { return $candidate }
            }
        } else {
            # Fallback: alphabetical directory order
            foreach ($preset in Get-ChildItem -Path $presetsDir -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -notlike '.*' }) {
                $candidate = Join-Path $preset.FullName "templates/$TemplateName.md"
                if (Test-Path $candidate) { return $candidate }
            }
        }
    }

    # Priority 3: Extension-provided templates
    $extDir = Join-Path $RepoRoot '.specify/extensions'
    if (Test-Path $extDir) {
        foreach ($ext in Get-ChildItem -Path $extDir -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -notlike '.*' } | Sort-Object Name) {
            $candidate = Join-Path $ext.FullName "templates/$TemplateName.md"
            if (Test-Path $candidate) { return $candidate }
        }
    }

    # Priority 4: Core templates
    $core = Join-Path $base "$TemplateName.md"
    if (Test-Path $core) { return $core }

    return $null
}

# Resolve a template name to composed content using composition strategies.
# Reads strategy metadata from preset manifests and composes content
# from multiple layers using prepend, append, or wrap strategies.
function Resolve-TemplateContent {
    param(
        [Parameter(Mandatory=$true)][string]$TemplateName,
        [Parameter(Mandatory=$true)][string]$RepoRoot
    )

    $base = Join-Path $RepoRoot '.specify/templates'

    # Collect all layers (highest priority first)
    $layerPaths = @()
    $layerStrategies = @()

    # Priority 1: Project overrides (always "replace")
    $override = Join-Path $base "overrides/$TemplateName.md"
    if (Test-Path $override) {
        $layerPaths += $override
        $layerStrategies += 'replace'
    }

    # Priority 2: Installed presets (sorted by priority from .registry)
    $presetsDir = Join-Path $RepoRoot '.specify/presets'
    if (Test-Path $presetsDir) {
        $registryFile = Join-Path $presetsDir '.registry'
        $sortedPresets = @()
        if (Test-Path $registryFile) {
            try {
                $registryData = Get-Content $registryFile -Raw | ConvertFrom-Json
                $presets = $registryData.presets
                if ($presets) {
                    $sortedPresets = $presets.PSObject.Properties |
                        Where-Object { $null -eq $_.Value.enabled -or $_.Value.enabled -ne $false } |
                        Sort-Object { if ($null -ne $_.Value.priority) { $_.Value.priority } else { 10 } } |
                        ForEach-Object { $_.Name }
                }
            } catch {
                $sortedPresets = @()
            }
        }

        if ($sortedPresets.Count -gt 0) {
            $pyCmd = Get-Python3Command
            if (-not $pyCmd) {
                # Check if any preset has strategy fields that would be ignored
                foreach ($pid in $sortedPresets) {
                    $mf = Join-Path $presetsDir "$pid/preset.yml"
                    if ((Test-Path $mf) -and (Select-String -Path $mf -Pattern 'strategy:' -Quiet -ErrorAction SilentlyContinue)) {
                        Write-Warning "No Python 3 found; preset composition strategies will be ignored"
                        break
                    }
                }
            }
            $yamlWarned = $false
            foreach ($presetId in $sortedPresets) {
                # Read strategy and file path from preset manifest
                $strategy = 'replace'
                $manifestFilePath = ''
                $manifest = Join-Path $presetsDir "$presetId/preset.yml"
                if ((Test-Path $manifest) -and $pyCmd) {
                    try {
                        # Use Python to parse YAML manifest for strategy and file path
                        $pyArgs = if ($pyCmd.Count -gt 1) { $pyCmd[1..($pyCmd.Count-1)] } else { @() }
                        $pyStderrFile = [System.IO.Path]::GetTempFileName()
                        $stratResult = & $pyCmd[0] @pyArgs -c @"
import sys
try:
    import yaml
except ImportError:
    print('yaml_missing', file=sys.stderr)
    print('replace\t')
    sys.exit(0)
try:
    with open(sys.argv[1]) as f:
        data = yaml.safe_load(f)
    for t in data.get('provides', {}).get('templates', []):
        if t.get('name') == sys.argv[2] and t.get('type', 'template') == 'template':
            print(t.get('strategy', 'replace') + '\t' + t.get('file', ''))
            sys.exit(0)
    print('replace\t')
except Exception:
    print('replace\t')
"@ $manifest $TemplateName 2>$pyStderrFile
                        if ($stratResult) {
                            $parts = $stratResult.Trim() -split "`t", 2
                            $strategy = $parts[0].ToLowerInvariant()
                            if ($parts.Count -gt 1 -and $parts[1]) { $manifestFilePath = $parts[1] }
                        }
                        if (-not $yamlWarned -and (Test-Path $pyStderrFile) -and (Get-Content $pyStderrFile -Raw -ErrorAction SilentlyContinue) -match 'yaml_missing') {
                            Write-Warning "PyYAML not available; composition strategies may be ignored"
                            $yamlWarned = $true
                        }
                        Remove-Item $pyStderrFile -Force -ErrorAction SilentlyContinue
                    } catch {
                        $strategy = 'replace'
                        if ($pyStderrFile) { Remove-Item $pyStderrFile -Force -ErrorAction SilentlyContinue }
                    }
                }
                # Try manifest file path first, then convention path
                $candidate = $null
                if ($manifestFilePath) {
                    # Reject absolute paths and parent traversal
                    if ([System.IO.Path]::IsPathRooted($manifestFilePath) -or $manifestFilePath -match '\.\.[\\/]') {
                        $manifestFilePath = ''
                    }
                }
                if ($manifestFilePath) {
                    $mf = Join-Path $presetsDir "$presetId/$manifestFilePath"
                    if (Test-Path $mf) { $candidate = $mf }
                }
                if (-not $candidate) {
                    $cf = Join-Path $presetsDir "$presetId/templates/$TemplateName.md"
                    if (Test-Path $cf) { $candidate = $cf }
                }
                if ($candidate) {
                    $layerPaths += $candidate
                    $layerStrategies += $strategy
                }
            }
        } else {
            # Fallback: alphabetical directory order (no registry or parse failure)
            foreach ($preset in Get-ChildItem -Path $presetsDir -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -notlike '.*' }) {
                $candidate = Join-Path $preset.FullName "templates/$TemplateName.md"
                if (Test-Path $candidate) {
                    $layerPaths += $candidate
                    $layerStrategies += 'replace'
                }
            }
        }
    }

    # Priority 3: Extension-provided templates (always "replace")
    $extDir = Join-Path $RepoRoot '.specify/extensions'
    if (Test-Path $extDir) {
        foreach ($ext in Get-ChildItem -Path $extDir -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -notlike '.*' } | Sort-Object Name) {
            $candidate = Join-Path $ext.FullName "templates/$TemplateName.md"
            if (Test-Path $candidate) {
                $layerPaths += $candidate
                $layerStrategies += 'replace'
            }
        }
    }

    # Priority 4: Core templates (always "replace")
    $core = Join-Path $base "$TemplateName.md"
    if (Test-Path $core) {
        $layerPaths += $core
        $layerStrategies += 'replace'
    }

    if ($layerPaths.Count -eq 0) { return $null }

    # If the top (highest-priority) layer is replace, it wins entirely —
    # lower layers are irrelevant regardless of their strategies.
    if ($layerStrategies[0] -eq 'replace') {
        return (Get-Content $layerPaths[0] -Raw)
    }

    # Check if any layer uses a non-replace strategy
    $hasComposition = $false
    foreach ($s in $layerStrategies) {
        if ($s -ne 'replace') { $hasComposition = $true; break }
    }

    if (-not $hasComposition) {
        return (Get-Content $layerPaths[0] -Raw)
    }

    # Find the effective base: scan from highest priority (index 0) downward
    # to find the nearest replace layer. Only compose layers above that base.
    $baseIdx = -1
    for ($i = 0; $i -lt $layerPaths.Count; $i++) {
        if ($layerStrategies[$i] -eq 'replace') {
            $baseIdx = $i
            break
        }
    }
    if ($baseIdx -lt 0) { return $null }

    $content = Get-Content $layerPaths[$baseIdx] -Raw

    for ($i = $baseIdx - 1; $i -ge 0; $i--) {
        $path = $layerPaths[$i]
        $strat = $layerStrategies[$i]
        $layerContent = Get-Content $path -Raw

        switch ($strat) {
            'replace' { $content = $layerContent }
            'prepend' { $content = "$layerContent`n`n$content" }
            'append'  { $content = "$content`n`n$layerContent" }
            'wrap'    {
                if (-not $layerContent.Contains('{CORE_TEMPLATE}')) {
                    throw "Wrap strategy missing {CORE_TEMPLATE} placeholder"
                }
                $content = $layerContent.Replace('{CORE_TEMPLATE}', $content)
            }
            default { throw "Unknown strategy: $strat" }
        }
    }

    return $content
}

# --- Private artifact storage: public/private resolution, confinement, ownership ---

$script:ArtifactRepositoryIdDefault = 'LamuFlix'
$script:ArtifactInitializeHint = 'pwsh -NoProfile -File .specify/scripts/powershell/initialize-artifact-storage.ps1'

function Get-ArtifactInitializeHint {
    return $script:ArtifactInitializeHint
}

function Test-ArtifactOnWindows {
    if ($null -ne $IsWindows) { return [bool]$IsWindows }
    return $true
}

function Test-ArtifactTicketId {
    param([string]$TicketId)
    if ([string]::IsNullOrWhiteSpace($TicketId)) { return $false }
    return ($TicketId.Trim() -cmatch '^[A-Z][A-Z0-9]*-[0-9]+$')
}

# A repository id is a portable slug: no separators, no drive letters, no traversal.
function Test-ArtifactRepositoryId {
    param([string]$RepositoryId)
    if ([string]::IsNullOrWhiteSpace($RepositoryId)) { return $false }
    if ($RepositoryId -ne $RepositoryId.Trim()) { return $false }
    return ($RepositoryId -match '^[A-Za-z0-9][A-Za-z0-9._-]*$')
}

# Absent, empty and whitespace-only values all mean the default; a nonempty invalid slug is rejected.
function Get-ArtifactRepositoryId {
    $raw = $env:LAMUFLIX_ARTIFACT_REPOSITORY_ID
    if ([string]::IsNullOrWhiteSpace($raw)) { return $script:ArtifactRepositoryIdDefault }
    $trimmed = $raw.Trim()
    if (-not (Test-ArtifactRepositoryId $trimmed)) {
        throw "LAMUFLIX_ARTIFACT_REPOSITORY_ID must be a portable slug of letters, digits, dot, underscore or hyphen; '$raw' is not valid."
    }
    return $trimmed
}

function Get-ArtifactDefaultRoot {
    $base = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::LocalApplicationData)
    if ([string]::IsNullOrWhiteSpace($base)) {
        throw 'Cannot resolve the platform local application data directory for private artifact storage. Set LAMUFLIX_ARTIFACT_ROOT to an absolute path outside every checkout.'
    }
    return [System.IO.Path]::GetFullPath((Join-Path $base 'LamuFlix/artifacts'))
}

function ConvertTo-ArtifactComparisonPath {
    param([string]$Path)
    return (([System.IO.Path]::GetFullPath($Path)) -replace '\\', '/').TrimEnd('/')
}

function Test-ArtifactPathInside {
    param([string]$Path, [string]$Container)
    if ([string]::IsNullOrWhiteSpace($Path) -or [string]::IsNullOrWhiteSpace($Container)) { return $false }
    $comparison = if (Test-ArtifactOnWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    $candidate = ConvertTo-ArtifactComparisonPath $Path
    $parent = ConvertTo-ArtifactComparisonPath $Container
    if ([string]::IsNullOrWhiteSpace($parent)) { return $false }
    if ([string]::Equals($candidate, $parent, $comparison)) { return $true }
    return $candidate.StartsWith("$parent/", $comparison)
}

function Get-ArtifactPathAncestors {
    param([string]$Path)
    $ancestors = [System.Collections.Generic.List[string]]::new()
    $current = [System.IO.Path]::GetFullPath($Path)
    while (-not [string]::IsNullOrEmpty($current)) {
        $ancestors.Add($current)
        $parent = [System.IO.Path]::GetDirectoryName($current)
        if ([string]::IsNullOrEmpty($parent) -or $parent -eq $current) { break }
        $current = $parent
    }
    return $ancestors
}

# A path is inside a checkout when it sits under a directory holding .git (directory or worktree file).
function Test-ArtifactInsideCheckout {
    param([string]$Path, [string]$RepoRoot)
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    $full = [System.IO.Path]::GetFullPath($Path)
    if (-not [string]::IsNullOrWhiteSpace($RepoRoot) -and (Test-ArtifactPathInside -Path $full -Container $RepoRoot)) { return $true }
    foreach ($ancestor in (Get-ArtifactPathAncestors $full)) {
        if (Test-Path -LiteralPath (Join-Path $ancestor '.git')) { return $true }
    }
    return $false
}

# Resolve link and junction targets along a path so a confined-looking path cannot point into a checkout.
function Get-ArtifactEffectivePath {
    param([string]$Path)
    $full = [System.IO.Path]::GetFullPath($Path)
    $remainder = [System.Collections.Generic.List[string]]::new()
    $current = $full
    $resolved = $null
    while (-not [string]::IsNullOrEmpty($current)) {
        if (Test-Path -LiteralPath $current) {
            $resolved = $current
            try {
                $item = Get-Item -LiteralPath $current -Force
                $target = $null
                if ($null -ne $item.PSObject.Properties['LinkTarget']) { $target = $item.LinkTarget }
                if ([string]::IsNullOrWhiteSpace($target) -and $null -ne $item.PSObject.Properties['Target']) {
                    $rawTarget = $item.Target
                    if ($rawTarget) { $target = @($rawTarget)[0] }
                }
                if (-not [string]::IsNullOrWhiteSpace($target)) {
                    if (-not [System.IO.Path]::IsPathRooted($target)) {
                        $target = Join-Path ([System.IO.Path]::GetDirectoryName($current)) $target
                    }
                    $resolved = [System.IO.Path]::GetFullPath($target)
                }
            } catch {
                $resolved = $current
            }
            break
        }
        $leaf = [System.IO.Path]::GetFileName($current)
        if (-not [string]::IsNullOrEmpty($leaf)) { $remainder.Insert(0, $leaf) }
        $parent = [System.IO.Path]::GetDirectoryName($current)
        if ([string]::IsNullOrEmpty($parent) -or $parent -eq $current) { break }
        $current = $parent
    }
    if ([string]::IsNullOrWhiteSpace($resolved)) { return $full }
    foreach ($part in $remainder) { $resolved = Join-Path $resolved $part }
    return [System.IO.Path]::GetFullPath($resolved)
}

# Resolve the private artifact root: absolute LAMUFLIX_ARTIFACT_ROOT override, else LocalApplicationData/LamuFlix/artifacts.
function Get-ArtifactRoot {
    param(
        [string]$RepositoryId,
        [string]$RepoRoot
    )
    $override = $env:LAMUFLIX_ARTIFACT_ROOT
    if (-not [string]::IsNullOrWhiteSpace($override)) {
        $raw = $override.Trim()
        if (-not [System.IO.Path]::IsPathRooted($raw)) {
            throw "LAMUFLIX_ARTIFACT_ROOT must be an absolute path; '$override' is relative."
        }
        $segments = $raw -split '[\\/]'
        if (@($segments | Where-Object { $_ -eq '..' }).Count -gt 0) {
            throw "LAMUFLIX_ARTIFACT_ROOT must not contain traversal segments; '$override' does."
        }
        $root = [System.IO.Path]::GetFullPath($raw)
    } else {
        $root = Get-ArtifactDefaultRoot
    }

    if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
        try { $RepoRoot = Get-RepoRoot } catch { $RepoRoot = $null }
    }
    if (Test-ArtifactInsideCheckout -Path $root -RepoRoot $RepoRoot) {
        throw "The private artifact root must live outside every repository and worktree; '$root' does not."
    }
    $effective = Get-ArtifactEffectivePath $root
    if (Test-ArtifactInsideCheckout -Path $effective -RepoRoot $RepoRoot) {
        throw "The private artifact root resolves into a repository or worktree through a link; '$root' points at '$effective'."
    }
    return $root
}

function Get-ArtifactTicketIdFromName {
    param([string]$Branch, [string]$FeatureDir)
    foreach ($candidate in @($Branch, $FeatureDir)) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        if ($candidate -match '([A-Za-z][A-Za-z0-9]*-[0-9]+)') { return $Matches[1].ToUpperInvariant() }
    }
    return $null
}

# Canonical <private-root>/<repository-id>/DEV-###/ directory for one ticket.
function Get-ArtifactTicketDir {
    param(
        [Parameter(Mandatory = $true)][string]$TicketId,
        [string]$Root,
        [string]$RepositoryId,
        [string]$RepoRoot
    )
    if (-not (Test-ArtifactTicketId $TicketId)) {
        throw "Invalid ticket identifier '$TicketId'; expected a shape such as DEV-123."
    }
    $ticket = $TicketId.Trim().ToUpperInvariant()
    if ([string]::IsNullOrWhiteSpace($RepositoryId)) { $RepositoryId = Get-ArtifactRepositoryId }
    if (-not (Test-ArtifactRepositoryId $RepositoryId)) {
        throw "Invalid repository identifier '$RepositoryId'; expected a portable slug."
    }
    if ([string]::IsNullOrWhiteSpace($Root)) {
        $Root = Get-ArtifactRoot -RepositoryId $RepositoryId -RepoRoot $RepoRoot
    }
    $rootFull = [System.IO.Path]::GetFullPath($Root)
    $dir = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $rootFull $RepositoryId) $ticket))
    if (-not (Test-ArtifactPathInside -Path $dir -Container $rootFull)) {
        throw "Refusing a ticket directory that escapes the private root: '$dir'."
    }
    if (Test-ArtifactInsideCheckout -Path $dir -RepoRoot $RepoRoot) {
        throw "Refusing a ticket directory inside a repository or worktree: '$dir'."
    }
    return $dir
}

# Join a confined child path under a base directory, rejecting rooted paths, traversal and escapes.
function New-ArtifactChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$BaseDir,
        [Parameter(Mandatory = $true)][string]$RelativePath
    )
    if ([System.IO.Path]::IsPathRooted($RelativePath)) {
        throw "Refusing the absolute artifact path '$RelativePath'."
    }
    foreach ($segment in ($RelativePath -split '[\\/]')) {
        if ([string]::IsNullOrEmpty($segment) -or $segment -eq '.') { continue }
        if ($segment -eq '..') { throw "Refusing the traversal segment in '$RelativePath'." }
        if ($segment -match '[\x00-\x1f<>:"|?*]') { throw "Refusing the invalid path segment '$segment'." }
    }
    $base = [System.IO.Path]::GetFullPath($BaseDir)
    $full = [System.IO.Path]::GetFullPath((Join-Path $base $RelativePath))
    if (-not (Test-ArtifactPathInside -Path $full -Container $base)) {
        throw "Refusing the artifact path '$RelativePath' because it escapes '$base'."
    }
    return $full
}

function Get-ArtifactTicketPaths {
    param([Parameter(Mandatory = $true)][string]$TicketDir)
    return [pscustomobject]@{
        TICKET_DIR     = $TicketDir
        BRIEF          = Join-Path $TicketDir 'brief.md'
        PLAN           = Join-Path $TicketDir 'plan.md'
        TASKS          = Join-Path $TicketDir 'tasks.md'
        CHECKLISTS_DIR = Join-Path $TicketDir 'checklists'
        EVIDENCE_DIR   = Join-Path $TicketDir 'evidence'
        RECEIPTS_DIR   = Join-Path $TicketDir 'receipts'
        RUN_DIR        = Join-Path $TicketDir 'run'
    }
}

# Ordinary workflow I/O state. Missing or unwritable storage never falls back to tracked specs/.
function Get-ArtifactStorageState {
    param([string]$Root, [string]$RepositoryId, [string]$RepoRoot)
    if ([string]::IsNullOrWhiteSpace($Root)) {
        $Root = Get-ArtifactRoot -RepositoryId $RepositoryId -RepoRoot $RepoRoot
    }
    $exists = Test-Path -LiteralPath $Root -PathType Container
    $writable = $false
    $probe = $null
    if ($exists) {
        $probe = Join-Path $Root ('.artifact-write-probe-' + [guid]::NewGuid().ToString('N'))
        try {
            [System.IO.File]::WriteAllText($probe, 'probe')
            $writable = $true
        } catch {
            $writable = $false
        } finally {
            if ($probe -and (Test-Path -LiteralPath $probe)) {
                Remove-Item -LiteralPath $probe -Force -ErrorAction SilentlyContinue
            }
        }
    }
    return [pscustomobject]@{ ROOT = $Root; EXISTS = $exists; WRITABLE = $writable }
}

function Assert-ArtifactStorage {
    param([string]$Root, [string]$RepositoryId, [string]$RepoRoot)
    $state = Get-ArtifactStorageState -Root $Root -RepositoryId $RepositoryId -RepoRoot $RepoRoot
    if (-not $state.EXISTS) {
        throw "Private artifact storage is not initialized at '$($state.ROOT)'. Run: $(Get-ArtifactInitializeHint)"
    }
    if (-not $state.WRITABLE) {
        throw "Private artifact storage at '$($state.ROOT)' is not writable. Fix permissions, then run: $(Get-ArtifactInitializeHint)"
    }
    return $state.ROOT
}

# Distinct public and private paths for one ticket; public reads never require private storage.
function Get-ArtifactPathsEnv {
    param(
        [string]$TicketId,
        [switch]$RequirePrivate
    )
    $paths = Get-FeaturePathsEnv
    $ticket = $TicketId
    if ([string]::IsNullOrWhiteSpace($ticket)) {
        $ticket = Get-ArtifactTicketIdFromName -Branch $paths.CURRENT_BRANCH -FeatureDir $paths.FEATURE_DIR
    }
    $repositoryId = Get-ArtifactRepositoryId
    $root = $null
    $ticketDir = $null
    $privateAvailable = $false
    if (-not [string]::IsNullOrWhiteSpace($ticket) -and (Test-ArtifactTicketId $ticket)) {
        $root = Get-ArtifactRoot -RepositoryId $repositoryId -RepoRoot $paths.REPO_ROOT
        $ticketDir = Get-ArtifactTicketDir -TicketId $ticket -Root $root -RepositoryId $repositoryId -RepoRoot $paths.REPO_ROOT
        $privateAvailable = Test-Path -LiteralPath $ticketDir -PathType Container
    }
    if ($RequirePrivate -and -not $privateAvailable) {
        Assert-ArtifactStorage -Root $root -RepositoryId $repositoryId -RepoRoot $paths.REPO_ROOT
        throw "Private storage for ticket '$ticket' is not initialized. Run: $(Get-ArtifactInitializeHint)"
    }

    $publicDir = $paths.FEATURE_DIR
    $result = [ordered]@{
        REPO_ROOT             = $paths.REPO_ROOT
        CURRENT_BRANCH        = $paths.CURRENT_BRANCH
        HAS_GIT               = $paths.HAS_GIT
        TICKET_ID             = if ([string]::IsNullOrWhiteSpace($ticket)) { $null } else { $ticket.Trim().ToUpperInvariant() }
        REPOSITORY_ID         = $repositoryId
        PUBLIC_FEATURE_DIR    = $publicDir
        PUBLIC_SPEC           = Join-Path $publicDir 'spec.md'
        PUBLIC_CONCLUSIONS    = Join-Path $publicDir 'CONCLUSIONS.md'
        PUBLIC_ASSUMPTIONS    = Join-Path $publicDir 'ASSUMPTIONS.md'
        PRIVATE_ROOT          = $root
        PRIVATE_TICKET_DIR    = $ticketDir
        PRIVATE_AVAILABLE     = $privateAvailable
        PRIVATE_BRIEF         = if ($ticketDir) { Join-Path $ticketDir 'brief.md' } else { $null }
        PRIVATE_PLAN          = if ($ticketDir) { Join-Path $ticketDir 'plan.md' } else { $null }
        PRIVATE_TASKS         = if ($ticketDir) { Join-Path $ticketDir 'tasks.md' } else { $null }
        PRIVATE_CHECKLISTS_DIR = if ($ticketDir) { Join-Path $ticketDir 'checklists' } else { $null }
        PRIVATE_EVIDENCE_DIR  = if ($ticketDir) { Join-Path $ticketDir 'evidence' } else { $null }
        PRIVATE_RECEIPTS_DIR  = if ($ticketDir) { Join-Path $ticketDir 'receipts' } else { $null }
        PRIVATE_RUN_DIR       = if ($ticketDir) { Join-Path $ticketDir 'run' } else { $null }
    }
    return [pscustomobject]$result
}

# Mutable run output and receipts are isolated by full commit SHA, else by worktree identifier.
function Get-ArtifactRunDir {
    param(
        [Parameter(Mandatory = $true)][string]$TicketDir,
        [string]$WorktreeId,
        [string]$CommitSha,
        [ValidateSet('run', 'receipts', 'evidence')][string]$Kind = 'run'
    )
    $isolation = $null
    if (-not [string]::IsNullOrWhiteSpace($CommitSha) -and $CommitSha.Trim() -match '^[0-9a-fA-F]{40}$') {
        $isolation = Join-Path 'sha' $CommitSha.Trim().ToLowerInvariant()
    } elseif (-not [string]::IsNullOrWhiteSpace($WorktreeId)) {
        $safe = ($WorktreeId.Trim() -replace '[^A-Za-z0-9._-]', '-').Trim('-')
        if ([string]::IsNullOrWhiteSpace($safe)) { throw "Invalid worktree identifier '$WorktreeId'." }
        $isolation = Join-Path 'worktree' $safe
    } else {
        throw 'Mutable run output must be isolated by a full 40-character commit SHA or a worktree identifier.'
    }
    return New-ArtifactChildPath -BaseDir (Join-Path $TicketDir $Kind) -RelativePath $isolation
}

# SHA provenance record attached to gate and review evidence.
function New-ArtifactProvenance {
    param(
        [Parameter(Mandatory = $true)][string]$TicketId,
        [string]$RepositoryId,
        [string]$CommitSha,
        [string]$WorktreeId,
        [string]$Kind = 'run',
        [string]$ArtifactPath = ''
    )
    if (-not (Test-ArtifactTicketId $TicketId)) { throw "Invalid ticket identifier '$TicketId'." }
    if ([string]::IsNullOrWhiteSpace($CommitSha) -or $CommitSha.Trim() -notmatch '^[0-9a-fA-F]{40}$') {
        throw 'Gate and review evidence requires the full 40-character commit SHA it was produced at.'
    }
    if ([string]::IsNullOrWhiteSpace($RepositoryId)) { $RepositoryId = Get-ArtifactRepositoryId }
    return [ordered]@{
        ticketId     = $TicketId.Trim().ToUpperInvariant()
        repositoryId = $RepositoryId
        commitSha    = $CommitSha.Trim().ToLowerInvariant()
        worktreeId   = if ([string]::IsNullOrWhiteSpace($WorktreeId)) { $null } else { $WorktreeId.Trim() }
        kind         = $Kind
        artifactPath = if ([string]::IsNullOrWhiteSpace($ArtifactPath)) { $null } else { $ArtifactPath }
        recordedUtc  = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    }
}

function Get-ArtifactOwnershipPath {
    param([Parameter(Mandatory = $true)][string]$TicketDir)
    return (Join-Path $TicketDir '.ownership.lock')
}

function Get-ArtifactOwnership {
    param([Parameter(Mandatory = $true)][string]$TicketDir)
    $path = Get-ArtifactOwnershipPath $TicketDir
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $null }
    try { return (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json) } catch { return $null }
}

function Get-ArtifactProperty {
    param($Object, [string]$Name)
    if (-not $Object) { return $null }
    if ($null -eq $Object.PSObject.Properties[$Name]) { return $null }
    return $Object.$Name
}

# One shared per-ticket exclusive ownership contract: atomic create, fail closed, explicit release.
function Acquire-ArtifactTicketOwnership {
    param(
        [Parameter(Mandatory = $true)][string]$TicketDir,
        [Parameter(Mandatory = $true)][string]$Owner,
        [string]$WorktreeId,
        [string]$CommitSha
    )
    if (-not (Test-Path -LiteralPath $TicketDir -PathType Container)) {
        throw "Cannot take ownership of '$TicketDir' because it does not exist. Run: $(Get-ArtifactInitializeHint)"
    }
    $path = Get-ArtifactOwnershipPath $TicketDir
    $token = [guid]::NewGuid().ToString('N')
    $record = [ordered]@{
        token       = $token
        owner       = $Owner
        processId   = $PID
        worktreeId  = if ([string]::IsNullOrWhiteSpace($WorktreeId)) { $null } else { $WorktreeId.Trim() }
        commitSha   = if ([string]::IsNullOrWhiteSpace($CommitSha)) { $null } else { $CommitSha.Trim() }
        acquiredUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    }
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes(($record | ConvertTo-Json -Compress))
    $stream = $null
    try {
        $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    } catch [System.IO.IOException] {
        $holder = Get-ArtifactOwnership $TicketDir
        $who = 'an unidentified writer'
        if ($holder) {
            $who = "$(Get-ArtifactProperty $holder 'owner') (pid $(Get-ArtifactProperty $holder 'processId'), worktree $(Get-ArtifactProperty $holder 'worktreeId'), since $(Get-ArtifactProperty $holder 'acquiredUtc'))"
        }
        throw "Ticket '$TicketDir' is already owned by $who. Competing writers fail closed and lock stealing is not automatic; the holder releases with explicit release."
    }
    try {
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush()
    } finally {
        $stream.Dispose()
    }
    return [pscustomobject]@{ TICKET_DIR = $TicketDir; PATH = $path; TOKEN = $token; OWNER = $Owner }
}

function Release-ArtifactTicketOwnership {
    param([Parameter(Mandatory = $true)]$Handle)
    if (-not $Handle) { throw 'An ownership handle is required to release a ticket.' }
    $path = Get-ArtifactProperty $Handle 'PATH'
    $token = Get-ArtifactProperty $Handle 'TOKEN'
    $ticketDir = Get-ArtifactProperty $Handle 'TICKET_DIR'
    if ([string]::IsNullOrWhiteSpace($path)) { throw 'The ownership handle has no lock path.' }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "The ownership lock '$path' is already released." }
    $holder = $null
    try { $holder = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json } catch { $holder = $null }
    $holderToken = Get-ArtifactProperty $holder 'token'
    if ([string]::IsNullOrWhiteSpace($holderToken) -or $holderToken -ne $token) {
        throw "Refusing to release ownership of '$ticketDir'; the lock is held by another writer."
    }
    Remove-Item -LiteralPath $path -Force
}

# Raw-byte SHA-256 of a file; no text or newline normalization.
function Get-ArtifactFileSha256 {
    param([Parameter(Mandatory = $true)][string]$Path)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::OpenRead($Path)
        try { $hash = $sha.ComputeHash($stream) } finally { $stream.Dispose() }
    } finally {
        $sha.Dispose()
    }
    return ([System.BitConverter]::ToString($hash) -replace '-', '').ToLowerInvariant()
}

# Guard-facing predicate: true only for a normalized path under <private-root>/<repository-id>/DEV-###/.
function Test-ArtifactPrivatePath {
    param([string]$Path, [string]$RepoRoot)
    try {
        if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
        $root = Get-ArtifactRoot -RepoRoot $RepoRoot
        $repositoryId = Get-ArtifactRepositoryId
        $base = [System.IO.Path]::GetFullPath((Join-Path $root $repositoryId))
        $effective = Get-ArtifactEffectivePath $Path
        if (-not (Test-ArtifactPathInside -Path $effective -Container $base)) { return $false }
        $relative = [System.IO.Path]::GetRelativePath([System.IO.Path]::GetFullPath($base), $effective)
        if ([string]::IsNullOrWhiteSpace($relative)) { return $false }
        $segments = @($relative -split '[\\/]')
        if ($segments.Count -lt 1) { return $false }
        if (-not (Test-ArtifactTicketId $segments[0])) { return $false }
        foreach ($segment in $segments) {
            if ($segment -eq '..') { return $false }
        }
        if (Test-ArtifactInsideCheckout -Path $effective -RepoRoot $RepoRoot) { return $false }
        return $true
    } catch {
        return $false
    }
}
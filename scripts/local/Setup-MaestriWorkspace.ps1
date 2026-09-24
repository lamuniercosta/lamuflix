# Setup-MaestriWorkspace.ps1
# Automates steps 3, 4, and 7 of LamuFlix workspace setup inside Maestri.
# Must be executed from an active Maestri terminal (where MAESTRI_PIPE is available).

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "=== Step 3: Recruit Patron and Palette ===" -ForegroundColor Cyan

$currentList = & maestri list

if ($currentList -notmatch '\bPatron\b') {
    Write-Host "Recruiting Patron (Codex preset)..."
    & maestri recruit "Patron" --preset "Codex"
} else {
    Write-Host "Patron terminal already exists on canvas."
}

if ($currentList -notmatch '\bPalette\b') {
    Write-Host "Recruiting Palette (Antigravity preset)..."
    & maestri recruit "Palette" --preset "Antigravity"
} else {
    Write-Host "Palette terminal already exists on canvas."
}

Write-Host "`n=== Step 4: Create and Assign Roles (Conductor last) ===" -ForegroundColor Cyan

$rolesDir = Join-Path $PSScriptRoot "roles"
if (-not (Test-Path $rolesDir)) {
    throw "Roles directory not found at: $rolesDir"
}

$roleMappings = @(
    @{ Key = "patron";   Terminal = "Patron";   Role = "LamuFlix Proxy Owner" },
    @{ Key = "palette";  Terminal = "Palette";  Role = "LamuFlix Designer" },
    @{ Key = "keel";     Terminal = "Keel";     Role = "LamuFlix Thinker" },
    @{ Key = "anvil";    Terminal = "Anvil";    Role = "LamuFlix Builder" },
    @{ Key = "cog";      Terminal = "Cog";      Role = "LamuFlix Mechanic" },
    @{ Key = "wisp";     Terminal = "Wisp";     Role = "LamuFlix Scout" },
    @{ Key = "gauge";    Terminal = "Gauge";    Role = "LamuFlix Verifier" },
    @{ Key = "sentry";   Terminal = "Sentry";   Role = "LamuFlix Challenger (Risk)" },
    @{ Key = "ledger";   Terminal = "Ledger";   Role = "LamuFlix Challenger (Standards)" },
    @{ Key = "compass";  Terminal = "Compass";  Role = "LamuFlix Challenger (Spec)" },
    @{ Key = "quill";    Terminal = "Quill";    Role = "LamuFlix Writer" },
    @{ Key = "rigger";   Terminal = "Rigger";   Role = "LamuFlix Operator" },
    @{ Key = "watcher";  Terminal = "Watcher";  Role = "LamuFlix Watcher" },
    @{ Key = "dudamel";  Terminal = "Dudamel";  Role = "LamuFlix Conductor" }
)

foreach ($item in $roleMappings) {
    $filePath = Join-Path $rolesDir "$($item.Key).md"
    if (-not (Test-Path $filePath)) {
        Write-Warning "Role file not found: $filePath"
        continue
    }

    $prompt = Get-Content -LiteralPath $filePath -Raw -Encoding utf8
    Write-Host "Configuring role '$($item.Role)' for '$($item.Terminal)'..."

    $roleExists = $false
    try {
        $existing = & maestri role show $item.Role 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($existing)) {
            $roleExists = $true
        }
    } catch {
        $roleExists = $false
    }

    if ($roleExists) {
        Write-Host "Role '$($item.Role)' already exists, updating..."
        try { & maestri role write $item.Role $prompt } catch { Write-Warning "Could not update role $($item.Role): $_" }
    } else {
        Write-Host "Creating role '$($item.Role)'..."
        try { 
            & maestri role create $item.Role $prompt 
        } catch {
            Write-Host "create threw, trying role write for '$($item.Role)'..."
            try { & maestri role write $item.Role $prompt } catch { Write-Warning "Could not write role $($item.Role): $_" }
        }
    }

    Write-Host "Assigning role '$($item.Role)' to '$($item.Terminal)'..."
    try {
        & maestri role assign $item.Terminal $item.Role
    } catch {
        Write-Warning "Could not assign role $($item.Role) to $($item.Terminal): $_"
    }
}


Write-Host "`n=== Step 7: Wiring & Routines ===" -ForegroundColor Cyan

Write-Host "Ensuring 'LamuFlix notes' stack exists with task-notes-keeper..."
try {
    & maestri note stack "task-notes-keeper" "LamuFlix notes"
} catch {
    Write-Warning "Could not stack task-notes-keeper into 'LamuFlix notes': $_"
}

$seats = @("Dudamel", "Keel", "Patron", "Palette", "Anvil", "Cog", "Wisp", "Gauge", "Sentry", "Ledger", "Compass", "Quill", "Rigger")
foreach ($s in $seats) {
    Write-Host "Connecting '$s' to 'LamuFlix notes'..."
    try {
        & maestri connect "LamuFlix notes" $s
    } catch {
        Write-Warning "Could not connect $s to 'LamuFlix notes': $_"
    }
}

Write-Host "Connecting Watcher -> Dudamel..."
try {
    & maestri connect "Watcher" "Dudamel"
} catch {
    Write-Warning "Could not connect Watcher to Dudamel: $_"
}

Write-Host "Connecting Patron -> Keel..."
try {
    & maestri connect "Patron" "Keel"
} catch {
    Write-Warning "Could not connect Patron to Keel: $_"
}

Write-Host "Configuring merge-reconcile routine..."
$routines = & maestri routine list
if ($routines -notmatch '\bmerge-reconcile\b') {
    & maestri routine create "merge-reconcile" --command "[from Routine] merge-reconcile: apply task-chain section 2.1 to every open-PR row. A merged spec PR (feature/<nnn>-spec) only removes its spec worktree; only a merged delivery PR (feature/DEV-###) sweeps and sets Done." --every 30m --terminal "Watcher"
    Write-Host "Routine 'merge-reconcile' created successfully."
} else {
    Write-Host "Routine 'merge-reconcile' already exists."
}

Write-Host "`n=== LamuFlix Workspace Setup Complete! ===" -ForegroundColor Green

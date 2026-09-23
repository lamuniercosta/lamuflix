#!/usr/bin/env pwsh
<#
  PreToolUse hook: agents never merge pull requests. The owner merges every PR.

  LamuFlix-local, not part of the vendored harness. guard.ps1 is kept verbatim
  so it can be re-synced from upstream; this hook sits beside it with the same
  decision contract (exit 2 + stderr to deny, Cursor JSON on stdout).

  Blocks, in any shell tool:
    - gh pr merge            (any flags, including --auto and --admin)
    - gh api .../pulls/<n>/merge
    - the mergePullRequest / enablePullRequestAutoMerge GraphQL mutations

  Deliberately blunt: the text is matched anywhere in the command, so a commit
  message that quotes the command is blocked too. Rephrase the message rather
  than working around the hook. Branch protection on main is the server-side
  backstop; this hook stops the attempt before it reaches GitHub.

  Anything unparseable exits 0: this hook must never wedge the session.
#>

param(
    [ValidateSet('Legacy', 'Cursor')]
    [string]$OutputContract = 'Legacy'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Allow {
    if ($OutputContract -eq 'Cursor') { [Console]::Out.WriteLine('{"permission":"allow"}') }
    exit 0
}

function Deny {
    param([string]$Reason)
    [Console]::Error.WriteLine("deny-merge: BLOCKED - $Reason")
    if ($OutputContract -eq 'Cursor') {
        $message = "deny-merge: BLOCKED - $Reason" | ConvertTo-Json -Compress
        [Console]::Out.WriteLine("{`"permission`":`"deny`",`"agent_message`":$message,`"user_message`":$message}")
    }
    exit 2
}

function Get-Prop {
    param($Object, [string[]]$Names)
    if (-not $Object) { return $null }
    $available = @($Object.PSObject.Properties.Name)
    foreach ($name in $Names) {
        if ($available -contains $name) { return $Object.$name }
    }
    return $null
}

$raw = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($raw)) { Allow }

try { $payload = ConvertFrom-Json -InputObject $raw } catch { Allow }
if (-not $payload) { Allow }

$tool = Get-Prop $payload @('tool_name', 'toolName', 'name')
if (-not $tool -or $tool -notmatch '^(Bash|Shell|PowerShell|run_terminal_cmd)$') { Allow }

$toolInput = Get-Prop $payload @('tool_input', 'toolInput', 'input', 'arguments')
$cmd = Get-Prop $toolInput @('command', 'cmd')
if ([string]::IsNullOrWhiteSpace($cmd)) { Allow }

$reason = 'agents never merge pull requests. Leave the PR open and tell the owner it is ready; the owner merges.'

# `gh [global flags] pr merge`, stopping at a shell separator so `gh pr view; merge` is not a hit.
if ($cmd -match '(?i)\bgh\b[^|;&\r\n]*\bpr\s+merge\b') { Deny $reason }

# REST merge endpoint: PUT repos/{owner}/{repo}/pulls/{n}/merge.
if ($cmd -match '(?i)\bgh\b[^|;&\r\n]*\bapi\b[^|;&\r\n]*pulls/[^/\s''"]+/merge\b') { Deny $reason }

# GraphQL mutations, however the query is passed.
if ($cmd -match '(?i)\b(mergePullRequest|enablePullRequestAutoMerge)\b') { Deny $reason }

Allow

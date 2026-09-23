#!/usr/bin/env pwsh
<#
  Pre-commit secret scan on staged files, in a single process.

  secret-scan.ps1 is a vendored agent hook that scans one file per invocation.
  Spawning pwsh per staged file cost ~0.45s each (8 minutes for ~1100 files),
  so this reads the scanner's $patterns table once from its AST and applies it
  here. The patterns stay defined in exactly one place; secret-scan.ps1 is not
  edited or executed.

  Scans the working-tree copy of each staged file, as the per-file version did.
  Never prints a matched value - only file, pattern name, line, and length.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scanner = Join-Path $PSScriptRoot 'secret-scan.ps1'
if (-not (Test-Path -LiteralPath $scanner)) { exit 0 }

# Same skips as secret-scan.ps1.
$maxBytes = 512KB
$binaryExtension = '^\.(png|jpg|jpeg|gif|ico|pdf|zip|dll|exe|so|dylib|nupkg)$'

function Get-ScannerPatterns {
    param([string]$Path)

    $ast = [System.Management.Automation.Language.Parser]::ParseFile($Path, [ref]$null, [ref]$null)
    $assignment = $ast.Find({
            param($node)
            $node -is [System.Management.Automation.Language.AssignmentStatementAst] -and
            $node.Left.Extent.Text -eq '$patterns'
        }, $true)
    $table = $assignment.Right.Expression.Child

    # SafeGetValue only evaluates constants, so nothing in the scanner runs.
    # Walking the pairs keeps the scanner's most-specific-first order.
    $patterns = [ordered]@{}
    foreach ($pair in $table.KeyValuePairs) {
        $name = $pair.Item1.SafeGetValue()
        $patterns[$name] = [regex]::new($pair.Item2.GetPureExpression().SafeGetValue())
    }
    return $patterns
}

function Find-Secrets {
    param([string]$File, [System.Collections.Specialized.OrderedDictionary]$Patterns)

    $info = Get-Item -LiteralPath $File
    if ($info.Length -gt $maxBytes -or $info.Extension -match $binaryExtension) { return }

    $content = [System.IO.File]::ReadAllText($info.FullName)
    if ([string]::IsNullOrWhiteSpace($content)) { return }

    foreach ($name in $Patterns.Keys) {
        $match = $Patterns[$name].Match($content)
        if (-not $match.Success) { continue }
        $line = ($content.Substring(0, $match.Index) -split "`n").Count
        "  - ${File}: $name (line ~$line, $($match.Length) chars)"
    }
}

# -z keeps non-ASCII paths unquoted.
$staged = (git diff --cached --name-only --diff-filter=ACM -z) -split "`0" | Where-Object { $_ }
if (-not $staged) { exit 0 }

try {
    $patterns = Get-ScannerPatterns -Path $scanner
} catch {
    # Fail closed: a scanner that silently matches nothing looks like a clean commit.
    Write-Host "pre-commit: could not read `$patterns from secret-scan.ps1 ($($_.Exception.Message)); commit blocked."
    exit 1
}
if ($patterns.Count -eq 0) {
    Write-Host 'pre-commit: secret-scan.ps1 defines no patterns; commit blocked.'
    exit 1
}

$hits = foreach ($file in $staged) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { continue }
    Find-Secrets -File $file -Patterns $patterns
}

if ($hits) {
    Write-Host 'pre-commit: possible credentials in staged files (values not printed):'
    $hits | ForEach-Object { Write-Host $_ }
    Write-Host 'pre-commit: commit blocked. Move real secrets to user secrets or environment variables.'
    exit 1
}

exit 0

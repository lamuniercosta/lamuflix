#!/usr/bin/env pwsh
# Pre-commit secret scan on staged files

$stagedFiles = git diff --cached --name-only --diff-filter=ACM
if (-not $stagedFiles) {
    exit 0
}

$hookDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$scanner = Join-Path $hookDir 'secret-scan.ps1'

if (-not (Test-Path -LiteralPath $scanner)) {
    exit 0
}

$failed = $false

foreach ($file in $stagedFiles) {
    if (-not (Test-Path -LiteralPath $file)) { continue }
    $resolved = (Resolve-Path $file).Path
    $payload = @{
        tool_input = @{
            file_path = $resolved
        }
    } | ConvertTo-Json -Compress

    $payload | pwsh -NoProfile -ExecutionPolicy Bypass -File $scanner
    if ($LASTEXITCODE -eq 2) {
        $failed = $true
    }
}

if ($failed) {
    Write-Host 'pre-commit: secret-scan reported findings; commit blocked.'
    exit 1
}

exit 0

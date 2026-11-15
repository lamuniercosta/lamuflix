#!/usr/bin/env pwsh
# Explicit private artifact storage provisioning and verified record migration.
#
# Ordinary workflow reads and writes stay fail-closed: only this script may create
# the private root. Migration copies without clobbering, verifies raw-byte SHA-256
# hashes and counts, and only then stages Git removals.

[CmdletBinding()]
param(
    [switch]$Initialize,
    [switch]$Bootstrap,
    [switch]$Migrate,
    [string]$TicketId,
    [string]$SourceDir,
    [switch]$Refresh,
    [switch]$StageRemovals,
    [switch]$Json,
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Usage {
    Write-Output 'Usage: ./initialize-artifact-storage.ps1 -Initialize | -Bootstrap -TicketId <DEV-###> -SourceDir <path> [-Refresh] | -Migrate -TicketId <DEV-###> -SourceDir <path> [-StageRemovals]'
    Write-Output '  -Initialize      Create the missing private artifact root and repository directory'
    Write-Output '  -Bootstrap       Copy one ticket operational record set into verified private storage and record the seed proof'
    Write-Output '  -Migrate         Copy the same way, then stage Git removals only after verification'
    Write-Output '  -TicketId        Ticket identifier such as DEV-473'
    Write-Output '  -SourceDir       Repository-relative directory holding the operational records'
    Write-Output '  -Refresh         Bootstrap only: replace a differing private seed copy and re-verify it byte for byte'
    Write-Output '  -StageRemovals   Stage git rm for verified non-allowlist sources (Migrate only)'
    Write-Output '  -Json            Output results in JSON format'
    Write-Output '  -Help            Show this help message'
}

if ($Help) { Write-Usage; exit 0 }

$modeCount = @($Initialize, $Bootstrap, $Migrate | Where-Object { $_ }).Count
if ($modeCount -ne 1) {
    [Console]::Error.WriteLine('ERROR: choose exactly one of -Initialize, -Bootstrap or -Migrate.')
    Write-Usage
    exit 1
}

if ($Refresh -and -not $Bootstrap) {
    [Console]::Error.WriteLine('ERROR: -Refresh applies to -Bootstrap only.')
    Write-Usage
    exit 1
}

. "$PSScriptRoot/common.ps1"

$repoRoot = Get-RepoRoot
$publicAllowlist = @('spec.md', 'CONCLUSIONS.md', 'ASSUMPTIONS.md')

function Test-AllowlistedName {
    param([string]$Name)
    return ($publicAllowlist -contains $Name)
}

function Get-MigrationEntry {
    param(
        [Parameter(Mandatory = $true)][string]$SourceRoot,
        [Parameter(Mandatory = $true)][string]$DestinationRoot,
        [switch]$SkipAllowlist
    )
    $entries = [System.Collections.Generic.List[object]]::new()
    foreach ($file in @(Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Force | Sort-Object FullName)) {
        $relative = [System.IO.Path]::GetRelativePath($SourceRoot, $file.FullName)
        $top = @($relative -split '[\\/]')[0]
        $allowlisted = Test-AllowlistedName $top
        if ($SkipAllowlist -and $allowlisted) { continue }
        $entries.Add([pscustomobject]@{
            RelativePath    = $relative
            Source          = $file.FullName
            Destination     = [System.IO.Path]::GetFullPath((Join-Path $DestinationRoot $relative))
            Allowlisted      = $allowlisted
            SourceSha256    = Get-ArtifactFileSha256 $file.FullName
            SourceLength    = $file.Length
        })
    }
    return $entries
}

function Copy-MigrationEntry {
    param(
        [Parameter(Mandatory = $true)]$Entry,
        [switch]$Refresh
    )
    $parent = [System.IO.Path]::GetDirectoryName($Entry.Destination)
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    if (Test-Path -LiteralPath $Entry.Destination -PathType Leaf) {
        $existingSha = Get-ArtifactFileSha256 $Entry.Destination
        if ($existingSha -eq $Entry.SourceSha256) {
            return [pscustomobject]@{ RelativePath = $Entry.RelativePath; Outcome = 'identical'; Verified = $true; Reason = $null }
        }
        if (-not $Refresh) {
            return [pscustomobject]@{ RelativePath = $Entry.RelativePath; Outcome = 'conflict'; Verified = $false; Reason = 'destination differs from the source' }
        }
        [System.IO.File]::Copy($Entry.Source, $Entry.Destination, $true)
        return [pscustomobject]@{ RelativePath = $Entry.RelativePath; Outcome = 'refreshed'; Verified = $false; Reason = $null }
    }
    [System.IO.File]::Copy($Entry.Source, $Entry.Destination, $false)
    return [pscustomobject]@{ RelativePath = $Entry.RelativePath; Outcome = 'copied'; Verified = $false; Reason = $null }
}

function Confirm-MigrationCopy {
    param($CopyResult, $Entry)
    if ($CopyResult.Outcome -eq 'conflict') { return $CopyResult }
    if (-not (Test-Path -LiteralPath $Entry.Destination -PathType Leaf)) {
        return [pscustomobject]@{ RelativePath = $Entry.RelativePath; Outcome = 'failed'; Verified = $false; Reason = 'destination is absent after copy' }
    }
    $destinationSha = Get-ArtifactFileSha256 $Entry.Destination
    if ($destinationSha -ne $Entry.SourceSha256) {
        return [pscustomobject]@{ RelativePath = $Entry.RelativePath; Outcome = 'failed'; Verified = $false; Reason = 'raw-byte SHA-256 mismatch' }
    }
    return [pscustomobject]@{ RelativePath = $Entry.RelativePath; Outcome = $CopyResult.Outcome; Verified = $true; Reason = $null }
}

function Invoke-Seed {
    param(
        [Parameter(Mandatory = $true)][string]$Ticket,
        [Parameter(Mandatory = $true)][string]$Source,
        [switch]$SkipAllowlist,
        [switch]$Removals,
        [switch]$SeedProof,
        [switch]$Refresh
    )
    $repositoryId = Get-ArtifactRepositoryId
    $root = Get-ArtifactRoot -RepositoryId $repositoryId -RepoRoot $repoRoot
    $ticketDir = Get-ArtifactTicketDir -TicketId $Ticket -Root $root -RepositoryId $repositoryId -RepoRoot $repoRoot

    if (-not (Test-Path -LiteralPath $Source -PathType Container)) {
        [Console]::Error.WriteLine("ERROR: source directory not found: $Source")
        exit 1
    }
    try {
        Assert-ArtifactStorage -Root $root -RepositoryId $repositoryId -RepoRoot $repoRoot | Out-Null
    } catch {
        [Console]::Error.WriteLine("ERROR: $($_.Exception.Message)")
        exit 1
    }
    New-Item -ItemType Directory -Path $ticketDir -Force | Out-Null

    $handle = $null
    try {
        $handle = Acquire-ArtifactTicketOwnership -TicketDir $ticketDir -Owner 'initialize-artifact-storage' -WorktreeId $repoRoot
        $entries = Get-MigrationEntry -SourceRoot $Source -DestinationRoot $ticketDir -SkipAllowlist:$SkipAllowlist
        $results = [System.Collections.Generic.List[object]]::new()
        foreach ($entry in $entries) {
            $copied = Copy-MigrationEntry -Entry $entry -Refresh:$Refresh
            $results.Add((Confirm-MigrationCopy $copied $entry))
        }
    } finally {
        if ($handle) { Release-ArtifactTicketOwnership $handle }
    }

    $failed = @($results | Where-Object { -not $_.Verified })
    $sourceCount = @(Get-ChildItem -LiteralPath $Source -Recurse -File -Force | Where-Object { -not (Test-AllowlistedName @([System.IO.Path]::GetRelativePath($Source, $_.FullName) -split '[\\/]')[0]) }).Count
    $countMatch = ($results.Count -eq $sourceCount) -and ($results.Count -eq $entries.Count)
    $verified = ($failed.Count -eq 0) -and $countMatch -and $results.Count -gt 0

    $seedProofPath = $null
    if ($SeedProof) {
        $seedProofPath = Get-ArtifactSeedProofPath $ticketDir
        if ($verified) {
            $proof = [ordered]@{
                ticketId     = $Ticket.Trim().ToUpperInvariant()
                repositoryId = $repositoryId
                sourceDir    = [System.IO.Path]::GetFullPath($Source)
                ticketDir    = $ticketDir
                verified     = $true
                rawByteSha256 = $true
                fileCount    = $results.Count
                commitSha    = (& git -C $repoRoot rev-parse HEAD 2>$null | Out-String).Trim()
                recordedUtc  = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
                entries      = @($entries | ForEach-Object { [ordered]@{ path = $_.RelativePath; sha256 = $_.SourceSha256; length = $_.SourceLength } })
            }
            [System.IO.File]::WriteAllText($seedProofPath, ($proof | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))
        } elseif (Test-Path -LiteralPath $seedProofPath -PathType Leaf) {
            Remove-Item -LiteralPath $seedProofPath -Force
        }
    }

    $staged = @()
    if ($StageRemovals -and $Removals) {
        if (-not $verified) {
            [Console]::Error.WriteLine('ERROR: verification failed, so no public removal is staged. Sources are intact.')
        } else {
            foreach ($entry in $entries) {
                if ($entry.Allowlisted) { continue }
                $sourcePath = [System.IO.Path]::GetFullPath($entry.Source)
                & git -C $repoRoot rm --quiet -- $sourcePath 2>&1 | Out-Null
                if ($LASTEXITCODE -eq 0) { $staged += $entry.RelativePath }
                else { [Console]::Error.WriteLine("ERROR: git rm failed for $($entry.RelativePath)") }
            }
        }
    }

    $payload = [pscustomobject]@{
        TICKET_ID      = $Ticket.Trim().ToUpperInvariant()
        REPOSITORY_ID  = $repositoryId
        PRIVATE_ROOT   = $root
        TICKET_DIR     = $ticketDir
        ENTRIES        = $results.Count
        VERIFIED       = $verified
        COUNT_MATCH    = $countMatch
        FAILURES       = @($failed | ForEach-Object { [pscustomobject]@{ path = $_.RelativePath; outcome = $_.Outcome; reason = $_.Reason } })
        RESULTS        = @($results | ForEach-Object { [pscustomobject]@{ path = $_.RelativePath; outcome = $_.Outcome; verified = $_.Verified } })
        SEED_PROOF     = $seedProofPath
        STAGED_REMOVALS = $staged
    }
    if ($Json) {
        $payload | ConvertTo-Json -Depth 6 -Compress
    } else {
        Write-Output "TICKET_DIR: $ticketDir"
        if ($seedProofPath) { Write-Output "SEED_PROOF: $seedProofPath" }
        foreach ($result in $results) {
            $state = if ($result.Verified) { 'verified' } else { 'FAILED' }
            Write-Output "  $($result.Outcome) $state  $($result.RelativePath)"
        }
        Write-Output "ENTRIES: $($results.Count)"
        Write-Output "VERIFIED: $verified"
    }
    if (-not $verified) { exit 1 }
    exit 0
}

if ($Initialize) {
    $repositoryId = Get-ArtifactRepositoryId
    $root = Get-ArtifactRoot -RepositoryId $repositoryId -RepoRoot $repoRoot
    $repositoryDir = [System.IO.Path]::GetFullPath((Join-Path $root $repositoryId))
    New-Item -ItemType Directory -Path $repositoryDir -Force | Out-Null
    Write-Output "PRIVATE_ROOT: $root"
    Write-Output "REPOSITORY_ID: $repositoryId"
    Write-Output "REPOSITORY_DIR: $repositoryDir"
    Write-Output 'Private artifact storage is initialized.'
    exit 0
}

if ([string]::IsNullOrWhiteSpace($TicketId)) {
    [Console]::Error.WriteLine('ERROR: -TicketId is required for -Bootstrap and -Migrate.')
    exit 1
}
if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    [Console]::Error.WriteLine('ERROR: -SourceDir is required for -Bootstrap and -Migrate.')
    exit 1
}
$sourceFull = if ([System.IO.Path]::IsPathRooted($SourceDir)) { $SourceDir } else { Join-Path $repoRoot $SourceDir }
$sourceFull = [System.IO.Path]::GetFullPath($sourceFull)

if ($Bootstrap) {
    Invoke-Seed -Ticket $TicketId -Source $sourceFull -SkipAllowlist -SeedProof -Refresh:$Refresh
    exit $LASTEXITCODE
}

Invoke-Seed -Ticket $TicketId -Source $sourceFull -SkipAllowlist -Removals
exit $LASTEXITCODE

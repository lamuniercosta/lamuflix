#!/usr/bin/env pwsh
# Explicit private artifact storage provisioning and verified record migration.

[CmdletBinding()]
param(
    [switch]$Initialize,
    [switch]$Bootstrap,
    [switch]$Migrate,
    [switch]$All,
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
    Write-Output 'Usage: ./initialize-artifact-storage.ps1 -Initialize | -Bootstrap -TicketId <DEV-###> -SourceDir <path> [-Refresh] | -Migrate -TicketId <DEV-###> -SourceDir <path> [-StageRemovals] | -Migrate -All [-StageRemovals] [-Json]'
    Write-Output '  -Initialize      Create the missing private artifact root and repository directory'
    Write-Output '  -Bootstrap       Copy one ticket operational record set into verified private storage and record the seed proof'
    Write-Output '  -Migrate         Archive the ticket records, or with -All every inventoried ticket plus repository-level operational evidence'
    Write-Output '  -All             Bulk inventory: each specs/DEV-### directory and docs/reviews evidence'
    Write-Output '  -TicketId        Ticket identifier such as DEV-473'
    Write-Output '  -SourceDir       Repository-relative directory holding the operational records'
    Write-Output '  -Refresh         Bootstrap only: replace a differing private seed copy and re-verify it byte for byte'
    Write-Output '  -StageRemovals   Stage git rm for verified non-allowlist sources (Migrate only)'
    Write-Output '  -Json            Output results in JSON format'
    Write-Output '  -Help            Show this help message'
    Write-Output ''
    Write-Output 'Operator lifecycle:'
    Write-Output '  The private root is $LAMUFLIX_ARTIFACT_ROOT when set, otherwise the platform local'
    Write-Output '  application-data directory plus LamuFlix/artifacts; the repository id is'
    Write-Output '  $LAMUFLIX_ARTIFACT_REPOSITORY_ID when set, otherwise LamuFlix.'
    Write-Output '  Only -Initialize creates the root; ordinary workflow reads and writes fail closed'
    Write-Output '  and never fall back to a tracked specs/ copy.'
    Write-Output '  Backup and cross-machine transfer are operator-owned: copy the root whole, outside'
    Write-Output '  every checkout. Deleting a task worktree does not delete the archive, and absent'
    Write-Output '  records block pickup instead of being reconstructed.'
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
        [switch]$SkipAllowlist,
        [hashtable]$OriginHashes = @{}
    )
    $entries = [System.Collections.Generic.List[object]]::new()
    foreach ($file in @(Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Force | Sort-Object FullName)) {
        $relative = [System.IO.Path]::GetRelativePath($SourceRoot, $file.FullName)
        $top = @($relative -split '[\\/]')[0]
        $allowlisted = Test-AllowlistedName $top
        if ($SkipAllowlist -and $allowlisted) { continue }
        $originKey = ($relative -replace '\\', '/')
        $entries.Add([pscustomobject]@{
            RelativePath    = $relative
            Source          = $file.FullName
            Destination     = [System.IO.Path]::GetFullPath((Join-Path $DestinationRoot $relative))
            Allowlisted      = $allowlisted
            SourceSha256    = Get-ArtifactFileSha256 $file.FullName
            SourceLength    = $file.Length
            OriginSha256    = if ($OriginHashes.ContainsKey($originKey)) { $OriginHashes[$originKey] } else { $null }
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
        if ($Entry.OriginSha256 -and $existingSha -eq $Entry.OriginSha256) {
            return [pscustomobject]@{ RelativePath = $Entry.RelativePath; Outcome = 'preserved'; Verified = $true; Reason = $null }
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
    if ($CopyResult.Outcome -eq 'preserved') {
        if ($destinationSha -ne $Entry.OriginSha256) {
            return [pscustomobject]@{ RelativePath = $Entry.RelativePath; Outcome = 'failed'; Verified = $false; Reason = 'recorded archive hash mismatch' }
        }
        return [pscustomobject]@{ RelativePath = $Entry.RelativePath; Outcome = $CopyResult.Outcome; Verified = $true; Reason = $null }
    }
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
        [Console]::Error.WriteLine("Pass -SourceDir with the repository-relative specs directory holding the ticket operational records.")
        exit 1
    }
    try {
        Assert-ArtifactStorage -Root $root -RepositoryId $repositoryId -RepoRoot $repoRoot -TicketId $Ticket | Out-Null
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
    $sanitizedTicket = @()
    if ($Removals -and $verified) {
        $publicConclusions = Join-Path $Source 'CONCLUSIONS.md'
        if ((Test-Path -LiteralPath $publicConclusions -PathType Leaf) -and (Update-PublicRecordPaths -Path $publicConclusions)) {
            $sanitizedTicket += (([System.IO.Path]::GetRelativePath($repoRoot, $publicConclusions)) -replace '\\', '/')
        }
    }
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
        SANITIZED      = @($sanitizedTicket)
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

$sanitizeRules = @(
    @{ Pattern = '(?i)F:[\\/]Dev[\\/]LamuFlix\.worktrees'; Replacement = '<task-worktree>' }
    @{ Pattern = '(?i)F:[\\/]Dev[\\/]LamuFlix'; Replacement = '<repository>' }
    @{ Pattern = '(?i)F:[\\/]Dev'; Replacement = '<workspace>' }
    @{ Pattern = '(?i)C:[\\/]Users[\\/][^\\/\s"'']+[\\/]AppData[\\/]Local[\\/]Temp'; Replacement = '<temp>' }
    @{ Pattern = '(?i)[EF]:[\\/]Filmes'; Replacement = '<library-root>' }
    @{ Pattern = '(?i)C:[\\/]TestLibrary'; Replacement = '<test-library>' }
    @{ Pattern = '(?<![\w./\\-])(?i)/Users/[^/\s"'']+'; Replacement = '<home>' }
    @{ Pattern = '(?<![\w./\\-])(?i)/home/[^/\s"'']+'; Replacement = '<home>' }
)

function Get-BulkSourceSet {
    param([Parameter(Mandatory = $true)][string]$RepositoryDir)
    $sets = [System.Collections.Generic.List[object]]::new()
    $specsDir = Join-Path $repoRoot 'specs'
    if (Test-Path -LiteralPath $specsDir -PathType Container) {
        foreach ($dir in @(Get-ChildItem -LiteralPath $specsDir -Directory | Sort-Object Name)) {
            if (-not (Test-ArtifactTicketId $dir.Name)) { continue }
            $ticket = $dir.Name.Trim().ToUpperInvariant()
            $sets.Add([pscustomobject]@{
                Kind          = 'ticket'
                TicketId      = $ticket
                Source        = $dir.FullName
                Destination   = [System.IO.Path]::GetFullPath((Join-Path $RepositoryDir $ticket))
                SkipAllowlist = $false
            })
        }
    }
    $reviewsDir = Join-Path $repoRoot 'docs/reviews'
    if (Test-Path -LiteralPath $reviewsDir -PathType Container) {
        $sets.Add([pscustomobject]@{
            Kind          = 'repository-evidence'
            TicketId      = $null
            Source        = $reviewsDir
            Destination   = [System.IO.Path]::GetFullPath((Join-Path $RepositoryDir 'reviews'))
            SkipAllowlist = $false
        })
    }
    return $sets
}

function Update-PublicRecordPaths {
    param([Parameter(Mandatory = $true)][string]$Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $encoding = if ($hasBom) { [System.Text.UTF8Encoding]::new($true) } else { [System.Text.UTF8Encoding]::new($false) }
    $content = $encoding.GetString($bytes, [int]$hasBom * 3, $bytes.Length - [int]$hasBom * 3)
    $updated = $content
    foreach ($rule in $sanitizeRules) { $updated = $updated -replace $rule.Pattern, $rule.Replacement }
    if ($updated -ceq $content) { return $false }
    [System.IO.File]::WriteAllText($Path, $updated, $encoding)
    return $true
}

function Get-MigrationIndexPath {
    param([Parameter(Mandatory = $true)][string]$RepositoryDir)
    return (Join-Path $RepositoryDir 'migration-index.json')
}

function Read-MigrationIndex {
    param([Parameter(Mandatory = $true)][string]$RepositoryDir)
    $path = Get-MigrationIndexPath -RepositoryDir $RepositoryDir
    $index = @{}
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $index }
    try { $loaded = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json } catch { return $index }
    foreach ($ticketProperty in @($loaded.PSObject.Properties)) {
        $inner = @{}
        foreach ($fileProperty in @($ticketProperty.Value.PSObject.Properties)) {
            $inner[$fileProperty.Name] = [string]$fileProperty.Value
        }
        $index[$ticketProperty.Name] = $inner
    }
    return $index
}

function Write-MigrationIndex {
    param(
        [Parameter(Mandatory = $true)][string]$RepositoryDir,
        [Parameter(Mandatory = $true)]$Index
    )
    $ordered = [ordered]@{}
    foreach ($ticketKey in ($Index.Keys | Sort-Object)) {
        $inner = [ordered]@{}
        foreach ($fileName in ($Index[$ticketKey].Keys | Sort-Object)) {
            $inner[$fileName] = $Index[$ticketKey][$fileName]
        }
        $ordered[$ticketKey] = $inner
    }
    $path = Get-MigrationIndexPath -RepositoryDir $RepositoryDir
    [System.IO.File]::WriteAllText($path, ($ordered | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))
}

function Invoke-BulkMigration {
    param(
        [switch]$Removals,
        [switch]$Refresh
    )
    $repositoryId = Get-ArtifactRepositoryId
    $root = Get-ArtifactRoot -RepositoryId $repositoryId -RepoRoot $repoRoot
    $repositoryDir = [System.IO.Path]::GetFullPath((Join-Path $root $repositoryId))
    try {
        Assert-ArtifactStorage -Root $root -RepositoryId $repositoryId -RepoRoot $repoRoot | Out-Null
    } catch {
        [Console]::Error.WriteLine("ERROR: $($_.Exception.Message)")
        exit 1
    }

    $sets = @(Get-BulkSourceSet -RepositoryDir $repositoryDir)
    if ($sets.Count -eq 0) {
        [Console]::Error.WriteLine('ERROR: the bulk inventory found no ticket directory and no docs/reviews evidence.')
        exit 1
    }

    $summaries = [System.Collections.Generic.List[object]]::new()
    $failures = [System.Collections.Generic.List[object]]::new()
    $sanitized = [System.Collections.Generic.List[string]]::new()
    $staged = [System.Collections.Generic.List[string]]::new()
    $totalVerified = 0
    $totalExpected = 0

    $archiveIndex = Read-MigrationIndex -RepositoryDir $repositoryDir

    foreach ($set in $sets) {
        $originHashes = @{}
        if ($set.Kind -eq 'ticket' -and $archiveIndex.ContainsKey($set.TicketId)) { $originHashes = $archiveIndex[$set.TicketId] }
        $entries = @(Get-MigrationEntry -SourceRoot $set.Source -DestinationRoot $set.Destination -SkipAllowlist:$set.SkipAllowlist -OriginHashes $originHashes)
        $handle = $null
        $results = [System.Collections.Generic.List[object]]::new()
        try {
            if (-not (Test-Path -LiteralPath $set.Destination -PathType Container)) {
                New-Item -ItemType Directory -Path $set.Destination -Force | Out-Null
            }
            if ($set.Kind -eq 'ticket') {
                $handle = Acquire-ArtifactTicketOwnership -TicketDir $set.Destination -Owner 'initialize-artifact-storage-bulk' -WorktreeId $repoRoot
            }
            foreach ($entry in $entries) {
                $copied = Copy-MigrationEntry -Entry $entry -Refresh:$Refresh
                $results.Add((Confirm-MigrationCopy $copied $entry))
            }
        } catch {
            $failures.Add([pscustomobject]@{ set = $set.Kind; ticketId = $set.TicketId; path = $set.Source; outcome = 'error'; reason = $_.Exception.Message })
        } finally {
            if ($handle) { Release-ArtifactTicketOwnership $handle }
        }

        $failed = @($results | Where-Object { -not $_.Verified })
        $totalExpected += $entries.Count
        $totalVerified += @($results | Where-Object { $_.Verified }).Count
        foreach ($failure in $failed) {
            $failures.Add([pscustomobject]@{ set = $set.Kind; ticketId = $set.TicketId; path = $failure.RelativePath; outcome = $failure.Outcome; reason = $failure.Reason })
        }
        if ($set.Kind -eq 'ticket' -and $failed.Count -eq 0) {
            $ticketIndex = @{}
            foreach ($entry in $entries) {
                if (-not $entry.Allowlisted) { continue }
                if (-not (Test-Path -LiteralPath $entry.Destination -PathType Leaf)) { continue }
                $ticketIndex[(@($entry.RelativePath -split '[\\/]') -join '/')] = Get-ArtifactFileSha256 $entry.Destination
            }
            if ($ticketIndex.Count -gt 0) { $archiveIndex[$set.TicketId] = $ticketIndex }
        }
        $summaries.Add([pscustomobject]@{
            KIND        = $set.Kind
            TICKET_ID   = $set.TicketId
            SOURCE      = $set.Source
            DESTINATION = $set.Destination
            ENTRIES     = $entries.Count
            VERIFIED    = @($results | Where-Object { $_.Verified }).Count
            FAILED      = $failed.Count
        })
    }

    $countMatch = ($totalVerified -eq $totalExpected) -and ($totalExpected -gt 0)
    $verified = ($failures.Count -eq 0) -and $countMatch

    if ($Removals -and $verified) {
        foreach ($set in $sets) {
            if ($set.Kind -ne 'ticket') { continue }
            $publicConclusions = Join-Path $set.Source 'CONCLUSIONS.md'
            if (-not (Test-Path -LiteralPath $publicConclusions -PathType Leaf)) { continue }
            if (Update-PublicRecordPaths -Path $publicConclusions) {
                $sanitized.Add((([System.IO.Path]::GetRelativePath($repoRoot, $publicConclusions)) -replace '\\', '/'))
            }
        }
    }
    if ($verified -and $archiveIndex.Count -gt 0) { Write-MigrationIndex -RepositoryDir $repositoryDir -Index $archiveIndex }

    if ($Removals) {
        if (-not $verified) {
            [Console]::Error.WriteLine('ERROR: verification failed, so no public removal is staged. Sources are intact.')
        } else {
            foreach ($set in $sets) {
                $entries = @(Get-MigrationEntry -SourceRoot $set.Source -DestinationRoot $set.Destination -SkipAllowlist:$set.SkipAllowlist)
                foreach ($entry in $entries) {
                    if ($entry.Allowlisted) { continue }
                    $sourcePath = [System.IO.Path]::GetFullPath($entry.Source)
                    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { continue }
                    & git -C $repoRoot rm --quiet -- $sourcePath 2>&1 | Out-Null
                    if ($LASTEXITCODE -eq 0) { $staged.Add(($entry.RelativePath)) }
                    else { [Console]::Error.WriteLine("ERROR: git rm failed for $($entry.RelativePath)") }
                }
            }
        }
    }

    $commitSha = (& git -C $repoRoot rev-parse HEAD 2>$null | Out-String).Trim()
    $receiptPath = $null
    if ($verified -and $commitSha -match '^[0-9a-fA-F]{40}$') {
        $receiptDir = [System.IO.Path]::GetFullPath((Join-Path $repositoryDir "receipts/sha/$($commitSha.ToLowerInvariant())"))
        New-Item -ItemType Directory -Path $receiptDir -Force | Out-Null
        $receiptPath = Join-Path $receiptDir 'bulk-migration.json'
        $receipt = [ordered]@{
            repositoryId   = $repositoryId
            commitSha      = $commitSha.ToLowerInvariant()
            recordedUtc    = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
            rawByteSha256  = $true
            verified       = $true
            fileCount      = $totalVerified
            sourceSets     = @($summaries)
            sanitizedPublic = @($sanitized)
            stagedRemovals = @($staged)
        }
        [System.IO.File]::WriteAllText($receiptPath, ($receipt | ConvertTo-Json -Depth 6), [System.Text.UTF8Encoding]::new($false))
    }

    $payload = [pscustomobject]@{
        REPOSITORY_ID    = $repositoryId
        PRIVATE_ROOT     = $root
        REPOSITORY_DIR   = $repositoryDir
        SOURCE_SETS      = @($summaries)
        FILE_COUNT       = $totalVerified
        EXPECTED_COUNT   = $totalExpected
        VERIFIED         = $verified
        FAILURES         = @($failures)
        SANITIZED        = @($sanitized)
        STAGED_REMOVALS  = @($staged)
        RECEIPT          = $receiptPath
    }
    if ($Json) {
        $payload | ConvertTo-Json -Depth 6 -Compress
    } else {
        Write-Output "REPOSITORY_DIR: $repositoryDir"
        foreach ($summary in $summaries) {
            Write-Output "  $($summary.KIND) $($summary.TICKET_ID) verified $($summary.VERIFIED)/$($summary.ENTRIES) failed $($summary.FAILED)"
        }
        foreach ($failure in $failures) {
            Write-Output "  FAILED $($failure.path) $($failure.outcome) $($failure.reason)"
        }
        Write-Output "SANITIZED: $($sanitized.Count)"
        Write-Output "STAGED_REMOVALS: $($staged.Count)"
        if ($receiptPath) { Write-Output "RECEIPT: $receiptPath" }
        Write-Output "VERIFIED: $verified"
    }
    if (-not $verified) { exit 1 }
    exit 0
}

if ($Initialize) {
    $repositoryId = Get-ArtifactRepositoryId
    $root = Get-ArtifactRoot -RepositoryId $repositoryId -RepoRoot $repoRoot
    $repositoryDir = [System.IO.Path]::GetFullPath((Join-Path $root $repositoryId))
    try {
        New-Item -ItemType Directory -Path $repositoryDir -Force -ErrorAction Stop | Out-Null
    } catch {
        [Console]::Error.WriteLine("ERROR: $(Get-ArtifactStorageGuidance -Root $root -RepositoryId $repositoryId -Problem 'cannot be created')")
        [Console]::Error.WriteLine("Detail: $($_.Exception.Message)")
        exit 1
    }
    Write-Output "PRIVATE_ROOT: $root"
    Write-Output "REPOSITORY_ID: $repositoryId"
    Write-Output "REPOSITORY_DIR: $repositoryDir"
    Write-Output 'Private artifact storage is initialized.'
    exit 0
}

if ($All) {
    if (-not $Migrate) {
        [Console]::Error.WriteLine('ERROR: -All applies to -Migrate only.')
        Write-Usage
        exit 1
    }
    if (-not [string]::IsNullOrWhiteSpace($TicketId) -or -not [string]::IsNullOrWhiteSpace($SourceDir)) {
        [Console]::Error.WriteLine('ERROR: -All derives the inventory itself; drop -TicketId and -SourceDir.')
        exit 1
    }
    Invoke-BulkMigration -Removals:$StageRemovals
    exit $LASTEXITCODE
}

if ([string]::IsNullOrWhiteSpace($TicketId)) {
    [Console]::Error.WriteLine('ERROR: -TicketId is required for -Bootstrap and -Migrate.')
    exit 1
}
if ([string]::IsNullOrWhiteSpace($SourceDir)) {
    [Console]::Error.WriteLine('ERROR: -SourceDir is required for -Bootstrap and -Migrate unless -All is used.')
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

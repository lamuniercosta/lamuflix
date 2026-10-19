#!/usr/bin/env pwsh
<#
  One-time library cutover: imports every movie folder under the library root through
  the same POST /api/movies/import the UI uses, so each movie lands Pending and the
  worker enriches it from OMDb.

    pwsh scripts/local/Import-Library.ps1 -WhatIf          # list what would be imported
    pwsh scripts/local/Import-Library.ps1                  # import everything not yet imported
    pwsh scripts/local/Import-Library.ps1 -Limit 300       # cap one run (OMDb free tier ~1,000/day)
    pwsh scripts/local/Import-Library.ps1 -RetryFailed     # re-queue Failed movies the next day

  Folder rules mirror the legacy scan (LamuFlix.Test UnitTest1.TestMethod1): a top-level
  folder whose name contains "Collection" is descended one level and each subfolder is a
  movie; every other top-level folder is a movie. A folder is skipped when its path is
  already in the library, holds no supported top-level video file, or its name contains
  '..' (LibraryPath rejects any '..' substring, per DEV-294; rename the folder to import it).

  Exit 0 when every attempted import (or retry) was accepted, 1 otherwise.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateNotNullOrEmpty()]
    [string]$LibraryRoot = 'E:\Filmes',

    [ValidateNotNullOrEmpty()]
    [string]$ApiBase = 'http://localhost:3000/api',

    # Library root as the api sees it (the Docker mount), used to match existing paths.
    [ValidateNotNullOrEmpty()]
    [string]$ApiLibraryRoot = '/media/library',

    [ValidateRange(0, [int]::MaxValue)]
    [int]$Limit = 0,

    [ValidateRange(0, 60000)]
    [int]$DelayMilliseconds = 1000,

    [switch]$RetryFailed
)

$ErrorActionPreference = 'Stop'

# Keep in step with DirectoryMediaLibraryScanner.SupportedExtensions.
$supportedExtensions = '.mkv', '.mp4', '.avi', '.m4v', '.rmvb', '.rm', '.flv'

function Get-LibraryMovies {
    $page = 1
    do {
        $uri = "$ApiBase/movies?page=$page&pageSize=100&sort=Title&direction=Ascending"
        $response = Invoke-RestMethod -Method Get -Uri $uri
        $response.items
        $page++
    } while (($page - 1) * 100 -lt $response.totalCount)
}

function Get-ImportedRelativePaths {
    $prefix = $ApiLibraryRoot.TrimEnd('/') + '/'
    $paths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($movie in Get-LibraryMovies) {
        $path = (Invoke-RestMethod -Method Get -Uri "$ApiBase/movies/$($movie.id)").path
        if ($path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            $path = $path.Substring($prefix.Length)
        }
        [void]$paths.Add($path.Replace('\', '/').Trim('/'))
    }
    , $paths
}

function Test-HasVideo([System.IO.DirectoryInfo]$Directory) {
    [bool](Get-ChildItem -LiteralPath $Directory.FullName -File |
        Where-Object { $supportedExtensions -contains $_.Extension.ToLowerInvariant() } |
        Select-Object -First 1)
}

function Get-CandidateFolders {
    foreach ($top in Get-ChildItem -LiteralPath $LibraryRoot -Directory | Sort-Object Name) {
        if ($top.Name -like '*Collection*') {
            foreach ($child in Get-ChildItem -LiteralPath $top.FullName -Directory | Sort-Object Name) {
                [pscustomobject]@{ Directory = $child; Relative = "$($top.Name)/$($child.Name)" }
            }
        }

        [pscustomobject]@{ Directory = $top; Relative = $top.Name }
    }
}

function Invoke-RetryFailed {
    $failed = @(Get-LibraryMovies | Where-Object enrichmentStatus -eq 'Failed')
    Write-Host "Failed movies: $($failed.Count)"
    $errors = 0
    foreach ($movie in $failed) {
        if (-not $PSCmdlet.ShouldProcess($movie.title, 'Retry enrichment')) { continue }
        try {
            Invoke-RestMethod -Method Post -Uri "$ApiBase/movies/$($movie.id)/enrichment" | Out-Null
            Write-Host "  retried  $($movie.title)"
        }
        catch {
            $errors++
            Write-Warning "  retry failed for '$($movie.title)': $($_.Exception.Message)"
        }
        Start-Sleep -Milliseconds $DelayMilliseconds
    }
    return $errors
}

if (-not (Test-Path -LiteralPath $LibraryRoot -PathType Container)) {
    Write-Error "Library root '$LibraryRoot' does not exist."
    exit 1
}

if ($RetryFailed) {
    exit ([int]((Invoke-RetryFailed) -gt 0))
}

$imported = Get-ImportedRelativePaths
$alreadyImported = 0
$noVideo = [System.Collections.Generic.List[string]]::new()
$dotDot = [System.Collections.Generic.List[string]]::new()
$accepted = 0
$rejected = [System.Collections.Generic.List[string]]::new()

foreach ($candidate in Get-CandidateFolders) {
    if ($imported.Contains($candidate.Relative)) { $alreadyImported++; continue }
    if (-not (Test-HasVideo $candidate.Directory)) {
        # Collection parents without a loose video are expected; only report movie folders.
        if ($candidate.Directory.Name -notlike '*Collection*') { $noVideo.Add($candidate.Relative) }
        continue
    }
    if ($candidate.Relative.Contains('..')) { $dotDot.Add($candidate.Relative); continue }
    if ($Limit -gt 0 -and $accepted -ge $Limit) { break }
    if (-not $PSCmdlet.ShouldProcess($candidate.Relative, 'Import')) { $accepted++; continue }

    try {
        $body = @{ folderPath = $candidate.Relative } | ConvertTo-Json -Compress
        Invoke-RestMethod -Method Post -Uri "$ApiBase/movies/import" -ContentType 'application/json; charset=utf-8' `
            -Body ([Text.Encoding]::UTF8.GetBytes($body)) | Out-Null
        $accepted++
        Write-Host "  imported $($candidate.Relative)"
    }
    catch {
        $rejected.Add($candidate.Relative)
        Write-Warning "  import failed for '$($candidate.Relative)': $($_.Exception.Message)"
    }
    Start-Sleep -Milliseconds $DelayMilliseconds
}

$verb = if ($WhatIfPreference) { 'Would import' } else { 'Imported' }
Write-Host ''
Write-Host "$verb`: $accepted   Already imported: $alreadyImported   No supported video: $($noVideo.Count)   Name contains '..': $($dotDot.Count)   Failed: $($rejected.Count)"
if ($Limit -gt 0 -and $accepted -ge $Limit) { Write-Host "Stopped at -Limit $Limit; run again to continue." }
foreach ($folder in $noVideo) { Write-Host "  no video  $folder" }
foreach ($folder in $dotDot) { Write-Host "  '..' in name  $folder" }
exit ([int]($rejected.Count -gt 0))

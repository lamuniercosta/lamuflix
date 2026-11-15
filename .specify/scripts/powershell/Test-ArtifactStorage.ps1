#Requires -Version 7.0
# Companion regressions for the private artifact storage resolver, initialization and guard.
# Every fixture lives under a unique GUID directory in the system temp path; the real scripts run
# as child processes with a controlled environment. No live operator archive is read or written.
# Exit 0 when every case passes, 1 otherwise.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:Failures = 0
$script:Checks = 0

$script:RepoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$script:CommonScript = Join-Path $PSScriptRoot 'common.ps1'
$script:InitScript = Join-Path $PSScriptRoot 'initialize-artifact-storage.ps1'
$script:CreateScript = Join-Path $PSScriptRoot 'create-new-feature.ps1'
$script:SetupPlanScript = Join-Path $PSScriptRoot 'setup-plan.ps1'
$script:SetupTasksScript = Join-Path $PSScriptRoot 'setup-tasks.ps1'
$script:CheckPrereqScript = Join-Path $PSScriptRoot 'check-prerequisites.ps1'
$script:GuardScript = [System.IO.Path]::GetFullPath((Join-Path $script:RepoRoot 'scripts/hooks/guard.ps1'))
$script:Root = Join-Path ([System.IO.Path]::GetTempPath()) ('lamuflix-artifact-test-' + [guid]::NewGuid().ToString('N'))

function Assert-Case {
    param([string]$Name, [bool]$Ok, [string]$Detail = '')
    $script:Checks++
    if ($Ok) {
        Write-Host "PASS  $Name"
    } else {
        Write-Host "FAIL  $Name $Detail" -ForegroundColor Red
        $script:Failures++
    }
}

function New-FixtureDir {
    param([string]$Name)
    $path = Join-Path $script:Root $Name
    New-Item -ItemType Directory -Force -Path $path | Out-Null
    return $path
}

function Write-FixtureText {
    param([string]$Path, [string]$Text)
    $parent = [System.IO.Path]::GetDirectoryName($Path)
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

# Find the last line of captured child output that parses as JSON (warnings share the stream).
function Get-JsonPayload {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return $null }
    $lines = @($Text -split "`r?`n" | Where-Object { $_.Trim() })
    for ($index = $lines.Count - 1; $index -ge 0; $index--) {
        $candidate = $lines[$index].Trim()
        if (-not ($candidate.StartsWith('{') -or $candidate.StartsWith('['))) { continue }
        try { return ($candidate | ConvertFrom-Json) } catch { continue }
    }
    return $null
}

# Safe property read for payloads that may be null or lack the member (StrictMode is on).
function Get-PayloadValue {
    param($Object, [string]$Name)
    if (-not $Object) { return $null }
    if ($null -eq $Object.PSObject.Properties[$Name]) { return $null }
    return $Object.$Name
}

# Raw-byte SHA-256 of a fixture file, computed here so the migration cases do not trust the subject.
function Get-ArtifactFileHash {
    param([Parameter(Mandatory = $true)][string]$Path)
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

# Throwaway Git checkout so the destructive removal cases never touch the real repository.
function New-GitFixtureCheckout {
    param([string]$Name)
    $path = New-FixtureDir $Name
    & git -C $path init --quiet --initial-branch=901-fixture 2>&1 | Out-Null
    & git -C $path config user.email 'fixture@example.invalid' 2>&1 | Out-Null
    & git -C $path config user.name 'Fixture' 2>&1 | Out-Null
    & git -C $path config commit.gpgsign false 2>&1 | Out-Null
    return $path
}

function Test-ArtifactPathEqual {    param([string]$Left, [string]$Right)    if ([string]::IsNullOrWhiteSpace($Left) -or [string]::IsNullOrWhiteSpace($Right)) { return $false }
    $comparison = if ($null -ne $IsWindows -and -not $IsWindows) { [StringComparison]::Ordinal } else { [StringComparison]::OrdinalIgnoreCase }
    return [string]::Equals((([System.IO.Path]::GetFullPath($Left)) -replace '\\', '/').TrimEnd('/'), (([System.IO.Path]::GetFullPath($Right)) -replace '\\', '/').TrimEnd('/'), $comparison)
}

# Run a real workflow script as a child process with a controlled environment.
function Invoke-ChildScript {
    param(
        [Parameter(Mandatory = $true)][string]$Script,
        [string[]]$Arguments = @(),
        [hashtable]$Environment = @{},
        [string]$StandardInput = '',
        [string]$WorkingDirectory = ''
    )
    $saved = @{}
    foreach ($key in $Environment.Keys) {
        $saved[$key] = [System.Environment]::GetEnvironmentVariable($key)
        [System.Environment]::SetEnvironmentVariable($key, $Environment[$key])
    }
    if (-not $Environment.ContainsKey('LAMUFLIX_ARTIFACT_ROOT')) {
        $saved['LAMUFLIX_ARTIFACT_ROOT'] = [System.Environment]::GetEnvironmentVariable('LAMUFLIX_ARTIFACT_ROOT')
        [System.Environment]::SetEnvironmentVariable('LAMUFLIX_ARTIFACT_ROOT', $null)
    }
    if (-not $Environment.ContainsKey('LAMUFLIX_ARTIFACT_REPOSITORY_ID')) {
        $saved['LAMUFLIX_ARTIFACT_REPOSITORY_ID'] = [System.Environment]::GetEnvironmentVariable('LAMUFLIX_ARTIFACT_REPOSITORY_ID')
        [System.Environment]::SetEnvironmentVariable('LAMUFLIX_ARTIFACT_REPOSITORY_ID', $null)
    }
    foreach ($selectionKey in @('SPECIFY_FEATURE_DIRECTORY', 'SPECIFY_FEATURE')) {
        if (-not $Environment.ContainsKey($selectionKey)) {
            $saved[$selectionKey] = [System.Environment]::GetEnvironmentVariable($selectionKey)
            [System.Environment]::SetEnvironmentVariable($selectionKey, $null)
        }
    }
    try {
        if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) { Push-Location -LiteralPath $WorkingDirectory }
        if (-not [string]::IsNullOrEmpty($StandardInput)) {
            $StandardInput | & pwsh -NoProfile -File $Script @Arguments 2>&1 | Out-Null
            $code = $LASTEXITCODE
            $output = @()
        } else {
            $output = @(& pwsh -NoProfile -File $Script @Arguments 2>&1 | ForEach-Object { [string]$_ })
            $code = $LASTEXITCODE
        }
    } finally {
        if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) { Pop-Location }
        foreach ($key in $saved.Keys) {
            [System.Environment]::SetEnvironmentVariable($key, $saved[$key])
        }
    }
    return [pscustomobject]@{ ExitCode = $code; Output = ($output -join "`n") }
}

# Evaluate an expression inside common.ps1 in its own process so a throw cannot poison this run.
function Invoke-CommonProbe {
    param(
        [Parameter(Mandatory = $true)][string]$Expression,
        [hashtable]$Environment = @{},
        [string]$WorkingDirectory = ''
    )
    $program = @'
param([string]$CommonScript, [string]$Expression)
$ErrorActionPreference = 'Stop'
. $CommonScript
try {
    $value = & ([scriptblock]::Create($Expression))
    $payload = [pscustomobject]@{ ok = $true; value = $value; message = $null }
} catch {
    $payload = [pscustomobject]@{ ok = $false; value = $null; message = $_.Exception.Message }
}
$payload | ConvertTo-Json -Depth 6 -Compress
'@
    $probeFile = Join-Path $script:Root ('probe-' + [guid]::NewGuid().ToString('N') + '.ps1')
    Write-FixtureText -Path $probeFile -Text $program
    $result = Invoke-ChildScript -Script $probeFile -Arguments @($script:CommonScript, $Expression) -Environment $Environment -WorkingDirectory $WorkingDirectory
    try {
        return ($result.Output | ConvertFrom-Json)
    } catch {
        return [pscustomobject]@{ ok = $false; value = $null; message = "probe did not return JSON: $($result.Output)" }
    }
}

# Drive the real guard hook with a host-shaped payload and return its exit code.
function Invoke-Guard {
    param(
        [Parameter(Mandatory = $true)][ValidateSet('Edit', 'Write', 'apply_patch', 'Bash')][string]$Tool,
        [string]$Path,
        [string]$Command,
        [Parameter(Mandatory = $true)][string]$Cwd,
        [hashtable]$Environment = @{}
    )
    $toolInput = [ordered]@{}
    if (-not [string]::IsNullOrWhiteSpace($Path)) { $toolInput['file_path'] = $Path }
    if (-not [string]::IsNullOrWhiteSpace($Command)) { $toolInput['command'] = $Command }
    $payload = @{ tool_name = $Tool; cwd = $Cwd; tool_input = $toolInput } | ConvertTo-Json -Compress -Depth 5
    $result = Invoke-ChildScript -Script $script:GuardScript -Arguments @() -Environment $Environment -StandardInput $payload
    return $result.ExitCode
}

New-Item -ItemType Directory -Force -Path $script:Root | Out-Null

try {
    Write-Host ''
    Write-Host 'Private root resolution:'

    $defaultRootProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRoot'
    $localAppData = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::LocalApplicationData)
    $expectedDefault = [System.IO.Path]::GetFullPath((Join-Path $localAppData 'LamuFlix/artifacts'))
    Assert-Case 'default root is LocalApplicationData/LamuFlix/artifacts' ($defaultRootProbe.ok -and $defaultRootProbe.value -eq $expectedDefault) "$($defaultRootProbe.message) got $($defaultRootProbe.value)"

    $overrideRoot = New-FixtureDir 'override-root'
    $overrideProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRoot' -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $overrideRoot }
    Assert-Case 'absolute override root is used' ($overrideProbe.ok -and $overrideProbe.value -eq $overrideRoot) "$($overrideProbe.message) got $($overrideProbe.value)"

    $relativeProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRoot' -Environment @{ LAMUFLIX_ARTIFACT_ROOT = 'relative/artifacts' }
    Assert-Case 'relative override root is rejected' ((-not $relativeProbe.ok) -and ($relativeProbe.message -match 'absolute')) $relativeProbe.message

    $inRepoProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRoot' -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $script:RepoRoot 'artifacts') }
    Assert-Case 'in-repo override root is rejected' ((-not $inRepoProbe.ok) -and ($inRepoProbe.message -match 'outside every repository')) $inRepoProbe.message

    $inWorktreeProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRoot' -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $script:RepoRoot '.specify/artifacts') }
    Assert-Case 'in-worktree override root is rejected' ((-not $inWorktreeProbe.ok) -and ($inWorktreeProbe.message -match 'outside every repository')) $inWorktreeProbe.message

    $traversalProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRoot' -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $script:Root 'a/../b') }
    Assert-Case 'traversal override root is rejected' ((-not $traversalProbe.ok) -and ($traversalProbe.message -match 'traversal')) $traversalProbe.message

    Write-Host ''
    Write-Host 'Repository identifier (P6):'

    $absentProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRepositoryId'
    Assert-Case 'absent repository id defaults to LamuFlix' ($absentProbe.ok -and $absentProbe.value -eq 'LamuFlix') "$($absentProbe.message) got $($absentProbe.value)"

    $emptyProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRepositoryId' -Environment @{ LAMUFLIX_ARTIFACT_REPOSITORY_ID = '' }
    Assert-Case 'empty repository id defaults to LamuFlix' ($emptyProbe.ok -and $emptyProbe.value -eq 'LamuFlix') "$($emptyProbe.message) got $($emptyProbe.value)"

    $whitespaceProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRepositoryId' -Environment @{ LAMUFLIX_ARTIFACT_REPOSITORY_ID = '   ' }
    Assert-Case 'whitespace repository id defaults to LamuFlix' ($whitespaceProbe.ok -and $whitespaceProbe.value -eq 'LamuFlix') "$($whitespaceProbe.message) got $($whitespaceProbe.value)"

    $customProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRepositoryId' -Environment @{ LAMUFLIX_ARTIFACT_REPOSITORY_ID = 'LamuFlix-Core.2' }
    Assert-Case 'valid custom repository id is accepted' ($customProbe.ok -and $customProbe.value -eq 'LamuFlix-Core.2') "$($customProbe.message) got $($customProbe.value)"

    foreach ($invalid in @('bad slug', 'has/slash', '../up', 'has\backslash')) {
        $invalidProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRepositoryId' -Environment @{ LAMUFLIX_ARTIFACT_REPOSITORY_ID = $invalid }
        Assert-Case "invalid repository id '$invalid' is rejected" (-not $invalidProbe.ok) "message: $($invalidProbe.message)"
    }

    Write-Host ''
    Write-Host 'Ticket directory confinement:'

    $canonicalProbe = Invoke-CommonProbe -Expression "Get-ArtifactTicketDir -TicketId 'DEV-473'" -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $overrideRoot }
    $expectedCanonical = [System.IO.Path]::GetFullPath((Join-Path $overrideRoot 'LamuFlix/DEV-473'))
    Assert-Case 'canonical ticket directory is <root>/<repository-id>/DEV-###' ($canonicalProbe.ok -and $canonicalProbe.value -eq $expectedCanonical) "$($canonicalProbe.message) got $($canonicalProbe.value)"

    foreach ($badTicket in @('DEV473', 'dev-473', '../../etc', 'DEV-473/../x', '')) {
        $badProbe = Invoke-CommonProbe -Expression "Get-ArtifactTicketDir -TicketId '$badTicket' -Root '$overrideRoot'" -Environment @{}
        Assert-Case "invalid ticket id '$badTicket' is rejected" (-not $badProbe.ok) "message: $($badProbe.message)"
    }

    $escapeProbe = Invoke-CommonProbe -Expression "New-ArtifactChildPath -BaseDir '$expectedCanonical' -RelativePath '../../escape.md'" -Environment @{}
    Assert-Case 'destination escape is rejected' ((-not $escapeProbe.ok) -and ($escapeProbe.message -match 'traversal')) $escapeProbe.message

    $absoluteChildProbe = Invoke-CommonProbe -Expression "New-ArtifactChildPath -BaseDir '$expectedCanonical' -RelativePath '$(Join-Path $script:Root 'abs.md')'" -Environment @{}
    Assert-Case 'absolute child path is rejected' ((-not $absoluteChildProbe.ok) -and ($absoluteChildProbe.message -match 'absolute')) $absoluteChildProbe.message

    $insideChildProbe = Invoke-CommonProbe -Expression "New-ArtifactChildPath -BaseDir '$expectedCanonical' -RelativePath 'checklists/requirements.md'" -Environment @{}
    Assert-Case 'confined child path is accepted' ($insideChildProbe.ok -and $insideChildProbe.value -eq (Join-Path $expectedCanonical 'checklists/requirements.md')) "$($insideChildProbe.message) got $($insideChildProbe.value)"

    $linkTarget = New-FixtureDir 'link-into-checkout'
    New-Item -ItemType Directory -Force -Path (Join-Path $linkTarget '.git') | Out-Null
    $linkPath = Join-Path $overrideRoot 'LamuFlix/linked-ticket'
    New-Item -ItemType Directory -Force -Path (Join-Path $overrideRoot 'LamuFlix') | Out-Null
    $linkCreated = $false
    try {
        New-Item -ItemType SymbolicLink -Path $linkPath -Target $linkTarget -ErrorAction Stop | Out-Null
        $linkCreated = $true
    } catch {
        $linkCreated = $false
    }
    if ($linkCreated) {
        $linkProbe = Invoke-CommonProbe -Expression "Get-ArtifactTicketDir -TicketId 'linked-ticket' -Root '$overrideRoot'" -Environment @{}
        Assert-Case 'symlink escape into a checkout is rejected' (-not $linkProbe.ok) "message: $($linkProbe.message)"
    } else {
        Write-Host 'SKIP  symlink escape case (link creation is not permitted on this host)'
    }

    Write-Host ''
    Write-Host 'Ordinary I/O fails closed with actionable guidance:'

    $missingRootState = Invoke-CommonProbe -Expression "Get-ArtifactStorageState -Root '$(Join-Path $script:Root 'absent-root')'" -Environment @{}
    Assert-Case 'missing root reports not initialized' ($missingRootState.ok -and (-not $missingRootState.value.EXISTS)) "$($missingRootState.message)"

    $missingInit = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Bootstrap', '-TicketId', 'DEV-473', '-SourceDir', (Join-Path $script:RepoRoot 'specs/DEV-473')) -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $script:Root 'absent-root') }
    Assert-Case 'bootstrap against a missing root fails closed' ($missingInit.ExitCode -eq 1) "bootstrap exit $($missingInit.ExitCode)"
    Assert-Case 'bootstrap names the initialization command' ($missingInit.Output -match 'initialize-artifact-storage\.ps1') $missingInit.Output
    $missingExpect = Invoke-CommonProbe -Expression "Assert-ArtifactStorage -Root '$(Join-Path $script:Root 'absent-root')'" -Environment @{}
    Assert-Case 'ordinary storage assertion names the initialization command' ((-not $missingExpect.ok) -and ($missingExpect.message -match 'initialize-artifact-storage\.ps1')) $missingExpect.message
    Assert-Case 'bootstrap does not create the private root implicitly' (-not (Test-Path -LiteralPath (Join-Path $script:Root 'absent-root'))) "bootstrap exit $($missingInit.ExitCode)"

    Write-Host ''
    Write-Host 'Explicit initialization:'

    $initRoot = New-FixtureDir 'init-root'
    $initResult = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $initRoot }
    Assert-Case 'explicit initialize exits 0' ($initResult.ExitCode -eq 0) "exit $($initResult.ExitCode): $($initResult.Output)"
    Assert-Case 'explicit initialize creates <root>/<repository-id>' (Test-Path -LiteralPath (Join-Path $initRoot 'LamuFlix') -PathType Container)
    Assert-Case 'explicit initialize reports the repository directory' ($initResult.Output -match 'REPOSITORY_DIR')

    $noMode = Invoke-ChildScript -Script $script:InitScript -Arguments @() -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $initRoot }
    Assert-Case 'a missing mode is rejected' ($noMode.ExitCode -eq 1) "exit $($noMode.ExitCode)"

    Write-Host ''
    Write-Host 'Public-only reads and unusable roots:'

    $publicProbe = Invoke-CommonProbe -Expression @'
$paths = Get-ArtifactPathsEnv
[pscustomobject]@{
    specReadable = (Test-Path -LiteralPath $paths.PUBLIC_SPEC -PathType Leaf)
    conclusionsReadable = (Test-Path -LiteralPath $paths.PUBLIC_CONCLUSIONS -PathType Leaf)
    privateAvailable = $paths.PRIVATE_AVAILABLE
    specIsPublic = $paths.PUBLIC_SPEC
    briefIsPrivate = $paths.PRIVATE_BRIEF
    ticket = $paths.TICKET_ID
}
'@ -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $script:Root 'public-only-root') }
    Assert-Case 'public spec is readable without private storage' ($publicProbe.ok -and $publicProbe.value.specReadable) "$($publicProbe.message)"
    Assert-Case 'public conclusions are readable without private storage' ($publicProbe.ok -and $publicProbe.value.conclusionsReadable) "$($publicProbe.message)"
    Assert-Case 'public inspection reports no private storage' ($publicProbe.ok -and (-not $publicProbe.value.privateAvailable)) "$($publicProbe.message)"
    Assert-Case 'private paths are distinct from public paths' ($publicProbe.ok -and $publicProbe.value.specIsPublic -ne $publicProbe.value.briefIsPrivate) "$($publicProbe.message)"

    $requirePrivateProbe = Invoke-CommonProbe -Expression 'Get-ArtifactPathsEnv -RequirePrivate | Out-Null' -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $script:Root 'public-only-root') }
    Assert-Case 'private-required resolution fails closed without private storage' ((-not $requirePrivateProbe.ok) -and ($requirePrivateProbe.message -match 'initialize-artifact-storage\.ps1')) $requirePrivateProbe.message

    $notADirectory = Join-Path $script:Root 'root-is-a-file'
    Write-FixtureText -Path $notADirectory -Text 'not a directory'
    $unusableProbe = Invoke-CommonProbe -Expression "Assert-ArtifactStorage -Root '$notADirectory'" -Environment @{}
    Assert-Case 'a non-directory root fails with initialization guidance' ((-not $unusableProbe.ok) -and ($unusableProbe.message -match 'initialize-artifact-storage\.ps1')) $unusableProbe.message

    Write-Host ''
    Write-Host 'Ownership contract (P4):'

    $ownerTicket = Join-Path $initRoot 'LamuFlix/DEV-473'
    New-Item -ItemType Directory -Force -Path $ownerTicket | Out-Null
    $ownerProbe = Invoke-CommonProbe -Expression @"
`$first = Acquire-ArtifactTicketOwnership -TicketDir '$ownerTicket' -Owner 'writer-a' -WorktreeId 'wt-a'
`$competing = `$null
`$message = `$null
try { `$competing = Acquire-ArtifactTicketOwnership -TicketDir '$ownerTicket' -Owner 'writer-b' -WorktreeId 'wt-b' } catch { `$message = `$_.Exception.Message }
`$steal = Test-Path -LiteralPath (Get-ArtifactOwnershipPath '$ownerTicket')
Release-ArtifactTicketOwnership `$first
`$released = -not (Test-Path -LiteralPath (Get-ArtifactOwnershipPath '$ownerTicket'))
`$afterRelease = Acquire-ArtifactTicketOwnership -TicketDir '$ownerTicket' -Owner 'writer-b' -WorktreeId 'wt-b'
Release-ArtifactTicketOwnership `$afterRelease
[pscustomobject]@{ competingBlocked = (`$null -eq `$competing); message = `$message; lockSurvived = `$steal; released = `$released }
"@ -Environment @{}
    Assert-Case 'a competing writer fails closed' ($ownerProbe.ok -and $ownerProbe.value.competingBlocked) "$($ownerProbe.message)"
    Assert-Case 'the denial names the current holder' ($ownerProbe.ok -and $ownerProbe.value.message -match 'writer-a') "$($ownerProbe.value.message)"
    Assert-Case 'the lock is not stolen automatically' ($ownerProbe.ok -and $ownerProbe.value.lockSurvived) "$($ownerProbe.value)"
    Assert-Case 'explicit release frees the ticket' ($ownerProbe.ok -and $ownerProbe.value.released) "$($ownerProbe.value)"

    $staleReleaseProbe = Invoke-CommonProbe -Expression @"
`$owner = Acquire-ArtifactTicketOwnership -TicketDir '$ownerTicket' -Owner 'writer-a' -WorktreeId 'wt-a'
`$foreign = [pscustomobject]@{ TICKET_DIR = '$ownerTicket'; PATH = (Get-ArtifactOwnershipPath '$ownerTicket'); TOKEN = 'not-the-token'; OWNER = 'writer-x' }
`$message = `$null
try { Release-ArtifactTicketOwnership `$foreign } catch { `$message = `$_.Exception.Message }
`$stillHeld = Test-Path -LiteralPath (Get-ArtifactOwnershipPath '$ownerTicket')
Release-ArtifactTicketOwnership `$owner
[pscustomobject]@{ refused = (`$null -ne `$message); stillHeld = `$stillHeld }
"@ -Environment @{}
    Assert-Case 'release by a non-holder is refused' ($staleReleaseProbe.ok -and $staleReleaseProbe.value.refused) "$($staleReleaseProbe.message)"
    Assert-Case 'a refused release leaves the lock intact' ($staleReleaseProbe.ok -and $staleReleaseProbe.value.stillHeld) "$($staleReleaseProbe.value)"

    Write-Host ''
    Write-Host 'Run isolation and SHA provenance (P4/P5):'

    $sha = 'a' * 40
    $runProbe = Invoke-CommonProbe -Expression "Get-ArtifactRunDir -TicketDir '$ownerTicket' -CommitSha '$sha' -Kind 'receipts'" -Environment @{}
    $expectedRun = [System.IO.Path]::GetFullPath((Join-Path $ownerTicket "receipts/sha/$sha"))
    Assert-Case 'run output is isolated by full SHA' ($runProbe.ok -and $runProbe.value -eq $expectedRun) "$($runProbe.message) got $($runProbe.value)"

    $worktreeRunProbe = Invoke-CommonProbe -Expression "Get-ArtifactRunDir -TicketDir '$ownerTicket' -WorktreeId 'DEV-473' -Kind 'run'" -Environment @{}
    $expectedWorktreeRun = [System.IO.Path]::GetFullPath((Join-Path $ownerTicket 'run/worktree/DEV-473'))
    Assert-Case 'run output is isolated by worktree when no SHA is given' ($worktreeRunProbe.ok -and $worktreeRunProbe.value -eq $expectedWorktreeRun) "$($worktreeRunProbe.message) got $($worktreeRunProbe.value)"

    $noIsolationProbe = Invoke-CommonProbe -Expression "Get-ArtifactRunDir -TicketDir '$ownerTicket'" -Environment @{}
    Assert-Case 'run output without isolation is refused' (-not $noIsolationProbe.ok) $noIsolationProbe.message

    $shortShaProbe = Invoke-CommonProbe -Expression "Get-ArtifactRunDir -TicketDir '$ownerTicket' -CommitSha 'abc123'" -Environment @{}
    Assert-Case 'a short SHA is not accepted as isolation' (-not $shortShaProbe.ok) $shortShaProbe.message

    $provenanceProbe = Invoke-CommonProbe -Expression "New-ArtifactProvenance -TicketId 'DEV-473' -CommitSha '$sha' -WorktreeId 'DEV-473' -Kind 'evidence'" -Environment @{}
    Assert-Case 'gate evidence carries the full SHA' ($provenanceProbe.ok -and $provenanceProbe.value.commitSha -eq $sha) "$($provenanceProbe.message)"
    Assert-Case 'provenance records ticket and repository id' ($provenanceProbe.ok -and $provenanceProbe.value.ticketId -eq 'DEV-473' -and $provenanceProbe.value.repositoryId -eq 'LamuFlix') "$($provenanceProbe.message)"

    $noShaProvenanceProbe = Invoke-CommonProbe -Expression "New-ArtifactProvenance -TicketId 'DEV-473'" -Environment @{}
    Assert-Case 'provenance without a full SHA is refused' (-not $noShaProvenanceProbe.ok) $noShaProvenanceProbe.message

    Write-Host ''
    Write-Host 'Guard private-ticket allowance (P2):'

    $guardRoot = New-FixtureDir 'guard-root'
    $guardTicket = Join-Path $guardRoot 'LamuFlix/DEV-473'
    New-Item -ItemType Directory -Force -Path $guardTicket | Out-Null

    $allowedPath = Join-Path $guardTicket 'brief.md'
    $allowedResult = Invoke-Guard -Tool 'Edit' -Path $allowedPath -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'an Edit to a confined private ticket path is allowed' ($allowedResult -eq 0) "exit $allowedResult"

    $allowedPatch = "*** Begin Patch`n*** Add File: $(Join-Path $guardTicket 'plan.md')`n*** End Patch"
    $patchResult = Invoke-Guard -Tool 'apply_patch' -Command $allowedPatch -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'an apply-patch to a confined private ticket path is allowed' ($patchResult -eq 0) "exit $patchResult"

    $outsideTicket = Join-Path $guardRoot 'LamuFlix/not-a-ticket/brief.md'
    $outsideResult = Invoke-Guard -Tool 'Edit' -Path $outsideTicket -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'a path under the root but outside DEV-### is denied' ($outsideResult -eq 2) "exit $outsideResult"

    $otherRepository = Join-Path $guardRoot 'OtherRepo/DEV-473/brief.md'
    $otherRepositoryResult = Invoke-Guard -Tool 'Edit' -Path $otherRepository -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'a path under a different repository id is denied' ($otherRepositoryResult -eq 2) "exit $otherRepositoryResult"

    $rootFile = Join-Path $guardRoot 'LamuFlix/brief.md'
    $rootFileResult = Invoke-Guard -Tool 'Edit' -Path $rootFile -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'a file directly under the repository id is denied' ($rootFileResult -eq 2) "exit $rootFileResult"

    $elsewherePath = Join-Path (Join-Path $script:Root 'elsewhere') 'brief.md'
    $elsewhereResult = Invoke-Guard -Tool 'Edit' -Path $elsewherePath -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'an unrelated external path is denied' ($elsewhereResult -eq 2) "exit $elsewhereResult"

    $traversalPath = Join-Path $guardTicket '../../escape.md'
    $traversalResult = Invoke-Guard -Tool 'Edit' -Path $traversalPath -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'a traversal path is denied inside the allowance' ($traversalResult -eq 2) "exit $traversalResult"

    $protectedObj = Join-Path $guardTicket 'obj/generated.cs'
    $protectedObjResult = Invoke-Guard -Tool 'Edit' -Path $protectedObj -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'protected build output stays denied inside the allowance' ($protectedObjResult -eq 2) "exit $protectedObjResult"

    $protectedGit = Join-Path $guardTicket '.git/config'
    $protectedGitResult = Invoke-Guard -Tool 'Edit' -Path $protectedGit -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'protected .git stays denied inside the allowance' ($protectedGitResult -eq 2) "exit $protectedGitResult"

    $fakeCheckoutRoot = New-FixtureDir 'fake-checkout-root'
    New-Item -ItemType Directory -Force -Path (Join-Path $fakeCheckoutRoot '.git') | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $fakeCheckoutRoot 'LamuFlix/DEV-473') | Out-Null
    $fakeCheckoutResult = Invoke-Guard -Tool 'Edit' -Path (Join-Path $fakeCheckoutRoot 'LamuFlix/DEV-473/brief.md') -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $fakeCheckoutRoot }
    Assert-Case 'a private root that is itself a checkout is not sanctioned' ($fakeCheckoutResult -eq 2) "exit $fakeCheckoutResult"

    $inRepoRootProbe = Invoke-CommonProbe -Expression "Get-ArtifactRoot" -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $script:RepoRoot 'artifacts') }
    Assert-Case 'the resolver rejects an in-repo private root' (-not $inRepoRootProbe.ok) $inRepoRootProbe.message

    $invalidRepoResult = Invoke-Guard -Tool 'Edit' -Path $allowedPath -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot; LAMUFLIX_ARTIFACT_REPOSITORY_ID = 'bad slug' }
    Assert-Case 'an invalid repository id removes the allowance' ($invalidRepoResult -eq 2) "exit $invalidRepoResult"

    $relativeResult = Invoke-Guard -Tool 'Edit' -Path 'LamuFlix/DEV-473/brief.md' -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'a relative path is not treated as a private ticket path' ($relativeResult -eq 0) "exit $relativeResult"

    $shellProbe = Invoke-Guard -Tool 'Bash' -Command "Set-Content -LiteralPath '$(Join-Path $guardTicket 'brief.md')' -Value 'x'" -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'the allowance grants no shell route' ($shellProbe -eq 0) "exit $shellProbe"

    $unrelatedResult = Invoke-Guard -Tool 'Edit' -Path (Join-Path $script:RepoRoot 'scripts/hooks/guard.ps1') -Cwd $script:RepoRoot -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $guardRoot }
    Assert-Case 'ordinary in-worktree edits are unaffected' ($unrelatedResult -eq 0) "exit $unrelatedResult"

    Write-Host ''
    Write-Host 'Bootstrap seed proof and activation gate (P1):'

    $seedRoot = New-FixtureDir 'seed-root'
    $seedRepository = Join-Path $seedRoot 'LamuFlix'
    New-Item -ItemType Directory -Force -Path $seedRepository | Out-Null
    $seedTicket = Join-Path $seedRepository 'DEV-900'
    New-Item -ItemType Directory -Force -Path $seedTicket | Out-Null

    $seedSource = Join-Path $script:Root 'seed-source/DEV-900'
    Write-FixtureText -Path (Join-Path $seedSource 'brief.md') -Text "# brief`r`nline two`r`n"
    Write-FixtureText -Path (Join-Path $seedSource 'plan.md') -Text "# plan`nunix newline stays byte identical`n"
    Write-FixtureText -Path (Join-Path $seedSource 'checklists/requirements.md') -Text "# requirements`r`n"

    $unseededActivation = Invoke-CommonProbe -Expression "Assert-ArtifactBootstrapSeed -TicketDir '$seedTicket'" -Environment @{}
    Assert-Case 'activation is blocked without seed proof' ((-not $unseededActivation.ok) -and ($unseededActivation.message -match 'bootstrap seed')) $unseededActivation.message

    $unseededRequirePrivate = Invoke-CommonProbe -Expression "Get-ArtifactPathsEnv -TicketId 'DEV-900' -RequirePrivate | Out-Null" -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $seedRoot }
    Assert-Case 'private-required resolution is blocked by a missing seed proof' ((-not $unseededRequirePrivate.ok) -and ($unseededRequirePrivate.message -match 'bootstrap seed')) $unseededRequirePrivate.message

    $seedResult = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Bootstrap', '-TicketId', 'DEV-900', '-SourceDir', $seedSource) -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $seedRoot }
    Assert-Case 'bootstrap seed exits 0 for a clean seed' ($seedResult.ExitCode -eq 0) "exit $($seedResult.ExitCode): $($seedResult.Output)"
    Assert-Case 'bootstrap seed records its proof file' (Test-Path -LiteralPath (Join-Path $seedTicket 'seed-proof.json') -PathType Leaf)
    Assert-Case 'bootstrap seed copies every source file' ((Test-Path -LiteralPath (Join-Path $seedTicket 'checklists/requirements.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $seedTicket 'brief.md') -PathType Leaf))

    $seedProofFile = Join-Path $seedTicket 'seed-proof.json'
    $seedProof = Get-Content -LiteralPath $seedProofFile -Raw | ConvertFrom-Json
    Assert-Case 'seed proof declares raw-byte SHA-256 verification' ([bool]$seedProof.rawByteSha256)
    Assert-Case 'seed proof counts every seeded file' ($seedProof.fileCount -eq 3) "fileCount $($seedProof.fileCount)"
    $crlfEntry = @($seedProof.entries | Where-Object { $_.path -eq 'brief.md' })[0]
    Assert-Case 'seed proof hashes the raw bytes, not normalized text' ($crlfEntry.sha256 -eq (Get-FileHash -LiteralPath (Join-Path $seedSource 'brief.md') -Algorithm SHA256).Hash.ToLowerInvariant()) $crlfEntry.sha256

    $seededActivation = Invoke-CommonProbe -Expression "Assert-ArtifactBootstrapSeed -TicketDir '$seedTicket'" -Environment @{}
    Assert-Case 'activation is allowed once the seed is proven' $seededActivation.ok $seededActivation.message

    $seededStateProbe = Invoke-CommonProbe -Expression "Get-ArtifactPathsEnv -TicketId 'DEV-900'" -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $seedRoot }
    Assert-Case 'resolved paths report the verified seed' ($seededStateProbe.ok -and $seededStateProbe.value.PRIVATE_SEED_VERIFIED) $seededStateProbe.message

    $seedRerun = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Bootstrap', '-TicketId', 'DEV-900', '-SourceDir', $seedSource) -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $seedRoot }
    Assert-Case 'an identical bootstrap rerun succeeds idempotently' ($seedRerun.ExitCode -eq 0) "exit $($seedRerun.ExitCode): $($seedRerun.Output)"

    Write-FixtureText -Path (Join-Path $seedSource 'brief.md') -Text "# brief`r`nline two, edited`r`n"
    $staleResult = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Bootstrap', '-TicketId', 'DEV-900', '-SourceDir', $seedSource) -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $seedRoot }
    Assert-Case 'a differing private copy blocks a plain bootstrap' ($staleResult.ExitCode -eq 1) "exit $($staleResult.ExitCode)"
    Assert-Case 'a failed bootstrap leaves no verified proof behind' (-not (Test-Path -LiteralPath $seedProofFile -PathType Leaf))

    $refreshed = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Bootstrap', '-TicketId', 'DEV-900', '-SourceDir', $seedSource, '-Refresh') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $seedRoot }
    Assert-Case 'an explicit refresh replaces the differing private copy' ($refreshed.ExitCode -eq 0) "exit $($refreshed.ExitCode): $($refreshed.Output)"
    Assert-Case 'the refreshed private copy matches the source byte for byte' ((Get-FileHash -LiteralPath (Join-Path $seedTicket 'brief.md') -Algorithm SHA256).Hash.ToLowerInvariant() -eq (Get-FileHash -LiteralPath (Join-Path $seedSource 'brief.md') -Algorithm SHA256).Hash.ToLowerInvariant())

    Write-FixtureText -Path (Join-Path $seedTicket 'brief.md') -Text 'tampered after the seed'
    $tamperedActivation = Invoke-CommonProbe -Expression "Assert-ArtifactBootstrapSeed -TicketDir '$seedTicket'" -Environment @{}
    Assert-Case 'tampering after the seed re-blocks activation' ((-not $tamperedActivation.ok) -and ($tamperedActivation.message -match 'Refresh')) $tamperedActivation.message

    $refreshOnly = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize', '-Refresh') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $seedRoot }
    Assert-Case 'refresh outside bootstrap is rejected' ($refreshOnly.ExitCode -eq 1) "exit $($refreshOnly.ExitCode)"

    Write-Host ''
    Write-Host 'US1 public-only inspection and private-required failure (T008):'

    $publicCheckout = New-FixtureDir 'us1-public-checkout'
    New-Item -ItemType Directory -Force -Path (Join-Path $publicCheckout '.specify') | Out-Null
    $publicTicketDir = Join-Path $publicCheckout 'specs/DEV-901'
    Write-FixtureText -Path (Join-Path $publicTicketDir 'spec.md') -Text "# DEV-901 spec`n"
    Write-FixtureText -Path (Join-Path $publicTicketDir 'CONCLUSIONS.md') -Text "# DEV-901 conclusions`n"
    $us1Root = Join-Path $script:Root 'us1-absent-root'
    $us1Env = @{ LAMUFLIX_ARTIFACT_ROOT = $us1Root; SPECIFY_FEATURE_DIRECTORY = $publicTicketDir }

    $us1PublicProbe = Invoke-CommonProbe -Expression @'
$paths = Get-ArtifactPathsEnv -TicketId 'DEV-901'
[pscustomobject]@{
    specReadable = (Test-Path -LiteralPath $paths.PUBLIC_SPEC -PathType Leaf)
    conclusionsReadable = (Test-Path -LiteralPath $paths.PUBLIC_CONCLUSIONS -PathType Leaf)
    privateAvailable = $paths.PRIVATE_AVAILABLE
    privateRoot = $paths.PRIVATE_ROOT
    ticketDir = $paths.PRIVATE_TICKET_DIR
    publicDir = $paths.PUBLIC_FEATURE_DIR
}
'@ -Environment $us1Env -WorkingDirectory $publicCheckout
    Assert-Case 'a public-spec inspection succeeds without private storage' ($us1PublicProbe.ok -and $us1PublicProbe.value.specReadable) "$($us1PublicProbe.message)"
    Assert-Case 'public conclusions are inspected without private storage' ($us1PublicProbe.ok -and $us1PublicProbe.value.conclusionsReadable) "$($us1PublicProbe.message)"
    Assert-Case 'the inspection reports no private storage for the ticket' ($us1PublicProbe.ok -and (-not $us1PublicProbe.value.privateAvailable)) "$($us1PublicProbe.message)"
    Assert-Case 'private and public ticket paths stay distinct' ($us1PublicProbe.ok -and $us1PublicProbe.value.ticketDir -and -not (Test-ArtifactPathEqual $us1PublicProbe.value.ticketDir $us1PublicProbe.value.publicDir)) "$($us1PublicProbe.message)"

    $publicBefore = @(Get-ChildItem -LiteralPath $publicTicketDir -Recurse -File -Force | ForEach-Object { $_.FullName } | Sort-Object) -join '|'
    $us1RequirePrivate = Invoke-CommonProbe -Expression "Get-ArtifactPathsEnv -TicketId 'DEV-901' -RequirePrivate | Out-Null" -Environment $us1Env -WorkingDirectory $publicCheckout
    Assert-Case 'a private-required stage fails closed without private storage' (-not $us1RequirePrivate.ok) $us1RequirePrivate.message
    Assert-Case 'the failure names the explicit initialization command' ($us1RequirePrivate.message -match 'initialize-artifact-storage\.ps1 -Initialize') $us1RequirePrivate.message
    Assert-Case 'the failure states that tracked specs/ is not a fallback' ($us1RequirePrivate.message -match 'no fallback to tracked specs/') $us1RequirePrivate.message
    Assert-Case 'the failure names the ticket it needs' ($us1RequirePrivate.message -match 'DEV-901') $us1RequirePrivate.message
    $publicAfter = @(Get-ChildItem -LiteralPath $publicTicketDir -Recurse -File -Force | ForEach-Object { $_.FullName } | Sort-Object) -join '|'
    Assert-Case 'a private-required failure recreates no operational record publicly' ($publicBefore -eq $publicAfter) "before $publicBefore after $publicAfter"
    Assert-Case 'a private-required failure creates no private root implicitly' (-not (Test-Path -LiteralPath $us1Root)) 'private root was created'

    $us1PublicOnly = @('spec.md', 'CONCLUSIONS.md', 'ASSUMPTIONS.md')
    $publicOperational = @(Get-ChildItem -LiteralPath $publicTicketDir -File -Force | Where-Object { $us1PublicOnly -notcontains $_.Name })
    Assert-Case 'the public ticket directory holds only allowlisted names' ($publicOperational.Count -eq 0) (@($publicOperational | ForEach-Object { $_.Name }) -join ', ')

    $missingRootGuidance = Invoke-CommonProbe -Expression "Assert-ArtifactStorage -Root '$us1Root' -TicketId 'DEV-901'" -Environment @{}
    Assert-Case 'missing-root guidance names the ticket seed command' ((-not $missingRootGuidance.ok) -and ($missingRootGuidance.message -match 'DEV-901') -and ($missingRootGuidance.message -match '-Bootstrap')) $missingRootGuidance.message

    $unwritableRoot = New-FixtureDir 'us1-unwritable-root'
    $unwritableProbe = Invoke-CommonProbe -Expression @"
`$target = Join-Path '$unwritableRoot' 'probe-file'
[System.IO.File]::WriteAllText(`$target, 'block')
Assert-ArtifactStorage -Root `$target
"@ -Environment @{}
    Assert-Case 'a non-directory storage path is reported as unusable' ((-not $unwritableProbe.ok) -and ($unwritableProbe.message -match 'initialize-artifact-storage\.ps1')) $unwritableProbe.message

    $unwritableInit = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Bootstrap', '-TicketId', 'DEV-901', '-SourceDir', $publicTicketDir) -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $unwritableRoot 'probe-file') }
    Assert-Case 'bootstrap into an unusable root fails closed' ($unwritableInit.ExitCode -eq 1) "exit $($unwritableInit.ExitCode)"
    Assert-Case 'bootstrap names a remedy for the unusable root' ($unwritableInit.Output -match 'initialize-artifact-storage\.ps1 -Initialize') $unwritableInit.Output

    Write-Host ''
    Write-Host 'US1 confinement regressions (T009):'

    $us1ConfinementRoot = New-FixtureDir 'us1-confinement-root'
    $relativeRootProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRoot' -Environment @{ LAMUFLIX_ARTIFACT_ROOT = './artifacts' }
    Assert-Case 'a relative override root is rejected before use' ((-not $relativeRootProbe.ok) -and ($relativeRootProbe.message -match 'absolute')) $relativeRootProbe.message

    $worktreeShaped = New-FixtureDir 'us1-worktree-shaped-checkout'
    Write-FixtureText -Path (Join-Path $worktreeShaped '.git') -Text "gitdir: $($script:RepoRoot)/.git/worktrees/us1`n"
    $worktreeRootProbe = Invoke-CommonProbe -Expression 'Get-ArtifactRoot' -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $worktreeShaped 'artifacts') }
    Assert-Case 'a root inside a worktree-shaped checkout is rejected' ((-not $worktreeRootProbe.ok) -and ($worktreeRootProbe.message -match 'outside every repository')) $worktreeRootProbe.message

    $us1Ticket = Join-Path $us1ConfinementRoot 'LamuFlix/DEV-901'
    New-Item -ItemType Directory -Force -Path $us1Ticket | Out-Null
    foreach ($badTicketId in @('DEV-901/../DEV-902', 'DEV-901/x', '..', 'DEV-901/sub', '901')) {
        $badTicketProbe = Invoke-CommonProbe -Expression "Get-ArtifactTicketDir -TicketId '$badTicketId' -Root '$us1ConfinementRoot'" -Environment @{}
        Assert-Case "ticket id '$badTicketId' is rejected" (-not $badTicketProbe.ok) $badTicketProbe.message
    }

    $badRepositoryProbe = Invoke-CommonProbe -Expression "Get-ArtifactTicketDir -TicketId 'DEV-901' -Root '$us1ConfinementRoot'" -Environment @{ LAMUFLIX_ARTIFACT_REPOSITORY_ID = '..' }
    Assert-Case 'an invalid repository id blocks ticket resolution' (-not $badRepositoryProbe.ok) $badRepositoryProbe.message

    $ticketEscapeProbe = Invoke-CommonProbe -Expression "New-ArtifactChildPath -BaseDir '$us1Ticket' -RelativePath 'evidence/../../escaped.md'" -Environment @{}
    Assert-Case 'a traversal destination under a ticket is rejected' ((-not $ticketEscapeProbe.ok) -and ($ticketEscapeProbe.message -match 'traversal')) $ticketEscapeProbe.message

    $linkOutTarget = New-FixtureDir 'us1-link-out-target'
    $linkedChild = Join-Path $us1Ticket 'linked'
    $childLinkCreated = $false
    try {
        New-Item -ItemType SymbolicLink -Path $linkedChild -Target $linkOutTarget -ErrorAction Stop | Out-Null
        $childLinkCreated = $true
    } catch {
        $childLinkCreated = $false
    }
    if ($childLinkCreated) {
        $linkChildProbe = Invoke-CommonProbe -Expression "New-ArtifactChildPath -BaseDir '$us1Ticket' -RelativePath 'linked/record.md'" -Environment @{}
        Assert-Case 'a linked destination escaping the ticket is rejected' ((-not $linkChildProbe.ok) -and ($linkChildProbe.message -match 'link')) $linkChildProbe.message
    } else {
        Write-Host 'SKIP  linked destination escape case (link creation is not permitted on this host)'
    }

    $junctionTarget = New-FixtureDir 'us1-junction-target'
    $junctionPath = Join-Path $us1ConfinementRoot 'LamuFlix/DEV-902'
    $junctionCreated = $false
    try {
        New-Item -ItemType Junction -Path $junctionPath -Target $junctionTarget -ErrorAction Stop | Out-Null
        $junctionCreated = $true
    } catch {
        $junctionCreated = $false
    }
    if ($junctionCreated) {
        $junctionProbe = Invoke-CommonProbe -Expression "Get-ArtifactTicketDir -TicketId 'DEV-902' -Root '$us1ConfinementRoot' -RepositoryId 'LamuFlix'" -Environment @{}
        $junctionEscape = Invoke-CommonProbe -Expression "New-ArtifactChildPath -BaseDir '$junctionPath' -RelativePath '../../outside.md'" -Environment @{}
        Assert-Case 'a junction ticket directory cannot be reached through traversal' (-not $junctionEscape.ok) $junctionEscape.message
        $junctionLinkProbe = Invoke-CommonProbe -Expression "Test-ArtifactPrivatePath -Path (Join-Path '$junctionPath' 'brief.md')" -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $us1ConfinementRoot }
        Assert-Case 'a junction-redirected ticket path is not a sanctioned private path' ($junctionLinkProbe.ok -and (-not $junctionLinkProbe.value)) "$($junctionLinkProbe.message) got $($junctionLinkProbe.value)"
    } else {
        Write-Host 'SKIP  junction escape case (junction creation is not permitted on this host)'
    }

    Write-Host ''
    Write-Host 'US1 portable feature selection (T011):'

    $portableCheckout = New-FixtureDir 'us1-portable-checkout'
    New-Item -ItemType Directory -Force -Path (Join-Path $portableCheckout '.specify') | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $portableCheckout 'specs/DEV-901') | Out-Null
    $portableJson = Join-Path $portableCheckout '.specify/feature.json'
    $portableRoot = New-FixtureDir 'us1-portable-private-root'
    $portableEnv = @{ LAMUFLIX_ARTIFACT_ROOT = $portableRoot }

    Write-FixtureText -Path $portableJson -Text ('{"feature_directory":"specs/DEV-901"}')
    $portableProbe = Invoke-CommonProbe -Expression "Test-ArtifactPortableFeatureSelection -RepoRoot '$portableCheckout'" -Environment $portableEnv
    Assert-Case 'a repository-relative feature.json is portable' ($portableProbe.ok -and $portableProbe.value.PRESENT -and $portableProbe.value.PORTABLE) "$($portableProbe.message) got $($portableProbe.value.REASON)"

    $absentJsonProbe = Invoke-CommonProbe -Expression "Test-ArtifactPortableFeatureSelection -RepoRoot '$($script:Root)'" -Environment $portableEnv
    Assert-Case 'a missing feature.json is not a portability failure' ($absentJsonProbe.ok -and (-not $absentJsonProbe.value.PRESENT) -and $absentJsonProbe.value.PORTABLE) $absentJsonProbe.message

    Write-FixtureText -Path $portableJson -Text ("{`"feature_directory`":`"$($portableRoot -replace '\\', '\\')/LamuFlix/DEV-901`"}")
    $privateRootInJson = Invoke-CommonProbe -Expression "Test-ArtifactPortableFeatureSelection -RepoRoot '$portableCheckout'" -Environment $portableEnv
    Assert-Case 'a stored private artifact root is refused' ($privateRootInJson.ok -and (-not $privateRootInJson.value.PORTABLE)) "$($privateRootInJson.message) got $($privateRootInJson.value.REASON)"
    $privateRootAssert = Invoke-CommonProbe -Expression "Assert-ArtifactPortableFeatureSelection -RepoRoot '$portableCheckout' | Out-Null" -Environment $portableEnv
    Assert-Case 'asserting an unportable feature selection fails closed' ((-not $privateRootAssert.ok) -and ($privateRootAssert.message -match 'private artifact root')) $privateRootAssert.message

    Write-FixtureText -Path $portableJson -Text ('{"feature_directory":"../outside-checkout/specs/DEV-901"}')
    $outsideJson = Invoke-CommonProbe -Expression "Test-ArtifactPortableFeatureSelection -RepoRoot '$portableCheckout'" -Environment $portableEnv
    Assert-Case 'a machine path outside the checkout is refused' ($outsideJson.ok -and (-not $outsideJson.value.PORTABLE)) "$($outsideJson.message) got $($outsideJson.value.REASON)"

    Write-FixtureText -Path $portableJson -Text 'not json'
    $brokenJson = Invoke-CommonProbe -Expression "Test-ArtifactPortableFeatureSelection -RepoRoot '$portableCheckout'" -Environment $portableEnv
    Assert-Case 'an unparseable feature.json is refused' ($brokenJson.ok -and (-not $brokenJson.value.PORTABLE)) "$($brokenJson.message) got $($brokenJson.value.REASON)"

    $selectionPrecedenceEnv = Join-Path $script:Root 'us1-precedence'
    New-Item -ItemType Directory -Force -Path $selectionPrecedenceEnv | Out-Null
    $explicitDir = Join-Path $publicCheckout 'specs/DEV-901'
    Write-FixtureText -Path $portableJson -Text ('{"feature_directory":"specs/DEV-901"}')
    $precedenceProbe = Invoke-CommonProbe -Expression @'
$paths = Get-FeaturePathsEnv
[pscustomobject]@{ featureDir = $paths.FEATURE_DIR; publicSpec = $paths.PUBLIC_SPEC }
'@ -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $portableRoot; SPECIFY_FEATURE_DIRECTORY = $explicitDir } -WorkingDirectory $portableCheckout
    Assert-Case 'the explicit feature directory still wins over feature.json' ($precedenceProbe.ok -and (Test-ArtifactPathEqual $precedenceProbe.value.featureDir $explicitDir)) "$($precedenceProbe.message) got $($precedenceProbe.value.featureDir)"

    $jsonPrecedenceProbe = Invoke-CommonProbe -Expression "Get-FeaturePathsEnv" -Environment $portableEnv -WorkingDirectory $portableCheckout
    Assert-Case 'a portable feature.json selects its feature directory' ($jsonPrecedenceProbe.ok -and (Test-ArtifactPathEqual $jsonPrecedenceProbe.value.FEATURE_DIR (Join-Path $portableCheckout 'specs/DEV-901'))) "$($jsonPrecedenceProbe.message) got $($jsonPrecedenceProbe.value.FEATURE_DIR)"

    $resolverOnly = $true
    foreach ($consumer in @('check-prerequisites.ps1', 'setup-plan.ps1', 'setup-tasks.ps1')) {
        $consumerPath = Join-Path $PSScriptRoot $consumer
        $consumerLines = @(Get-Content -LiteralPath $consumerPath)
        if (-not (@($consumerLines | Where-Object { $_ -match 'Get-FeaturePathsEnv' }).Count -gt 0)) {
            $resolverOnly = $false
            Write-Host "      $consumer does not call the shared resolver" -ForegroundColor Yellow
        }
        $duplicated = @($consumerLines | Where-Object { $_.TrimStart() -notmatch '^#' -and $_ -match 'feature\.json|feature_directory' })
        if ($duplicated.Count -gt 0) {
            $resolverOnly = $false
            Write-Host "      $consumer reads feature selection outside the shared resolver: $($duplicated -join ' ')" -ForegroundColor Yellow
        }
    }
    Assert-Case 'feature-selection consumers use only the shared resolver' $resolverOnly 'a consumer duplicates feature selection'

    New-Item -ItemType Directory -Force -Path (Join-Path $portableCheckout 'specs/DEV-901') | Out-Null
    Write-FixtureText -Path $portableJson -Text ("{`"feature_directory`":`"$($portableRoot -replace '\\', '\\')`"}")
    $unportablePrivate = Invoke-CommonProbe -Expression "Get-ArtifactPathsEnv -TicketId 'DEV-901' -RequirePrivate | Out-Null" -Environment $portableEnv -WorkingDirectory $portableCheckout
    Assert-Case 'a private-required stage refuses an unportable feature selection' ((-not $unportablePrivate.ok) -and ($unportablePrivate.message -match 'private artifact root')) $unportablePrivate.message

    Write-FixtureText -Path $portableJson -Text ('{"feature_directory":"specs/DEV-901"}')
    $portablePrivate = Invoke-CommonProbe -Expression "Get-ArtifactPathsEnv -TicketId 'DEV-901'" -Environment $portableEnv -WorkingDirectory $portableCheckout
    Assert-Case 'a portable selection resolves without refusing' ($portablePrivate.ok -and $portablePrivate.value.PUBLIC_SPEC -match 'DEV-901') $portablePrivate.message

    Write-Host ''
    Write-Host 'US2 new-ticket create path writes private, public allowlist only (T012):'

    $newTicketCheckout = New-FixtureDir 'us2-new-ticket-checkout'
    New-Item -ItemType Directory -Force -Path (Join-Path $newTicketCheckout '.specify/templates') | Out-Null
    Write-FixtureText -Path (Join-Path $newTicketCheckout '.specify/templates/spec-template.md') -Text "# spec template`n"
    $newTicketRoot = Join-Path (New-FixtureDir 'us2-new-ticket-root') 'artifacts'

    $newTicketInit = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $newTicketRoot } -WorkingDirectory $newTicketCheckout
    Assert-Case 'the disposable private root initializes for the cut-over create path' ($newTicketInit.ExitCode -eq 0) "exit $($newTicketInit.ExitCode): $($newTicketInit.Output)"

    $createResult = Invoke-ChildScript -Script $script:CreateScript -Arguments @('-Json', '-TicketId', 'DEV-910', '-ShortName', 'new-ticket', 'Create a new ticket') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $newTicketRoot } -WorkingDirectory $newTicketCheckout
    Assert-Case 'the cut-over create path exits 0' ($createResult.ExitCode -eq 0) "exit $($createResult.ExitCode): $($createResult.Output)"
    $createPayload = Get-JsonPayload -Text $createResult.Output
    Assert-Case 'the create path reports the same private ticket directory the resolver derives' ($createPayload -and (Test-ArtifactPathEqual (Get-PayloadValue $createPayload 'PRIVATE_TICKET_DIR') (Join-Path $newTicketRoot 'LamuFlix/DEV-910'))) "got $(Get-PayloadValue $createPayload 'PRIVATE_TICKET_DIR')"

    $branchName = Get-PayloadValue $createPayload 'BRANCH_NAME'
    Assert-Case 'the create path reports the branch name' (-not [string]::IsNullOrWhiteSpace($branchName)) "got $branchName"
    $publicTicketDir = Join-Path $newTicketCheckout ('specs/' + $branchName)
    Assert-Case 'the public ticket directory holds spec.md only' (@(Get-ChildItem -LiteralPath $publicTicketDir -File -Force | ForEach-Object { $_.Name }) -join ',' -eq 'spec.md') (@(Get-ChildItem -LiteralPath $publicTicketDir -File -Force | ForEach-Object { $_.Name }) -join ',')
    Assert-Case 'the public ticket directory holds no operational subdirectory' (@(Get-ChildItem -LiteralPath $publicTicketDir -Directory -Force).Count -eq 0)
    Assert-Case 'no operational record lands under public specs/' (@(Get-ChildItem -LiteralPath (Join-Path $newTicketCheckout 'specs') -Recurse -File -Force | Where-Object { $_.Name -match '^(brief|plan|tasks)\.md$' -or $_.DirectoryName -match 'checklists$|evidence$|receipts$' }).Count -eq 0)

    $privateNewTicket = Join-Path $newTicketRoot 'LamuFlix/DEV-910'
    Assert-Case 'the private ticket directory carries the ticket record' (Test-Path -LiteralPath (Join-Path $privateNewTicket 'ticket.json') -PathType Leaf)
    Assert-Case 'the private ticket directory carries the operational subdirectories' ((Test-Path -LiteralPath (Join-Path $privateNewTicket 'checklists') -PathType Container) -and (Test-Path -LiteralPath (Join-Path $privateNewTicket 'evidence') -PathType Container) -and (Test-Path -LiteralPath (Join-Path $privateNewTicket 'receipts') -PathType Container))
    Assert-Case 'the private ticket record names the ticket and public directory' (@(Get-Content -LiteralPath (Join-Path $privateNewTicket 'ticket.json') -Raw | ConvertFrom-Json)[0].ticketId -eq 'DEV-910')

    Write-FixtureText -Path (Join-Path $publicTicketDir 'spec.md') -Text "# authored spec`n"
    $rerunCreate = Invoke-ChildScript -Script $script:CreateScript -Arguments @('-Json', '-AllowExistingBranch', '-TicketId', 'DEV-910', '-ShortName', 'new-ticket', 'Create a new ticket') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $newTicketRoot } -WorkingDirectory $newTicketCheckout
    Assert-Case 'a repeated create does not overwrite an authored spec' ((Get-Content -LiteralPath (Join-Path $publicTicketDir 'spec.md') -Raw) -eq "# authored spec`n") "exit $($rerunCreate.ExitCode)"

    $noRootCreate = Invoke-ChildScript -Script $script:CreateScript -Arguments @('-Json', '-TicketId', 'DEV-911', '-ShortName', 'no-root', 'Create without storage') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $script:Root 'us2-absent-root') } -WorkingDirectory $newTicketCheckout
    Assert-Case 'the create path fails closed without initialized private storage' ($noRootCreate.ExitCode -ne 0) "exit $($noRootCreate.ExitCode)"
    Assert-Case 'the create path names the initialization command' ($noRootCreate.Output -match 'initialize-artifact-storage\.ps1') $noRootCreate.Output
    Assert-Case 'the create path creates no public fallback when private storage is missing' (-not (Test-Path -LiteralPath (Join-Path $newTicketCheckout 'specs/001-no-root') -PathType Container)) 'public fallback was created'

    Write-Host ''
    Write-Host 'US2 competing real caller processes fail closed (T013):'

    $contentionRoot = New-FixtureDir 'us2-contention-root'
    $contentionCheckout = New-FixtureDir 'us2-contention-checkout'
    New-Item -ItemType Directory -Force -Path (Join-Path $contentionCheckout '.specify/templates') | Out-Null
    Write-FixtureText -Path (Join-Path $contentionCheckout '.specify/templates/spec-template.md') -Text "# spec template`n"
    Write-FixtureText -Path (Join-Path $contentionCheckout '.specify/templates/plan-template.md') -Text "# plan template`n"
    $contentionResult = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $contentionRoot } -WorkingDirectory $contentionCheckout
    Assert-Case 'the contention fixture private root initializes' ($contentionResult.ExitCode -eq 0) "exit $($contentionResult.ExitCode)"
    $contentionTicket = Join-Path $contentionRoot 'LamuFlix/DEV-912'
    New-Item -ItemType Directory -Force -Path $contentionTicket | Out-Null

    $heldHandle = Invoke-CommonProbe -Expression "Acquire-ArtifactTicketOwnership -TicketDir '$contentionTicket' -Owner 'writer-a' -WorktreeId 'worktree-a'" -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $contentionRoot }
    Assert-Case 'the first real writer takes ownership' $heldHandle.ok $heldHandle.message

    $contentionEnv = @{ LAMUFLIX_ARTIFACT_ROOT = $contentionRoot; SPECIFY_FEATURE_DIRECTORY = (Join-Path $contentionCheckout 'specs/DEV-912') }
    New-Item -ItemType Directory -Force -Path (Join-Path $contentionCheckout 'specs/DEV-912') | Out-Null
    $blockedWriter = Invoke-ChildScript -Script $script:SetupPlanScript -Arguments @('-Json') -Environment $contentionEnv -WorkingDirectory $contentionCheckout
    Assert-Case 'a competing real writer process fails closed' ($blockedWriter.ExitCode -ne 0) "exit $($blockedWriter.ExitCode): $($blockedWriter.Output)"
    Assert-Case 'the competing writer names the holder' ($blockedWriter.Output -match 'writer-a') $blockedWriter.Output
    Assert-Case 'no private record is written by the blocked writer' (-not (Test-Path -LiteralPath (Join-Path $contentionTicket 'plan.md') -PathType Leaf))
    Assert-Case 'no public fallback record is written by the blocked writer' (-not (Test-Path -LiteralPath (Join-Path $contentionCheckout 'specs/DEV-912/plan.md') -PathType Leaf))

    $secondBlocked = Invoke-ChildScript -Script $script:SetupPlanScript -Arguments @('-Json') -Environment $contentionEnv -WorkingDirectory $contentionCheckout
    Assert-Case 'a second competing writer process also fails closed' ($secondBlocked.ExitCode -ne 0) "exit $($secondBlocked.ExitCode)"
    $lockProbe = Invoke-CommonProbe -Expression "Test-Path -LiteralPath (Get-ArtifactOwnershipPath '$contentionTicket')" -Environment @{}
    Assert-Case 'denied writers never steal the lock' ($lockProbe.ok -and $lockProbe.value) "$($lockProbe.message)"

    $releaseProbe = Invoke-CommonProbe -Expression @"
`$holder = Get-ArtifactOwnership '$contentionTicket'
Release-ArtifactTicketOwnership ([pscustomobject]@{ TICKET_DIR = '$contentionTicket'; PATH = (Get-ArtifactOwnershipPath '$contentionTicket'); TOKEN = (Get-ArtifactProperty `$holder 'token'); OWNER = (Get-ArtifactProperty `$holder 'owner') })
"@ -Environment @{}
    Assert-Case 'the holder releases explicitly' $releaseProbe.ok $releaseProbe.message

    $releasedWriter = Invoke-ChildScript -Script $script:SetupPlanScript -Arguments @('-Json') -Environment $contentionEnv -WorkingDirectory $contentionCheckout
    Assert-Case 'the real writer proceeds after explicit release' ($releasedWriter.ExitCode -eq 0) "exit $($releasedWriter.ExitCode): $($releasedWriter.Output)"
    Assert-Case 'the released writer writes its record privately' (Test-Path -LiteralPath (Join-Path $contentionTicket 'plan.md') -PathType Leaf)
    Assert-Case 'the released writer writes no public duplicate' (-not (Test-Path -LiteralPath (Join-Path $contentionCheckout 'specs/001-contended/plan.md') -PathType Leaf))

    $mutableProbe = Invoke-CommonProbe -Expression @"
`$runDir = Get-ArtifactRunDir -TicketDir '$contentionTicket' -CommitSha ('c' * 40) -Kind 'receipts'
`$evidence = New-ArtifactProvenance -TicketId 'DEV-912' -CommitSha ('c' * 40) -WorktreeId 'worktree-a' -Kind 'receipts'
[pscustomobject]@{ runDir = `$runDir; sha = `$evidence.commitSha; ticket = `$evidence.ticketId }
"@ -Environment @{}
    Assert-Case 'mutable receipt output is isolated by full SHA' ($mutableProbe.ok -and $mutableProbe.value.runDir -match 'receipts[/\\]sha') $mutableProbe.message
    Assert-Case 'mutable output carries SHA provenance' ($mutableProbe.ok -and $mutableProbe.value.sha -eq ('c' * 40)) $mutableProbe.message

    Write-Host ''
    Write-Host 'US2 consumer routes pass the narrow guard allowance (T013b):'

    $routeRoot = New-FixtureDir 'us2-route-root'
    $routeCheckout = $script:RepoRoot
    $routeInit = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $routeRoot } -WorkingDirectory $routeCheckout
    Assert-Case 'the consumer-route fixture private root initializes' ($routeInit.ExitCode -eq 0) "exit $($routeInit.ExitCode)"

    $routePaths = Invoke-CommonProbe -Expression @"
`$ticketDir = New-ArtifactTicketWorkspace -TicketId 'DEV-913' -Root '$routeRoot' -RepoRoot '$routeCheckout'
[pscustomobject]@{
    ticketDir = `$ticketDir
    plan = (Join-Path `$ticketDir 'plan.md')
    brief = (Join-Path `$ticketDir 'brief.md')
    tasks = (Join-Path `$ticketDir 'tasks.md')
    checklist = (Join-Path (Join-Path `$ticketDir 'checklists') 'requirements.md')
    receipt = (Join-Path (Get-ArtifactRunDir -TicketDir `$ticketDir -CommitSha ('d' * 40) -Kind 'receipts') 'receipt.json')
    evidence = (Join-Path (Get-ArtifactRunDir -TicketDir `$ticketDir -CommitSha ('d' * 40) -Kind 'evidence') 'gate.json')
    outsideTicket = (Join-Path '$routeRoot' 'LamuFlix/brief.md')
    otherRepository = (Join-Path '$routeRoot' 'OtherRepo/DEV-913/brief.md')
    unrelated = (Join-Path '$routeRoot' 'elsewhere/brief.md')
}
"@ -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $routeRoot } -WorkingDirectory $routeCheckout
    Assert-Case 'the consumer-route fixture resolves its private record paths' $routePaths.ok $routePaths.message

    foreach ($routeName in @('plan', 'brief', 'tasks', 'checklist', 'receipt', 'evidence')) {
        $routePath = $routePaths.value.$routeName
        foreach ($tool in @('Edit', 'Write')) {
            $routeExit = Invoke-Guard -Tool $tool -Path $routePath -Cwd $routeCheckout -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $routeRoot }
            Assert-Case "the real $routeName write route is allowed through the guard ($tool)" ($routeExit -eq 0) "exit $routeExit for $routePath"
        }
    }

    foreach ($deniedName in @('outsideTicket', 'otherRepository', 'unrelated')) {
        $deniedPath = $routePaths.value.$deniedName
        $deniedExit = Invoke-Guard -Tool 'Edit' -Path $deniedPath -Cwd $routeCheckout -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $routeRoot }
        Assert-Case "the $deniedName route stays denied" ($deniedExit -eq 2) "exit $deniedExit for $deniedPath"
    }

    $patchRoute = "*** Begin Patch`n*** Add File: $($routePaths.value.plan)`n*** End Patch"
    $patchRouteExit = Invoke-Guard -Tool 'apply_patch' -Command $patchRoute -Cwd $routeCheckout -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $routeRoot }
    Assert-Case 'the real plan route is allowed through an apply-patch' ($patchRouteExit -eq 0) "exit $patchRouteExit"

    $shellRoute = Invoke-Guard -Tool 'Bash' -Command "Set-Content -LiteralPath '$($routePaths.value.plan)' -Value 'x'" -Cwd $routeCheckout -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $routeRoot }
    Assert-Case 'a shell command gains no private-write route from the allowance' ($shellRoute -eq 0) "exit $shellRoute"

    Write-Host ''
    Write-Host 'US2 prerequisite stage detection is public/private aware (T015):'

    $stageCheckout = New-FixtureDir 'us2-stage-checkout'
    New-Item -ItemType Directory -Force -Path (Join-Path $stageCheckout '.specify') | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $stageCheckout '.git') | Out-Null
    $stageSpecs = Join-Path $stageCheckout 'specs/001-staged'
    Write-FixtureText -Path (Join-Path $stageSpecs 'spec.md') -Text "# staged spec`n"
    $stageRoot = Join-Path (New-FixtureDir 'us2-stage-root') 'artifacts'
    $stageEnv = @{ LAMUFLIX_ARTIFACT_ROOT = $stageRoot; SPECIFY_FEATURE_DIRECTORY = $stageSpecs; SPECIFY_FEATURE = '001-staged' }

    $stagePublic = Invoke-ChildScript -Script $script:CheckPrereqScript -Arguments @('-Json', '-PathsOnly') -Environment $stageEnv -WorkingDirectory $stageCheckout
    Assert-Case 'prerequisite discovery succeeds without private storage' ($stagePublic.ExitCode -eq 0) "exit $($stagePublic.ExitCode): $($stagePublic.Output)"
    $stagePayload = Get-JsonPayload -Text $stagePublic.Output
    Assert-Case 'prerequisite discovery reports the public spec path' ((Get-PayloadValue $stagePayload 'FEATURE_SPEC') -like '*001-staged*') "got $(Get-PayloadValue $stagePayload 'FEATURE_SPEC')"
    Assert-Case 'prerequisite discovery reports no private availability' ((Get-PayloadValue $stagePayload 'PRIVATE_AVAILABLE') -eq $false) "got $(Get-PayloadValue $stagePayload 'PRIVATE_AVAILABLE')"
    Assert-Case 'prerequisite discovery creates no private root' (-not (Test-Path -LiteralPath $stageRoot)) 'the private root was created'

    $stageRequired = Invoke-ChildScript -Script $script:CheckPrereqScript -Arguments @('-Json', '-PathsOnly', '-RequirePrivate') -Environment $stageEnv -WorkingDirectory $stageCheckout
    Assert-Case 'a private-required stage detection fails closed without storage' ($stageRequired.ExitCode -ne 0) "exit $($stageRequired.ExitCode)"
    Assert-Case 'the private-required failure names the initialization command' ($stageRequired.Output -match 'initialize-artifact-storage\.ps1') $stageRequired.Output
    Assert-Case 'a private-required failure creates no private root implicitly' (-not (Test-Path -LiteralPath $stageRoot)) 'the private root was created'
    Assert-Case 'a private-required failure writes no public operational record' (@(Get-ChildItem -LiteralPath $stageSpecs -Recurse -File -Force | ForEach-Object { $_.Name }) -join ',' -eq 'spec.md') (@(Get-ChildItem -LiteralPath $stageSpecs -Recurse -File -Force | ForEach-Object { $_.Name }) -join ',')

    $stageInit = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize') -Environment $stageEnv -WorkingDirectory $stageCheckout
    Assert-Case 'the stage fixture root initializes' ($stageInit.ExitCode -eq 0) "exit $($stageInit.ExitCode)"
    $stageWorkspace = Invoke-CommonProbe -Expression "New-ArtifactTicketWorkspace -TicketId 'DEV-914' -Root '$stageRoot' -RepoRoot '$stageCheckout'" -Environment $stageEnv -WorkingDirectory $stageCheckout
    Assert-Case 'the stage fixture ticket workspace resolves' $stageWorkspace.ok $stageWorkspace.message
    Write-FixtureText -Path (Join-Path $stageWorkspace.value 'plan.md') -Text "# private plan`n"
    $stageTasksEnv = @{ LAMUFLIX_ARTIFACT_ROOT = $stageRoot; SPECIFY_FEATURE_DIRECTORY = (Join-Path $stageCheckout 'specs/DEV-914'); SPECIFY_FEATURE = 'DEV-914' }
    New-Item -ItemType Directory -Force -Path (Join-Path $stageCheckout 'specs/DEV-914') | Out-Null
    Write-FixtureText -Path (Join-Path $stageCheckout 'specs/DEV-914/spec.md') -Text "# DEV-914 spec`n"

    $stagePrivate = Invoke-ChildScript -Script $script:CheckPrereqScript -Arguments @('-Json') -Environment $stageTasksEnv -WorkingDirectory $stageCheckout
    Assert-Case 'stage detection resolves a plan the private directory holds' ($stagePrivate.ExitCode -eq 0) "exit $($stagePrivate.ExitCode): $($stagePrivate.Output)"
    $privatePlanPayload = Get-JsonPayload -Text $stagePrivate.Output
    Assert-Case 'stage detection reports the private plan path' ((Get-PayloadValue $privatePlanPayload 'IMPL_PLAN') -like '*DEV-914*plan.md') "got $(Get-PayloadValue $privatePlanPayload 'IMPL_PLAN')"
    Assert-Case 'stage detection does not require a bootstrap seed proof for a new ticket' ($stagePrivate.ExitCode -eq 0) "exit $($stagePrivate.ExitCode)"
    Assert-Case 'stage detection performs no migration side effect' (-not (Test-Path -LiteralPath (Join-Path $stageWorkspace.value 'tasks.md') -PathType Leaf)) 'a record was migrated'

    $stageMissingPlan = Invoke-ChildScript -Script $script:CheckPrereqScript -Arguments @('-Json') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $stageRoot; SPECIFY_FEATURE_DIRECTORY = (Join-Path $stageCheckout 'specs/DEV-915'); SPECIFY_FEATURE = 'DEV-915' } -WorkingDirectory $stageCheckout
    Assert-Case 'a missing plan fails the stage rather than being recreated publicly' ($stageMissingPlan.ExitCode -ne 0) "exit $($stageMissingPlan.ExitCode)"
    Assert-Case 'no public plan is recreated by stage detection' (-not (Test-Path -LiteralPath (Join-Path $stageCheckout 'specs/DEV-915/plan.md') -PathType Leaf)) 'a public plan was created'

    Write-Host ''
    Write-Host 'US3 verified migration copies without clobber and verifies raw bytes (T019):'

    $migrationRepo = New-GitFixtureCheckout 'us3-migration-repo'
    $migrationRoot = New-FixtureDir 'us3-migration-root'
    $migrationEnv = @{ LAMUFLIX_ARTIFACT_ROOT = $migrationRoot; SPECIFY_FEATURE_DIRECTORY = ''; SPECIFY_FEATURE = ''; PYTHONUTF8 = '1' }

    $ticketA = Join-Path $migrationRepo 'specs/DEV-901'
    # A machine-looking source path is the input this case exists to see sanitized. It is
    # assembled from segments rather than written as one literal so this companion does not
    # reintroduce into tracked files the very machine path DEV-473 removes.
    $machineRootPath = @('F:', 'Dev', 'LamuFlix.worktrees', 'DEV-901', 'evidence', 'ruling.txt') -join '/'
    $machineRootPrefix = @('F:', 'Dev', 'LamuFlix.worktrees') -join '/'
    Write-FixtureText -Path (Join-Path $ticketA 'spec.md') -Text "# DEV-901 spec`r`n"
    Write-FixtureText -Path (Join-Path $ticketA 'CONCLUSIONS.md') -Text "# DEV-901 conclusions`r`nRuling recorded at $machineRootPath and C:\Users\fixture\AppData\Local\Temp\ruling.txt.`r`n"
    Write-FixtureText -Path (Join-Path $ticketA 'ASSUMPTIONS.md') -Text "# DEV-901 assumptions`n"
    Write-FixtureText -Path (Join-Path $ticketA 'brief.md') -Text "# DEV-901 brief`r`nline two`r`n"
    Write-FixtureText -Path (Join-Path $ticketA 'plan.md') -Text "# DEV-901 plan`nunix newline must stay byte identical`n"
    Write-FixtureText -Path (Join-Path $ticketA 'tasks.md') -Text "# DEV-901 tasks`r`n"
    Write-FixtureText -Path (Join-Path $ticketA 'checklists/requirements.md') -Text "# DEV-901 requirements`r`n"

    $ticketB = Join-Path $migrationRepo 'specs/DEV-902'
    Write-FixtureText -Path (Join-Path $ticketB 'spec.md') -Text "# DEV-902 spec`n"
    Write-FixtureText -Path (Join-Path $ticketB 'brief.md') -Text "# DEV-902 brief`n"

    $reviewEvidence = Join-Path $migrationRepo 'docs/reviews/restyle'
    Write-FixtureText -Path (Join-Path $reviewEvidence 'README.md') -Text "# restyle review`n"
    Write-FixtureText -Path (Join-Path $reviewEvidence 'evidence/build.txt') -Text "build ok`n"
    Write-FixtureText -Path (Join-Path $migrationRepo 'specs/PRODUCT.md') -Text "# product`n"

    & git -C $migrationRepo add -A 2>&1 | Out-Null
    & git -C $migrationRepo commit --quiet -m 'migration fixture' 2>&1 | Out-Null
    Assert-Case 'the migration fixture repository commits its tracked records' (@(& git -C $migrationRepo rev-list --count HEAD 2>&1) -join '' -match '^\s*1\s*$') (@(& git -C $migrationRepo status --porcelain 2>&1) -join ' ')

    $migrationInit = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize') -Environment $migrationEnv -WorkingDirectory $migrationRepo
    Assert-Case 'the migration fixture private root initializes' ($migrationInit.ExitCode -eq 0) "exit $($migrationInit.ExitCode): $($migrationInit.Output)"

    $dryRun = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Migrate', '-All', '-Json') -Environment $migrationEnv -WorkingDirectory $migrationRepo
    Assert-Case 'bulk migration copies and verifies without removing anything' ($dryRun.ExitCode -eq 0) "exit $($dryRun.ExitCode): $($dryRun.Output)"
    $dryRunPayload = Get-JsonPayload -Text $dryRun.Output
    Assert-Case 'bulk migration reports a verified inventory' ((Get-PayloadValue $dryRunPayload 'VERIFIED') -eq $true) "payload $(Get-PayloadValue $dryRunPayload 'VERIFIED')"
    Assert-Case 'bulk migration verifies every inventoried file' ((Get-PayloadValue $dryRunPayload 'FILE_COUNT') -eq (Get-PayloadValue $dryRunPayload 'EXPECTED_COUNT')) "counts $(Get-PayloadValue $dryRunPayload 'FILE_COUNT')/$(Get-PayloadValue $dryRunPayload 'EXPECTED_COUNT')"
    Assert-Case 'bulk migration stages no removal when it was not asked to' (@(Get-PayloadValue $dryRunPayload 'STAGED_REMOVALS').Count -eq 0) 'a removal was staged'
    Assert-Case 'bulk migration leaves every public source in place' ((Test-Path -LiteralPath (Join-Path $ticketA 'brief.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $ticketB 'brief.md') -PathType Leaf))

    $privateRootRepo = Join-Path $migrationRoot 'LamuFlix'
    $privateA = Join-Path $privateRootRepo 'DEV-901'
    $privateB = Join-Path $privateRootRepo 'DEV-902'
    Assert-Case 'bulk migration files the ticket records under the canonical ticket directory' ((Test-Path -LiteralPath (Join-Path $privateA 'checklists/requirements.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $privateA 'brief.md') -PathType Leaf)) 'private ticket record missing'
    Assert-Case 'bulk migration copies the public allowlist into private storage too' ((Test-Path -LiteralPath (Join-Path $privateA 'spec.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $privateA 'CONCLUSIONS.md') -PathType Leaf))
    Assert-Case 'bulk migration files repository-level review evidence' (Test-Path -LiteralPath (Join-Path $privateRootRepo 'reviews/restyle/evidence/build.txt') -PathType Leaf)
    Assert-Case 'bulk migration keeps the product document out of the inventory' (-not (Test-Path -LiteralPath (Join-Path $privateRootRepo 'PRODUCT.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $migrationRepo 'specs/PRODUCT.md') -PathType Leaf))

    $crlfPrivate = Join-Path $privateA 'brief.md'
    Assert-Case 'bulk migration copies raw bytes without newline normalization' ((Get-ArtifactFileHash -Path $crlfPrivate) -eq (Get-ArtifactFileHash -Path (Join-Path $ticketA 'brief.md'))) 'raw-byte hash differs'

    $idempotentRerun = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Migrate', '-All', '-Json') -Environment $migrationEnv -WorkingDirectory $migrationRepo
    Assert-Case 'an identical bulk rerun succeeds idempotently' ($idempotentRerun.ExitCode -eq 0) "exit $($idempotentRerun.ExitCode): $($idempotentRerun.Output)"
    Assert-Case 'an identical bulk rerun reports no failure' (@((Get-JsonPayload -Text $idempotentRerun.Output).FAILURES).Count -eq 0) 'a rerun failure was reported'

    Write-FixtureText -Path $crlfPrivate -Text "# DEV-901 brief`r`nconflicting private copy`r`n"
    $conflict = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Migrate', '-All', '-StageRemovals', '-Json') -Environment $migrationEnv -WorkingDirectory $migrationRepo
    Assert-Case 'a differing destination blocks the migration' ($conflict.ExitCode -eq 1) "exit $($conflict.ExitCode)"
    $conflictPayload = Get-JsonPayload -Text $conflict.Output
    Assert-Case 'a conflicting destination is reported as a conflict' (@((Get-PayloadValue $conflictPayload 'FAILURES') | Where-Object { $_.outcome -eq 'conflict' }).Count -ge 1) (@(Get-PayloadValue $conflictPayload 'FAILURES') | ConvertTo-Json -Compress)
    Assert-Case 'a blocked migration stages no removal' (@(Get-PayloadValue $conflictPayload 'STAGED_REMOVALS').Count -eq 0) 'a removal was staged'
    Assert-Case 'a blocked migration keeps every public source intact' ((Test-Path -LiteralPath (Join-Path $ticketA 'brief.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $ticketB 'brief.md') -PathType Leaf))
    Assert-Case 'a blocked migration does not clobber the differing private copy' ((Get-Content -LiteralPath $crlfPrivate -Raw) -match 'conflicting private copy') 'the private copy was overwritten'

    $unsanitizedPrivateConclusions = Get-ArtifactFileHash -Path (Join-Path $ticketA 'CONCLUSIONS.md')
    & git -C $migrationRepo reset --hard --quiet HEAD 2>&1 | Out-Null
    Remove-Item -LiteralPath $crlfPrivate -Force
    $sanitizedRun = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Migrate', '-All', '-StageRemovals', '-Json') -Environment $migrationEnv -WorkingDirectory $migrationRepo
    Assert-Case 'a verified bulk migration with removals succeeds' ($sanitizedRun.ExitCode -eq 0) "exit $($sanitizedRun.ExitCode): $($sanitizedRun.Output)"
    $sanitizedPayload = Get-JsonPayload -Text $sanitizedRun.Output
    Assert-Case 'the migration stages the verified non-allowlist removals' (@(Get-PayloadValue $sanitizedPayload 'STAGED_REMOVALS').Count -ge 5) (@(Get-PayloadValue $sanitizedPayload 'STAGED_REMOVALS') -join ', ')

    Assert-Case 'the sanitized public conclusions stay published' (Test-Path -LiteralPath (Join-Path $ticketA 'CONCLUSIONS.md') -PathType Leaf)
    $publicConclusions = Get-Content -LiteralPath (Join-Path $ticketA 'CONCLUSIONS.md') -Raw
    Assert-Case 'the published conclusions keep their decision substance' ($publicConclusions -match 'DEV-901 conclusions') $publicConclusions
    Assert-Case 'the published conclusions carry no machine path' (-not ($publicConclusions -match '[A-Za-z]:[\\/]')) $publicConclusions
    Assert-Case 'the published conclusions use portable identifiers' ($publicConclusions -match '<task-worktree>' -and $publicConclusions -match '<temp>') $publicConclusions
    Assert-Case 'the public spec is preserved and untouched' ((Get-Content -LiteralPath (Join-Path $ticketA 'spec.md') -Raw) -eq "# DEV-901 spec`r`n")
    Assert-Case 'the public assumptions file is preserved' (Test-Path -LiteralPath (Join-Path $ticketA 'ASSUMPTIONS.md') -PathType Leaf)
    Assert-Case 'the public spec is never a removal candidate' (@((Get-PayloadValue $sanitizedPayload 'STAGED_REMOVALS') | Where-Object { $_ -match '(^|[\\/])(spec|CONCLUSIONS|ASSUMPTIONS)\.md$' }).Count -eq 0) (@(Get-PayloadValue $sanitizedPayload 'STAGED_REMOVALS') -join ', ')

    Assert-Case 'the private unsanitized conclusions remain byte-identical (FR-010)' ((Get-ArtifactFileHash -Path (Join-Path $privateA 'CONCLUSIONS.md')) -eq $unsanitizedPrivateConclusions)
    Assert-Case 'the private original still holds the machine path' ((Get-Content -LiteralPath (Join-Path $privateA 'CONCLUSIONS.md') -Raw) -match [regex]::Escape($machineRootPrefix))

    Assert-Case 'the migrated ticket directory keeps only the public allowlist' (@(Get-ChildItem -LiteralPath $ticketA -File -Force | ForEach-Object { $_.Name } | Sort-Object) -join ',' -eq 'ASSUMPTIONS.md,CONCLUSIONS.md,spec.md') (@(Get-ChildItem -LiteralPath $ticketA -File -Force | ForEach-Object { $_.Name }) -join ',')
    Assert-Case 'the migrated ticket directory keeps no operational subdirectory' (@(Get-ChildItem -LiteralPath $ticketA -Directory -Force).Count -eq 0)
    Assert-Case 'a ticket without public conclusions receives no synthesized file (P7)' (-not (Test-Path -LiteralPath (Join-Path $ticketB 'CONCLUSIONS.md')) -and -not (Test-Path -LiteralPath (Join-Path $privateB 'CONCLUSIONS.md')))
    Assert-Case 'a ticket without public conclusions keeps only its allowlist' (@(Get-ChildItem -LiteralPath $ticketB -File -Force | ForEach-Object { $_.Name }) -join ',' -eq 'spec.md') (@(Get-ChildItem -LiteralPath $ticketB -File -Force | ForEach-Object { $_.Name }) -join ',')
    Assert-Case 'the archived ticket keeps its operational records privately' ((Test-Path -LiteralPath (Join-Path $privateB 'brief.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $privateA 'tasks.md') -PathType Leaf))
    Assert-Case 'the review evidence leaves the public tree' (-not (Test-Path -LiteralPath (Join-Path $migrationRepo 'docs/reviews/restyle/README.md')) -and (Test-Path -LiteralPath (Join-Path $privateRootRepo 'reviews/restyle/README.md') -PathType Leaf))
    Assert-Case 'the migration rewrites no Git history' (@(& git -C $migrationRepo rev-list --count HEAD 2>&1) -join '' -match '^\s*1\s*$') (@(& git -C $migrationRepo rev-list --count HEAD 2>&1) -join ' ')
    Assert-Case 'the migration records its verification receipt with SHA provenance' ((Get-PayloadValue $sanitizedPayload 'RECEIPT') -and (Test-Path -LiteralPath (Get-PayloadValue $sanitizedPayload 'RECEIPT') -PathType Leaf))
    $bulkReceipt = Get-Content -LiteralPath (Get-PayloadValue $sanitizedPayload 'RECEIPT') -Raw | ConvertFrom-Json
    Assert-Case 'the migration receipt declares raw-byte verification' ([bool]$bulkReceipt.rawByteSha256)
    Assert-Case 'the migration receipt carries the full commit SHA' ($bulkReceipt.commitSha -match '^[0-9a-f]{40}$') "commitSha $($bulkReceipt.commitSha)"

    Write-Host ''
    Write-Host 'US3 ordinary scripts never migrate as a side effect (T020):'

    $noSideEffectRepo = New-GitFixtureCheckout 'us3-side-effect-repo'
    $noSideEffectRoot = New-FixtureDir 'us3-side-effect-root'
    $noSideEffectTicket = Join-Path $noSideEffectRepo 'specs/DEV-901'
    Write-FixtureText -Path (Join-Path $noSideEffectTicket 'spec.md') -Text "# DEV-901 spec`n"
    Write-FixtureText -Path (Join-Path $noSideEffectTicket 'brief.md') -Text "# DEV-901 brief`n"
    Write-FixtureText -Path (Join-Path $noSideEffectTicket 'plan.md') -Text "# DEV-901 plan`n"
    & git -C $noSideEffectRepo add -A 2>&1 | Out-Null
    & git -C $noSideEffectRepo commit --quiet -m 'side-effect fixture' 2>&1 | Out-Null
    $noSideEffectInit = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $noSideEffectRoot } -WorkingDirectory $noSideEffectRepo
    Assert-Case 'the side-effect fixture private root initializes' ($noSideEffectInit.ExitCode -eq 0) "exit $($noSideEffectInit.ExitCode)"
    $publicBefore = @(Get-ChildItem -LiteralPath $noSideEffectTicket -Recurse -File -Force | ForEach-Object { $_.Name } | Sort-Object) -join ','

    $prereqSideEffect = Invoke-ChildScript -Script $script:CheckPrereqScript -Arguments @('-Json', '-PathsOnly') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $noSideEffectRoot; SPECIFY_FEATURE_DIRECTORY = $noSideEffectTicket; SPECIFY_FEATURE = '901-fixture' } -WorkingDirectory $noSideEffectRepo
    Assert-Case 'prerequisite discovery completes on a migrated-shaped ticket' ($prereqSideEffect.ExitCode -eq 0) "exit $($prereqSideEffect.ExitCode): $($prereqSideEffect.Output)"
    Assert-Case 'prerequisite discovery migrates nothing' (-not (Test-Path -LiteralPath (Join-Path $noSideEffectRoot 'LamuFlix/DEV-901') -PathType Container)) 'a private ticket directory was created'
    Assert-Case 'prerequisite discovery removes no public record' (@(Get-ChildItem -LiteralPath $noSideEffectTicket -Recurse -File -Force | ForEach-Object { $_.Name } | Sort-Object) -join ',' -eq $publicBefore) 'a public record changed'
    Assert-Case 'prerequisite discovery stages no Git removal' (@(& git -C $noSideEffectRepo status --porcelain 2>&1).Count -eq 0) (@(& git -C $noSideEffectRepo status --porcelain 2>&1) -join ' ')

    $planSideEffect = Invoke-ChildScript -Script $script:SetupPlanScript -Arguments @('-Json') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $noSideEffectRoot; SPECIFY_FEATURE_DIRECTORY = $noSideEffectTicket; SPECIFY_FEATURE = '901-fixture' } -WorkingDirectory $noSideEffectRepo
    Assert-Case 'plan setup on an uncut-over ticket writes no private archive of the old records' (-not (Test-Path -LiteralPath (Join-Path $noSideEffectRoot 'LamuFlix/DEV-901/brief.md') -PathType Leaf)) 'brief.md was archived as a side effect'
    Assert-Case 'plan setup leaves the public brief and tasks in place' ((Test-Path -LiteralPath (Join-Path $noSideEffectTicket 'brief.md') -PathType Leaf) -and -not (Test-Path -LiteralPath (Join-Path $noSideEffectTicket 'tasks.md') -PathType Leaf)) "exit $($planSideEffect.ExitCode)"

    Write-Host ''
    Write-Host 'US3 historical end state and non-synthesis (T020b):'

    $migrationSpecsDir = Join-Path $migrationRepo 'specs'
    $noConclusionsTickets = @(Get-ChildItem -LiteralPath $migrationSpecsDir -Directory -Force | Where-Object { -not (Test-Path -LiteralPath (Join-Path $_.FullName 'CONCLUSIONS.md')) })
    Assert-Case 'the fixture keeps a ticket that has no public conclusions' (@($noConclusionsTickets | Where-Object { $_.Name -eq 'DEV-902' }).Count -eq 1) (@($noConclusionsTickets | ForEach-Object { $_.Name }) -join ',')
    Assert-Case 'no historical conclusions file was invented anywhere' (@(Get-ChildItem -LiteralPath $migrationSpecsDir -Recurse -File -Force -Filter 'CONCLUSIONS.md' | Where-Object { $_.DirectoryName -match 'DEV-902$' }).Count -eq 0)
    Assert-Case 'every migrated ticket directory holds only allowlisted names' (@(Get-ChildItem -LiteralPath $migrationSpecsDir -Directory -Force | ForEach-Object { @(Get-ChildItem -LiteralPath $_.FullName -Recurse -File -Force | Where-Object { $_.Name -notin @('spec.md', 'CONCLUSIONS.md', 'ASSUMPTIONS.md') }).Count } | Measure-Object -Sum).Sum -eq 0) 'an operational record survived publicly'
    Assert-Case 'the archive holds the operational records the public tree shed' ((Test-Path -LiteralPath (Join-Path $privateA 'plan.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $privateA 'checklists/requirements.md') -PathType Leaf))

    Write-Host ''
    Write-Host 'US4 retained tracked content carries no machine-root path (T024):'

    # The machine root is assembled from segments so this companion never becomes the
    # tracked file it is checking for. Both slash spellings must be absent, case-insensitively.
    $scrubRootBackslash = (@('F:', 'Dev') -join '\')
    $scrubRootForward = (@('F:', 'Dev') -join '/')
    $trackedMatches = @(& git -C $script:RepoRoot grep -n -I -i -F -e $scrubRootBackslash -e $scrubRootForward -- . ':!.gitignore' 2>$null)
    Assert-Case 'no retained tracked file outside .gitignore holds the machine root' ($trackedMatches.Count -eq 0) (@($trackedMatches | Select-Object -First 5) -join ' | ')

    $trackedForwardOnly = @(& git -C $script:RepoRoot grep -n -I -i -F -e $scrubRootForward -- . ':!.gitignore' 2>$null)
    Assert-Case 'the forward-slash variant of the machine root is absent too' ($trackedForwardOnly.Count -eq 0) (@($trackedForwardOnly | Select-Object -First 5) -join ' | ')

    $harnessText = Get-Content -LiteralPath (Join-Path $script:RepoRoot 'harness.yml') -Raw
    Assert-Case 'the tracked harness config keeps no machine-specific worktree root' (-not ($harnessText -match '(?i)worktreeRoot\s*:\s*[A-Za-z]:')) $harnessText

    $homeDefaults = @(Get-ChildItem -LiteralPath (Join-Path $script:RepoRoot 'scripts/home') -Filter '*.ps1' -File | Where-Object { $_.Name -notlike 'Test-*' })
    $homeAbsoluteDefaults = @($homeDefaults | Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match '(?i)\$ConfigPath\s*=\s*''[A-Za-z]:' })
    Assert-Case 'no Home operator script keeps an absolute ConfigPath default' ($homeAbsoluteDefaults.Count -eq 0) (@($homeAbsoluteDefaults | ForEach-Object { $_.Name }) -join ', ')

    Write-Host ''
    Write-Host 'US4 scrubbed defaults preserve intended behaviour (T024b):'

    # A disposable repository proves the default worktree root and its explicit override.
    # The real checkout is never the subject: this repo's own new-task-branch.ps1 would
    # otherwise create a real worktree beside the real repository.
    $behaviourRoot = New-FixtureDir 'us4-behaviour'
    $behaviourBare = Join-Path $behaviourRoot 'origin.git'
    & git init --quiet --bare --initial-branch=main $behaviourBare 2>&1 | Out-Null
    $behaviourRepo = Join-Path $behaviourRoot 'repo'
    & git clone --quiet $behaviourBare $behaviourRepo 2>&1 | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $behaviourRepo 'scripts') | Out-Null
    Write-FixtureText -Path (Join-Path $behaviourRepo 'README.md') -Text "# fixture`n"
    foreach ($name in @('new-task-branch.ps1', '_gate-common.ps1', '_harness-config.ps1', 'get-task.ps1')) {
        Copy-Item -LiteralPath (Join-Path $script:RepoRoot "scripts/$name") -Destination (Join-Path $behaviourRepo "scripts/$name") -Force
    }
    Write-FixtureText -Path (Join-Path $behaviourRepo 'harness.yml') -Text @"
harnessVersion: 0.7.1
pack: dotnet
baseBranch: main
tracker: none
solution: null
task:
  worktree: true
"@
    & git -C $behaviourRepo add -A 2>&1 | Out-Null
    & git -C $behaviourRepo -c user.email='fixture@example.invalid' -c user.name='Fixture' -c commit.gpgsign=false commit --quiet -m 'behaviour fixture' 2>&1 | Out-Null
    & git -C $behaviourRepo push --quiet -u origin main 2>&1 | Out-Null
    & git -C $behaviourRepo remote set-head origin main 2>&1 | Out-Null
    Write-FixtureText -Path (Join-Path $behaviourRoot 'kubeconfig') -Text "# scratch`n"

    $defaultRootRun = Invoke-ChildScript -Script (Join-Path $behaviourRepo 'scripts/new-task-branch.ps1') -Arguments @('-Description', 'Fixture default root', '-Type', 'feature') -Environment @{ LAMUFLIX_HOME_CONFIG = '' } -WorkingDirectory $behaviourRepo
    Assert-Case 'the scrubbed harness default still creates a task branch' ($defaultRootRun.ExitCode -eq 0) "exit $($defaultRootRun.ExitCode): $($defaultRootRun.Output)"
    $siblingRoot = Join-Path $behaviourRoot 'repo.worktrees'
    Assert-Case 'the default worktree root resolves to the sibling <repo>.worktrees folder' (Test-Path -LiteralPath $siblingRoot -PathType Container) "missing '$siblingRoot'"
    Assert-Case 'the default root held the new worktree, proving the resolution, not just the folder' (@(Get-ChildItem -LiteralPath $siblingRoot -Directory -Force).Count -ge 1) (@(Get-ChildItem -LiteralPath $siblingRoot -Directory -Force | ForEach-Object { $_.Name }) -join ',')

    Write-FixtureText -Path (Join-Path $behaviourRepo 'harness.yml') -Text @"
harnessVersion: 0.7.1
pack: dotnet
baseBranch: main
tracker: none
solution: null
task:
  worktree: true
  worktreeRoot: ../explicit-root
"@
    $overrideRun = Invoke-ChildScript -Script (Join-Path $behaviourRepo 'scripts/new-task-branch.ps1') -Arguments @('-Description', 'Fixture override root', '-Type', 'feature') -Environment @{ LAMUFLIX_HOME_CONFIG = '' } -WorkingDirectory $behaviourRepo
    Assert-Case 'an explicit worktreeRoot still wins over the derived default' ($overrideRun.ExitCode -eq 0) "exit $($overrideRun.ExitCode): $($overrideRun.Output)"
    Assert-Case 'the explicit worktree root receives the worktree' (@(Get-ChildItem -LiteralPath (Join-Path $behaviourRoot 'explicit-root') -Directory -Force -ErrorAction SilentlyContinue).Count -ge 1) 'the override root stayed empty'

    # Home config default: a disposable checkout resolves its own sibling <repo>-home folder,
    # and the environment override wins. No live operator config is read or written.
    $homeProbe = Join-Path $behaviourRepo 'scripts/home'
    New-Item -ItemType Directory -Force -Path $homeProbe | Out-Null
    Copy-Item -LiteralPath (Join-Path $script:RepoRoot 'scripts/home/_home-common.ps1') -Destination (Join-Path $homeProbe '_home-common.ps1') -Force
    # The Home helper lives in _home-common.ps1, not common.ps1, so it gets its own probe.
    $homeProbeProgram = @'
param([string]$HelperScript, [string]$RepoRoot)
$ErrorActionPreference = 'Stop'
. $HelperScript
try {
    $value = Get-HomeConfigPathDefault -RepoRoot $RepoRoot
    $payload = [pscustomobject]@{ ok = $true; value = $value; message = $null }
} catch {
    $payload = [pscustomobject]@{ ok = $false; value = $null; message = $_.Exception.Message }
}
$payload | ConvertTo-Json -Depth 4 -Compress
'@
    $homeProbeFile = Join-Path $script:Root 'probe-home-config.ps1'
    Write-FixtureText -Path $homeProbeFile -Text $homeProbeProgram
    $homeHelper = Join-Path $homeProbe '_home-common.ps1'
    $homeExpected = [System.IO.Path]::GetFullPath((Join-Path $behaviourRoot 'repo-home/home.config.json'))
    $homeRun = Invoke-ChildScript -Script $homeProbeFile -Arguments @($homeHelper, $behaviourRepo) -Environment @{ LAMUFLIX_HOME_CONFIG = '' } -WorkingDirectory $behaviourRepo
    $homeResolved = $null
    try { $homeResolved = $homeRun.Output | ConvertFrom-Json } catch { $homeResolved = $null }
    Assert-Case 'the Home config default resolves a sibling <repo>-home folder' ([bool]$homeResolved -and [bool]$homeResolved.ok) "probe did not resolve: $($homeRun.Output)"
    Assert-Case 'the Home config default is the sibling home.config.json' ([string]::Equals([string]$homeResolved.value, $homeExpected, [System.StringComparison]::OrdinalIgnoreCase)) "got '$($homeResolved.value)' expected '$homeExpected'"
    $envOverridePath = Join-Path $behaviourRoot 'override-home/config.json'
    $homeOverrideRun = Invoke-ChildScript -Script $homeProbeFile -Arguments @($homeHelper, $behaviourRepo) -Environment @{ LAMUFLIX_HOME_CONFIG = $envOverridePath } -WorkingDirectory $behaviourRepo
    $homeOverride = $null
    try { $homeOverride = $homeOverrideRun.Output | ConvertFrom-Json } catch { $homeOverride = $null }
    Assert-Case 'LAMUFLIX_HOME_CONFIG overrides the Home config default' ([bool]$homeOverride -and [string]::Equals([string]$homeOverride.value, $envOverridePath, [System.StringComparison]::OrdinalIgnoreCase)) "got '$($homeOverride.value)'"

    $homeConfigOps = Join-Path $behaviourRoot 'home-ops'
    $homeConfigPath = Join-Path $homeConfigOps 'home.config.json'
    Write-FixtureText -Path $homeConfigPath -Text (@{
        clusterName    = 'fixture-absent-cluster'
        libraryPath    = (Join-Path $behaviourRoot 'lib')
        backupPath     = (Join-Path $behaviourRoot 'backup')
        kubeconfigPath = (Join-Path $homeConfigOps 'kubeconfig')
        kind           = 'kind-absent-on-path'
        kubectl        = 'kubectl-absent-on-path'
        docker         = 'docker-absent-on-path'
        waitSeconds    = 5
        checkPorts     = $false
    } | ConvertTo-Json)
    $statusRun = Invoke-ChildScript -Script (Join-Path $script:RepoRoot 'scripts/home/Get-HomeStatus.ps1') -Arguments @('-ConfigPath', $homeConfigPath) -Environment @{} -WorkingDirectory $behaviourRepo
    Assert-Case 'the scrubbed Home scripts still read an explicit config and fail closed without a cluster' ($statusRun.ExitCode -eq 1 -and $statusRun.Output -match 'cluster') "exit $($statusRun.ExitCode): $($statusRun.Output)"
    Assert-Case 'the Home status run created no folder and removed nothing' (-not (Test-Path -LiteralPath (Join-Path $homeConfigOps 'kubeconfig'))) 'a kubeconfig was written'

    Write-Host ''
    Write-Host 'US4 host adapters keep parity and carry no public operational write (T024c):'

    # Adapters are maintained sources, not generated output. The two host trees differ only
    # in host spellings: the '$name' vs '/name' invocation sigil, and AGENTS.md vs CLAUDE.md.
    # Anything else is drift and fails here.
    $adapterRoot = Join-Path $script:RepoRoot '.agents/skills'
    $adapterNames = @(Get-ChildItem -LiteralPath $adapterRoot -Directory | ForEach-Object { $_.Name })
    Assert-Case 'the agent adapter tree is present to check' ($adapterNames.Count -gt 0) "no adapters under '$adapterRoot'"
    $drifted = [System.Collections.Generic.List[string]]::new()
    $missingTwin = [System.Collections.Generic.List[string]]::new()
    $normalise = {
        param([string]$Text)
        $body = $Text
        if ($body -match '^(?s)---\r?\n.*?\r?\n---\r?\n') { $body = $body.Substring($Matches[0].Length) }
        $body = $body -replace '\$([A-Za-z_][A-Za-z0-9_-]*)', '/$1'
        return ($body -replace '\b(AGENTS|CLAUDE)\.md\b', '<agent-context>.md')
    }
    foreach ($name in $adapterNames) {
        $agentFile = Join-Path $adapterRoot "$name/SKILL.md"
        $claudeFile = Join-Path (Join-Path $script:RepoRoot '.claude/skills') "$name/SKILL.md"
        if (-not (Test-Path -LiteralPath $claudeFile -PathType Leaf)) { $missingTwin.Add($name); continue }
        $agentBody = & $normalise (Get-Content -LiteralPath $agentFile -Raw)
        $claudeBody = & $normalise (Get-Content -LiteralPath $claudeFile -Raw)
        if ($agentBody -cne $claudeBody) { $drifted.Add($name) }
    }
    Assert-Case 'every agent adapter has a .claude twin' ($missingTwin.Count -eq 0) ($missingTwin -join ', ')
    Assert-Case 'every adapter pair is in parity apart from host spellings' ($drifted.Count -eq 0) ($drifted -join ', ')

    $adapterWriteResidual = [System.Collections.Generic.List[string]]::new()
    $adapterFiles = @(Get-ChildItem -LiteralPath (Join-Path $script:RepoRoot '.agents/skills') -Recurse -File -Filter 'SKILL.md') +
        @(Get-ChildItem -LiteralPath (Join-Path $script:RepoRoot '.claude/skills') -Recurse -File -Filter 'SKILL.md')
    foreach ($file in $adapterFiles) {
        $text = Get-Content -LiteralPath $file.FullName -Raw
        if ($text -match '(?i)write\s+(?:the\s+)?(?:brief|plan|tasks|checklists?)[^\n]*\bspecs/') {
            $adapterWriteResidual.Add(([System.IO.Path]::GetRelativePath($script:RepoRoot, $file.FullName)) -replace '\\', '/')
        }
    }
    Assert-Case 'no adapter instructs a public operational write under specs/' ($adapterWriteResidual.Count -eq 0) ($adapterWriteResidual -join ', ')

    $generatedUntouched = @(& git -C $script:RepoRoot status --porcelain -- CodeMetricsConfig.txt stryker-config.json .editorconfig 2>$null)
    Assert-Case 'the generated gate outputs stay untouched' ($generatedUntouched.Count -eq 0) (@($generatedUntouched) -join ' | ')

    Write-Host ''
    Write-Host 'US5 clean-checkout new-ticket smoke (T028):'

    # Disposable only: a throwaway checkout, a GUID temp private root, and a temp "absent" root.
    # No live operator archive is read or written, and the real repository is never the subject.
    $smokeRepo = New-GitFixtureCheckout 'us5-clean-checkout'
    New-Item -ItemType Directory -Force -Path (Join-Path $smokeRepo '.specify/templates') | Out-Null
    Write-FixtureText -Path (Join-Path $smokeRepo '.specify/templates/spec-template.md') -Text "# spec template`n"
    & git -C $smokeRepo add -A 2>&1 | Out-Null
    & git -C $smokeRepo commit --quiet -m 'clean checkout fixture' 2>&1 | Out-Null
    $smokeHead = ((& git -C $smokeRepo rev-parse HEAD 2>&1) -join '').Trim()
    Assert-Case 'the clean-checkout fixture records the tree HEAD it runs at' ($smokeHead -match '^[0-9a-f]{40}$') "HEAD '$smokeHead'"

    $smokeRoot = New-FixtureDir 'us5-clean-root'
    $smokeInit = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $smokeRoot } -WorkingDirectory $smokeRepo
    Assert-Case 'the clean-checkout private root initializes explicitly' ($smokeInit.ExitCode -eq 0) "exit $($smokeInit.ExitCode): $($smokeInit.Output)"

    $smokeCreate = Invoke-ChildScript -Script $script:CreateScript -Arguments @('-Json', '-TicketId', 'DEV-920', '-ShortName', 'pickup-smoke', 'Smoke a clean checkout ticket') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $smokeRoot } -WorkingDirectory $smokeRepo
    Assert-Case 'a new ticket authored from a clean checkout exits 0' ($smokeCreate.ExitCode -eq 0) "exit $($smokeCreate.ExitCode): $($smokeCreate.Output)"
    $smokePayload = Get-JsonPayload -Text $smokeCreate.Output
    $smokeTicketDir = Join-Path $smokeRoot 'LamuFlix/DEV-920'
    Assert-Case 'the clean-checkout ticket writes into the canonical private ticket directory' (Test-ArtifactPathEqual (Get-PayloadValue $smokePayload 'PRIVATE_TICKET_DIR') $smokeTicketDir) "got $(Get-PayloadValue $smokePayload 'PRIVATE_TICKET_DIR')"
    Assert-Case 'the clean-checkout ticket keeps its operational record external' (Test-Path -LiteralPath (Join-Path $smokeTicketDir 'ticket.json') -PathType Leaf) 'no private ticket record'
    $smokeBranch = Get-PayloadValue $smokePayload 'BRANCH_NAME'
    $smokePublic = Join-Path $smokeRepo ('specs/' + $smokeBranch)
    Assert-Case 'the clean-checkout public directory holds the spec and nothing operational' ((@(Get-ChildItem -LiteralPath $smokePublic -Recurse -File -Force | ForEach-Object { $_.Name }) -join ',') -eq 'spec.md') (@(Get-ChildItem -LiteralPath $smokePublic -Recurse -File -Force | ForEach-Object { $_.Name }) -join ',')

    $smokeReadProbe = Invoke-CommonProbe -Expression @'
$paths = Get-ArtifactPathsEnv -TicketId 'DEV-920'
[pscustomobject]@{
    specReadable = (Test-Path -LiteralPath $paths.PUBLIC_SPEC -PathType Leaf)
    privateAvailable = $paths.PRIVATE_AVAILABLE
    privateRootExists = (Test-Path -LiteralPath $paths.PRIVATE_ROOT -PathType Container)
}
'@ -Environment @{ LAMUFLIX_ARTIFACT_ROOT = (Join-Path $script:Root 'us5-clean-absent-root'); SPECIFY_FEATURE_DIRECTORY = $smokePublic } -WorkingDirectory $smokeRepo
    Assert-Case 'the new ticket public spec stays readable without private storage' ($smokeReadProbe.ok -and $smokeReadProbe.value.specReadable) "$($smokeReadProbe.message)"
    Assert-Case 'the public-only read reports no private storage and creates none' ($smokeReadProbe.ok -and (-not $smokeReadProbe.value.privateAvailable) -and (-not $smokeReadProbe.value.privateRootExists)) "$($smokeReadProbe.message)"

    Write-Host ''
    Write-Host 'US5 absent-private pickup is a named blocking check (T029):'

    # Phase A authors the durable private records, then the Phase A worktree is deleted entirely.
    # Phase B picks up from a different disposable checkout with the same private root, so the
    # canonical ticket directory is the only route to brief/plan/tasks.
    $phaseARepo = New-GitFixtureCheckout 'us5-phase-a'
    $phaseATicket = Join-Path $phaseARepo 'specs/DEV-921'
    Write-FixtureText -Path (Join-Path $phaseATicket 'spec.md') -Text "# DEV-921 spec`n"
    Write-FixtureText -Path (Join-Path $phaseATicket 'brief.md') -Text "# DEV-921 brief`r`nPhase A durable record`r`n"
    Write-FixtureText -Path (Join-Path $phaseATicket 'plan.md') -Text "# DEV-921 plan`n"
    Write-FixtureText -Path (Join-Path $phaseATicket 'tasks.md') -Text "# DEV-921 tasks`n- [ ] T001 pickup`n"
    Write-FixtureText -Path (Join-Path $phaseATicket 'checklists/requirements.md') -Text "# DEV-921 requirements`n"
    & git -C $phaseARepo add -A 2>&1 | Out-Null
    & git -C $phaseARepo commit --quiet -m 'phase a records' 2>&1 | Out-Null
    $phaseAHead = ((& git -C $phaseARepo rev-parse HEAD 2>&1) -join '').Trim()
    Assert-Case 'the Phase A fixture records its tree HEAD' ($phaseAHead -match '^[0-9a-f]{40}$') "HEAD '$phaseAHead'"

    $pickupRoot = New-FixtureDir 'us5-pickup-root'
    $pickupInit = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize') -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $pickupRoot } -WorkingDirectory $phaseARepo
    Assert-Case 'the pickup fixture private root initializes explicitly' ($pickupInit.ExitCode -eq 0) "exit $($pickupInit.ExitCode): $($pickupInit.Output)"
    $phaseASeed = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Bootstrap', '-TicketId', 'DEV-921', '-SourceDir', $phaseATicket) -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $pickupRoot } -WorkingDirectory $phaseARepo
    Assert-Case 'Phase A seeds its durable private records with a verified proof' ($phaseASeed.ExitCode -eq 0) "exit $($phaseASeed.ExitCode): $($phaseASeed.Output)"

    $pickupTicketDir = Join-Path $pickupRoot 'LamuFlix/DEV-921'
    $archivedBriefHash = Get-ArtifactFileHash -Path (Join-Path $pickupTicketDir 'brief.md')
    Assert-Case 'the canonical ticket directory holds the seeded brief and plan' ((Test-Path -LiteralPath (Join-Path $pickupTicketDir 'brief.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $pickupTicketDir 'plan.md') -PathType Leaf))

    # Delete the Phase A worktree: the durable records must not depend on it.
    Remove-Item -LiteralPath $phaseARepo -Recurse -Force
    Assert-Case 'the Phase A worktree is gone before Phase B picks up' (-not (Test-Path -LiteralPath $phaseARepo))

    $phaseBRepo = New-GitFixtureCheckout 'us5-phase-b'
    $phaseBPublic = Join-Path $phaseBRepo 'specs/DEV-921'
    Write-FixtureText -Path (Join-Path $phaseBPublic 'spec.md') -Text "# DEV-921 spec`n"
    & git -C $phaseBRepo add -A 2>&1 | Out-Null
    & git -C $phaseBRepo commit --quiet -m 'phase b checkout' 2>&1 | Out-Null
    $phaseBHead = ((& git -C $phaseBRepo rev-parse HEAD 2>&1) -join '').Trim()
    Assert-Case 'Phase B runs at its own recorded HEAD' ($phaseBHead -match '^[0-9a-f]{40}$') "HEAD '$phaseBHead'"
    Assert-Case 'Phase B holds no operational record of its own' (@(Get-ChildItem -LiteralPath $phaseBPublic -File -Force | ForEach-Object { $_.Name }) -join ',' -eq 'spec.md') (@(Get-ChildItem -LiteralPath $phaseBPublic -File -Force | ForEach-Object { $_.Name }) -join ',')

    $pickupEnv = @{ LAMUFLIX_ARTIFACT_ROOT = $pickupRoot; SPECIFY_FEATURE_DIRECTORY = $phaseBPublic; SPECIFY_FEATURE = '901-fixture' }
    $pickup = Invoke-ChildScript -Script $script:CheckPrereqScript -Arguments @('-Json') -Environment $pickupEnv -WorkingDirectory $phaseBRepo
    Assert-Case 'Phase B pickup resolves through the canonical private ticket directory' ($pickup.ExitCode -eq 0) "exit $($pickup.ExitCode): $($pickup.Output)"
    $pickupPayload = Get-JsonPayload -Text $pickup.Output
    Assert-Case 'pickup reports the private plan, not a public or old-worktree one' (Test-ArtifactPathEqual (Get-PayloadValue $pickupPayload 'IMPL_PLAN') (Join-Path $pickupTicketDir 'plan.md')) "got $(Get-PayloadValue $pickupPayload 'IMPL_PLAN')"
    Assert-Case 'pickup reports the private tasks list' (Test-ArtifactPathEqual (Get-PayloadValue $pickupPayload 'TASKS') (Join-Path $pickupTicketDir 'tasks.md')) "got $(Get-PayloadValue $pickupPayload 'TASKS')"
    Assert-Case 'the picked-up brief is the durable Phase A record byte for byte' ((Get-ArtifactFileHash -Path (Join-Path $pickupTicketDir 'brief.md')) -eq $archivedBriefHash)
    Assert-Case 'pickup reconstructs nothing into the Phase B public directory' (@(Get-ChildItem -LiteralPath $phaseBPublic -File -Force | ForEach-Object { $_.Name }) -join ',' -eq 'spec.md') (@(Get-ChildItem -LiteralPath $phaseBPublic -File -Force | ForEach-Object { $_.Name }) -join ',')

    # Absent private inputs: a stage that needs them must fail closed and reconstruct nothing.
    $absentRoot = Join-Path $script:Root 'us5-absent-pickup-root'
    $absentEnv = @{ LAMUFLIX_ARTIFACT_ROOT = $absentRoot; SPECIFY_FEATURE_DIRECTORY = $phaseBPublic; SPECIFY_FEATURE = '901-fixture' }
    $absentPickup = Invoke-ChildScript -Script $script:CheckPrereqScript -Arguments @('-Json', '-RequirePrivate') -Environment $absentEnv -WorkingDirectory $phaseBRepo
    Assert-Case 'absent private inputs block pickup as a named blocking check' ($absentPickup.ExitCode -eq 1) "exit $($absentPickup.ExitCode): $($absentPickup.Output)"
    Assert-Case 'the blocked pickup names the initialization command' ($absentPickup.Output -match 'initialize-artifact-storage\.ps1') $absentPickup.Output
    Assert-Case 'the blocked pickup invents no approved receipt' ([string]::IsNullOrWhiteSpace(($absentPickup.Output -split "`n" | Where-Object { $_ -match 'seed-proof|bulk-migration\.json' }) -join '')) $absentPickup.Output
    Assert-Case 'the blocked pickup creates no private root' (-not (Test-Path -LiteralPath $absentRoot)) 'the private root was created'
    Assert-Case 'the blocked pickup recreates nothing publicly' (@(Get-ChildItem -LiteralPath $phaseBPublic -File -Force | ForEach-Object { $_.Name }) -join ',' -eq 'spec.md') (@(Get-ChildItem -LiteralPath $phaseBPublic -File -Force | ForEach-Object { $_.Name }) -join ',')

    Write-Host ''
    Write-Host 'US5 archive lifecycle and receipt readback (T030):'

    $lifecycleRepo = New-GitFixtureCheckout 'us5-lifecycle'
    $lifecycleTicket = Join-Path $lifecycleRepo 'specs/DEV-922'
    Write-FixtureText -Path (Join-Path $lifecycleTicket 'spec.md') -Text "# DEV-922 spec`n"
    Write-FixtureText -Path (Join-Path $lifecycleTicket 'CONCLUSIONS.md') -Text "# DEV-922 conclusions`nDecision recorded.`n"
    Write-FixtureText -Path (Join-Path $lifecycleTicket 'brief.md') -Text "# DEV-922 brief`r`n"
    Write-FixtureText -Path (Join-Path $lifecycleTicket 'plan.md') -Text "# DEV-922 plan`n"
    Write-FixtureText -Path (Join-Path $lifecycleTicket 'tasks.md') -Text "# DEV-922 tasks`n"
    Write-FixtureText -Path (Join-Path $lifecycleTicket 'checklists/requirements.md') -Text "# DEV-922 requirements`n"
    & git -C $lifecycleRepo add -A 2>&1 | Out-Null
    & git -C $lifecycleRepo commit --quiet -m 'lifecycle fixture' 2>&1 | Out-Null
    $lifecycleHead = ((& git -C $lifecycleRepo rev-parse HEAD 2>&1) -join '').Trim()
    Assert-Case 'the lifecycle fixture records the HEAD its receipts must cite' ($lifecycleHead -match '^[0-9a-f]{40}$') "HEAD '$lifecycleHead'"

    $lifecycleRoot = New-FixtureDir 'us5-lifecycle-root'
    $lifecycleEnv = @{ LAMUFLIX_ARTIFACT_ROOT = $lifecycleRoot; SPECIFY_FEATURE_DIRECTORY = ''; SPECIFY_FEATURE = ''; PYTHONUTF8 = '1' }
    $lifecycleInit = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Initialize') -Environment $lifecycleEnv -WorkingDirectory $lifecycleRepo
    Assert-Case 'the lifecycle fixture private root initializes explicitly' ($lifecycleInit.ExitCode -eq 0) "exit $($lifecycleInit.ExitCode): $($lifecycleInit.Output)"
    $lifecycleMigration = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Migrate', '-All', '-StageRemovals', '-Json') -Environment $lifecycleEnv -WorkingDirectory $lifecycleRepo
    Assert-Case 'the lifecycle migration verifies and stages its removals' ($lifecycleMigration.ExitCode -eq 0) "exit $($lifecycleMigration.ExitCode): $($lifecycleMigration.Output)"
    $lifecyclePayload = Get-JsonPayload -Text $lifecycleMigration.Output

    $archiveTicketDir = Join-Path $lifecycleRoot 'LamuFlix/DEV-922'
    $archivePlanHash = Get-ArtifactFileHash -Path (Join-Path $archiveTicketDir 'plan.md')
    $migrationReceiptPath = Get-PayloadValue $lifecyclePayload 'RECEIPT'
    Assert-Case 'the migration receipt lands under its full commit SHA' ((Test-ArtifactPathEqual ([System.IO.Path]::GetDirectoryName($migrationReceiptPath)) (Join-Path $lifecycleRoot "LamuFlix/receipts/sha/$lifecycleHead")) -and (Test-Path -LiteralPath $migrationReceiptPath -PathType Leaf)) "receipt $migrationReceiptPath"
    $migrationReceipt = Get-Content -LiteralPath $migrationReceiptPath -Raw | ConvertFrom-Json
    Assert-Case 'the migration receipt reads back the full SHA it was produced at' ($migrationReceipt.commitSha -eq $lifecycleHead) "receipt commitSha $($migrationReceipt.commitSha) expected $lifecycleHead"
    Assert-Case 'the migration receipt reads back as verified for every file' (([bool]$migrationReceipt.verified) -and ($migrationReceipt.fileCount -eq (Get-PayloadValue $lifecyclePayload 'FILE_COUNT'))) "fileCount $($migrationReceipt.fileCount)"

    $gateProbe = Invoke-CommonProbe -Expression @"
`$ticketDir = '$archiveTicketDir'
`$provenance = New-ArtifactProvenance -TicketId 'DEV-922' -CommitSha '$lifecycleHead' -WorktreeId 'us5-lifecycle' -Kind 'evidence'
`$gateDir = Get-ArtifactRunDir -TicketDir `$ticketDir -CommitSha '$lifecycleHead' -Kind 'evidence'
`$gatePath = Write-ArtifactRecord -TicketDir `$ticketDir -RelativePath ('evidence/sha/$lifecycleHead/gate.json') -Text ((`$provenance | ConvertTo-Json -Depth 4) + "``n")
[pscustomobject]@{ gatePath = `$gatePath; gateDir = `$gateDir; commitSha = `$provenance.commitSha }
"@ -Environment @{ LAMUFLIX_ARTIFACT_ROOT = $lifecycleRoot }
    Assert-Case 'a gate receipt is recorded under the SHA-isolated evidence area' ($gateProbe.ok -and (Test-Path -LiteralPath $gateProbe.value.gatePath -PathType Leaf)) $gateProbe.message
    $gateReceipt = Get-Content -LiteralPath $gateProbe.value.gatePath -Raw | ConvertFrom-Json
    Assert-Case 'the gate receipt reads back the full SHA and ticket it belongs to' ($gateReceipt.commitSha -eq $lifecycleHead -and $gateReceipt.ticketId -eq 'DEV-922') "commitSha $($gateReceipt.commitSha)"

    # Deleting the task worktree must not touch the private archive.
    Remove-Item -LiteralPath $lifecycleRepo -Recurse -Force
    Assert-Case 'the task worktree is deleted' (-not (Test-Path -LiteralPath $lifecycleRepo))
    Assert-Case 'the private archive survives the worktree deletion' ((Test-Path -LiteralPath (Join-Path $archiveTicketDir 'plan.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $archiveTicketDir 'brief.md') -PathType Leaf) -and (Test-Path -LiteralPath (Join-Path $archiveTicketDir 'checklists/requirements.md') -PathType Leaf)) 'an archived record disappeared'
    Assert-Case 'the surviving archive is the byte-identical archived copy' ((Get-ArtifactFileHash -Path (Join-Path $archiveTicketDir 'plan.md')) -eq $archivePlanHash)
    Assert-Case 'the migration and gate receipts survive the worktree deletion' ((Test-Path -LiteralPath $migrationReceiptPath -PathType Leaf) -and (Test-Path -LiteralPath $gateProbe.value.gatePath -PathType Leaf))

    # The Phase B pickup archive is still intact after the Phase A deletion too.
    Assert-Case 'the Phase A pickup archive also survives its worktree deletion' ((Get-ArtifactFileHash -Path (Join-Path $pickupTicketDir 'brief.md')) -eq $archivedBriefHash)

    Write-Host ''
    Write-Host 'US5 operator init, backup and transfer guidance (T031):'

    # The operator lifecycle is documented on the cut-over surfaces touched by T016/T017:
    # both maintained adapter twins and the initializer help. Logical identifiers only.
    $lifecycleTerms = @(
        @{ Label = 'initialization'; Pattern = '(?i)-Initialize' },
        @{ Label = 'backup'; Pattern = '(?i)backup' },
        @{ Label = 'transfer'; Pattern = '(?i)transfer' }
    )
    $guidanceFiles = @(
        '.agents/skills/task/SKILL.md',
        '.claude/skills/task/SKILL.md',
        'scripts/load/README.md'
    )
    foreach ($relativeGuidance in $guidanceFiles) {
        $guidancePath = Join-Path $script:RepoRoot $relativeGuidance
        $guidanceText = if (Test-Path -LiteralPath $guidancePath -PathType Leaf) { Get-Content -LiteralPath $guidancePath -Raw } else { '' }
        foreach ($term in $lifecycleTerms) {
            Assert-Case "$relativeGuidance documents operator $($term.Label)" ($guidanceText -match $term.Pattern) "no $($term.Label) guidance in $relativeGuidance"
        }
    }

    $helpRun = Invoke-ChildScript -Script $script:InitScript -Arguments @('-Help') -Environment @{}
    Assert-Case 'the initializer help documents the operator lifecycle' ($helpRun.ExitCode -eq 0 -and $helpRun.Output -match '(?i)Operator lifecycle') "exit $($helpRun.ExitCode): $($helpRun.Output)"
    Assert-Case 'the initializer help names backup and transfer ownership' ($helpRun.Output -match '(?i)Backup and cross-machine transfer are operator-owned') $helpRun.Output
    Assert-Case 'the initializer help names the logical root override, not a machine path' (($helpRun.Output -match 'LAMUFLIX_ARTIFACT_ROOT') -and -not ($helpRun.Output -match '[A-Za-z]:[\\/]')) $helpRun.Output

    $guidanceLeaks = [System.Collections.Generic.List[string]]::new()
    foreach ($relativeGuidance in $guidanceFiles) {
        $guidancePath = Join-Path $script:RepoRoot $relativeGuidance
        if (-not (Test-Path -LiteralPath $guidancePath -PathType Leaf)) { continue }
        $guidanceText = Get-Content -LiteralPath $guidancePath -Raw
        if ($guidanceText -match 'LAMUFLIX_ARTIFACT_ROOT\s*=\s*[A-Za-z]:') { $guidanceLeaks.Add($relativeGuidance) }
    }
    Assert-Case 'no operator guidance copies a machine-local private path into tracked docs' ($guidanceLeaks.Count -eq 0) ($guidanceLeaks -join ', ')
} finally {
    if (Test-Path -LiteralPath $script:Root) {
        Remove-Item -LiteralPath $script:Root -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ''
Write-Host "Artifact storage companion: $script:Failures of $script:Checks checks failed"
if ($script:Failures -gt 0) { exit 1 }
exit 0

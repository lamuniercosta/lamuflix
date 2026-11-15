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

function Test-ArtifactPathEqual {
    param([string]$Left, [string]$Right)
    if ([string]::IsNullOrWhiteSpace($Left) -or [string]::IsNullOrWhiteSpace($Right)) { return $false }
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
} finally {
    if (Test-Path -LiteralPath $script:Root) {
        Remove-Item -LiteralPath $script:Root -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ''
Write-Host "Artifact storage companion: $script:Failures of $script:Checks checks failed"
if ($script:Failures -gt 0) { exit 1 }
exit 0

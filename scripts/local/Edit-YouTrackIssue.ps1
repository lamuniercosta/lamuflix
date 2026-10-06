#!/usr/bin/env pwsh
<#
  Creates, edits, or shows one YouTrack issue, and reads every change back.
  Rigger runs it. Patron decides what changes and supplies the text as files,
  so Markdown survives shell quoting.

  THE ONLY WAY TO FILE A NEW TICKET. Never curl, raw REST, MCP, a hand-written
  /api/commands call, or a temp script that dot-sources _youtrack.ps1: those leave
  the issue as Type=Bug, Priority=Normal, with no parent link, no Estimated
  Time, no Order and no sprint. Parent, estimate and size belong in the
  parameters below, never only in the description body.

  Run -Create and -Show from the MAIN checkout, by absolute path, never from a
  task worktree (a worktree branched earlier carries an old copy of this script):
    pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Create ...
  -Create and -Show never read or write scripts/youtrack-plan.json, so this is
  safe. Only an -Ticket summary/description edit syncs the plan, and that must
  run from the worktree whose PR carries the plan change.

    # New ticket under an epic:
    pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Create -Summary '<text>' `
        -DescriptionFile <md> -Parent DEV-93 -Estimate 1d -Type Task -Tag size:M

    # Same, but only read and print every planned write (zero non-GET calls):
    pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Create -DryRun -Summary '<text>' `
        -DescriptionFile <md> -Parent DEV-93 -Estimate 1d -Type Task -Tag size:M

    # Edit a ticket; pass any combination of the change parameters:
    pwsh scripts/local/Edit-YouTrackIssue.ps1 -Ticket DEV-290 `
        [-Summary '<text>'] [-DescriptionFile <md>] [-CommentFile <md>] [-Tag size:S]

    # Read-only: summary, State, Type, parent, tags, estimate, Order (get-task.ps1 omits tags):
    pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Ticket DEV-290 -Show

  -Create parameters
    -Type        Task (default), Bug, Feature, Cosmetics, Exception, 'Usability Problem',
                 'Performance Problem'. The project's own default is Bug, so the script
                 always sets it; it is also checked against the project's Type bundle.
                 Epic and Repository are not creatable here.
    -Priority    Normal (default, the project's own default). Checked against the
                 project's Priority bundle (Show-stopper, Critical, Major, Normal, Minor).
    -Repository  lamuflix (default). Any other repository gets no Order.
    -Estimate    Required, e.g. 1h, 4h, 1d. Sets Estimated Time (1d = 8h); read back.
    -Parent      Required. Sets the "subtask of" link; read back.
    -DryRun      Reads parent, bundles, duplicate check, tags, Order slot and the full
                 renumber list, and the current sprint, prints the exact planned writes,
                 and exits 0 (1 if a same-summary issue in any state would block it)
                 without a single non-GET.

  What -Create does, in order, under one machine-wide lock
  (named mutex Global\LamuFlix-YouTrack-Create; -DryRun does not take it):
    1. Duplicate guard: an issue in the parent's project with the same summary
       (case-insensitive, trimmed) blocks the create (exit 1) in ANY state: open,
       Done or Closed. A resolved ticket is named with its State ("DEV-404 (Done)
       already has this summary"): it is not open, but it is still the ticket for
       that work. A genuinely new ticket needs a different summary. The check does
       not depend on the search index: besides the phrase search it lists the 100
       newest issues and reads the next ids directly, so an issue created seconds
       ago is still seen.
    2. ONE POST /api/issues carrying summary, description and the custom fields
       Type, Priority, Repository and Estimated Time. The ticket is therefore
       never left as a bare Type=Bug issue.
    3. Re-runs the duplicate check. If another issue (any state) with the same
       summary and a LOWER id now exists (a concurrent agent won the race), it
       reports both ids and exits 1. Nothing is deleted or edited: the new issue is
       left without parent, Order and sprint for Patron to close.
    4. The parent link ("subtask of DEV-x"), then tags (-Tag, existing tags only,
       by REST). No command string uses braces: YouTrack takes them literally.
    5. Order (Repository lamuflix only). Order is one global, unique, contiguous
       1..N sequence over lamuflix epics and Done tickets; each epic is followed by its
       Done children, then its open children. The new issue takes the slot directly
       after its parent's last Done child (or right after the parent if it has none),
       and every issue with Order >= slot is renumbered +1, highest first. Every
       renumber is printed ("renumber DEV-x: 54 -> 55"). The script refuses to write
       when the existing sequence is not already 1..N. The new issue's own Order is
       written after the duplicate check, so a lost race never leaves two issues
       with one Order.
    6. Sprint: the issue is added to the current sprint of board 204-3 (the agile's
       currentSprint, else the sprint whose start <= now <= finish). No current sprint
       is a warning, not a failure.
    7. Reads everything back (text, Type, Priority, Repository, Estimated Time, parent,
       tags, Order sequence 1..N with no gaps or duplicates, sprint membership).

  (Checkouts from before commit dd79fc6 only; later ones have no plan and skip this.)
  scripts/sync_youtrack_board.py rewrites the summary and description of every
  ticket scripts/youtrack-plan.json manages. A summary or description edit on
  such a ticket is therefore written into the plan too (via _plan_text.py).
  Run the tool from the worktree whose PR will carry that plan change, and
  commit the plan with it. Comments and tags are never touched by the sync.

  -Tag resolves an existing tag and posts its id to /api/issues/{id}/tags.
  It does not create a tag.

  Exit 0: every requested change read back as written (or -DryRun finished).
  Exit 1: a same-summary issue, open or resolved, blocked -Create (or was detected
          right after it): use the existing ticket (or pick a different summary if
          the work is genuinely new) and do not retry; or an edit did not stick; or
          the plan could not be updated.
  Exit 2: nothing was created: configuration or HTTP error, the lock could not be
          taken, an invalid Type/Priority/Repository, a non-contiguous Order
          sequence, or -Tag does not name exactly one existing tag.
  Exit 3: -Create made the ticket, then a later step failed or did not read back.
          stderr starts with "CREATED <id> but <step> failed" and lists the steps
          done and not done. The ticket EXISTS: never re-run -Create. Read it with
          -Show, fix text or tags with -Ticket <id>, and hand any other field
          (parent, estimate, Order, sprint) to Patron.

  Credentials resolve as documented in _youtrack.ps1 (same order as
  scripts/get-task.ps1). The token is sent only as Authorization: Bearer and
  is never printed.

  Offline-test seams (production callers omit them): -EnvironmentReader,
  -RestMethodInvoker, -DpapiFileReader (see _youtrack.ps1), -PlanDirectory
  (the folder holding youtrack-plan.json), -LockName and -LockTimeoutSeconds.
#>

[CmdletBinding(DefaultParameterSetName = 'Edit')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Create')]
    [switch]$Create,

    [Parameter(ParameterSetName = 'Create')]
    [switch]$DryRun,

    [Parameter(Mandatory, ParameterSetName = 'Edit')]
    [Parameter(Mandatory, ParameterSetName = 'Show')]
    [ValidatePattern('^[A-Z][A-Z0-9]*-\d+$')]
    [string]$Ticket,

    [Parameter(Mandatory, ParameterSetName = 'Show')]
    [switch]$Show,

    [Parameter(Mandatory, ParameterSetName = 'Create')]
    [Parameter(ParameterSetName = 'Edit')]
    [ValidateNotNullOrEmpty()]
    [string]$Summary,

    [Parameter(Mandatory, ParameterSetName = 'Create')]
    [Parameter(ParameterSetName = 'Edit')]
    [ValidateNotNullOrEmpty()]
    [string]$DescriptionFile,

    [Parameter(ParameterSetName = 'Edit')]
    [ValidateNotNullOrEmpty()]
    [string]$CommentFile,

    [Parameter(ParameterSetName = 'Create')]
    [Parameter(ParameterSetName = 'Edit')]
    [ValidatePattern('^[^{}\s]+$')]
    [string[]]$Tag,

    [Parameter(Mandatory, ParameterSetName = 'Create')]
    [ValidatePattern('^[A-Z][A-Z0-9]*-\d+$')]
    [string]$Parent,

    [Parameter(Mandatory, ParameterSetName = 'Create')]
    [ValidatePattern('^\d+[wdhm]( \d+[wdhm])*$')]
    [string]$Estimate,

    [Parameter(ParameterSetName = 'Create')]
    [ValidateSet('Task', 'Bug', 'Feature', 'Cosmetics', 'Exception', 'Usability Problem', 'Performance Problem')]
    [string]$Type = 'Task',

    [Parameter(ParameterSetName = 'Create')]
    [ValidateNotNullOrEmpty()]
    [string]$Priority = 'Normal',

    [Parameter(ParameterSetName = 'Create')]
    [ValidateNotNullOrEmpty()]
    [string]$Repository = 'lamuflix',

    [Parameter(ParameterSetName = 'Create')]
    [string]$LockName = 'Global\LamuFlix-YouTrack-Create',

    [Parameter(ParameterSetName = 'Create')]
    [ValidateRange(1, 3600)]
    [int]$LockTimeoutSeconds = 120,

    [string]$PlanDirectory = (Split-Path -Parent $PSScriptRoot),
    [scriptblock]$EnvironmentReader,
    [scriptblock]$RestMethodInvoker,
    [scriptblock]$DpapiFileReader
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$YouTrackTool = 'Edit-YouTrackIssue'
. (Join-Path $PSScriptRoot '_youtrack.ps1')

# A dry run must never write, whatever the caller below does: refuse any non-GET.
$sharedInvokeYouTrack = ${function:Invoke-YouTrack}
function Invoke-YouTrack {
    param([string]$Method, [string]$Uri, [hashtable]$Headers, [string]$Body)
    if ($DryRun -and $Method -ne 'Get') { Stop-WithError "internal error: -DryRun attempted $Method $Uri" }
    return & $sharedInvokeYouTrack -Method $Method -Uri $Uri -Headers $Headers -Body $Body
}

# Once -Create has made the issue, no failure may look like "nothing happened": every
# error exits 3 and names the id, so the caller repairs the ticket instead of re-creating it.
$script:CreatedId = $null
$script:PostingIssue = $false
$script:CurrentStep = 'creating the issue'
$script:PlannedSteps = @('set parent link', 'add tags', 'renumber and set Order', 'add to sprint', 'read-back verification')
$script:CompletedSteps = [Collections.Generic.List[string]]::new()

function Stop-AfterCreate {
    param([string]$Detail)
    $id = $script:CreatedId
    $done = @($script:CompletedSteps)
    $notDone = @($script:PlannedSteps | Where-Object { $done -notcontains $_ })
    $rule = '!' * 78
    $text = @(
        $rule
        "CREATED $id but $($script:CurrentStep) failed: $Detail"
        "$id EXISTS in YouTrack. Do NOT re-run -Create: it would file a second ticket."
        "Read it:  pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Ticket $id -Show"
        "Fix text or tags with -Ticket $id. For parent, estimate, Order or sprint, tell Patron."
        "Never hand-roll /api/commands, raw REST, or a script that dot-sources _youtrack.ps1."
        "Steps done: $(if ($done) { $done -join ', ' } else { 'none (the issue POST itself succeeded)' })"
        "Steps NOT done: $(if ($notDone) { $notDone -join ', ' } else { 'none' })"
        $rule)
    foreach ($line in $text) { [Console]::Error.WriteLine($line) }
    [Console]::Out.WriteLine("CREATED $id but $($script:CurrentStep) failed (exit 3); do NOT re-run -Create.")
    exit 3
}

function Stop-WithError {
    param([string]$Message)
    if ($script:CreatedId) { Stop-AfterCreate $Message }
    [Console]::Error.WriteLine("${YouTrackTool}: $Message")
    if ($script:PostingIssue) {
        [Console]::Error.WriteLine("${YouTrackTool}: the create request failed, so nothing is known to exist. If the error was a timeout the ticket may exist: run the same command with -DryRun first (it exits 1 naming the ticket if it does) before any real retry.")
    }
    exit 2
}

$issueFields = 'id,idReadable,summary,description,project(id,shortName),tags(name),' +
    'customFields(name,value(name,minutes,presentation)),links(direction,linkType(name),issues(idReadable))'
$listFields = 'id,idReadable,resolved,customFields(name,value(name,minutes)),' +
    'links(direction,linkType(name),issues(idReadable))'
$BoardAgileId = '204-3'
$OrderRepository = 'lamuflix'   # Order exists only for this Repository
$ProbeWindow = 5                # ids probed directly, beside the (indexed) searches
$RecentCount = 100
$mismatches = [Collections.Generic.List[string]]::new()

# YouTrack stores LF and drops trailing whitespace; compare what it will keep.
function ConvertTo-IssueText {
    param([string]$Text)
    return ($Text -replace "`r`n", "`n").TrimEnd()
}

function Read-TextFile {
    param([string]$Path, [string]$Label)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { Stop-WithError "$Label file not found: $Path" }
    $text = ConvertTo-IssueText ([IO.File]::ReadAllText((Resolve-Path -LiteralPath $Path).ProviderPath))
    if (-not $text) { Stop-WithError "$Label file is empty: $Path" }
    return $text
}

function Get-IssuePath {
    param([string]$Id, [string]$Suffix = '')
    return "/api/issues/$([uri]::EscapeDataString($Id))$Suffix"
}

function Get-Issue {
    param([string]$Id)
    return Send-YouTrack Get (Get-IssuePath $Id "?fields=$issueFields")
}

function Get-IssueNumber {
    param([string]$Id)
    return [int]($Id -replace '^.*-', '')
}

function Get-TagName {
    param($Issue)
    foreach ($entry in @(Get-JsonPath -Object $Issue -Path 'tags')) {
        if ($entry) { [string](Get-JsonPath -Object $entry -Path 'name') }
    }
}

function Get-ParentId {
    param($Issue)
    foreach ($link in @(Get-JsonPath -Object $Issue -Path 'links')) {
        if ((Get-JsonPath -Object $link -Path 'direction') -ne 'INWARD') { continue }
        if ((Get-JsonPath -Object $link -Path 'linkType', 'name') -ne 'Subtask') { continue }
        foreach ($linked in @(Get-JsonPath -Object $link -Path 'issues')) {
            if ($linked) { [string](Get-JsonPath -Object $linked -Path 'idReadable') }
        }
    }
}

function Get-CustomFieldValue {
    # The raw value of a custom field (an int for Order), or $null when empty.
    param($Issue, [string]$Field)
    foreach ($entry in @(Get-JsonPath -Object $Issue -Path 'customFields')) {
        if ((Get-JsonPath -Object $entry -Path 'name') -ne $Field) { continue }
        return Get-JsonPath -Object $entry -Path 'value'
    }
    return $null
}

function Get-CustomFieldMinutes {
    param($Issue, [string]$Field)
    $value = Get-CustomFieldValue $Issue $Field
    $minutes = Get-JsonPath -Object $value -Path 'minutes'
    if ($null -eq $minutes) { return $null }
    return [int]$minutes
}

function ConvertTo-Minutes {
    # 1w = 5d, 1d = 8h: YouTrack's defaults, the same ones the burndown uses.
    param([string]$Period)
    $units = @{ w = 2400; d = 480; h = 60; m = 1 }
    $total = 0
    foreach ($part in $Period -split ' ') { $total += [int]$part.Substring(0, $part.Length - 1) * $units[$part.Substring($part.Length - 1)] }
    return $total
}

function Send-IssueCommand {
    # One YouTrack command against one issue. No braces anywhere: YouTrack takes them
    # literally. Only single-token commands ("subtask of DEV-93") are used here.
    param([string]$Id, [string]$Query)
    $null = Send-YouTrack Post '/api/commands' @{ query = $Query; issues = @(@{ idReadable = $Id }) }
}

function Get-ExistingTagId {
    param([string]$Name)
    $query = [uri]::EscapeDataString($Name)
    $hits = @(Send-YouTrack Get "/api/tags?fields=id,name&`$top=42&query=$query")
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($hit in $hits) {
        if (-not $hit) { continue }
        if (([string](Get-JsonPath -Object $hit -Path 'name')) -cne $Name) { continue }
        $id = [string](Get-JsonPath -Object $hit -Path 'id')
        if ($id) { $null = $ids.Add($id) }
    }
    if ($ids.Count -eq 1) { return @($ids)[0] }
    if ($ids.Count -eq 0) { Stop-WithError "no existing tag named '$Name'." }
    Stop-WithError "more than one tag is named '$Name'."
}

function Add-Tag {
    param([string]$Id)
    foreach ($name in @($Tag | Where-Object { $_ })) {
        $null = Send-YouTrack Post (Get-IssuePath $Id '/tags') @{ id = (Get-ExistingTagId $name) }
    }
}

function Assert-Same {
    param([string]$Label, [string]$Expected, [string]$Actual)
    if ((ConvertTo-IssueText $Actual) -ceq $Expected) { return }
    $mismatches.Add("$Label did not read back as written")
}

function Assert-IssueText {
    param($Issue, [string]$Description)
    if ($Summary) { Assert-Same 'summary' $Summary.Trim() (Get-JsonPath -Object $Issue -Path 'summary') }
    if ($Description) { Assert-Same 'description' $Description (Get-JsonPath -Object $Issue -Path 'description') }
}

function Assert-Tag {
    param($Issue)
    $present = @(Get-TagName $Issue)
    foreach ($name in @($Tag | Where-Object { $_ })) {
        if ($present -notcontains $name) { $mismatches.Add("tag '$name' is missing") }
    }
}

function Assert-Created {
    param($Issue)
    $actualType = Get-CustomFieldName $Issue 'Type'
    if ($actualType -ne $Type) { $mismatches.Add("Type is '$actualType', expected '$Type'") }
    $actualPriority = Get-CustomFieldName $Issue 'Priority'
    if ($actualPriority -ne $Priority) { $mismatches.Add("Priority is '$actualPriority', expected '$Priority'") }
    $actualRepository = Get-CustomFieldName $Issue 'Repository'
    if ($actualRepository -ne $Repository) { $mismatches.Add("Repository is '$actualRepository', expected '$Repository'") }
    $actualMinutes = Get-CustomFieldMinutes $Issue 'Estimated Time'
    if ($actualMinutes -ne (ConvertTo-Minutes $Estimate)) {
        $mismatches.Add("Estimated Time is '$(if ($null -eq $actualMinutes) { '<empty>' } else { "$actualMinutes min" })', expected '$Estimate' ($(ConvertTo-Minutes $Estimate) min)")
    }
    if (@(Get-ParentId $Issue) -notcontains $Parent) { $mismatches.Add("not a subtask of $Parent") }
}

function Write-Issue {
    param($Issue)
    $id = Get-JsonPath -Object $Issue -Path 'idReadable'
    $tags = @(Get-TagName $Issue)
    $order = Get-CustomFieldValue $Issue 'Order'
    $estimate = Get-JsonPath -Object (Get-CustomFieldValue $Issue 'Estimated Time') -Path 'presentation'
    Write-Output "${id}: $(Get-JsonPath -Object $Issue -Path 'summary')"
    Write-Output "  State: $(Get-CustomFieldName $Issue 'State')  Type: $(Get-CustomFieldName $Issue 'Type')  Parent: $(@(Get-ParentId $Issue) -join ', ')"
    Write-Output "  Priority: $(Get-CustomFieldName $Issue 'Priority')  Repository: $(Get-CustomFieldName $Issue 'Repository')  Estimate: $(if ($estimate) { $estimate } else { '<none>' })  Order: $(if ($null -ne $order) { $order } else { '<none>' })"
    Write-Output "  Tags: $(if ($tags) { $tags -join ', ' } else { '<none>' })"
}

function Stop-OnMismatch {
    param([string]$Id)
    if ($mismatches.Count -eq 0) { return }
    foreach ($problem in $mismatches) { [Console]::Error.WriteLine("${YouTrackTool}: ${Id}: $problem") }
    exit 1
}

function Sync-PlanText {
    # Ok is $false when the plan manages the ticket but could not be updated.
    param([string]$Id)
    $summaryFile = $null
    $tool = Join-Path $PSScriptRoot '_plan_text.py'
    if (-not (Test-Path -LiteralPath $tool -PathType Leaf)) {
        # The plan and its sync were removed from the repository (commit dd79fc6); a checkout
        # without them has nothing to keep in step, so the edit must not fail over it.
        return [pscustomobject]@{ Ok = $true; Lines = @('plan: _plan_text.py is not in this checkout (the YouTrack plan was retired); nothing to sync') }
    }
    $arguments = @($tool, $Id, '--scripts-dir', $PlanDirectory)
    try {
        if ($Summary) {
            $summaryFile = [IO.Path]::GetTempFileName()
            [IO.File]::WriteAllText($summaryFile, $Summary.Trim(), [Text.UTF8Encoding]::new($false))
            $arguments += '--summary-file', $summaryFile
        }
        if ($DescriptionFile) { $arguments += '--description-file', (Resolve-Path -LiteralPath $DescriptionFile).ProviderPath }
        $env:PYTHONUTF8 = '1'
        $lines = @(& python @arguments 2>&1 | ForEach-Object { [string]$_ })
        return [pscustomobject]@{ Ok = $LASTEXITCODE -in 0, 3; Lines = $lines }
    }
    catch {
        return [pscustomobject]@{ Ok = $false; Lines = @("could not run _plan_text.py: $($_.Exception.Message)") }
    }
    finally {
        if ($summaryFile) { [IO.File]::Delete($summaryFile) }
    }
}

# ---- -Create: reads -------------------------------------------------------

function Invoke-SoftGet {
    # A GET whose failure the caller handles itself: returns Ok/Value/Error, never exits.
    param([string]$Path)
    $connection = $script:YouTrackConnection
    try {
        $value = Invoke-YouTrack -Method Get -Uri "$($connection.Base)$Path" -Headers $connection.Headers -Body $null
        return [pscustomobject]@{ Ok = $true; Value = $value; Error = $null }
    }
    catch {
        return [pscustomobject]@{ Ok = $false; Value = $null; Error = $_.Exception.Message.Replace($connection.Token, '<redacted>') }
    }
}

function Assert-BundleValues {
    # Type, Priority and Repository must exist in the project's bundles. A token that cannot read
    # the bundles only loses this check (warning); YouTrack still rejects bad values.
    param([string]$ProjectId)
    $read = Invoke-SoftGet "/api/admin/projects/$ProjectId/customFields?fields=field(name),bundle(values(name))&`$top=50"
    if (-not $read.Ok) {
        Write-Warning "${YouTrackTool}: could not read the project's Type/Priority/Repository bundles ($($read.Error)); not validating them."
        return
    }
    foreach ($entry in @($read.Value)) {
        $field = [string](Get-JsonPath -Object $entry -Path 'field', 'name')
        $wanted = switch ($field) { 'Type' { $Type } 'Priority' { $Priority } 'Repository' { $Repository } default { $null } }
        if (-not $wanted) { continue }
        $values = @(Get-JsonPath -Object $entry -Path 'bundle', 'values' | ForEach-Object { [string](Get-JsonPath -Object $_ -Path 'name') })
        if ($values -cnotcontains $wanted) { Stop-WithError "$field '$wanted' is not in the project's $field values: $($values -join ', ')." }
    }
}

function Get-IssueIfExists {
    # Reads one issue by id (no search index); $null when it does not exist.
    param([string]$Id)
    $read = Invoke-SoftGet (Get-IssuePath $Id '?fields=idReadable,summary,resolved')
    if ($read.Ok) { return $read.Value }
    if ($read.Error -match '404|Not Found') { return $null }
    Stop-WithError "YouTrack Get $(Get-IssuePath $Id) failed: $($read.Error)"
}

$script:SameSummaryLabel = @{}

function Get-SameSummaryLabel {
    # "DEV-404 (Done)" for a resolved issue, the bare id for an open one. Filled by
    # Find-SameSummary; the State comes from one extra single-issue read.
    param([string]$Id)
    if ($script:SameSummaryLabel.ContainsKey($Id)) { return $script:SameSummaryLabel[$Id] }
    return $Id
}

function Get-IssueState {
    # The State name of one issue ('resolved' when it cannot be read; the check must
    # still block, so a failed read never changes the verdict).
    param([string]$Id)
    $read = Invoke-SoftGet (Get-IssuePath $Id '?fields=idReadable,customFields(name,value(name))')
    if (-not $read.Ok) { return 'resolved' }
    $name = Get-CustomFieldName $read.Value 'State'
    if ($name) { return $name }
    return 'resolved'
}

function Find-SameSummary {
    # Ids of issues in the project whose summary equals -Summary (case-insensitive,
    # trimmed), in ANY state: open, Done or Closed. Three sources, because the phrase search runs
    # through YouTrack's asynchronous index and can miss an issue created seconds
    # ago: the phrase search, the newest issues by created, and a direct read of
    # the ids around the newest one (pre-create) or just below $OwnId (post-create).
    param([string]$ProjectShortName, [string]$OwnId)
    $candidates = [Collections.Generic.Dictionary[string, object]]::new()
    $phrase = $Summary.Trim().Replace('"', '')
    $search = [uri]::EscapeDataString("project: $ProjectShortName `"$phrase`"")
    foreach ($hit in @(Send-YouTrack Get "/api/issues?fields=idReadable,summary,resolved&`$top=50&query=$search")) {
        if ($hit) { $candidates[[string](Get-JsonPath -Object $hit -Path 'idReadable')] = $hit }
    }
    $recentQuery = [uri]::EscapeDataString("project: $ProjectShortName sort by: created desc")
    $highest = 0
    foreach ($hit in @(Send-YouTrack Get "/api/issues?fields=idReadable,summary,resolved&`$top=$RecentCount&query=$recentQuery")) {
        if (-not $hit) { continue }
        $hitId = [string](Get-JsonPath -Object $hit -Path 'idReadable')
        $candidates[$hitId] = $hit
        $highest = [Math]::Max($highest, (Get-IssueNumber $hitId))
    }
    $probe = @()
    if ($OwnId) { $own = Get-IssueNumber $OwnId; $probe = ($own - $ProbeWindow)..($own - 1) }
    elseif ($highest) { $probe = ($highest + 1)..($highest + $ProbeWindow) }
    foreach ($number in $probe) {
        if ($number -lt 1 -or $candidates.ContainsKey("$ProjectShortName-$number")) { continue }
        $found = Get-IssueIfExists "$ProjectShortName-$number"
        if ($found) { $candidates["$ProjectShortName-$number"] = $found }
    }
    foreach ($id in $candidates.Keys) {
        $hit = $candidates[$id]
        if ($id -eq $OwnId) { continue }
        $hitSummary = [string](Get-JsonPath -Object $hit -Path 'summary')
        if (-not $hitSummary.Trim().Equals($Summary.Trim(), [StringComparison]::OrdinalIgnoreCase)) { continue }
        $script:SameSummaryLabel[$id] = $id
        if ($null -ne (Get-JsonPath -Object $hit -Path 'resolved')) {
            $state = Get-IssueState $id
            $script:SameSummaryLabel[$id] = "$id ($state)"
        }
        $id
    }
}

function Get-ProjectIssue {
    # Every issue of the project (paged), with the fields the Order plan needs.
    param([string]$ProjectShortName)
    $query = [uri]::EscapeDataString("project: $ProjectShortName")
    $page = 500
    for ($skip = 0; ; $skip += $page) {
        $batch = @(Send-YouTrack Get "/api/issues?fields=$listFields&`$top=$page&`$skip=$skip&query=$query" | Where-Object { $_ })
        $batch
        if ($batch.Count -lt $page) { return }
    }
}

function ConvertTo-IssueRecord {
    param($Issue)
    $order = Get-CustomFieldValue $Issue 'Order'
    return [pscustomobject]@{
        Id         = [string](Get-JsonPath -Object $Issue -Path 'idReadable')
        Order      = if ($null -ne $order) { [int]$order } else { $null }
        State      = Get-CustomFieldName $Issue 'State'
        Type       = Get-CustomFieldName $Issue 'Type'
        Repository = Get-CustomFieldName $Issue 'Repository'
        Parents    = @(Get-ParentId $Issue)
    }
}

function Test-OrderSequence {
    # $null when the orders are exactly 1..N; otherwise the first problem.
    param([int[]]$Orders)
    $sorted = @($Orders | Sort-Object)
    for ($i = 0; $i -lt $sorted.Count; $i++) {
        if ($i -gt 0 -and $sorted[$i] -eq $sorted[$i - 1]) { return "Order $($sorted[$i]) is used twice" }
        if ($sorted[$i] -ne $i + 1) { return "Order $($i + 1) is missing (next value is $($sorted[$i]))" }
    }
    return $null
}

function Get-OrderPlan {
    # The slot for a new child of $ParentId and the renumbers it forces. $null when
    # -Repository is not the Order repository.
    param([object[]]$Records, [string]$ParentId)
    if ($Repository -cne $OrderRepository) { return $null }
    $ordered = @($Records | Where-Object { $null -ne $_.Order } | Sort-Object Order)
    $problem = Test-OrderSequence @($ordered | ForEach-Object { $_.Order })
    if ($problem) { Stop-WithError "the existing Order sequence is not 1..N ($problem); fix it before filing tickets." }
    $total = $ordered.Count

    $anchor = $ordered | Where-Object { $_.Id -eq $ParentId } | Select-Object -First 1
    if (-not $anchor) {
        $slot = $total + 1
        $why = "$ParentId has no Order, so the ticket goes at the end"
    }
    else {
        # Only children inside the parent's own block count: it ends at the next epic.
        $nextEpic = $ordered | Where-Object { $_.Type -eq 'Epic' -and $_.Order -gt $anchor.Order } | Select-Object -First 1
        $bound = if ($nextEpic) { $nextEpic.Order } else { $total + 1 }
        $doneChildren = @($ordered | Where-Object {
                $_.Parents -contains $ParentId -and $_.State -eq 'Done' -and $_.Order -gt $anchor.Order -and $_.Order -lt $bound })
        $last = if ($doneChildren) { $doneChildren[-1] } else { $anchor }
        $slot = $last.Order + 1
        $why = if ($doneChildren) { "directly after $($last.Id) (Order $($last.Order)), the last Done child of $ParentId" }
               else { "directly after $ParentId (Order $($anchor.Order)); it has no Done child" }
    }
    $shifts = @($ordered | Where-Object { $_.Order -ge $slot } | Sort-Object Order -Descending |
            ForEach-Object { [pscustomobject]@{ Id = $_.Id; From = $_.Order; To = $_.Order + 1 } })
    return [pscustomobject]@{ Slot = $slot; Why = $why; Total = $total; Shifts = $shifts }
}

function Get-TargetSprint {
    # The board's currentSprint, else the sprint whose start <= now <= finish; $null if none.
    $agile = Send-YouTrack Get "/api/agiles/${BoardAgileId}?fields=currentSprint(id,name)"
    $current = Get-JsonPath -Object $agile -Path 'currentSprint'
    if ($current -and (Get-JsonPath -Object $current -Path 'id')) {
        return [pscustomobject]@{ Id = [string]$current.id; Name = [string](Get-JsonPath -Object $current -Path 'name'); Source = 'currentSprint' }
    }
    $now = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $hits = @(Send-YouTrack Get "/api/agiles/$BoardAgileId/sprints?fields=id,name,start,finish,archived" | Where-Object {
            $_ -and -not (Get-JsonPath -Object $_ -Path 'archived') -and
            $null -ne (Get-JsonPath -Object $_ -Path 'start') -and $null -ne (Get-JsonPath -Object $_ -Path 'finish') -and
            [long]$_.start -le $now -and $now -le [long]$_.finish } | Sort-Object { [long]$_.start } -Descending)
    if ($hits.Count -eq 0) {
        Write-Warning "${YouTrackTool}: board $BoardAgileId has no current sprint; the ticket will not be added to one."
        return $null
    }
    return [pscustomobject]@{ Id = [string]$hits[0].id; Name = [string](Get-JsonPath -Object $hits[0] -Path 'name'); Source = 'date match' }
}

# ---- -Create: writes ------------------------------------------------------

function Set-IssueOrder {
    param([string]$Id, [int]$Order)
    $null = Send-YouTrack Post (Get-IssuePath $Id '?fields=idReadable') @{
        customFields = @(@{ name = 'Order'; '$type' = 'SimpleIssueCustomField'; value = $Order })
    }
}

function Add-ToSprint {
    param([string]$DbId, $Sprint)
    $null = Send-YouTrack Post "/api/agiles/$BoardAgileId/sprints/$($Sprint.Id)/issues?fields=id" @{ id = $DbId; '$type' = 'Issue' }
}

function Write-Renumber {
    # Highest Order first, so no two issues ever share a value, even transiently.
    param($Plan, [string]$NewId)
    foreach ($shift in $Plan.Shifts) {
        Set-IssueOrder $shift.Id $shift.To
        Write-Output "renumber $($shift.Id): $($shift.From) -> $($shift.To)"
    }
    Set-IssueOrder $NewId $Plan.Slot
    Write-Output "order $NewId`: $($Plan.Slot) ($($Plan.Why))"
}

function Assert-Order {
    # The new issue sits in its slot, every renumber stuck, and 1..N has no gap or duplicate.
    param($Plan, $NewIssue, [string]$NewId, [string]$ProjectShortName)
    $records = [Collections.Generic.Dictionary[string, object]]::new()
    foreach ($issue in @(Get-ProjectIssue $ProjectShortName)) {
        $record = ConvertTo-IssueRecord $issue
        $records[$record.Id] = $record
    }
    $records[$NewId] = ConvertTo-IssueRecord $NewIssue   # read by id: the index may not list it yet
    if ($records[$NewId].Order -ne $Plan.Slot) { $mismatches.Add("Order is '$($records[$NewId].Order)', expected $($Plan.Slot)") }
    foreach ($shift in $Plan.Shifts) {
        $actual = if ($records.ContainsKey($shift.Id)) { $records[$shift.Id].Order } else { $null }
        if ($actual -ne $shift.To) { $mismatches.Add("$($shift.Id) Order is '$actual', expected $($shift.To)") }
    }
    $orders = @($records.Values | Where-Object { $null -ne $_.Order } | ForEach-Object { $_.Order })
    $problem = Test-OrderSequence $orders
    if ($problem) { $mismatches.Add("Order sequence is not contiguous after the write: $problem") }
    elseif ($orders.Count -ne $Plan.Total + 1) { $mismatches.Add("Order sequence has $($orders.Count) issues, expected $($Plan.Total + 1)") }
}

function Assert-Sprint {
    param($Sprint, [string]$NewId)
    $members = @(Send-YouTrack Get "/api/agiles/$BoardAgileId/sprints/$($Sprint.Id)/issues?fields=idReadable&`$top=1000" | Where-Object { $_ } |
            ForEach-Object { [string](Get-JsonPath -Object $_ -Path 'idReadable') })
    if ($members -notcontains $NewId) { $mismatches.Add("not a member of sprint '$($Sprint.Name)' ($($Sprint.Id))") }
}

function Enter-CreateLock {
    $mutex = [Threading.Mutex]::new($false, $LockName)
    $held = $false
    try { $held = $mutex.WaitOne([TimeSpan]::FromSeconds($LockTimeoutSeconds)) }
    catch [Threading.AbandonedMutexException] { $held = $true }   # a crashed creator; the lock is ours
    if (-not $held) {
        $mutex.Dispose()
        Stop-WithError "another -Create on this machine holds $LockName (waited ${LockTimeoutSeconds}s). Nothing was created; check YouTrack for the other ticket before running again."
    }
    return $mutex
}

function Exit-CreateLock {
    param($Mutex)
    if (-not $Mutex) { return }
    try { $Mutex.ReleaseMutex() } catch { }
    $Mutex.Dispose()
}

function Stop-OnSameSummary {
    param([string]$Existing, [switch]$DryRunNote)
    $suffix = if ($DryRunNote) { ' (dry run: nothing would be created)' } else { '' }
    $label = Get-SameSummaryLabel $Existing
    $resolvedNote = if ($label -ne $Existing) { " It is resolved, not open; a new ticket needs a different summary." } else { '' }
    [Console]::Error.WriteLine("${YouTrackTool}: not created: $label already has this summary. Edit it with -Ticket $Existing; do not retry -Create.$resolvedNote$suffix")
    exit 1
}

function Get-CreateBody {
    # Every field YouTrack's own defaults would get wrong goes in this one POST, so the
    # issue never exists as a bare Type=Bug ticket. Fields are named, not addressed by id.
    param([string]$ProjectId, [string]$Description)
    $enum = { param($Name, $Value) @{ name = $Name; '$type' = 'SingleEnumIssueCustomField'; value = @{ name = $Value } } }
    return @{
        project      = @{ id = $ProjectId }
        summary      = $Summary.Trim()
        description  = $Description
        customFields = @(
            (& $enum 'Type' $Type)
            (& $enum 'Priority' $Priority)
            (& $enum 'Repository' $Repository)
            @{ name = 'Estimated Time'; '$type' = 'PeriodIssueCustomField'; value = @{ minutes = (ConvertTo-Minutes $Estimate) } }
        )
    }
}

function Get-CreateCommand {
    return "subtask of $Parent"
}

function Write-CreatePlan {
    param($Body, $Plan, $Sprint, [string]$ProjectShortName)
    Write-Output "DRY RUN: every read ran; no write was sent."
    Write-Output "  duplicate check: no $ProjectShortName issue in any state (open, Done, Closed) has this summary"
    Write-Output "  Type '$Type', Priority '$Priority' and Repository '$Repository' are in the project's bundles"
    Write-Output "  planned POST /api/issues?fields=idReadable,id   (one call: text plus Type, Priority, Repository, Estimated Time)"
    Write-Output "    $(ConvertTo-Json -InputObject $Body -Compress -Depth 8)"
    Write-Output "  then the post-create duplicate re-check (reads only; a lower-id duplicate exits 1, nothing deleted)"
    Write-Output "  planned POST /api/commands on the new issue"
    Write-Output "    query: $(Get-CreateCommand)"
    foreach ($name in @($Tag | Where-Object { $_ })) {
        Write-Output "  planned POST /api/issues/<new>/tags  {`"id`":`"$(Get-ExistingTagId $name)`"}  (tag '$name')"
    }
    if (-not $Plan) {
        Write-Output "  Order: none (Repository '$Repository' is not '$OrderRepository')"
    }
    else {
        Write-Output "  Order: slot $($Plan.Slot), $($Plan.Why); $($Plan.Total) ordered issues become $($Plan.Total + 1)"
        foreach ($shift in $Plan.Shifts) { Write-Output "    $($shift.Id): $($shift.From) -> $($shift.To)" }
        Write-Output "    <new>: Order $($Plan.Slot)   (each a POST /api/issues/<id>, highest Order first, the new issue last)"
    }
    if ($Sprint) {
        Write-Output "  Sprint: $($Sprint.Name) (id $($Sprint.Id), via $($Sprint.Source))"
        Write-Output "    planned POST /api/agiles/$BoardAgileId/sprints/$($Sprint.Id)/issues  {`"id`":`"<new issue id>`",`"`$type`":`"Issue`"}"
    }
    else { Write-Output "  Sprint: none current; the issue would not be added to a sprint" }
    Write-Output "  then GET read-back of every field, Order 1..N and sprint membership"
}

function Invoke-Step {
    # A step after the issue exists: its name is what Stop-AfterCreate reports.
    param([string]$Name, [scriptblock]$Action)
    $script:CurrentStep = $Name
    & $Action
    $script:CompletedSteps.Add($Name)
}

function Invoke-PostCreate {
    param([string]$Id, [string]$DbId, $Plan, $Sprint, [string]$Description, [string]$ProjectShortName)

    # A concurrent creator that slipped past the lock (another machine, a raw POST)
    # leaves two open issues with one summary: report it, touch nothing.
    $script:CurrentStep = 'the post-create duplicate check'
    $winners = @(Find-SameSummary $ProjectShortName $Id | Where-Object { (Get-IssueNumber $_) -lt (Get-IssueNumber $Id) })
    if ($winners) {
        [Console]::Error.WriteLine("${YouTrackTool}: DUPLICATE: $Id was created, but $((@($winners | ForEach-Object { Get-SameSummaryLabel $_ })) -join ', ') has the same summary and a lower id. " +
            "Nothing was deleted or edited. $Id was left without parent link, tags, Order or sprint; Patron decides which one to close. Use $($winners[0]); do not retry -Create.")
        exit 1
    }

    Invoke-Step 'set parent link' { Send-IssueCommand $Id (Get-CreateCommand) }
    Invoke-Step 'add tags' { Add-Tag $Id }
    Invoke-Step 'renumber and set Order' { if ($Plan) { Write-Renumber $Plan $Id } }
    Invoke-Step 'add to sprint' {
        if (-not $Sprint) { return }
        if (-not $DbId) { Stop-WithError "YouTrack returned no internal id for $Id; cannot add it to a sprint." }
        Add-ToSprint $DbId $Sprint
    }
    Invoke-Step 'read-back verification' {
        $issue = Get-Issue $Id
        Assert-IssueText $issue $Description
        Assert-Created $issue
        Assert-Tag $issue
        if ($Plan) { Assert-Order $Plan $issue $Id $ProjectShortName }
        if ($Sprint) { Assert-Sprint $Sprint $Id }
        if ($mismatches.Count -gt 0) { Stop-AfterCreate ($mismatches -join '; ') }
        Write-Output "$Id created (verified)"
        Write-Issue $issue
        if ($Sprint) { Write-Output "  Sprint: $($Sprint.Name)" }
        Write-Output "plan: $Id is not in youtrack-plan.json; the sync leaves it alone"
    }
}

function Invoke-CreateLocked {
    param([string]$ProjectId, [string]$ProjectShortName, [string]$Description)

    # Name the ticket the caller should use: an open one first, then Done, Closed last;
    # the lowest id within each.
    $existing = @(Find-SameSummary $ProjectShortName) | Sort-Object `
        { switch -Regex (Get-SameSummaryLabel $_) { '\(Closed\)$' { 2 } '\(.+\)$' { 1 } default { 0 } } },
        { Get-IssueNumber $_ } | Select-Object -First 1
    if ($existing) { Stop-OnSameSummary $existing -DryRunNote:$DryRun }

    $plan = $null
    if ($Repository -ceq $OrderRepository) {
        $records = @(Get-ProjectIssue $ProjectShortName | ForEach-Object { ConvertTo-IssueRecord $_ })
        $plan = Get-OrderPlan $records $Parent
    }
    $sprint = Get-TargetSprint
    $body = Get-CreateBody $ProjectId $Description

    if ($DryRun) {
        Write-CreatePlan $body $plan $sprint $ProjectShortName
        return
    }

    # If this POST fails, nothing is known to exist. A timeout can still lose the reply,
    # so a failure here points at the dry run, whose duplicate guard does not use the index.
    $script:PostingIssue = $true
    $created = Send-YouTrack Post '/api/issues?fields=idReadable,id' $body
    $script:PostingIssue = $false
    $id = [string](Get-JsonPath -Object $created -Path 'idReadable')
    $dbId = [string](Get-JsonPath -Object $created -Path 'id')
    if (-not $id) { Stop-WithError 'YouTrack accepted the create but returned no issue id.' }
    $script:CreatedId = $id

    try { Invoke-PostCreate $id $dbId $plan $sprint $Description $ProjectShortName }
    catch { Stop-AfterCreate $_.Exception.Message.Replace($script:YouTrackConnection.Token, '<redacted>') }
}

function Invoke-Create {
    $description = Read-TextFile $DescriptionFile 'Description'
    $parentIssue = Get-Issue $Parent
    $projectId = Get-JsonPath -Object $parentIssue -Path 'project', 'id'
    $projectShortName = Get-JsonPath -Object $parentIssue -Path 'project', 'shortName'
    if (-not $projectId) { Stop-WithError "could not read the project of $Parent." }
    Assert-BundleValues $projectId

    $lock = if ($DryRun) { $null } else { Enter-CreateLock }
    try { Invoke-CreateLocked $projectId $projectShortName $description }
    finally { Exit-CreateLock $lock }
}

function Add-Comment {
    param([string]$Id, [string]$Text)
    $created = Send-YouTrack Post (Get-IssuePath $Id '/comments?fields=id') @{ text = $Text }
    $commentId = [string](Get-JsonPath -Object $created -Path 'id')
    if (-not $commentId) { $mismatches.Add('comment was not created'); return }
    $stored = Send-YouTrack Get (Get-IssuePath $Id "/comments/$([uri]::EscapeDataString($commentId))?fields=id,text")
    Assert-Same 'comment' $Text (Get-JsonPath -Object $stored -Path 'text')
}

function Invoke-Edit {
    if (-not ($Summary -or $DescriptionFile -or $CommentFile -or $Tag)) {
        Stop-WithError 'nothing to change: pass -Summary, -DescriptionFile, -CommentFile, or -Tag (or -Show to read).'
    }
    $description = if ($DescriptionFile) { Read-TextFile $DescriptionFile 'Description' } else { $null }
    $comment = if ($CommentFile) { Read-TextFile $CommentFile 'Comment' } else { $null }

    $text = @{}
    if ($Summary) { $text.summary = $Summary.Trim() }
    if ($description) { $text.description = $description }
    if ($text.Count -gt 0) { $null = Send-YouTrack Post (Get-IssuePath $Ticket '?fields=idReadable') $text }
    Add-Tag $Ticket
    if ($comment) { Add-Comment $Ticket $comment }

    $issue = Get-Issue $Ticket
    Assert-IssueText $issue $description
    Assert-Tag $issue
    Stop-OnMismatch $Ticket
    Write-Output "$Ticket updated (verified): $(@($text.Keys | Sort-Object) + @(if ($Tag) { 'tags' }) + @(if ($comment) { 'comment' }) -join ', ')"
    Write-Issue $issue

    if ($text.Count -eq 0) { return }
    $plan = Sync-PlanText $Ticket
    $plan.Lines | Write-Output
    if (-not $plan.Ok) {
        [Console]::Error.WriteLine("${YouTrackTool}: $Ticket was updated in YouTrack but youtrack-plan.json was not; the next sync_youtrack_board.py run will revert it.")
        exit 1
    }
}

Connect-YouTrack
switch ($PSCmdlet.ParameterSetName) {
    'Show' { Write-Issue (Get-Issue $Ticket) }
    'Create' { Invoke-Create }
    default { Invoke-Edit }
}
exit 0

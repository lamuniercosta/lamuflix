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

    # New ticket under an epic:
    pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Create -Summary '<text>' `
        -DescriptionFile <md> -Parent DEV-93 -Estimate 1d -Type Task -Tag size:M

    # Same, but only read and print every planned write (zero non-GET calls):
    pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Create -DryRun -Summary '<text>' `
        -DescriptionFile <md> -Parent DEV-93 -Estimate 1d -Type Task -Tag size:M

    # Edit a ticket; pass any combination of the change parameters:
    pwsh scripts/local/Edit-YouTrackIssue.ps1 -Ticket DEV-290 `
        [-Summary '<text>'] [-DescriptionFile <md>] [-CommentFile <md>] [-Tag size:S]

    # Repair a ticket a -Create left half done (exit 3); one step or several in one call:
    pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Ticket DEV-123 -Parent DEV-284
    pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Ticket DEV-123 -Estimate 4h -Type Task
    pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Ticket DEV-123 -Order -Sprint

    # Same, but only read and print every planned write (zero non-GET calls):
    pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Ticket DEV-123 -DryRun -Parent DEV-284 -Order -Sprint

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

  -Tag resolves an existing tag and posts its id to /api/issues/{id}/tags.
  It does not create a tag.

  -Ticket repair parameters (each is read back; a value the ticket already has is a no-op)
    -Parent DEV-x   Adds the "subtask of DEV-x" link (command string without braces, as -Create
                    does). Already a subtask of DEV-x: no write. A DIFFERENT parent already
                    there is refused (exit 2): this tool never removes a link, so a parent is
                    only ever added to a ticket that has none. A ticket is never its own parent.
    -Estimate <p>   Sets Estimated Time by REST, same format as -Create (1h, 4h, 1d, '2h 30m').
    -Type <value>   Sets Type by REST, same values as -Create. An Epic is refused (exit 2).
    -Order          Slots a ticket that has NO Order into the 1..N sequence: directly after its
                    parent's last Done child, then renumbers (highest first, every renumber
                    printed), exactly as -Create does. Refused (exit 2, nothing written) when the
                    ticket already has an Order (it is never moved), its Repository is not
                    lamuflix, it is an Epic, or it has no parent. A parent that -Parent adds in
                    the same call counts, so "-Parent DEV-284 -Order" repairs both in one go.
                    A real run takes -Create's machine-wide lock before it reads the sequence
                    (no concurrent -Create can interleave); a lock timeout is exit 2, nothing
                    written. -DryRun does not take it.
    -Sprint         Adds the ticket to the board's CURRENT sprint (read live, never hardcoded).
                    Already a member: no write, still exit 0. No current sprint: exit 2.
    -DryRun         Also valid with -Ticket: reads everything, prints each planned write
                    (including the renumber list), refuses exactly as a real run would, and exits
                    0 without a single non-GET.
  Every refusal is decided from reads alone, before the first write of the call.

  Exit 0: every requested change read back as written (or -DryRun finished).
  Exit 1: a same-summary issue, open or resolved, blocked -Create (or was detected
          right after it): use the existing ticket (or pick a different summary if
          the work is genuinely new) and do not retry; or an edit did not stick.
  Exit 2: NOTHING was created or written: configuration or HTTP error, the lock could
          not be taken, an invalid Type/Priority/Repository, a non-contiguous Order
          sequence, or -Tag does not name exactly one existing tag. With -Ticket: a
          refused repair (different parent, existing Order, wrong Repository, no parent,
          no current sprint, an Epic), decided before the first write.
  Exit 3: -Create made the ticket, then a later step failed or did not read back.
          stderr starts with "CREATED <id> but <step> failed", lists the steps done and
          not done, and prints the exact runnable -Ticket repair command for every step
          not done (-Parent, -Tag, -Order, -Sprint, or -Type / -Estimate for a read-back
          mismatch). The ticket EXISTS: never re-run -Create. Run the printed commands in
          the order given and read the result with -Show.
          -Ticket also exits 3: some of its writes went through, then one failed (or the
          read-back call failed). stderr starts with "<id> PARTLY UPDATED" and lists what is
          Written, Failed and Not attempted. Re-run -Show first; a half-done Order renumber
          (it says so) is Patron's: do not re-run -Order, report it.

  Credentials resolve as documented in _youtrack.ps1 (same order as
  scripts/get-task.ps1). The token is sent only as Authorization: Bearer and
  is never printed.

  Offline-test seams (production callers omit them): -EnvironmentReader,
  -RestMethodInvoker, -DpapiFileReader (see _youtrack.ps1), -LockName and
  -LockTimeoutSeconds.
#>

[CmdletBinding(DefaultParameterSetName = 'Edit')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Create')]
    [switch]$Create,

    [Parameter(ParameterSetName = 'Create')]
    [Parameter(ParameterSetName = 'Edit')]
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
    [Parameter(ParameterSetName = 'Edit')]
    [ValidatePattern('^[A-Z][A-Z0-9]*-\d+$')]
    [string]$Parent,

    [Parameter(Mandatory, ParameterSetName = 'Create')]
    [Parameter(ParameterSetName = 'Edit')]
    [ValidatePattern('^\d+[wdhm]( \d+[wdhm])*$')]
    [string]$Estimate,

    [Parameter(ParameterSetName = 'Create')]
    [Parameter(ParameterSetName = 'Edit')]
    [ValidateSet('Task', 'Bug', 'Feature', 'Cosmetics', 'Exception', 'Usability Problem', 'Performance Problem')]
    [string]$Type = 'Task',

    [Parameter(ParameterSetName = 'Edit')]
    [switch]$Order,

    [Parameter(ParameterSetName = 'Edit')]
    [switch]$Sprint,

    [Parameter(ParameterSetName = 'Create')]
    [ValidateNotNullOrEmpty()]
    [string]$Priority = 'Normal',

    [Parameter(ParameterSetName = 'Create')]
    [ValidateNotNullOrEmpty()]
    [string]$Repository = 'lamuflix',

    [Parameter(ParameterSetName = 'Create')]
    [Parameter(ParameterSetName = 'Edit')]
    [string]$LockName = 'Global\LamuFlix-YouTrack-Create',

    [Parameter(ParameterSetName = 'Create')]
    [Parameter(ParameterSetName = 'Edit')]
    [ValidateRange(1, 3600)]
    [int]$LockTimeoutSeconds = 120,

    [scriptblock]$EnvironmentReader,
    [scriptblock]$RestMethodInvoker,
    [scriptblock]$DpapiFileReader
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$YouTrackTool = 'Edit-YouTrackIssue'
$TypeRequested = $PSBoundParameters.ContainsKey('Type')   # -Type defaults to Task for -Create only
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
# -Ticket: the writes that went through, the ones planned, and the Order POSTs sent. Once any
# write has gone through, a later failure is exit 3 (Stop-AfterEditWrite), never exit 2.
$script:EditWritten = [Collections.Generic.List[string]]::new()
$script:EditPlanned = @()
$script:OrderWrites = 0

function ConvertTo-ShellArgument {
    # A value as it must be typed in pwsh: bare when it is plain, single-quoted otherwise.
    param([string]$Value)
    if ($Value -match '^[A-Za-z0-9:._-]+$') { return $Value }
    return "'" + $Value.Replace("'", "''") + "'"
}

function Get-StepRepair {
    # The runnable -Ticket command that finishes one step -Create did not do; nothing when
    # the step needs none (no tags asked for, or a Repository that has no Order).
    param([string]$Step, [string]$Tool)
    switch ($Step) {
        'set parent link' { "$Tool -Parent $Parent" }
        'add tags' { if ($Tag) { "$Tool -Tag $((@($Tag | Where-Object { $_ } | ForEach-Object { ConvertTo-ShellArgument $_ })) -join ',')" } }
        'renumber and set Order' { if ($Repository -ceq $OrderRepository) { "$Tool -Order" } }
        'add to sprint' { "$Tool -Sprint" }
    }
}

function Get-ReadBackRepair {
    # A read-back mismatch names the field; Type, Estimate, parent, tag and sprint each have a
    # command. Order, Priority, Repository and text do not: those are Patron's.
    param([string]$Detail, [string]$Tool)
    if ($Detail -match 'Type is') { "$Tool -Type $(ConvertTo-ShellArgument $Type)" }
    if ($Detail -match 'Estimated Time is') { "$Tool -Estimate $(ConvertTo-ShellArgument $Estimate)" }
    if ($Detail -match 'not a subtask of') { "$Tool -Parent $Parent" }
    if ($Detail -match "tag '.*' is missing") { "$Tool -Tag $((@($Tag | Where-Object { $_ } | ForEach-Object { ConvertTo-ShellArgument $_ })) -join ',')" }
    if ($Detail -match 'not a member of sprint') { "$Tool -Sprint" }
}

function Get-RepairLine {
    param([string]$Id, [string[]]$NotDone, [string]$Detail)
    $tool = "pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Ticket $Id"
    $commands = @(foreach ($step in $NotDone) {
            if ($step -ne 'read-back verification') { Get-StepRepair $step $tool }
            elseif ($script:CurrentStep -eq $step) { Get-ReadBackRepair $Detail $tool }
        })
    $lines = @("Repair, in this order (each is read back; one that already holds is a no-op):")
    $lines += @($commands | ForEach-Object { "  $_" })
    if ($script:CurrentStep -eq 'read-back verification' -and $Detail -match 'Order|Priority|Repository|summary|description') {
        $lines += "  (an Order, Priority, Repository or text mismatch has no repair command: tell Patron)"
    }
    if ($commands -match ' -Order$') { $lines += "  (if -Order refuses on a gap in the Order sequence, a renumber stopped half way: tell Patron)" }
    $lines += "Then check it: pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Ticket $Id -Show"
    return $lines
}

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
        "Never hand-roll /api/commands, raw REST, or a script that dot-sources _youtrack.ps1."
        "Steps done: $(if ($done) { $done -join ', ' } else { 'none (the issue POST itself succeeded)' })"
        "Steps NOT done: $(if ($notDone) { $notDone -join ', ' } else { 'none' })"
        (Get-RepairLine $id $notDone $Detail)
        $rule)
    foreach ($line in $text) { [Console]::Error.WriteLine($line) }
    [Console]::Out.WriteLine("CREATED $id but $($script:CurrentStep) failed (exit 3); do NOT re-run -Create.")
    exit 3
}

function Stop-AfterEditWrite {
    # A -Ticket call failed after its first write: say what is written, what is not, and exit 3.
    param([string]$Detail)
    $failed = $script:CurrentStep
    $written = @($script:EditWritten)
    $half = $failed -eq 'order' -and $script:OrderWrites -gt 0
    if ($half) { $written += "order (PARTLY: $($script:OrderWrites) Order write(s))" }
    $notDone = @($script:EditPlanned | Where-Object { $script:EditWritten -notcontains $_ -and $_ -ne $failed })
    $rule = '!' * 78
    $failedNote = if ($failed -eq 'read-back verification') { ' (every write went through; none was confirmed)' } else { ' (a step that writes several things may have written part of them)' }
    $closing = @(if ($half) { 'The Order renumber is HALF DONE: do not re-run -Order or fix it by hand; report to Patron.' }
        else { 'Report to Patron what is Written, Failed and Not attempted; a comment is not idempotent, so never repeat -CommentFile.' })
    $text = @(
        $rule
        "$Ticket PARTLY UPDATED: $failed failed: $Detail"
        "Written: $(if ($written) { $written -join ', ' } else { 'none' })"
        "Failed: $failed$failedNote"
        "Not attempted: $(if ($notDone) { $notDone -join ', ' } else { 'none' })"
        "Read it before anything else:  pwsh F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1 -Ticket $Ticket -Show"
        'Never hand-roll /api/commands, raw REST, or a script that dot-sources _youtrack.ps1.'
        $closing
        $rule)
    foreach ($line in $text) { [Console]::Error.WriteLine($line) }
    [Console]::Out.WriteLine("$Ticket PARTLY UPDATED but $failed failed (exit 3); run -Show and report to Patron.")
    exit 3
}

function Stop-WithError {
    param([string]$Message)
    if ($script:CreatedId) { Stop-AfterCreate $Message }
    if ($script:EditWritten.Count -gt 0 -or $script:OrderWrites -gt 0) { Stop-AfterEditWrite $Message }
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

function Get-TagId {
    # Every -Tag resolved to its id, reads only, so a bad name stops a run before its first write.
    foreach ($name in @($Tag | Where-Object { $_ })) { Get-ExistingTagId $name }
}

function Add-Tag {
    param([string]$Id, [string[]]$TagId)
    foreach ($one in @($TagId)) { $null = Send-YouTrack Post (Get-IssuePath $Id '/tags') @{ id = $one } }
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

function Assert-TypeSet {
    param($Issue)
    $actualType = Get-CustomFieldName $Issue 'Type'
    if ($actualType -ne $Type) { $mismatches.Add("Type is '$actualType', expected '$Type'") }
}

function Assert-EstimateSet {
    param($Issue)
    $actualMinutes = Get-CustomFieldMinutes $Issue 'Estimated Time'
    if ($actualMinutes -ne (ConvertTo-Minutes $Estimate)) {
        $mismatches.Add("Estimated Time is '$(if ($null -eq $actualMinutes) { '<empty>' } else { "$actualMinutes min" })', expected '$Estimate' ($(ConvertTo-Minutes $Estimate) min)")
    }
}

function Assert-ParentSet {
    param($Issue)
    if (@(Get-ParentId $Issue) -notcontains $Parent) { $mismatches.Add("not a subtask of $Parent") }
}

function Assert-Created {
    param($Issue)
    Assert-TypeSet $Issue
    $actualPriority = Get-CustomFieldName $Issue 'Priority'
    if ($actualPriority -ne $Priority) { $mismatches.Add("Priority is '$actualPriority', expected '$Priority'") }
    $actualRepository = Get-CustomFieldName $Issue 'Repository'
    if ($actualRepository -ne $Repository) { $mismatches.Add("Repository is '$actualRepository', expected '$Repository'") }
    Assert-EstimateSet $Issue
    Assert-ParentSet $Issue
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
    $script:OrderWrites++
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

function Get-SprintMember {
    param($Sprint)
    return @(Send-YouTrack Get "/api/agiles/$BoardAgileId/sprints/$($Sprint.Id)/issues?fields=idReadable&`$top=1000" | Where-Object { $_ } |
            ForEach-Object { [string](Get-JsonPath -Object $_ -Path 'idReadable') })
}

function Assert-Sprint {
    param($Sprint, [string]$NewId)
    if ((Get-SprintMember $Sprint) -notcontains $NewId) { $mismatches.Add("not a member of sprint '$($Sprint.Name)' ($($Sprint.Id))") }
}

function Enter-CreateLock {
    $mutex = [Threading.Mutex]::new($false, $LockName)
    $held = $false
    try { $held = $mutex.WaitOne([TimeSpan]::FromSeconds($LockTimeoutSeconds)) }
    catch [Threading.AbandonedMutexException] { $held = $true }   # a crashed creator; the lock is ours
    if (-not $held) {
        $mutex.Dispose()
        Stop-WithError "another -Create or -Ticket -Order on this machine holds $LockName (waited ${LockTimeoutSeconds}s). Nothing was written; check YouTrack for what the other run did before running again."
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

function Get-ParentCommand {
    return "subtask of $Parent"
}

function Write-OrderPlan {
    param($Plan, [string]$Label, [string]$Noun)
    Write-Output "  Order: slot $($Plan.Slot), $($Plan.Why); $($Plan.Total) ordered issues become $($Plan.Total + 1)"
    foreach ($shift in $Plan.Shifts) { Write-Output "    $($shift.Id): $($shift.From) -> $($shift.To)" }
    Write-Output "    ${Label}: Order $($Plan.Slot)   (each a POST /api/issues/<id>, highest Order first, $Noun last)"
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
    Write-Output "    query: $(Get-ParentCommand)"
    foreach ($name in @($Tag | Where-Object { $_ })) {
        Write-Output "  planned POST /api/issues/<new>/tags  {`"id`":`"$(Get-ExistingTagId $name)`"}  (tag '$name')"
    }
    if (-not $Plan) { Write-Output "  Order: none (Repository '$Repository' is not '$OrderRepository')" }
    else { Write-OrderPlan $Plan '<new>' 'the new issue' }
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

    Invoke-Step 'set parent link' { Send-IssueCommand $Id (Get-ParentCommand) }
    Invoke-Step 'add tags' { Add-Tag $Id @(Get-TagId) }
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

# ---- -Ticket: repair parameters --------------------------------------------
# Every refusal is decided from reads, before the first write of the call, so a refused
# repair leaves the ticket exactly as it was.

function Get-ParentRepair {
    # The parent to link, or $null when the ticket already has it. A DIFFERENT parent is
    # refused: this tool only adds links, it never removes one.
    param($Issue)
    if (-not $Parent) { return $null }
    $current = @(Get-ParentId $Issue)
    if ($current -contains $Parent) { return $null }
    if ($Parent -ceq $Ticket) { Stop-WithError "$Ticket cannot be its own parent." }
    if ($current) { Stop-WithError "$Ticket is already a subtask of $($current -join ', '); refusing to add $Parent. This tool never removes a link: Patron changes a parent by hand." }
    $null = Get-Issue $Parent   # exits 2 when it does not exist
    return $Parent
}

function Get-EstimateRepair {
    # The minutes to write, or $null when Estimated Time already holds them.
    param($Issue)
    if (-not $Estimate) { return $null }
    $minutes = ConvertTo-Minutes $Estimate
    if ((Get-CustomFieldMinutes $Issue 'Estimated Time') -eq $minutes) { return $null }
    return $minutes
}

function Get-TypeRepair {
    # The Type to write, or $null when it is already set. An Epic is not retyped: the
    # Order plan treats Epics as block boundaries.
    param($Issue)
    if (-not $TypeRequested) { return $null }
    $current = Get-CustomFieldName $Issue 'Type'
    if ($current -ceq 'Epic') { Stop-WithError "$Ticket is an Epic; -Type does not retype an Epic." }
    if ($current -ceq $Type) { return $null }
    return $Type
}

function Get-OrderRepair {
    # The Order plan for a ticket that has none, or $null when -Order was not given. Get-OrderPlan
    # reads $Repository, whose default is the Order repository; the ticket's own is checked here.
    param($Issue, [string]$ParentId)
    if (-not $Order) { return $null }
    $current = Get-CustomFieldValue $Issue 'Order'
    if ($null -ne $current) { Stop-WithError "$Ticket already has Order $current; -Order only slots a ticket that has none and never moves one." }
    $ticketRepository = Get-CustomFieldName $Issue 'Repository'
    if ($ticketRepository -cne $OrderRepository) { Stop-WithError "$Ticket has Repository '$ticketRepository'; only '$OrderRepository' tickets have an Order." }
    if ((Get-CustomFieldName $Issue 'Type') -ceq 'Epic') { Stop-WithError "$Ticket is an Epic; -Order slots a child under its parent." }
    if (-not $ParentId) { Stop-WithError "$Ticket has no parent, so there is no slot for it; pass -Parent DEV-x with -Order." }
    $shortName = [string](Get-JsonPath -Object $Issue -Path 'project', 'shortName')
    $records = @(Get-ProjectIssue $shortName | ForEach-Object { ConvertTo-IssueRecord $_ })
    return Get-OrderPlan $records $ParentId
}

function Get-SprintRepair {
    # The current sprint and whether the ticket is already in it; $null when -Sprint was not given.
    param($Issue)
    if (-not $Sprint) { return $null }
    $target = Get-TargetSprint
    if (-not $target) { Stop-WithError "board $BoardAgileId has no current sprint; there is nothing to add $Ticket to." }
    $member = (Get-SprintMember $target) -contains $Ticket
    if (-not $member -and -not (Get-JsonPath -Object $Issue -Path 'id')) { Stop-WithError "YouTrack returned no internal id for $Ticket; cannot add it to a sprint." }
    return [pscustomobject]@{ Sprint = $target; Member = $member }
}

function Get-EditRepair {
    param($Issue)
    $addedParent = Get-ParentRepair $Issue
    # -Order needs a parent: the one this call adds counts, so "-Parent X -Order" works in one go.
    $parentForOrder = if ($addedParent) { $addedParent } else { @(Get-ParentId $Issue) | Select-Object -First 1 }
    return [pscustomobject]@{
        Parent  = $addedParent
        Minutes = Get-EstimateRepair $Issue
        Type    = Get-TypeRepair $Issue
        Order   = Get-OrderRepair $Issue $parentForOrder
        Sprint  = Get-SprintRepair $Issue
    }
}

function Get-CustomFieldBody {
    param([string]$Name, [string]$FieldType, $Value)
    return @{ customFields = @(@{ name = $Name; '$type' = $FieldType; value = $Value }) }
}

function Set-IssueCustomField {
    param([string]$Id, [string]$Name, [string]$FieldType, $Value)
    $null = Send-YouTrack Post (Get-IssuePath $Id '?fields=idReadable') (Get-CustomFieldBody $Name $FieldType $Value)
}

function Invoke-EditStep {
    # A -Ticket write: its name is what Stop-AfterEditWrite reports as written or failed.
    param([string]$Name, [scriptblock]$Action)
    $script:CurrentStep = $Name
    & $Action
    $script:EditWritten.Add($Name)
}

function Write-Repair {
    param($Repairs, [string]$Id, [string]$DbId)
    if ($Repairs.Parent) { Invoke-EditStep 'parent' { Send-IssueCommand $Id (Get-ParentCommand) } }
    if ($null -ne $Repairs.Minutes) { Invoke-EditStep 'estimate' { Set-IssueCustomField $Id 'Estimated Time' 'PeriodIssueCustomField' @{ minutes = $Repairs.Minutes } } }
    if ($Repairs.Type) { Invoke-EditStep 'type' { Set-IssueCustomField $Id 'Type' 'SingleEnumIssueCustomField' @{ name = $Repairs.Type } } }
    if ($Repairs.Order) { Invoke-EditStep 'order' { Write-Renumber $Repairs.Order $Id } }
    if ($Repairs.Sprint -and -not $Repairs.Sprint.Member) { Invoke-EditStep 'sprint' { Add-ToSprint $DbId $Repairs.Sprint.Sprint } }
}

function Assert-Repair {
    param($Issue, $Repairs)
    if ($Parent) { Assert-ParentSet $Issue }
    if ($Estimate) { Assert-EstimateSet $Issue }
    if ($TypeRequested) { Assert-TypeSet $Issue }
    if ($Repairs.Order) { Assert-Order $Repairs.Order $Issue $Ticket ([string](Get-JsonPath -Object $Issue -Path 'project', 'shortName')) }
    if ($Repairs.Sprint) { Assert-Sprint $Repairs.Sprint.Sprint $Ticket }
}

function Get-RepairSummary {
    # Which repair parameters wrote something (Changed) and which already held (Held).
    param($Repairs)
    $changed = @(
        if ($Repairs.Parent) { 'parent' }
        if ($null -ne $Repairs.Minutes) { 'estimate' }
        if ($Repairs.Type) { 'type' }
        if ($Repairs.Order) { 'order' }
        if ($Repairs.Sprint -and -not $Repairs.Sprint.Member) { 'sprint' })
    $held = @(
        if ($Parent -and -not $Repairs.Parent) { 'parent' }
        if ($Estimate -and $null -eq $Repairs.Minutes) { 'estimate' }
        if ($TypeRequested -and -not $Repairs.Type) { 'type' }
        if ($Repairs.Sprint -and $Repairs.Sprint.Member) { 'sprint' })
    return [pscustomobject]@{ Changed = $changed; Held = $held }
}

function Write-EditPlan {
    # -DryRun on -Ticket: every planned write, from the reads already made. Nothing is sent.
    param([hashtable]$TextBody, $Comment, [string[]]$TagId, $Repairs, [string]$DbId)
    $outcome = Get-RepairSummary $Repairs
    $fieldPost = "planned POST $(Get-IssuePath $Ticket '?fields=idReadable')"
    $planned = 0
    Write-Output "DRY RUN: every read ran; no write was sent."
    if ($TextBody.Count -gt 0) {
        $planned++
        Write-Output "  $fieldPost   ($(@($TextBody.Keys | Sort-Object) -join ', '))"
        Write-Output "    $(ConvertTo-Json -InputObject $TextBody -Compress -Depth 8)"
    }
    $names = @($Tag | Where-Object { $_ })
    for ($i = 0; $i -lt $names.Count; $i++) {
        $planned++
        Write-Output "  planned POST $(Get-IssuePath $Ticket '/tags')  {`"id`":`"$($TagId[$i])`"}  (tag '$($names[$i])')"
    }
    if ($Comment) { $planned++; Write-Output "  planned POST $(Get-IssuePath $Ticket '/comments?fields=id')  ($($Comment.Length) characters of comment text)" }
    if ($Repairs.Parent) { $planned++; Write-Output "  planned POST /api/commands on $Ticket`n    query: $(Get-ParentCommand)" }
    if ($null -ne $Repairs.Minutes) {
        $planned++
        Write-Output "  $fieldPost   (Estimated Time $Estimate)"
        Write-Output "    $(ConvertTo-Json -InputObject (Get-CustomFieldBody 'Estimated Time' 'PeriodIssueCustomField' @{ minutes = $Repairs.Minutes }) -Compress -Depth 8)"
    }
    if ($Repairs.Type) {
        $planned++
        Write-Output "  $fieldPost   (Type $Type)"
        Write-Output "    $(ConvertTo-Json -InputObject (Get-CustomFieldBody 'Type' 'SingleEnumIssueCustomField' @{ name = $Type }) -Compress -Depth 8)"
    }
    if ($Repairs.Order) { $planned++; Write-OrderPlan $Repairs.Order $Ticket 'the ticket' }
    if ($Repairs.Sprint -and -not $Repairs.Sprint.Member) {
        $planned++
        $target = $Repairs.Sprint.Sprint
        Write-Output "  Sprint: $($target.Name) (id $($target.Id), via $($target.Source))"
        Write-Output "    planned POST /api/agiles/$BoardAgileId/sprints/$($target.Id)/issues  {`"id`":`"$DbId`",`"`$type`":`"Issue`"}"
    }
    if ($outcome.Held) { Write-Output "  already as requested, nothing to write: $($outcome.Held -join ', ')" }
    if ($planned -eq 0) { Write-Output "  nothing would be written: $Ticket already has everything asked for" }
    else { Write-Output "  then GET read-back of every changed field (Order 1..N and sprint membership included)" }
}

function Add-Comment {
    param([string]$Id, [string]$Text)
    $created = Send-YouTrack Post (Get-IssuePath $Id '/comments?fields=id') @{ text = $Text }
    $commentId = [string](Get-JsonPath -Object $created -Path 'id')
    if (-not $commentId) { $mismatches.Add('comment was not created'); return }
    $stored = Send-YouTrack Get (Get-IssuePath $Id "/comments/$([uri]::EscapeDataString($commentId))?fields=id,text")
    Assert-Same 'comment' $Text (Get-JsonPath -Object $stored -Path 'text')
}

function Invoke-EditLocked {
    param([string]$Description, [string]$Comment)
    # All reads and refusals first: nothing is written until every requested change is known to be possible.
    $before = Get-Issue $Ticket
    $dbId = [string](Get-JsonPath -Object $before -Path 'id')
    $repairs = Get-EditRepair $before
    $tagIds = @(Get-TagId)
    $text = @{}
    if ($Summary) { $text.summary = $Summary.Trim() }
    if ($Description) { $text.description = $Description }
    if ($DryRun) { Write-EditPlan $text $Comment $tagIds $repairs $dbId; return }

    $script:EditPlanned = @(
        if ($text.Count -gt 0) { 'summary/description' }
        if ($tagIds.Count -gt 0) { 'tags' }
        if ($Comment) { 'comment' }
        @((Get-RepairSummary $repairs).Changed)
        'read-back verification')
    if ($text.Count -gt 0) { Invoke-EditStep 'summary/description' { $null = Send-YouTrack Post (Get-IssuePath $Ticket '?fields=idReadable') $text } }
    if ($tagIds.Count -gt 0) { Invoke-EditStep 'tags' { Add-Tag $Ticket $tagIds } }
    if ($Comment) { Invoke-EditStep 'comment' { Add-Comment $Ticket $Comment } }
    Write-Repair $repairs $Ticket $dbId

    $script:CurrentStep = 'read-back verification'
    $issue = Get-Issue $Ticket
    Assert-IssueText $issue $Description
    Assert-Tag $issue
    Assert-Repair $issue $repairs
    Stop-OnMismatch $Ticket
    $outcome = Get-RepairSummary $repairs
    $changed = @($text.Keys | Sort-Object) + @(if ($Tag) { 'tags' }) + @(if ($Comment) { 'comment' }) + @($outcome.Changed)
    if ($changed) { Write-Output "$Ticket updated (verified): $($changed -join ', ')" }
    else { Write-Output "$Ticket already as requested (verified): nothing written" }
    if ($outcome.Held) { Write-Output "  already as requested, nothing written: $($outcome.Held -join ', ')" }
    Write-Issue $issue
    if ($repairs.Sprint) { Write-Output "  Sprint: $($repairs.Sprint.Sprint.Name)" }
}

function Invoke-Edit {
    if (-not ($Summary -or $DescriptionFile -or $CommentFile -or $Tag -or $Parent -or $Estimate -or $TypeRequested -or $Order -or $Sprint)) {
        Stop-WithError 'nothing to change: pass -Summary, -DescriptionFile, -CommentFile, -Tag, -Parent, -Estimate, -Type, -Order or -Sprint (or -Show to read).'
    }
    $description = if ($DescriptionFile) { Read-TextFile $DescriptionFile 'Description' } else { $null }
    $comment = if ($CommentFile) { Read-TextFile $CommentFile 'Comment' } else { $null }

    # -Order renumbers the whole sequence, so it takes -Create's lock before it reads the sequence;
    # a lock timeout is exit 2 with nothing written. -DryRun writes nothing and never waits.
    $lock = if ($Order -and -not $DryRun) { Enter-CreateLock } else { $null }
    try { Invoke-EditLocked $description $comment }
    finally { Exit-CreateLock $lock }
}

Connect-YouTrack
switch ($PSCmdlet.ParameterSetName) {
    'Show' { Write-Issue (Get-Issue $Ticket) }
    'Create' { Invoke-Create }
    default { Invoke-Edit }
}
exit 0

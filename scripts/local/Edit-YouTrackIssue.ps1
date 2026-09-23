#!/usr/bin/env pwsh
<#
  Creates, edits, or shows one YouTrack issue, and reads every change back.
  Rigger runs it. Patron decides what changes and supplies the text as files,
  so Markdown survives shell quoting.

    # New ticket under an epic (a same-summary ticket in the project blocks it):
    pwsh scripts/local/Edit-YouTrackIssue.ps1 -Create -Summary '<text>' `
        -DescriptionFile <md> -Parent DEV-93 -Estimate 1d -Tag size:M

    # Edit a ticket; pass any combination of the change parameters:
    pwsh scripts/local/Edit-YouTrackIssue.ps1 -Ticket DEV-290 `
        [-Summary '<text>'] [-DescriptionFile <md>] [-CommentFile <md>] [-Tag size:S]

    # Read-only: summary, State, Type, parent, and tags (get-task.ps1 omits tags):
    pwsh scripts/local/Edit-YouTrackIssue.ps1 -Ticket DEV-290 -Show

  scripts/sync_youtrack_board.py rewrites the summary and description of every
  ticket scripts/youtrack-plan.json manages. A summary or description edit on
  such a ticket is therefore written into the plan too (via _plan_text.py).
  Run the tool from the worktree whose PR will carry that plan change, and
  commit the plan with it. Comments and tags are never touched by the sync.

  Exit 0: every requested change read back as written. Exit 1: a change did
  not stick, a duplicate blocked -Create, or the plan could not be updated.
  Exit 2: configuration or HTTP error.

  Credentials resolve as documented in _youtrack.ps1 (same order as
  scripts/get-task.ps1). The token is sent only as Authorization: Bearer and
  is never printed.

  Offline-test seams (production callers omit them): -EnvironmentReader,
  -RestMethodInvoker, -DpapiFileReader (see _youtrack.ps1), and
  -PlanDirectory (the folder holding youtrack-plan.json).
#>

[CmdletBinding(DefaultParameterSetName = 'Edit')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Create')]
    [switch]$Create,

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
    [ValidateNotNullOrEmpty()]
    [string]$Priority = 'Major',

    [Parameter(ParameterSetName = 'Create')]
    [ValidateNotNullOrEmpty()]
    [string]$Repository = 'lamuflix',

    [string]$PlanDirectory = (Split-Path -Parent $PSScriptRoot),
    [scriptblock]$EnvironmentReader,
    [scriptblock]$RestMethodInvoker,
    [scriptblock]$DpapiFileReader
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$YouTrackTool = 'Edit-YouTrackIssue'
. (Join-Path $PSScriptRoot '_youtrack.ps1')

$issueFields = 'idReadable,summary,description,project(id,shortName),tags(name),' +
    'customFields(name,value(name)),links(direction,linkType(name),issues(idReadable))'
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

function Send-IssueCommand {
    # One YouTrack command against one issue; braces keep multi-word values whole.
    param([string]$Id, [string]$Query)
    $null = Send-YouTrack Post '/api/commands' @{ query = $Query; issues = @(@{ idReadable = $Id }) }
}

function Add-Tag {
    param([string]$Id)
    foreach ($name in @($Tag | Where-Object { $_ })) { Send-IssueCommand $Id "tag {$name}" }
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
    $type = Get-CustomFieldName $Issue 'Type'
    if ($type -ne 'Task') { $mismatches.Add("Type is '$type', expected 'Task'") }
    if (@(Get-ParentId $Issue) -notcontains $Parent) { $mismatches.Add("not a subtask of $Parent") }
}

function Find-SameSummary {
    # Phrase search, then an exact comparison: the search also matches descriptions.
    param([string]$ProjectShortName)
    $phrase = $Summary.Trim().Replace('"', '')
    $query = [uri]::EscapeDataString("project: {$ProjectShortName} `"$phrase`"")
    foreach ($hit in @(Send-YouTrack Get "/api/issues?fields=idReadable,summary&`$top=50&query=$query")) {
        if (-not $hit) { continue }
        $hitSummary = [string](Get-JsonPath -Object $hit -Path 'summary')
        if ($hitSummary.Trim().Equals($Summary.Trim(), [StringComparison]::OrdinalIgnoreCase)) {
            return [string](Get-JsonPath -Object $hit -Path 'idReadable')
        }
    }
    return $null
}

function Write-Issue {
    param($Issue)
    $id = Get-JsonPath -Object $Issue -Path 'idReadable'
    $tags = @(Get-TagName $Issue)
    Write-Output "${id}: $(Get-JsonPath -Object $Issue -Path 'summary')"
    Write-Output "  State: $(Get-CustomFieldName $Issue 'State')  Type: $(Get-CustomFieldName $Issue 'Type')  Parent: $(@(Get-ParentId $Issue) -join ', ')"
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
    $arguments = @((Join-Path $PSScriptRoot '_plan_text.py'), $Id, '--scripts-dir', $PlanDirectory)
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

function Invoke-Create {
    $description = Read-TextFile $DescriptionFile 'Description'
    $parentIssue = Get-Issue $Parent
    $projectId = Get-JsonPath -Object $parentIssue -Path 'project', 'id'
    $projectShortName = Get-JsonPath -Object $parentIssue -Path 'project', 'shortName'
    if (-not $projectId) { Stop-WithError "could not read the project of $Parent." }

    $existing = Find-SameSummary $projectShortName
    if ($existing) {
        [Console]::Error.WriteLine("${YouTrackTool}: not created: $existing already has this summary. Edit it with -Ticket $existing.")
        exit 1
    }

    $body = @{ project = @{ id = $projectId }; summary = $Summary.Trim(); description = $description }
    $id = [string](Get-JsonPath -Object (Send-YouTrack Post '/api/issues?fields=idReadable' $body) -Path 'idReadable')
    if (-not $id) { Stop-WithError 'YouTrack accepted the create but returned no issue id.' }

    Send-IssueCommand $id "Type Task Repository {$Repository} Priority {$Priority} Estimated Time $Estimate subtask of $Parent"
    Add-Tag $id

    $issue = Get-Issue $id
    Assert-IssueText $issue $description
    Assert-Created $issue
    Assert-Tag $issue
    Stop-OnMismatch $id
    Write-Output "$id created (verified)"
    Write-Issue $issue
    Write-Output "plan: $id is not in youtrack-plan.json; the sync leaves it alone"
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

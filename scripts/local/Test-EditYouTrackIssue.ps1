#!/usr/bin/env pwsh
# Offline self-test for Edit-YouTrackIssue.ps1. No network, no real credentials.
# A stateful fake YouTrack (a JSON file) makes every read-back reflect what the
# script actually wrote. Needs python on PATH for the plan-sync cases.
#
#   pwsh ./scripts/local/Test-EditYouTrackIssue.ps1

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'Edit-YouTrackIssue.ps1'
$failures = 0
$checks = 0
$work = Join-Path ([IO.Path]::GetTempPath()) ('ytedit-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $work

function Assert-True {
    param([string]$Name, [bool]$Condition)
    $script:checks++
    if ($Condition) { Write-Host "  ok       $Name" }
    else { Write-Host "  FAIL     $Name" -ForegroundColor Red; $script:failures++ }
}

# The fake: routes a request against the state file and returns what
# Invoke-RestMethod would (PSCustomObjects, not hashtables).
$fake = @'
function ConvertTo-Response { param($Value) ConvertTo-Json -InputObject $Value -Depth 10 | ConvertFrom-Json }

function Get-IssueView {
    param([string]$Id, $Issue)
    $fields = @(@{ name = 'Type'; value = $(if ($Issue.type) { @{ name = $Issue.type } } else { $null }) },
                @{ name = 'State'; value = @{ name = $Issue.state } })
    $links = @(if ($Issue.parent) { @{ direction = 'INWARD'; linkType = @{ name = 'Subtask' }; issues = @(@{ idReadable = $Issue.parent }) } })
    return @{ idReadable = $Id; summary = $Issue.summary; description = $Issue.description
              project = @{ id = '0-1'; shortName = 'DEV' }; tags = @(@($Issue.tags) | ForEach-Object { @{ name = $_ } })
              customFields = $fields; links = $links }
}

function Invoke-FakeYouTrack {
    param($State, [string]$Method, [string]$Uri, [string]$Body)
    if ($State.options.throw) { throw 'HTTP 500 with perm-test in the message' }
    $path = ([uri]$Uri).AbsolutePath
    $b = if ($Body) { $Body | ConvertFrom-Json -AsHashtable } else { $null }
    if ("$Method $path" -eq 'Post /api/issues') {
        $State.issues['DEV-900'] = @{ summary = $b.summary; description = $b.description; tags = @(); type = $null; parent = $null; state = 'Open' }
        return ConvertTo-Response @{ idReadable = 'DEV-900' }
    }
    if ("$Method $path" -eq 'Post /api/commands') {
        $issue = $State.issues[$b.issues[0].idReadable]
        if ($b.query -match '^tag \{(.+)\}$') { if (-not $State.options.dropTag) { $issue.tags = @(@($issue.tags) + $Matches[1]) } }
        elseif ($b.query -match '^Type (\S+) .* subtask of (\S+)$') { $issue.type = $Matches[1]; $issue.parent = $Matches[2] }
        return ConvertTo-Response @{}
    }
    if ("$Method $path" -eq 'Get /api/issues') { return @($State.search | ForEach-Object { ConvertTo-Response $_ }) }
    if ($path -match '^/api/issues/([^/]+)/comments/(.+)$') {
        $text = if ($State.options.mangleComment) { 'mangled' } else { $State.comments[$Matches[2]] }
        return ConvertTo-Response @{ id = $Matches[2]; text = $text }
    }
    if ($path -match '^/api/issues/([^/]+)/comments$') {
        $State.comments['c1'] = $b.text
        return ConvertTo-Response @{ id = 'c1' }
    }
    if ($path -match '^/api/issues/([^/]+)$') {
        $id = $Matches[1]
        $issue = $State.issues[$id]
        if ($Method -eq 'Get') { return ConvertTo-Response (Get-IssueView $id $issue) }
        if ($b.ContainsKey('summary')) { $issue.summary = $b.summary }
        # YouTrack may echo a trailing newline; the script must tolerate it.
        if ($b.ContainsKey('description')) { $issue.description = $b.description + "`n" }
        return ConvertTo-Response @{ idReadable = $id }
    }
    throw "fake: no route for $Method $path"
}
'@
$fakeFile = Join-Path $work 'fake.ps1'
Set-Content -LiteralPath $fakeFile -Value $fake

function New-State {
    param([object[]]$Search = @(), [switch]$DropTag, [switch]$MangleComment, [switch]$Throw)
    return @{
        options  = @{ dropTag = [bool]$DropTag; mangleComment = [bool]$MangleComment; throw = [bool]$Throw }
        search   = $Search
        comments = @{}
        issues   = @{
            'DEV-93'  = @{ summary = 'Epic'; description = 'e'; tags = @(); type = 'Epic'; parent = $null; state = 'Open' }
            'DEV-290' = @{ summary = 'Old summary'; description = 'Old text'; tags = @('size:M'); type = 'Task'; parent = 'DEV-93'; state = 'Open' }
            'DEV-700' = @{ summary = 'Unplanned'; description = 'u'; tags = @(); type = 'Task'; parent = 'DEV-93'; state = 'Open' }
        }
    }
}

# Mirrors json.dumps(indent=2, ensure_ascii=False) with CRLF and no trailing newline.
function Get-PlanText {
    param([string]$Summary, [string]$Description)
    return (@('{', '  "epics": [],', '  "tasks": [', '    {', '      "key": "task-1.2",', '      "action": "create",',
        "      `"summary`": `"$Summary`",", "      `"description`": `"$Description`"", '    }', '  ]', '}') -join "`r`n")
}

function New-PlanDirectory {
    $dir = Join-Path $work ('plan-' + [guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $dir
    [IO.File]::WriteAllText((Join-Path $dir 'youtrack-plan.json'), (Get-PlanText 'Old summary' 'Old — text'), [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $dir 'created-tasks.json'), '{ "task-1.2": "DEV-290" }')
    return $dir
}

function New-TextFile {
    param([string]$Text)
    $path = Join-Path $work ([guid]::NewGuid().ToString('N') + '.md')
    [IO.File]::WriteAllText($path, $Text, [Text.UTF8Encoding]::new($false))
    return $path
}

# Runs the script in a child pwsh so its `exit` codes are observable.
# $Arguments is PowerShell source for the script's own parameters.
function Invoke-Edit {
    param([string]$Arguments, [hashtable]$State = (New-State), [string]$PlanDirectory = (New-PlanDirectory), [switch]$NoUrl)

    $log = Join-Path $work ([guid]::NewGuid().ToString('N') + '.log')
    $stateFile = Join-Path $work ([guid]::NewGuid().ToString('N') + '.json')
    Set-Content -LiteralPath $stateFile -Value (ConvertTo-Json -InputObject $State -Depth 10)
    $driver = @"
`$env:YT_TEST_LOG = '$log'
`$env:YT_TEST_STATE = '$stateFile'
. '$fakeFile'
`$envReader = { param(`$Name, `$Target)
    if (`$Target -ne 'Process') { return `$null }
    if (`$Name -eq 'YOUTRACK_URL') { return $(if ($NoUrl) { '$null' } else { "'https://yt.example/'" }) }
    if (`$Name -eq 'YOUTRACK_TOKEN') { return 'perm-test' }
}
`$invoker = { param(`$Method, `$Uri, `$Headers, `$Body)
    Add-Content -LiteralPath `$env:YT_TEST_LOG -Value "`$Method `$Uri `$Body auth=`$(`$Headers.Authorization)"
    `$state = Get-Content -LiteralPath `$env:YT_TEST_STATE -Raw | ConvertFrom-Json -AsHashtable
    try { return Invoke-FakeYouTrack `$state `$Method `$Uri `$Body }
    finally { Set-Content -LiteralPath `$env:YT_TEST_STATE -Value (ConvertTo-Json -InputObject `$state -Depth 10) }
}
# A parameter-validation failure never reaches the script's own exit codes.
try { & '$script' $Arguments -PlanDirectory '$PlanDirectory' -EnvironmentReader `$envReader -RestMethodInvoker `$invoker -DpapiFileReader { `$null } }
catch { Write-Output "binding: `$(`$_.Exception.Message)"; exit 3 }
exit `$LASTEXITCODE
"@
    $output = @(& pwsh -NoProfile -NonInteractive -Command $driver 2>&1)
    $code = $LASTEXITCODE
    $calls = @(if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log })
    $after = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json -AsHashtable
    return [pscustomobject]@{ ExitCode = $code; Output = ($output -join "`n"); Calls = $calls; State = $after; PlanDirectory = $PlanDirectory }
}

function Read-Plan { param([string]$Directory) [IO.File]::ReadAllText((Join-Path $Directory 'youtrack-plan.json')) }

try {
    Write-Host 'Edit-YouTrackIssue'
    $description = New-TextFile "### Overview`r`nFollow-up text.`r`n"

    $show = Invoke-Edit "-Ticket DEV-290 -Show"
    Assert-True 'show: exit 0' ($show.ExitCode -eq 0)
    Assert-True 'show: prints summary, State, and tags' ($show.Output -match 'DEV-290: Old summary' -and $show.Output -match 'State: Open' -and $show.Output -match 'Tags: size:M')
    Assert-True 'show: reads only' (@($show.Calls | Where-Object { $_ -notmatch '^Get ' }).Count -eq 0)

    $create = Invoke-Edit "-Create -Summary 'Close DEV-289 gaps' -DescriptionFile '$description' -Parent DEV-93 -Estimate 1d -Tag size:M"
    Assert-True 'create: exit 0' ($create.ExitCode -eq 0)
    Assert-True 'create: reports verified id' ($create.Output -match 'DEV-900 created \(verified\)')
    Assert-True 'create: uses the parent''s project id' (@($create.Calls -match '^Post https://yt\.example/api/issues\?fields=idReadable .*"project":\{"id":"0-1"\}').Count -gt 0)
    Assert-True 'create: sets type, estimate, and parent' (@($create.Calls -match '"query":"Type Task Repository \{lamuflix\} Priority \{Major\} Estimated Time 1d subtask of DEV-93"').Count -gt 0)
    Assert-True 'create: adds the tag' (@($create.Calls -match '"query":"tag \{size:M\}"').Count -gt 0)
    Assert-True 'create: stores LF text without trailing whitespace' ($create.State.issues['DEV-900'].description -ceq "### Overview`nFollow-up text.")
    Assert-True 'create: sends the token as Bearer' ($create.Calls[0] -match 'auth=Bearer perm-test$')
    Assert-True 'create: leaves the plan alone' ((Read-Plan $create.PlanDirectory) -ceq (Get-PlanText 'Old summary' 'Old — text'))

    $dupe = Invoke-Edit "-Create -Summary 'Close DEV-289 gaps' -DescriptionFile '$description' -Parent DEV-93 -Estimate 1d" `
        -State (New-State -Search @(@{ idReadable = 'DEV-500'; summary = 'close dev-289 GAPS ' }))
    Assert-True 'create: exit 1 on a same-summary ticket' ($dupe.ExitCode -eq 1 -and $dupe.Output -match 'DEV-500 already has this summary')
    Assert-True 'create: duplicate makes no issue' (@($dupe.Calls -match '^Post https://yt\.example/api/issues\?').Count -eq 0)

    $near = Invoke-Edit "-Create -Summary 'Close DEV-289 gaps' -DescriptionFile '$description' -Parent DEV-93 -Estimate 1d" `
        -State (New-State -Search @(@{ idReadable = 'DEV-501'; summary = 'Close DEV-289 gaps later' }))
    Assert-True 'create: a phrase hit with another summary is not a duplicate' ($near.ExitCode -eq 0)

    $comment = New-TextFile "Patron ruling: keep ADR 0001.`n"
    $commented = Invoke-Edit "-Ticket DEV-290 -CommentFile '$comment'"
    Assert-True 'comment: exit 0 and read back' ($commented.ExitCode -eq 0 -and $commented.Output -match 'updated \(verified\): comment')
    Assert-True 'comment: plan untouched' ((Read-Plan $commented.PlanDirectory) -ceq (Get-PlanText 'Old summary' 'Old — text'))

    $mangled = Invoke-Edit "-Ticket DEV-290 -CommentFile '$comment'" -State (New-State -MangleComment)
    Assert-True 'comment: exit 1 when the stored text differs' ($mangled.ExitCode -eq 1 -and $mangled.Output -match 'comment did not read back')

    $dropped = Invoke-Edit "-Ticket DEV-290 -Tag size:S" -State (New-State -DropTag)
    Assert-True 'tag: exit 1 when the tag did not stick' ($dropped.ExitCode -eq 1 -and $dropped.Output -match "tag 'size:S' is missing")

    $newText = New-TextFile "Line one — ü`nLine two"
    $planned = Invoke-Edit "-Ticket DEV-290 -Summary 'New summary' -DescriptionFile '$newText'"
    Assert-True 'plan: exit 0 on a planned ticket' ($planned.ExitCode -eq 0 -and $planned.Output -match 'entry task-1.2 updated for DEV-290')
    Assert-True 'plan: rewrites only the edited fields, keeping CRLF' ((Read-Plan $planned.PlanDirectory) -ceq (Get-PlanText 'New summary' 'Line one — ü\nLine two'))
    Assert-True 'plan: YouTrack has the new summary' ($planned.State.issues['DEV-290'].summary -ceq 'New summary')

    $unplanned = Invoke-Edit "-Ticket DEV-700 -Summary 'Renamed'"
    Assert-True 'plan: a ticket outside the plan is reported and left alone' ($unplanned.ExitCode -eq 0 -and $unplanned.Output -match 'DEV-700 is not in youtrack-plan.json' -and (Read-Plan $unplanned.PlanDirectory) -ceq (Get-PlanText 'Old summary' 'Old — text'))

    $nothing = Invoke-Edit "-Ticket DEV-290"
    Assert-True 'edit: exit 2 with nothing to change' ($nothing.ExitCode -eq 2 -and $nothing.Calls.Count -eq 0)

    $missing = Invoke-Edit "-Ticket DEV-290 -DescriptionFile '$(Join-Path $work 'absent.md')'"
    Assert-True 'edit: exit 2 on a missing text file, before any request' ($missing.ExitCode -eq 2 -and $missing.Calls.Count -eq 0)

    $noUrl = Invoke-Edit "-Ticket DEV-290 -Show" -NoUrl
    Assert-True 'exit 2 without YOUTRACK_URL' ($noUrl.ExitCode -eq 2 -and $noUrl.Calls.Count -eq 0)

    $http = Invoke-Edit "-Ticket DEV-290 -Show" -State (New-State -Throw)
    Assert-True 'exit 2 on HTTP failure' ($http.ExitCode -eq 2)
    Assert-True 'redacts the token from errors' ($http.Output -notmatch 'perm-test' -and $http.Output -match '<redacted>')

    $badTicket = Invoke-Edit "-Ticket 'dev 290' -Show"
    Assert-True 'rejects a malformed ticket id' ($badTicket.ExitCode -eq 3 -and $badTicket.Calls.Count -eq 0)

    $noParent = Invoke-Edit "-Create -Summary 'x' -DescriptionFile '$description' -Estimate 1d"
    Assert-True 'create: requires -Parent' ($noParent.ExitCode -eq 3 -and $noParent.Calls.Count -eq 0)
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host ''
if ($failures -gt 0) {
    Write-Host "$failures of $checks check(s) failed." -ForegroundColor Red
    exit 1
}
Write-Host "All $checks checks passed."
exit 0

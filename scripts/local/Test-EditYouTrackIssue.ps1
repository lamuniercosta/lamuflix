#!/usr/bin/env pwsh
# Offline self-test for Edit-YouTrackIssue.ps1. No network, no real credentials.
# A stateful fake YouTrack (a JSON file) makes every read-back reflect what the
# script actually wrote.
#
#   pwsh ./scripts/local/Test-EditYouTrackIssue.ps1

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'Edit-YouTrackIssue.ps1'
$failures = 0
$checks = 0
$work = Join-Path ([IO.Path]::GetTempPath()) ('ytedit-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $work
$testLock = 'ytedit-test-' + [guid]::NewGuid().ToString('N')   # never the real creation lock

function Assert-True {
    param([string]$Name, [bool]$Condition)
    $script:checks++
    if ($Condition) { Write-Host "  ok       $Name" }
    else { Write-Host "  FAIL     $Name" -ForegroundColor Red; $script:failures++ }
}

# The fake: routes a request against the state file and returns what
# Invoke-RestMethod would (PSCustomObjects, not hashtables). An issue flagged `hidden`
# is invisible to every list and search (an index that lags) but readable by id.
# Options (State.options): throw, dropTag, mangleComment, postFails, lag, race,
# ignoreFields, wrongType, wrongRepository, dropEstimate, dropOrder, dropSprint,
# failStep = parent | tags | order | sprint.
$fake = @'
function ConvertTo-Response { param($Value) ConvertTo-Json -InputObject $Value -Depth 10 | ConvertFrom-Json }

function ConvertTo-FakeMinutes {
    param([string]$Period)
    $units = @{ w = 2400; d = 480; h = 60; m = 1 }
    $total = 0
    foreach ($part in $Period -split ' ') { $total += [int]$part.Substring(0, $part.Length - 1) * $units[$part.Substring($part.Length - 1)] }
    return $total
}

function Get-FakeOption { param($State, [string]$Name) if ($State.options.ContainsKey($Name)) { return $State.options[$Name] } return $null }

function Get-FakeNumber { param([string]$Id) [int]($Id -replace '^.*-', '') }

function Get-FakeIssueView {
    param([string]$Id, $Issue)
    $named = { param($n) if ($n) { @{ name = $n } } else { $null } }
    $estimate = if ($Issue.estimate) { @{ minutes = [int]$Issue.estimate; presentation = "$($Issue.estimate)m" } } else { $null }
    $fields = @(@{ name = 'Type'; value = (& $named $Issue.type) },
                @{ name = 'State'; value = @{ name = $Issue.state } },
                @{ name = 'Priority'; value = (& $named $Issue.priority) },
                @{ name = 'Repository'; value = (& $named $Issue.repository) },
                @{ name = 'Estimated Time'; value = $estimate },
                @{ name = 'Order'; value = $Issue.order })
    $links = @(if ($Issue.parent) { @{ direction = 'INWARD'; linkType = @{ name = 'Subtask' }; issues = @(@{ idReadable = $Issue.parent }) } })
    return @{ idReadable = $Id; id = "3-$(Get-FakeNumber $Id)"; summary = $Issue.summary; description = $Issue.description
              resolved = $(if ($Issue.resolved) { 1790000000000 } else { $null })
              project = @{ id = '0-1'; shortName = 'DEV' }; tags = @(@($Issue.tags) | ForEach-Object { @{ name = $_ } })
              customFields = $fields; links = $links }
}

function New-FakeIssue {
    param([string]$Summary, [string]$Description, [bool]$Hidden)
    return @{ summary = $Summary; description = $Description; tags = @(); type = 'Bug'; priority = 'Normal'; repository = $null
              estimate = $null; order = $null; parent = $null; state = 'Todo'; resolved = $false; hidden = $Hidden }
}

function Invoke-FakeYouTrack {
    param($State, [string]$Method, [string]$Uri, [string]$Body)
    if ((Get-FakeOption $State 'throw')) { throw 'HTTP 500 with perm-test in the message' }
    $path = ([uri]$Uri).AbsolutePath
    $query = [uri]::UnescapeDataString(([uri]$Uri).Query)
    $b = if ($Body) { $Body | ConvertFrom-Json -AsHashtable } else { $null }
    $fail = (Get-FakeOption $State 'failStep')

    if ("$Method $path" -eq 'Post /api/issues') {
        if ((Get-FakeOption $State 'postFails')) { throw 'HTTP 500 perm-test on create' }
        $typeOf = @{ Type = 'SingleEnumIssueCustomField'; Priority = 'SingleEnumIssueCustomField'; Repository = 'SingleEnumIssueCustomField'; 'Estimated Time' = 'PeriodIssueCustomField' }
        $cf = @{}
        foreach ($field in @($b.customFields)) {
            if ($field.'$type' -ne $typeOf[$field.name]) { throw "fake: bad `$type for custom field $($field.name)" }
            $cf[$field.name] = $field
        }
        $issue = New-FakeIssue $b.summary $b.description ([bool](Get-FakeOption $State 'lag'))
        if (-not (Get-FakeOption $State 'ignoreFields')) {
            $issue.type = if ((Get-FakeOption $State 'wrongType')) { 'Bug' } else { $cf['Type'].value.name }
            $issue.priority = $cf['Priority'].value.name
            $issue.repository = if ((Get-FakeOption $State 'wrongRepository')) { 'essay-reviewer' } else { $cf['Repository'].value.name }
            $issue.estimate = if ((Get-FakeOption $State 'dropEstimate')) { $null } else { [int]$cf['Estimated Time'].value.minutes }
        }
        $State.issues['DEV-900'] = $issue
        if ((Get-FakeOption $State 'race')) { $State.issues[(Get-FakeOption $State 'race')] = New-FakeIssue $b.summary 'raced' $true }
        return ConvertTo-Response @{ idReadable = 'DEV-900'; id = '3-900' }
    }
    if ("$Method $path" -eq 'Post /api/commands') {
        if ($fail -eq 'parent') { throw 'HTTP 400 perm-test bad command' }
        if ($b.query -match '[{}]') { throw 'HTTP 400 braces are taken literally' }
        $issue = $State.issues[$b.issues[0].idReadable]
        if ($b.query -match '^subtask of (\S+)$') { $issue.parent = $Matches[1] }
        else { throw "HTTP 400 unexpected command: $($b.query)" }
        return ConvertTo-Response @{}
    }
    if ($path -eq '/api/tags' -and $Method -eq 'Get') {
        $name = if ($query -match '(?:^|\?|&)query=([^&]*)') { $Matches[1] } else { '' }
        $hits = @($State.tags | Where-Object { $_.name -like "*$name*" })
        return @($hits | ForEach-Object { ConvertTo-Response @{ id = $_.id; name = $_.name } })
    }
    if ($path -match '^/api/issues/([^/]+)/tags$' -and $Method -eq 'Post') {
        if ($fail -eq 'tags') { throw 'HTTP 500 perm-test on tags' }
        $issue = $State.issues[$Matches[1]]
        $tag = @($State.tags | Where-Object { $_.id -eq $b.id })[0]
        if (-not $tag) { throw 'fake: unknown tag id' }
        if (-not (Get-FakeOption $State 'dropTag')) { $issue.tags = @(@($issue.tags) + $tag.name) }
        return ConvertTo-Response @{ id = $tag.id; name = $tag.name }
    }
    if ("$Method $path" -eq 'Get /api/issues') {
        $text = if ($query -match '[?&]query=(.*)$') { $Matches[1] } else { '' }
        $top = if ($query -match '\$top=(\d+)') { [int]$Matches[1] } else { 100 }
        $skip = if ($query -match '\$skip=(\d+)') { [int]$Matches[1] } else { 0 }
        $visible = @($State.issues.Keys | Where-Object { -not $State.issues[$_].hidden } | Sort-Object { Get-FakeNumber $_ })
        if ($text -match 'sort by: created desc') { $picked = @($visible | Sort-Object { Get-FakeNumber $_ } -Descending) }
        elseif ($text -match '"(.*)"') {
            $phrase = $Matches[1]
            $unresolvedOnly = $text -match '#Unresolved'
            $picked = @($visible | Where-Object { (-not $unresolvedOnly -or -not $State.issues[$_].resolved) -and $State.issues[$_].summary -like "*$phrase*" })
        }
        else { $picked = $visible }
        return @($picked | Select-Object -Skip $skip -First $top | ForEach-Object { ConvertTo-Response (Get-FakeIssueView $_ $State.issues[$_]) })
    }
    if ($path -match '^/api/issues/([^/]+)/comments/(.+)$') {
        $text = if ((Get-FakeOption $State 'mangleComment')) { 'mangled' } else { $State.comments[$Matches[2]] }
        return ConvertTo-Response @{ id = $Matches[2]; text = $text }
    }
    if ($path -match '^/api/issues/([^/]+)/comments$') {
        $State.comments['c1'] = $b.text
        return ConvertTo-Response @{ id = 'c1' }
    }
    if ($path -match '^/api/issues/([^/]+)$') {
        $id = $Matches[1]
        $issue = $State.issues[$id]
        if (-not $issue) { throw "fake: 404 Not Found for $id" }
        if ($Method -eq 'Get') { return ConvertTo-Response (Get-FakeIssueView $id $issue) }
        if ($b.ContainsKey('summary')) { $issue.summary = $b.summary }
        # YouTrack may echo a trailing newline; the script must tolerate it.
        if ($b.ContainsKey('description')) { $issue.description = $b.description + "`n" }
        if ($b.ContainsKey('customFields')) {
            if ($fail -eq 'order') { throw 'HTTP 500 perm-test on order' }
            foreach ($field in $b.customFields) {
                if ($field.name -eq 'Order' -and -not (Get-FakeOption $State 'dropOrder')) { $issue.order = [int]$field.value }
            }
        }
        return ConvertTo-Response @{ idReadable = $id }
    }
    if ($path -eq '/api/admin/projects/0-1/customFields') {
        if ((Get-FakeOption $State 'bundlesFail')) { throw '403 Forbidden' }
        return @(ConvertTo-Response @{ field = @{ name = 'Type' }; bundle = @{ values = @(@('Bug', 'Task', 'Feature', 'Epic') | ForEach-Object { @{ name = $_ } }) } }
                 ConvertTo-Response @{ field = @{ name = 'Priority' }; bundle = @{ values = @(@('Critical', 'Major', 'Normal', 'Minor') | ForEach-Object { @{ name = $_ } }) } }
                 ConvertTo-Response @{ field = @{ name = 'Repository' }; bundle = @{ values = @(@('lamuflix', 'essay-reviewer') | ForEach-Object { @{ name = $_ } }) } }
                 ConvertTo-Response @{ field = @{ name = 'State' }; bundle = @{ values = @(@{ name = 'Todo' }) } })
    }
    if ($path -eq '/api/agiles/204-3' -and $Method -eq 'Get') { return ConvertTo-Response @{ currentSprint = $State.currentSprint } }
    if ($path -eq '/api/agiles/204-3/sprints' -and $Method -eq 'Get') { return @($State.sprints | ForEach-Object { ConvertTo-Response $_ }) }
    if ($path -match '^/api/agiles/204-3/sprints/([^/]+)/issues$') {
        $sprintId = $Matches[1]
        if ($Method -eq 'Post') {
            if ($fail -eq 'sprint') { throw 'HTTP 500 perm-test on sprint' }
            $owner = @($State.issues.Keys | Where-Object { "3-$(Get-FakeNumber $_)" -eq $b.id })[0]
            if (-not (Get-FakeOption $State 'dropSprint')) { $State.members[$sprintId] = @(@($State.members[$sprintId]) + $owner) }
            return ConvertTo-Response @{ id = $b.id }
        }
        return @(@($State.members[$sprintId]) | Where-Object { $_ } | ForEach-Object { ConvertTo-Response @{ idReadable = $_ } })
    }
    throw "fake: no route for $Method $path"
}
'@
$fakeFile = Join-Path $work 'fake.ps1'
Set-Content -LiteralPath $fakeFile -Value $fake

function New-Seed {
    param([string]$Summary, [string]$State = 'Todo', [string]$Type = 'Task', $Parent = $null, $Order = $null, [string]$Repository = 'lamuflix', [bool]$Resolved = $false)
    return @{ summary = $Summary; description = 'seed'; tags = @(); type = $Type; priority = 'Normal'; repository = $Repository; estimate = 480
              order = $Order; parent = $Parent; state = $State; resolved = $Resolved; hidden = $false }
}

# Orders 1..13. DEV-370 is a stray Done child of DEV-281 that sits after DEV-285's block.
function New-OrderedIssue {
    return [ordered]@{
        'DEV-281' = New-Seed 'Epic 1' -State Done -Type Epic -Parent 'DEV-93' -Order 1 -Resolved $true
        'DEV-359' = New-Seed 'Done under epic 1' -State Done -Parent 'DEV-281' -Order 2 -Resolved $true
        'DEV-361' = New-Seed 'Open under epic 1' -Parent 'DEV-281' -Order 3
        'DEV-282' = New-Seed 'Epic 2' -State Done -Type Epic -Parent 'DEV-93' -Order 4 -Resolved $true
        'DEV-295' = New-Seed 'Done under epic 2' -State Done -Parent 'DEV-282' -Order 5 -Resolved $true
        'DEV-283' = New-Seed 'Epic 3' -Type Epic -Parent 'DEV-93' -Order 6
        'DEV-390' = New-Seed 'Open under epic 3' -Parent 'DEV-283' -Order 7
        'DEV-284' = New-Seed 'Epic 4' -Type Epic -Parent 'DEV-93' -Order 8
        'DEV-307' = New-Seed 'Done one under epic 4' -State Done -Parent 'DEV-284' -Order 9 -Resolved $true
        'DEV-308' = New-Seed 'Done two under epic 4' -State Done -Parent 'DEV-284' -Order 10 -Resolved $true
        'DEV-310' = New-Seed 'Open under epic 4' -Parent 'DEV-284' -Order 11
        'DEV-285' = New-Seed 'Epic 5' -Type Epic -Parent 'DEV-93' -Order 12
        'DEV-370' = New-Seed 'Stray done under epic 1' -State Done -Parent 'DEV-281' -Order 13 -Resolved $true
    }
}

function New-State {
    param([object[]]$Search = @(), [switch]$DropTag, [switch]$MangleComment, [switch]$Throw, [hashtable]$Option = @{}, [switch]$BrokenOrder)
    $now = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $day = 86400000
    $options = @{ dropTag = [bool]$DropTag; mangleComment = [bool]$MangleComment; throw = [bool]$Throw }
    foreach ($key in $Option.Keys) { $options[$key] = $Option[$key] }
    $issues = [ordered]@{
        'DEV-93'  = @{ summary = 'Epic'; description = 'e'; tags = @(); type = 'Epic'; parent = $null; state = 'Open'; resolved = $false; hidden = $false; order = $null; repository = 'lamuflix'; priority = 'Normal'; estimate = $null }
        'DEV-290' = @{ summary = 'Old summary'; description = 'Old text'; tags = @('size:M'); type = 'Task'; parent = 'DEV-93'; state = 'Open'; resolved = $false; hidden = $false; order = $null; repository = 'lamuflix'; priority = 'Normal'; estimate = $null }
        'DEV-700' = @{ summary = 'Unplanned'; description = 'u'; tags = @(); type = 'Task'; parent = 'DEV-93'; state = 'Open'; resolved = $false; hidden = $false; order = $null; repository = 'lamuflix'; priority = 'Normal'; estimate = $null }
    }
    $ordered = New-OrderedIssue
    foreach ($key in $ordered.Keys) { $issues[$key] = $ordered[$key] }
    if ($BrokenOrder) { $issues['DEV-310'].order = 15 }
    foreach ($entry in $Search) {
        $seed = New-Seed $entry.summary
        if ($entry.ContainsKey('resolved')) { $seed.resolved = [bool]$entry.resolved }
        if ($entry.ContainsKey('state')) { $seed.state = [string]$entry.state }
        if ($entry.ContainsKey('hidden')) { $seed.hidden = [bool]$entry.hidden }
        $issues[$entry.idReadable] = $seed
    }
    return @{
        options       = $options
        comments      = @{}
        members       = @{}
        currentSprint = @{ id = '218-8'; name = 'Sprint 1' }
        sprints       = @(
            @{ id = '218-4'; name = 'First sprint'; start = $null; finish = $null; archived = $true }
            @{ id = '218-8'; name = 'Sprint 1'; start = $now - 3 * $day; finish = $now + 5 * $day; archived = $false }
            @{ id = '218-9'; name = 'Sprint 2'; start = $now + 6 * $day; finish = $now + 16 * $day; archived = $false }
        )
        tags          = @(
            @{ name = 'size:S'; id = '10-2' }
            @{ name = 'size:M'; id = '10-3' }
            @{ name = 'size:L'; id = '10-4' }
            @{ name = '{size:S}'; id = '10-6' }
            @{ name = '{size:M}'; id = '10-7' }
            @{ name = '{size:L}'; id = '10-9' }
        )
        issues        = $issues
    }
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
    param([string]$Arguments, [hashtable]$State = (New-State), [switch]$NoUrl)

    if ($Arguments -match '-Create' -and $Arguments -notmatch '-LockName') { $Arguments += " -LockName '$testLock'" }
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
# A parameter-validation failure never reaches the script's own exit codes (3 is the script's).
try { & '$script' $Arguments -EnvironmentReader `$envReader -RestMethodInvoker `$invoker -DpapiFileReader { `$null } }
catch { Write-Output "binding: `$(`$_.Exception.Message)"; exit 9 }
exit `$LASTEXITCODE
"@
    $output = @(& pwsh -NoProfile -NonInteractive -Command $driver 2>&1)
    $code = $LASTEXITCODE
    $calls = @(if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log })
    $after = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json -AsHashtable
    return [pscustomobject]@{ ExitCode = $code; Output = ($output -join "`n"); Calls = $calls; State = $after }
}

function Get-NonGetCall { param($Result) return , @($Result.Calls | Where-Object { $_ -notmatch '^Get ' }) }

function Get-CallBody {
    # The parsed JSON bodies of the calls whose "<Method> <uri>" matches $Pattern.
    param($Result, [string]$Pattern)
    foreach ($call in @($Result.Calls | Where-Object { $_ -match $Pattern })) {
        ($call -replace '^\S+ \S+ ', '' -replace ' auth=.*$', '') | ConvertFrom-Json -AsHashtable
    }
}

function Get-OrderOf {
    # Every ordered issue's Order from the state, as an ascending list.
    param($Result)
    @($Result.State.issues.Values | Where-Object { $null -ne $_.order } | ForEach-Object { [int]$_.order } | Sort-Object)
}

function Test-Contiguous {
    param([int[]]$Orders)
    for ($i = 0; $i -lt $Orders.Count; $i++) { if ($Orders[$i] -ne $i + 1) { return $false } }
    return $true
}

# A real HTTP server for the one case the fake cannot cover: Invoke-YouTrack's own
# Invoke-RestMethod call, with no -RestMethodInvoker. A one-element JSON array must
# reach the caller as an element, not as an Object[] wrapped in one.
function Invoke-EditOverHttp {
    param([string]$Arguments)
    $port = Get-Random -Minimum 20000 -Maximum 40000
    $listener = [Net.HttpListener]::new()
    $listener.Prefixes.Add("http://localhost:$port/")
    $listener.Start()
    $requests = [Collections.Concurrent.ConcurrentQueue[string]]::new()
    $server = [powershell]::Create()
    $null = $server.AddScript({
            param($Listener, $Requests)
            $issue = '{"id":"3-290","idReadable":"DEV-290","summary":"Old summary","description":"Old text","project":{"id":"0-1","shortName":"DEV"},"tags":[{"name":"size:S"}],"customFields":[{"name":"State","value":{"name":"Open"}}],"links":[]}'
            while ($Listener.IsListening) {
                try { $context = $Listener.GetContext() } catch { break }
                $request = $context.Request
                $Requests.Enqueue("$($request.HttpMethod) $($request.Url.AbsolutePath)")
                $json = switch ("$($request.HttpMethod) $($request.Url.AbsolutePath)") {
                    'GET /api/tags' { '[{"id":"10-2","name":"size:S"}]' }
                    'POST /api/issues/DEV-290/tags' { '{"id":"10-2","name":"size:S"}' }
                    'GET /api/issues/DEV-290' { $issue }
                    default { '{}' }
                }
                $bytes = [Text.Encoding]::UTF8.GetBytes($json)
                $context.Response.ContentType = 'application/json'
                $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
                $context.Response.Close()
            }
        }).AddArgument($listener).AddArgument($requests)
    $handle = $server.BeginInvoke()
    try {
        $driver = @"
`$envReader = { param(`$Name, `$Target)
    if (`$Target -ne 'Process') { return `$null }
    if (`$Name -eq 'YOUTRACK_URL') { return 'http://localhost:$port' }
    if (`$Name -eq 'YOUTRACK_TOKEN') { return 'perm-test' }
}
& '$script' $Arguments -EnvironmentReader `$envReader -DpapiFileReader { `$null }
exit `$LASTEXITCODE
"@
        $output = @(& pwsh -NoProfile -NonInteractive -Command $driver 2>&1)
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output -join "`n"); Requests = @($requests.ToArray()) }
    }
    finally {
        $listener.Stop()
        $listener.Close()
        $server.Dispose()
    }
}

try {
    Write-Host 'Edit-YouTrackIssue'
    $description = New-TextFile "### Overview`r`nFollow-up text.`r`n"
    $createArguments = "-Summary 'Close DEV-289 gaps' -DescriptionFile '$description' -Parent DEV-284 -Estimate 1d -Tag size:M"

    $show = Invoke-Edit "-Ticket DEV-290 -Show"
    Assert-True 'show: exit 0' ($show.ExitCode -eq 0)
    Assert-True 'show: prints summary, State, and tags' ($show.Output -match 'DEV-290: Old summary' -and $show.Output -match 'State: Open' -and $show.Output -match 'Tags: size:M')
    Assert-True 'show: reads only' ((Get-NonGetCall $show).Count -eq 0)

    # ---- -Create: the happy path ----
    $create = Invoke-Edit "-Create $createArguments"
    $issuePosts = @(Get-CallBody $create '^Post https://yt\.example/api/issues\?fields=idReadable,id ')
    $fieldBody = if ($issuePosts.Count -eq 1) { $issuePosts[0] } else { @{ customFields = @() } }
    $fieldByName = @{}
    foreach ($field in $fieldBody.customFields) { $fieldByName[$field.name] = $field }
    Assert-True 'create: exit 0' ($create.ExitCode -eq 0)
    Assert-True 'create: reports verified id' ($create.Output -match 'DEV-900 created \(verified\)')
    Assert-True 'create: one POST /api/issues with the parent''s project id' ($issuePosts.Count -eq 1 -and $issuePosts[0].project.id -ceq '0-1')
    Assert-True 'create: Type, Priority and Repository are enum custom fields in that POST' (
        $fieldByName['Type'].'$type' -ceq 'SingleEnumIssueCustomField' -and $fieldByName['Type'].value.name -ceq 'Task' -and
        $fieldByName['Priority'].value.name -ceq 'Normal' -and $fieldByName['Repository'].value.name -ceq 'lamuflix')
    Assert-True 'create: Estimated Time is a period custom field in minutes (1d = 480)' ($fieldByName['Estimated Time'].'$type' -ceq 'PeriodIssueCustomField' -and $fieldByName['Estimated Time'].value.minutes -eq 480)
    $commands = @(Get-CallBody $create '^Post https://yt\.example/api/commands ')
    Assert-True 'create: the only command sets the parent, and no command string has a brace' ($commands.Count -eq 1 -and $commands[0].query -ceq 'subtask of DEV-284' -and $commands[0].query -notmatch '[{}]')
    Assert-True 'create: attaches the existing tag by id' ((@($create.Calls | Where-Object { $_ -match 'Get https://yt\.example/api/tags\?fields=id,name&\$top=42&query=size%3AM ' }).Count -eq 1) -and (@($create.Calls | Where-Object { $_ -match 'Post https://yt\.example/api/issues/DEV-900/tags \{"id":"10-3"\}' }).Count -eq 1))
    Assert-True 'create: stores LF text without trailing whitespace' ($create.State.issues['DEV-900'].description -ceq "### Overview`nFollow-up text.")
    Assert-True 'create: sends the token as Bearer' ($create.Calls[0] -match 'auth=Bearer perm-test$')
    Assert-True 'create: the issue reads back with Type, Priority, Repository, estimate, parent, tag' (
        $create.State.issues['DEV-900'].type -ceq 'Task' -and $create.State.issues['DEV-900'].priority -ceq 'Normal' -and
        $create.State.issues['DEV-900'].repository -ceq 'lamuflix' -and $create.State.issues['DEV-900'].estimate -eq 480 -and
        $create.State.issues['DEV-900'].parent -ceq 'DEV-284' -and $create.State.issues['DEV-900'].tags -contains 'size:M')
    Assert-True 'create: prints Priority, Repository, estimate and Order on read-back' ($create.Output -match 'Priority: Normal\s+Repository: lamuflix\s+Estimate: 480m\s+Order: 11')

    # ---- Order: slot with Done children, the printed renumber list, contiguity ----
    Assert-True 'order: with Done children the slot is right after the last one (DEV-308 = 10, so 11)' ($create.State.issues['DEV-900'].order -eq 11 -and $create.Output -match 'order DEV-900: 11 \(directly after DEV-308')
    $expectedShifts = @('DEV-370: 13 -> 14', 'DEV-285: 12 -> 13', 'DEV-310: 11 -> 12')
    Assert-True 'order: every renumber is printed' (@($expectedShifts | Where-Object { $create.Output -match [regex]::Escape("renumber $_") }).Count -eq 3)
    $shiftIndexes = @($expectedShifts | ForEach-Object { $create.Output.IndexOf("renumber $_") })
    Assert-True 'order: renumbers are printed from the highest Order down' ($shiftIndexes[0] -lt $shiftIndexes[1] -and $shiftIndexes[1] -lt $shiftIndexes[2])
    $orderWrites = @($create.Calls | Where-Object { $_ -match '^Post https://yt\.example/api/issues/(DEV-\d+)\?fields=idReadable \{"customFields"' } |
            ForEach-Object { ($_ -split ' ')[1] -replace '^.*/issues/(DEV-\d+).*$', '$1' })
    Assert-True 'order: the POSTs go highest first, the new issue last' (($orderWrites -join ',') -ceq 'DEV-370,DEV-285,DEV-310,DEV-900')
    Assert-True 'order: Order is written as a SimpleIssueCustomField integer' (@(Get-CallBody $create 'Post https://yt\.example/api/issues/DEV-900\?fields=idReadable ' | Where-Object { $_.customFields[0].'$type' -ceq 'SimpleIssueCustomField' -and $_.customFields[0].value -eq 11 }).Count -eq 1)
    Assert-True 'order: afterwards 1..14 is contiguous, no gap, no duplicate' ((Test-Contiguous (Get-OrderOf $create)) -and (Get-OrderOf $create).Count -eq 14)
    Assert-True 'order: the shifted issues hold their new values' ($create.State.issues['DEV-310'].order -eq 12 -and $create.State.issues['DEV-285'].order -eq 13 -and $create.State.issues['DEV-370'].order -eq 14 -and $create.State.issues['DEV-308'].order -eq 10)

    $noDone = Invoke-Edit "-Create -Summary 'Under epic three' -DescriptionFile '$description' -Parent DEV-283 -Estimate 2h"
    Assert-True 'order: with no Done child the slot is right after the parent (6, so 7)' ($noDone.ExitCode -eq 0 -and $noDone.State.issues['DEV-900'].order -eq 7 -and $noDone.Output -match 'directly after DEV-283 \(Order 6\); it has no Done child')
    Assert-True 'order: no Done child shifts 7..13 up, contiguous 1..14' ($noDone.State.issues['DEV-390'].order -eq 8 -and $noDone.State.issues['DEV-370'].order -eq 14 -and (Test-Contiguous (Get-OrderOf $noDone)) -and @([regex]::Matches($noDone.Output, 'renumber DEV-')).Count -eq 7)

    $stray = Invoke-Edit "-Create -Summary 'Under epic one' -DescriptionFile '$description' -Parent DEV-281 -Estimate 2h"
    Assert-True 'order: a stray Done child beyond the next epic does not move the slot (3, not 14)' ($stray.ExitCode -eq 0 -and $stray.State.issues['DEV-900'].order -eq 3 -and (Test-Contiguous (Get-OrderOf $stray)))

    $unordered = Invoke-Edit ("-Create $createArguments").Replace('DEV-284', 'DEV-93')
    Assert-True 'order: a parent without Order puts the ticket last (14)' ($unordered.ExitCode -eq 0 -and $unordered.State.issues['DEV-900'].order -eq 14 -and @([regex]::Matches($unordered.Output, 'renumber DEV-')).Count -eq 0)

    $broken = Invoke-Edit "-Create $createArguments" -State (New-State -BrokenOrder)
    Assert-True 'order: refuses to write when the existing sequence has a gap (exit 2, no POST)' ($broken.ExitCode -eq 2 -and $broken.Output -match 'not 1\.\.N' -and (Get-NonGetCall $broken).Count -eq 0)

    $other = Invoke-Edit "-Create $createArguments -Repository essay-reviewer"
    Assert-True 'order: a non-lamuflix ticket gets no Order and no renumber' ($other.ExitCode -eq 0 -and $null -eq $other.State.issues['DEV-900'].order -and @($other.Calls | Where-Object { $_ -match 'Order' }).Count -eq 0 -and $other.Output -notmatch 'renumber|order DEV-900' -and $other.State.issues['DEV-370'].order -eq 13)
    Assert-True 'order: the other repository is set, read back, and still joins the sprint' ($other.State.issues['DEV-900'].repository -ceq 'essay-reviewer' -and $other.State.members['218-8'] -contains 'DEV-900')

    # ---- Sprint ----
    Assert-True 'sprint: added to the board''s current sprint with the internal id' (
        @(Get-CallBody $create '^Post https://yt\.example/api/agiles/204-3/sprints/218-8/issues' | Where-Object { $_.id -ceq '3-900' -and $_.'$type' -ceq 'Issue' }).Count -eq 1 -and
        @($create.Calls | Where-Object { $_ -match '^Get https://yt\.example/api/agiles/204-3\?fields=currentSprint\(id,name\) ' }).Count -eq 1)
    Assert-True 'sprint: membership is read back and printed' ($create.State.members['218-8'] -contains 'DEV-900' -and $create.Output -match 'Sprint: Sprint 1' -and @($create.Calls | Where-Object { $_ -match '^Get https://yt\.example/api/agiles/204-3/sprints/218-8/issues' }).Count -ge 1)

    $byDate = New-State
    $byDate.currentSprint = $null
    $byDateRun = Invoke-Edit "-Create $createArguments" -State $byDate
    Assert-True 'sprint: with no currentSprint it picks the sprint whose dates contain now' ($byDateRun.ExitCode -eq 0 -and $byDateRun.State.members['218-8'] -contains 'DEV-900' -and -not $byDateRun.State.members.ContainsKey('218-9'))

    $none = New-State
    $none.currentSprint = $null
    $none.sprints = @($none.sprints | Where-Object { $_.id -ne '218-8' })   # only an archived and a future sprint left
    $noSprint = Invoke-Edit "-Create $createArguments" -State $none
    Assert-True 'sprint: no current sprint warns and still creates (exit 0)' ($noSprint.ExitCode -eq 0 -and $noSprint.Output -match 'no current sprint' -and $noSprint.State.issues.ContainsKey('DEV-900'))
    Assert-True 'sprint: no current sprint posts no sprint write' (@($noSprint.Calls | Where-Object { $_ -match '^Post .*/sprints/' }).Count -eq 0)

    $droppedSprint = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ dropSprint = $true })
    Assert-True 'sprint: exit 3 when membership did not read back' ($droppedSprint.ExitCode -eq 3 -and $droppedSprint.Output -match 'CREATED DEV-900 but read-back verification failed' -and $droppedSprint.Output -match "not a member of sprint 'Sprint 1'")

    # ---- read-back verification: Type, Estimate, Repository (exit 3: the issue exists) ----
    foreach ($case in @(
            @{ Option = 'wrongType'; Expect = "Type is 'Bug', expected 'Task'" }
            @{ Option = 'dropEstimate'; Expect = "Estimated Time is '<empty>', expected '1d' \(480 min\)" }
            @{ Option = 'wrongRepository'; Expect = "Repository is 'essay-reviewer', expected 'lamuflix'" }
            @{ Option = 'dropOrder'; Expect = 'Order is' })) {
        $run = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ $case.Option = $true })
        Assert-True "verify: $($case.Option) is caught on read-back (exit 3 naming DEV-900)" ($run.ExitCode -eq 3 -and $run.Output -match 'CREATED DEV-900 but read-back verification failed' -and $run.Output -match $case.Expect)
    }
    $ignored = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ ignoreFields = $true })
    Assert-True 'verify: a server that ignores every custom field fails all three field checks plus the estimate' ($ignored.ExitCode -eq 3 -and $ignored.Output -match "Type is 'Bug'" -and $ignored.Output -match "Repository is ''" -and $ignored.Output -match 'Estimated Time is')
    $droppedTag = Invoke-Edit "-Create $createArguments" -State (New-State -DropTag)
    Assert-True 'verify: a tag that did not stick after create is exit 3, not 1' ($droppedTag.ExitCode -eq 3 -and $droppedTag.Output -match "tag 'size:M' is missing")

    $hours = Invoke-Edit "-Create -Summary 'Two parts' -DescriptionFile '$description' -Parent DEV-284 -Estimate '2h 30m'"
    Assert-True 'estimate: a compound period sums to minutes (2h 30m = 150)' ($hours.ExitCode -eq 0 -and $hours.State.issues['DEV-900'].estimate -eq 150)

    $badPriority = Invoke-Edit "-Create $createArguments -Priority Urgent"
    Assert-True 'priority: a value outside the project bundle is exit 2 before any write' ($badPriority.ExitCode -eq 2 -and $badPriority.Output -match "Priority 'Urgent' is not in" -and (Get-NonGetCall $badPriority).Count -eq 0)
    $badType = Invoke-Edit "-Create $createArguments -Type Epic"
    Assert-True 'type: Epic is not creatable (parameter rejected, nothing sent)' ($badType.ExitCode -eq 9 -and $badType.Calls.Count -eq 0)
    $major = Invoke-Edit "-Create $createArguments -Priority Major -Type Bug"
    Assert-True 'priority/type: explicit values are sent and read back' ($major.ExitCode -eq 0 -and $major.State.issues['DEV-900'].priority -ceq 'Major' -and $major.State.issues['DEV-900'].type -ceq 'Bug')

    # ---- exit 3: the issue exists and a later step failed ----
    foreach ($case in @(
            @{ Step = 'parent'; Name = 'set parent link'; Done = 'none' }
            @{ Step = 'tags'; Name = 'add tags'; Done = 'set parent link' }
            @{ Step = 'order'; Name = 'renumber and set Order'; Done = 'set parent link, add tags' }
            @{ Step = 'sprint'; Name = 'add to sprint'; Done = 'set parent link, add tags, renumber and set Order' })) {
        $failed = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ failStep = $case.Step })
        $label = "exit 3: a failing $($case.Step) step"
        Assert-True "$label names the id and the step, unmissably" ($failed.ExitCode -eq 3 -and $failed.Output -match "CREATED DEV-900 but $($case.Name) failed" -and $failed.Output -match '!{40}')
        Assert-True "$label says do NOT re-run -Create and to repair with -Ticket" ($failed.Output -match 'Do NOT re-run -Create' -and $failed.Output -match '-Ticket DEV-900')
        Assert-True "$label lists the steps done and not done" ($failed.Output -match 'Steps done: ' -and $failed.Output -match "Steps NOT done: .*$([regex]::Escape($case.Name))")
        Assert-True "$label made exactly one issue and never retried it" (@($failed.Calls | Where-Object { $_ -match '^Post https://yt\.example/api/issues\?' }).Count -eq 1)
        Assert-True "$label redacts the token" ($failed.Output -notmatch 'perm-test' -and $failed.Output -match '<redacted>')
    }
    $secondFailure = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ failStep = 'tags' })
    Assert-True 'exit 3: steps done before the failure are listed (parent link)' ($secondFailure.Output -match 'Steps done: set parent link')

    $postFailed = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ postFails = $true })
    Assert-True 'exit 2, not 3, when the create POST itself fails (nothing exists)' ($postFailed.ExitCode -eq 2 -and $postFailed.Output -notmatch 'CREATED' -and $postFailed.Output -match 'nothing is known to exist' -and $postFailed.Output -match '-DryRun')

    # ---- duplicate guard: any state, index independent ----
    $dupe = Invoke-Edit "-Create $createArguments" -State (New-State -Search @(@{ idReadable = 'DEV-500'; summary = 'close dev-289 GAPS ' }))
    Assert-True 'dupe: exit 1 on an open same-summary ticket, naming it' ($dupe.ExitCode -eq 1 -and $dupe.Output -match 'DEV-500 already has this summary' -and $dupe.Output -match 'do not retry -Create')
    Assert-True 'dupe: a duplicate makes no issue and writes nothing' ((Get-NonGetCall $dupe).Count -eq 0)

    foreach ($resolvedState in 'Done', 'Closed') {
        $resolved = Invoke-Edit "-Create $createArguments" -State (New-State -Search @(@{ idReadable = 'DEV-501'; summary = 'Close DEV-289 gaps'; resolved = $true; state = $resolvedState }))
        Assert-True "dupe: a resolved ($resolvedState) same-summary ticket blocks (exit 1)" ($resolved.ExitCode -eq 1 -and -not $resolved.State.issues.ContainsKey('DEV-900'))
        Assert-True "dupe: the $resolvedState block names the ticket and its State, says do not retry, and offers a different summary" ($resolved.Output -match "DEV-501 \($resolvedState\) already has this summary" -and $resolved.Output -match 'do not retry -Create' -and $resolved.Output -match 'different summary')
        Assert-True "dupe: the $resolvedState block makes zero non-GET calls" ((Get-NonGetCall $resolved).Count -eq 0)
    }
    $resolvedDry = Invoke-Edit "-Create -DryRun $createArguments" -State (New-State -Search @(@{ idReadable = 'DEV-501'; summary = 'Close DEV-289 gaps'; resolved = $true; state = 'Done' }))
    Assert-True 'dupe: -DryRun on a resolved same-summary ticket is exit 1 with zero non-GET calls' ($resolvedDry.ExitCode -eq 1 -and $resolvedDry.Output -match 'DEV-501 \(Done\) already has this summary' -and (Get-NonGetCall $resolvedDry).Count -eq 0)
    $resolvedLag = Invoke-Edit "-Create $createArguments" -State (New-State -Search @(@{ idReadable = 'DEV-703'; summary = 'Close DEV-289 gaps'; resolved = $true; state = 'Done'; hidden = $true }))
    Assert-True 'dupe: a resolved duplicate the search index has not listed is still found (direct id read)' ($resolvedLag.ExitCode -eq 1 -and $resolvedLag.Output -match 'DEV-703 \(Done\) already has this summary')

    $near = Invoke-Edit "-Create $createArguments" -State (New-State -Search @(@{ idReadable = 'DEV-502'; summary = 'Close DEV-289 gaps later' }))
    Assert-True 'dupe: a phrase hit with another summary is not a duplicate' ($near.ExitCode -eq 0)

    $lagging = Invoke-Edit "-Create $createArguments" -State (New-State -Search @(@{ idReadable = 'DEV-702'; summary = 'Close DEV-289 gaps'; hidden = $true }))
    Assert-True 'dupe: an open duplicate the search index has not listed yet is still found (direct id read)' ($lagging.ExitCode -eq 1 -and $lagging.Output -match 'DEV-702 already has this summary')
    Assert-True 'dupe: that check read the neighbouring ids directly' (@($lagging.Calls | Where-Object { $_ -match '^Get https://yt\.example/api/issues/DEV-70\d\?fields=idReadable,summary,resolved' }).Count -ge 1)

    $race = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ race = 'DEV-899' })
    Assert-True 'race: a lower-id duplicate that appears after the POST is exit 1 naming both ids' ($race.ExitCode -eq 1 -and $race.Output -match 'DUPLICATE: DEV-900 was created, but DEV-899 has the same summary' -and $race.Output -match 'do not retry -Create')
    Assert-True 'race: nothing is deleted or edited afterwards (the POST is the only write)' ((Get-NonGetCall $race).Count -eq 1 -and $race.State.issues.ContainsKey('DEV-899') -and $race.State.issues.ContainsKey('DEV-900'))
    Assert-True 'race: the loser leaves the Order sequence untouched (no second issue with an Order)' ($null -eq $race.State.issues['DEV-900'].order -and (Test-Contiguous (Get-OrderOf $race)) -and (Get-OrderOf $race).Count -eq 13)

    $lagged = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ lag = $true })
    Assert-True 'lag: the new issue the index does not list yet is still ordered and verified by id (exit 0, 1..14)' ($lagged.ExitCode -eq 0 -and (Test-Contiguous (Get-OrderOf $lagged)) -and (Get-OrderOf $lagged).Count -eq 14)

    # ---- -DryRun ----
    $dry = Invoke-Edit "-Create -DryRun $createArguments"
    Assert-True 'dryrun: exit 0' ($dry.ExitCode -eq 0 -and $dry.Output -match 'DRY RUN')
    Assert-True 'dryrun: zero non-GET calls through the seam' ((Get-NonGetCall $dry).Count -eq 0 -and $dry.Calls.Count -gt 0)
    Assert-True 'dryrun: creates nothing in the fake' (-not $dry.State.issues.ContainsKey('DEV-900'))
    Assert-True 'dryrun: prints the planned POST body, the parent command, the tag, the sprint and every renumber' (
        $dry.Output -match 'planned POST /api/issues' -and $dry.Output -match '"minutes":480' -and $dry.Output -match 'query: subtask of DEV-284' -and
        $dry.Output -match '"id":"10-3"' -and $dry.Output -match 'Sprint: Sprint 1' -and $dry.Output -match 'DEV-370: 13 -> 14' -and $dry.Output -match 'slot 11')
    $dryDupe = Invoke-Edit "-Create -DryRun $createArguments" -State (New-State -Search @(@{ idReadable = 'DEV-500'; summary = 'Close DEV-289 gaps' }))
    Assert-True 'dryrun: a duplicate is reported as exit 1 with zero writes' ($dryDupe.ExitCode -eq 1 -and $dryDupe.Output -match 'DEV-500 already has this summary' -and (Get-NonGetCall $dryDupe).Count -eq 0)
    $dryOther = Invoke-Edit "-Create -DryRun $createArguments -Repository essay-reviewer"
    Assert-True 'dryrun: a non-lamuflix ticket plans no Order' ($dryOther.ExitCode -eq 0 -and $dryOther.Output -match 'Order: none' -and (Get-NonGetCall $dryOther).Count -eq 0)

    # ---- the machine-wide lock ----
    $held = [Threading.Mutex]::new($true, $testLock + '-held')
    try {
        $locked = Invoke-Edit "-Create $createArguments -LockName '$($testLock + '-held')' -LockTimeoutSeconds 1"
        Assert-True 'lock: a held lock times out with exit 2 and writes nothing' ($locked.ExitCode -eq 2 -and $locked.Output -match 'another -Create on this machine holds' -and (Get-NonGetCall $locked).Count -eq 0)
        $dryLocked = Invoke-Edit "-Create -DryRun $createArguments -LockName '$($testLock + '-held')' -LockTimeoutSeconds 1"
        Assert-True 'lock: -DryRun does not need the lock' ($dryLocked.ExitCode -eq 0)
    }
    finally { $held.ReleaseMutex(); $held.Dispose() }

    # ---- tags and edits (unchanged behaviour) ----
    $comment = New-TextFile "Patron ruling: keep ADR 0001.`n"
    $commented = Invoke-Edit "-Ticket DEV-290 -CommentFile '$comment'"
    Assert-True 'comment: exit 0 and read back' ($commented.ExitCode -eq 0 -and $commented.Output -match 'updated \(verified\): comment')

    $mangled = Invoke-Edit "-Ticket DEV-290 -CommentFile '$comment'" -State (New-State -MangleComment)
    Assert-True 'comment: exit 1 when the stored text differs' ($mangled.ExitCode -eq 1 -and $mangled.Output -match 'comment did not read back')

    $dropped = Invoke-Edit "-Ticket DEV-290 -Tag size:S" -State (New-State -DropTag)
    Assert-True 'tag: exit 1 when the tag did not stick (edit keeps exit 1)' ($dropped.ExitCode -eq 1 -and $dropped.Output -match "tag 'size:S' is missing")

    $tagged = Invoke-Edit "-Ticket DEV-290 -Tag size:S"
    Assert-True 'tag: exit 0 and read back' ($tagged.ExitCode -eq 0 -and $tagged.Output -match 'updated \(verified\): tags')
    Assert-True 'tag: looks up the name and posts its id' ((@($tagged.Calls | Where-Object { $_ -match 'Get https://yt\.example/api/tags\?fields=id,name&\$top=42&query=size%3AS ' }).Count -eq 1) -and (@($tagged.Calls | Where-Object { $_ -match 'Post https://yt\.example/api/issues/DEV-290/tags \{"id":"10-2"\}' }).Count -eq 1))
    Assert-True 'tag: ignores the brace-named decoy and skips commands' (@($tagged.Calls | Where-Object { $_ -match '"id":"10-6"' -or $_ -match 'api/commands' }).Count -eq 0)
    Assert-True 'tag: stores size:S beside the existing tag' (($tagged.State.issues['DEV-290'].tags -contains 'size:S') -and ($tagged.State.issues['DEV-290'].tags -contains 'size:M'))

    $missingTag = Invoke-Edit "-Ticket DEV-290 -Tag nope"
    Assert-True 'tag: exit 2 when the name does not exist' ($missingTag.ExitCode -eq 2 -and $missingTag.Output -match "no existing tag named 'nope'")
    Assert-True 'tag: a missing name is not created' (@($missingTag.Calls | Where-Object { $_ -match 'Post .*/tags' }).Count -eq 0)

    $duplicate = New-State
    $duplicate.tags = @($duplicate.tags) + @{ name = 'size:M'; id = '10-8' }
    $ambiguous = Invoke-Edit "-Ticket DEV-290 -Tag size:M" -State $duplicate
    Assert-True 'tag: exit 2 when two tags share the name' ($ambiguous.ExitCode -eq 2 -and $ambiguous.Output -match "more than one tag is named 'size:M'")
    Assert-True 'tag: an ambiguous name is not attached' (@($ambiguous.Calls | Where-Object { $_ -match 'Post .*/api/issues/.*/tags' }).Count -eq 0)

    $newText = New-TextFile "Line one — ü`nLine two"
    $edited = Invoke-Edit "-Ticket DEV-290 -Summary 'New summary' -DescriptionFile '$newText'"
    Assert-True 'edit: a summary/description edit exits 0 and reads back' ($edited.ExitCode -eq 0 -and $edited.Output -match 'updated \(verified\): description, summary')
    Assert-True 'edit: YouTrack has the new summary and description' ($edited.State.issues['DEV-290'].summary -ceq 'New summary' -and $edited.State.issues['DEV-290'].description -match '^Line one .*\nLine two')
    Assert-True 'edit: prints no plan line' ($edited.Output -notmatch '(?m)^plan:')

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
    Assert-True 'rejects a malformed ticket id' ($badTicket.ExitCode -eq 9 -and $badTicket.Calls.Count -eq 0)

    $noParent = Invoke-Edit "-Create -Summary 'x' -DescriptionFile '$description' -Estimate 1d"
    Assert-True 'create: requires -Parent' ($noParent.ExitCode -eq 9 -and $noParent.Calls.Count -eq 0)

    # ---- the real Invoke-YouTrack (no seam): a one-element JSON array must unroll ----
    $overHttp = Invoke-EditOverHttp "-Ticket DEV-290 -Tag size:S"
    Assert-True 'http: a one-element JSON array from the real Invoke-RestMethod unrolls (tag lookup works, exit 0)' ($overHttp.ExitCode -eq 0 -and $overHttp.Output -match 'updated \(verified\): tags')
    Assert-True 'http: the tag was looked up and attached over the wire' (($overHttp.Requests -contains 'GET /api/tags') -and ($overHttp.Requests -contains 'POST /api/issues/DEV-290/tags'))
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

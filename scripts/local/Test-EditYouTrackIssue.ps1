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
# dropParent, dropType, failStep = parent | tags | order | sprint, failOrderAfter = N (the
# (N+1)th Order POST fails, so a renumber stops half way).
# The board is field-linked (sprint ids 218-10 and 218-11); the sprint POST still adds a member.
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
        if ($b.query -match '^subtask of (\S+)$') { if (-not (Get-FakeOption $State 'dropParent')) { $issue.parent = $Matches[1] } }
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
            foreach ($field in $b.customFields) {
                if ($field.name -eq 'Order') {
                    if ($fail -eq 'order') { throw 'HTTP 500 perm-test on order' }
                    $limit = Get-FakeOption $State 'failOrderAfter'
                    if ($null -ne $limit) {
                        $sent = if ($State.ContainsKey('orderPosts')) { [int]$State.orderPosts } else { 0 }
                        if ($sent -ge [int]$limit) { throw 'HTTP 500 perm-test on a renumber write' }
                        $State.orderPosts = $sent + 1
                    }
                    if (-not (Get-FakeOption $State 'dropOrder')) { $issue.order = [int]$field.value }
                }
                elseif ($field.name -eq 'Estimated Time') {
                    if ($field.'$type' -ne 'PeriodIssueCustomField') { throw 'fake: bad $type for Estimated Time' }
                    if (-not (Get-FakeOption $State 'dropEstimate')) { $issue.estimate = [int]$field.value.minutes }
                }
                elseif ($field.name -eq 'Type') {
                    if ($field.'$type' -ne 'SingleEnumIssueCustomField') { throw 'fake: bad $type for Type' }
                    if (-not (Get-FakeOption $State 'dropType')) { $issue.type = $field.value.name }
                }
                else { throw "fake: cannot set custom field $($field.name)" }
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
        currentSprint = @{ id = '218-10'; name = 'Sprint 1' }
        sprints       = @(
            @{ id = '218-4'; name = 'First sprint'; start = $null; finish = $null; archived = $true }
            @{ id = '218-10'; name = 'Sprint 1'; start = $now - 3 * $day; finish = $now + 5 * $day; archived = $false }
            @{ id = '218-11'; name = 'Sprint 2'; start = $now + 6 * $day; finish = $now + 16 * $day; archived = $false }
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

    if ($Arguments -match '-(Create|Order)\b' -and $Arguments -notmatch '-LockName') { $Arguments += " -LockName '$testLock'" }
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
    Assert-True 'order: the other repository is set, read back, and still joins the sprint' ($other.State.issues['DEV-900'].repository -ceq 'essay-reviewer' -and $other.State.members['218-10'] -contains 'DEV-900')

    # ---- Sprint ----
    Assert-True 'sprint: added to the board''s current sprint with the internal id' (
        @(Get-CallBody $create '^Post https://yt\.example/api/agiles/204-3/sprints/218-10/issues' | Where-Object { $_.id -ceq '3-900' -and $_.'$type' -ceq 'Issue' }).Count -eq 1 -and
        @($create.Calls | Where-Object { $_ -match '^Get https://yt\.example/api/agiles/204-3\?fields=currentSprint\(id,name\) ' }).Count -eq 1)
    Assert-True 'sprint: membership is read back and printed' ($create.State.members['218-10'] -contains 'DEV-900' -and $create.Output -match 'Sprint: Sprint 1' -and @($create.Calls | Where-Object { $_ -match '^Get https://yt\.example/api/agiles/204-3/sprints/218-10/issues' }).Count -ge 1)

    $byDate = New-State
    $byDate.currentSprint = $null
    $byDateRun = Invoke-Edit "-Create $createArguments" -State $byDate
    Assert-True 'sprint: with no currentSprint it picks the sprint whose dates contain now' ($byDateRun.ExitCode -eq 0 -and $byDateRun.State.members['218-10'] -contains 'DEV-900' -and -not $byDateRun.State.members.ContainsKey('218-11'))

    $none = New-State
    $none.currentSprint = $null
    $none.sprints = @($none.sprints | Where-Object { $_.id -ne '218-10' })   # only an archived and a future sprint left
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
        Assert-True 'lock: a held lock times out with exit 2 and writes nothing' ($locked.ExitCode -eq 2 -and $locked.Output -match 'another -Create or -Ticket -Order on this machine holds' -and $locked.Output -match 'Nothing was written' -and (Get-NonGetCall $locked).Count -eq 0)
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

    # ---- -Ticket repair parameters (DEV-405) ----
    # DEV-290 is the repair target: Task, lamuflix, Parent DEV-93, no Order, no estimate, no sprint.
    function New-RepairState {
        param([hashtable]$Option = @{}, $Parent = 'DEV-93', $Estimate = $null, [string]$Repository = 'lamuflix', [string]$Type = 'Task', [switch]$BrokenOrder)
        $state = New-State -Option $Option -BrokenOrder:$BrokenOrder
        $state.issues['DEV-290'].parent = $Parent
        $state.issues['DEV-290'].estimate = $Estimate
        $state.issues['DEV-290'].repository = $Repository
        $state.issues['DEV-290'].type = $Type
        return $state
    }
    $edit = 'F:\Dev\LamuFlix\scripts\local\Edit-YouTrackIssue.ps1'
    $unchanged = {
        param($Run, $Before)
        (ConvertTo-Json -InputObject $Run.State.issues -Depth 10 -Compress) -ceq (ConvertTo-Json -InputObject $Before.issues -Depth 10 -Compress)
    }

    # -Parent
    $parentless = New-RepairState -Parent $null
    $addParent = Invoke-Edit "-Ticket DEV-290 -Parent DEV-284" -State $parentless
    $parentCommands = @(Get-CallBody $addParent '^Post https://yt\.example/api/commands ')
    Assert-True 'parent: exit 0, read back, and named in the summary' ($addParent.ExitCode -eq 0 -and $addParent.Output -match 'updated \(verified\): parent' -and $addParent.State.issues['DEV-290'].parent -ceq 'DEV-284')
    Assert-True 'parent: one brace-free "subtask of" command on the ticket, and no other write' (
        $parentCommands.Count -eq 1 -and $parentCommands[0].query -ceq 'subtask of DEV-284' -and $parentCommands[0].issues[0].idReadable -ceq 'DEV-290' -and
        (Get-NonGetCall $addParent).Count -eq 1)
    $sameParent = Invoke-Edit "-Ticket DEV-290 -Parent DEV-93" -State (New-RepairState)
    Assert-True 'parent: the parent it already has is a no-op (exit 0, zero writes)' ($sameParent.ExitCode -eq 0 -and (Get-NonGetCall $sameParent).Count -eq 0 -and $sameParent.Output -match 'nothing written' -and $sameParent.Output -match 'already as requested')
    $otherParentState = New-RepairState
    $otherParent = Invoke-Edit "-Ticket DEV-290 -Parent DEV-284" -State $otherParentState
    Assert-True 'parent: a different parent is refused (exit 2), names the current one, writes nothing' ($otherParent.ExitCode -eq 2 -and $otherParent.Output -match 'already a subtask of DEV-93' -and $otherParent.Output -match 'never removes a link' -and (Get-NonGetCall $otherParent).Count -eq 0 -and $otherParent.State.issues['DEV-290'].parent -ceq 'DEV-93')
    $selfParent = Invoke-Edit "-Ticket DEV-290 -Parent DEV-290" -State (New-RepairState -Parent $null)
    Assert-True 'parent: a ticket cannot be its own parent (exit 2, zero writes)' ($selfParent.ExitCode -eq 2 -and $selfParent.Output -match 'its own parent' -and (Get-NonGetCall $selfParent).Count -eq 0)
    $ghostParent = Invoke-Edit "-Ticket DEV-290 -Parent DEV-999" -State (New-RepairState -Parent $null)
    Assert-True 'parent: a parent that does not exist is exit 2 before any write' ($ghostParent.ExitCode -eq 2 -and (Get-NonGetCall $ghostParent).Count -eq 0)
    $droppedParent = Invoke-Edit "-Ticket DEV-290 -Parent DEV-284" -State (New-RepairState -Parent $null -Option @{ dropParent = $true })
    Assert-True 'parent: exit 1 when the link did not stick' ($droppedParent.ExitCode -eq 1 -and $droppedParent.Output -match 'not a subtask of DEV-284')
    Assert-True 'parent: a bad id is rejected by the parameter, nothing sent' ((Invoke-Edit "-Ticket DEV-290 -Parent 'dev 284'").ExitCode -eq 9)

    # -Estimate
    $estimateState = New-RepairState
    $setEstimate = Invoke-Edit "-Ticket DEV-290 -Estimate '2h 30m'" -State $estimateState
    $estimateBodies = @(Get-CallBody $setEstimate '^Post https://yt\.example/api/issues/DEV-290\?fields=idReadable ')
    Assert-True 'estimate: exit 0 and read back as minutes (2h 30m = 150)' ($setEstimate.ExitCode -eq 0 -and $setEstimate.Output -match 'updated \(verified\): estimate' -and $setEstimate.State.issues['DEV-290'].estimate -eq 150)
    Assert-True 'estimate: one period custom field POST in minutes, nothing else written' ($estimateBodies.Count -eq 1 -and $estimateBodies[0].customFields[0].name -ceq 'Estimated Time' -and $estimateBodies[0].customFields[0].'$type' -ceq 'PeriodIssueCustomField' -and $estimateBodies[0].customFields[0].value.minutes -eq 150 -and (Get-NonGetCall $setEstimate).Count -eq 1)
    $sameEstimate = Invoke-Edit "-Ticket DEV-290 -Estimate 4h" -State (New-RepairState -Estimate 240)
    Assert-True 'estimate: the value it already has is a no-op (exit 0, zero writes)' ($sameEstimate.ExitCode -eq 0 -and (Get-NonGetCall $sameEstimate).Count -eq 0 -and $sameEstimate.Output -match 'already as requested')
    $droppedEstimate = Invoke-Edit "-Ticket DEV-290 -Estimate 1d" -State (New-RepairState -Option @{ dropEstimate = $true })
    Assert-True 'estimate: exit 1 when it did not stick' ($droppedEstimate.ExitCode -eq 1 -and $droppedEstimate.Output -match "Estimated Time is '<empty>', expected '1d' \(480 min\)")
    Assert-True 'estimate: a malformed period is rejected by the parameter, nothing sent' ((Invoke-Edit "-Ticket DEV-290 -Estimate 4x").ExitCode -eq 9)

    # -Type
    $setType = Invoke-Edit "-Ticket DEV-290 -Type Bug" -State (New-RepairState)
    $typeBodies = @(Get-CallBody $setType '^Post https://yt\.example/api/issues/DEV-290\?fields=idReadable ')
    Assert-True 'type: exit 0 and read back' ($setType.ExitCode -eq 0 -and $setType.Output -match 'updated \(verified\): type' -and $setType.State.issues['DEV-290'].type -ceq 'Bug')
    Assert-True 'type: one enum custom field POST by name, nothing else written' ($typeBodies.Count -eq 1 -and $typeBodies[0].customFields[0].name -ceq 'Type' -and $typeBodies[0].customFields[0].'$type' -ceq 'SingleEnumIssueCustomField' -and $typeBodies[0].customFields[0].value.name -ceq 'Bug' -and (Get-NonGetCall $setType).Count -eq 1)
    $sameType = Invoke-Edit "-Ticket DEV-290 -Type Task" -State (New-RepairState)
    Assert-True 'type: the Type it already has is a no-op (exit 0, zero writes)' ($sameType.ExitCode -eq 0 -and (Get-NonGetCall $sameType).Count -eq 0)
    $droppedType = Invoke-Edit "-Ticket DEV-290 -Type Bug" -State (New-RepairState -Option @{ dropType = $true })
    Assert-True 'type: exit 1 when it did not stick' ($droppedType.ExitCode -eq 1 -and $droppedType.Output -match "Type is 'Task', expected 'Bug'")
    Assert-True 'type: Epic is not accepted (same ValidateSet as -Create)' ((Invoke-Edit "-Ticket DEV-290 -Type Epic").ExitCode -eq 9)
    $epicState = New-State
    $epic = Invoke-Edit "-Ticket DEV-283 -Type Task" -State $epicState
    Assert-True 'type: an Epic is not retyped (exit 2, zero writes)' ($epic.ExitCode -eq 2 -and $epic.Output -match 'is an Epic' -and (Get-NonGetCall $epic).Count -eq 0)
    $noType = Invoke-Edit "-Ticket DEV-290 -Estimate 1d" -State (New-RepairState -Type Bug)
    Assert-True 'type: without -Type the edit never touches Type (no default Task leaks in)' ($noType.ExitCode -eq 0 -and $noType.State.issues['DEV-290'].type -ceq 'Bug' -and @($noType.Calls | Where-Object { $_ -match '"name":"Type"' }).Count -eq 0)

    # -Order
    $orderState = New-RepairState -Parent 'DEV-284'
    $orderRun = Invoke-Edit "-Ticket DEV-290 -Order" -State $orderState
    Assert-True 'order: exit 0, verified, slot right after the parent''s last Done child (DEV-308 = 10, so 11)' ($orderRun.ExitCode -eq 0 -and $orderRun.Output -match 'updated \(verified\): order' -and $orderRun.State.issues['DEV-290'].order -eq 11 -and $orderRun.Output -match 'order DEV-290: 11 \(directly after DEV-308')
    Assert-True 'order: every renumber is printed, highest first' (@($expectedShifts | Where-Object { $orderRun.Output -match [regex]::Escape("renumber $_") }).Count -eq 3 -and $orderRun.Output.IndexOf('renumber DEV-370') -lt $orderRun.Output.IndexOf('renumber DEV-285') -and $orderRun.Output.IndexOf('renumber DEV-285') -lt $orderRun.Output.IndexOf('renumber DEV-310'))
    $editOrderWrites = @($orderRun.Calls | Where-Object { $_ -match '^Post https://yt\.example/api/issues/(DEV-\d+)\?fields=idReadable \{"customFields"' } | ForEach-Object { ($_ -split ' ')[1] -replace '^.*/issues/(DEV-\d+).*$', '$1' })
    Assert-True 'order: the POSTs go highest first and the ticket last, and nothing else is written' (($editOrderWrites -join ',') -ceq 'DEV-370,DEV-285,DEV-310,DEV-290' -and (Get-NonGetCall $orderRun).Count -eq 4)
    Assert-True 'order: afterwards 1..14 is contiguous and the shifted tickets hold their values' ((Test-Contiguous (Get-OrderOf $orderRun)) -and (Get-OrderOf $orderRun).Count -eq 14 -and $orderRun.State.issues['DEV-310'].order -eq 12 -and $orderRun.State.issues['DEV-370'].order -eq 14)
    $withParent = Invoke-Edit "-Ticket DEV-290 -Parent DEV-284 -Order" -State (New-RepairState -Parent $null)
    Assert-True 'order: -Parent and -Order in one call: the parent the call adds is used for the slot' ($withParent.ExitCode -eq 0 -and $withParent.State.issues['DEV-290'].parent -ceq 'DEV-284' -and $withParent.State.issues['DEV-290'].order -eq 11 -and $withParent.Output -match 'updated \(verified\): order, parent|updated \(verified\): parent, order')
    $lastSlot = Invoke-Edit "-Ticket DEV-290 -Order" -State (New-RepairState)
    Assert-True 'order: a parent without an Order puts the ticket last (14), no renumber' ($lastSlot.ExitCode -eq 0 -and $lastSlot.State.issues['DEV-290'].order -eq 14 -and @([regex]::Matches($lastSlot.Output, 'renumber DEV-')).Count -eq 0)
    $hasOrder = Invoke-Edit "-Ticket DEV-390 -Order" -State (New-State)
    Assert-True 'order: refused (exit 2, zero writes) when the ticket already has an Order' ($hasOrder.ExitCode -eq 2 -and $hasOrder.Output -match 'already has Order 7' -and (Get-NonGetCall $hasOrder).Count -eq 0 -and $hasOrder.State.issues['DEV-390'].order -eq 7)
    $otherRepo = Invoke-Edit "-Ticket DEV-290 -Order" -State (New-RepairState -Parent 'DEV-284' -Repository 'essay-reviewer')
    Assert-True 'order: refused (exit 2, zero writes) when the Repository is not lamuflix' ($otherRepo.ExitCode -eq 2 -and $otherRepo.Output -match "Repository 'essay-reviewer'" -and (Get-NonGetCall $otherRepo).Count -eq 0 -and $null -eq $otherRepo.State.issues['DEV-290'].order)
    $orphan = Invoke-Edit "-Ticket DEV-290 -Order" -State (New-RepairState -Parent $null)
    Assert-True 'order: refused (exit 2, zero writes) when the ticket has no parent, pointing at -Parent' ($orphan.ExitCode -eq 2 -and $orphan.Output -match 'has no parent' -and $orphan.Output -match '-Parent' -and (Get-NonGetCall $orphan).Count -eq 0)
    $gap = Invoke-Edit "-Ticket DEV-290 -Order" -State (New-RepairState -Parent 'DEV-284' -BrokenOrder)
    Assert-True 'order: refused (exit 2, zero writes) when the existing sequence is not 1..N' ($gap.ExitCode -eq 2 -and $gap.Output -match 'not 1\.\.N' -and (Get-NonGetCall $gap).Count -eq 0)
    $refusedWithOthers = Invoke-Edit "-Ticket DEV-390 -Order -Estimate 4h -Type Bug -Tag size:S -Sprint" -State (New-State)
    Assert-True 'order: a refusal stops the whole call before any write, even for the other parameters' ($refusedWithOthers.ExitCode -eq 2 -and (Get-NonGetCall $refusedWithOthers).Count -eq 0)
    $droppedOrder = Invoke-Edit "-Ticket DEV-290 -Order" -State (New-RepairState -Parent 'DEV-284' -Option @{ dropOrder = $true })
    Assert-True 'order: exit 1 when the Order did not stick' ($droppedOrder.ExitCode -eq 1 -and $droppedOrder.Output -match 'Order is')

    # -Sprint
    $sprintRun = Invoke-Edit "-Ticket DEV-290 -Sprint" -State (New-RepairState)
    Assert-True 'sprint: exit 0, verified, the board''s current sprint is read live and the ticket is a member' (
        $sprintRun.ExitCode -eq 0 -and $sprintRun.Output -match 'updated \(verified\): sprint' -and $sprintRun.Output -match 'Sprint: Sprint 1' -and $sprintRun.State.members['218-10'] -contains 'DEV-290' -and
        @($sprintRun.Calls | Where-Object { $_ -match '^Get https://yt\.example/api/agiles/204-3\?fields=currentSprint\(id,name\) ' }).Count -eq 1)
    Assert-True 'sprint: one POST with the internal id, and nothing else is written' (
        @(Get-CallBody $sprintRun '^Post https://yt\.example/api/agiles/204-3/sprints/218-10/issues' | Where-Object { $_.id -ceq '3-290' -and $_.'$type' -ceq 'Issue' }).Count -eq 1 -and (Get-NonGetCall $sprintRun).Count -eq 1)
    $memberState = New-RepairState
    $memberState.members['218-10'] = @('DEV-290')
    $already = Invoke-Edit "-Ticket DEV-290 -Sprint" -State $memberState
    Assert-True 'sprint: already a member is idempotent (exit 0, zero writes)' ($already.ExitCode -eq 0 -and (Get-NonGetCall $already).Count -eq 0 -and $already.Output -match 'already as requested' -and $already.State.members['218-10'].Count -eq 1)
    $noCurrent = New-RepairState
    $noCurrent.currentSprint = $null
    $noCurrent.sprints = @($noCurrent.sprints | Where-Object { $_.id -ne '218-10' })
    $noSprint = Invoke-Edit "-Ticket DEV-290 -Sprint -Estimate 4h" -State $noCurrent
    Assert-True 'sprint: no current sprint is refused (exit 2, zero writes, even with other parameters)' ($noSprint.ExitCode -eq 2 -and $noSprint.Output -match 'no current sprint' -and (Get-NonGetCall $noSprint).Count -eq 0)
    $droppedMember = Invoke-Edit "-Ticket DEV-290 -Sprint" -State (New-RepairState -Option @{ dropSprint = $true })
    Assert-True 'sprint: exit 1 when membership did not read back' ($droppedMember.ExitCode -eq 1 -and $droppedMember.Output -match "not a member of sprint 'Sprint 1'")

    # everything in one call, plus a text edit and a tag
    $everything = Invoke-Edit "-Ticket DEV-290 -Parent DEV-284 -Estimate 4h -Type Bug -Order -Sprint -Tag size:S" -State (New-RepairState -Parent $null)
    Assert-True 'all: every repair parameter in one call lands and reads back (exit 0)' (
        $everything.ExitCode -eq 0 -and $everything.State.issues['DEV-290'].parent -ceq 'DEV-284' -and $everything.State.issues['DEV-290'].estimate -eq 240 -and $everything.State.issues['DEV-290'].type -ceq 'Bug' -and
        $everything.State.issues['DEV-290'].order -eq 11 -and $everything.State.members['218-10'] -contains 'DEV-290' -and $everything.State.issues['DEV-290'].tags -contains 'size:S' -and (Test-Contiguous (Get-OrderOf $everything)))

    # ---- -Ticket -Order takes -Create's lock; a failure after the first write is exit 3 ----
    $heldOrder = [Threading.Mutex]::new($true, $testLock + '-order')
    try {
        $orderLock = "-LockName '$($testLock + '-order')' -LockTimeoutSeconds 1"
        $lockedOrder = Invoke-Edit "-Ticket DEV-290 -Order $orderLock" -State (New-RepairState -Parent 'DEV-284')
        Assert-True 'order lock: a held lock times out with exit 2 and zero non-GET calls' (
            $lockedOrder.ExitCode -eq 2 -and $lockedOrder.Output -match 'another -Create or -Ticket -Order on this machine holds' -and $lockedOrder.Output -match 'Nothing was written' -and (Get-NonGetCall $lockedOrder).Count -eq 0)
        Assert-True 'order lock: it is taken before the Order reads (the project list is never fetched)' (@($lockedOrder.Calls | Where-Object { $_ -match '/api/issues\?' }).Count -eq 0)
        $dryOrderHeld = Invoke-Edit "-Ticket DEV-290 -DryRun -Order $orderLock" -State (New-RepairState -Parent 'DEV-284')
        Assert-True 'order lock: -DryRun -Order never takes the lock (exit 0 while it is held)' ($dryOrderHeld.ExitCode -eq 0 -and $dryOrderHeld.Output -match 'DRY RUN' -and (Get-NonGetCall $dryOrderHeld).Count -eq 0)
        $noOrderHeld = Invoke-Edit "-Ticket DEV-290 -Estimate 4h $orderLock" -State (New-RepairState)
        Assert-True 'order lock: a repair without -Order does not need the lock (exit 0 while it is held)' ($noOrderHeld.ExitCode -eq 0 -and $noOrderHeld.State.issues['DEV-290'].estimate -eq 240)
        $refusedHeld = Invoke-Edit "-Ticket DEV-390 -Order $orderLock" -State (New-State)
        Assert-True 'order lock: a refusal that needs the Order reads waits for the lock too (exit 2, zero writes)' ($refusedHeld.ExitCode -eq 2 -and $refusedHeld.Output -match 'another -Create or -Ticket -Order' -and (Get-NonGetCall $refusedHeld).Count -eq 0)
    }
    finally { $heldOrder.ReleaseMutex(); $heldOrder.Dispose() }
    $freeOrder = Invoke-Edit "-Ticket DEV-290 -Order -LockName '$($testLock + '-order')' -LockTimeoutSeconds 1" -State (New-RepairState -Parent 'DEV-284')
    Assert-True 'order lock: released afterwards (the same name is takeable again)' ($freeOrder.ExitCode -eq 0 -and $freeOrder.State.issues['DEV-290'].order -eq 11)

    # Exit 3 after a partial write: a refusal or a failed FIRST write stays exit 2.
    $half = Invoke-Edit "-Ticket DEV-290 -Order" -State (New-RepairState -Parent 'DEV-284' -Option @{ failOrderAfter = 2 })
    Assert-True 'exit 3: a renumber that stops partway names the renumber as half done' (
        $half.ExitCode -eq 3 -and $half.Output -match 'PARTLY UPDATED' -and $half.Output -match 'order failed' -and $half.Output -match 'HALF DONE' -and $half.Output -match 'PARTLY: 2 Order write' -and $half.Output -match 'report to Patron' -and $half.Output -match 'do not re-run -Order')
    Assert-True 'exit 3: the half-done state is what the two completed writes left (DEV-370 and DEV-285 moved, DEV-310 and the ticket not)' (
        $half.State.issues['DEV-370'].order -eq 14 -and $half.State.issues['DEV-285'].order -eq 13 -and $half.State.issues['DEV-310'].order -eq 11 -and $null -eq $half.State.issues['DEV-290'].order)
    $firstFails = Invoke-Edit "-Ticket DEV-290 -Order" -State (New-RepairState -Parent 'DEV-284' -Option @{ failOrderAfter = 0 })
    Assert-True 'exit 2: the first write failing is still "nothing was written" (no PARTLY UPDATED)' ($firstFails.ExitCode -eq 2 -and $firstFails.Output -notmatch 'PARTLY UPDATED' -and $firstFails.State.issues['DEV-370'].order -eq 13)
    $parentThenSprint = Invoke-Edit "-Ticket DEV-290 -Parent DEV-284 -Sprint" -State (New-RepairState -Parent $null -Option @{ failStep = 'sprint' })
    Assert-True 'exit 3: a sprint failure after -Parent succeeded lists parent as written and sprint as failed' (
        $parentThenSprint.ExitCode -eq 3 -and $parentThenSprint.Output -match 'sprint failed' -and $parentThenSprint.Output -match 'Written: parent' -and $parentThenSprint.Output -match 'Failed: sprint' -and
        $parentThenSprint.State.issues['DEV-290'].parent -ceq 'DEV-284' -and $parentThenSprint.Output -match '-Show')
    $textThenTag = Invoke-Edit "-Ticket DEV-290 -Summary 'Renamed' -Tag size:S" -State (New-State -Option @{ failStep = 'tags' })
    Assert-True 'exit 3: a tag failure after the text POST lists the text as written and the tag as failed' (
        $textThenTag.ExitCode -eq 3 -and $textThenTag.Output -match 'Written: summary/description' -and $textThenTag.Output -match 'Failed: tags' -and $textThenTag.Output -match 'Not attempted: read-back verification' -and
        $textThenTag.Output -notmatch 'youtrack-plan' -and $textThenTag.State.issues['DEV-290'].summary -ceq 'Renamed')
    $firstTagFails = Invoke-Edit "-Ticket DEV-290 -Tag size:S" -State (New-State -Option @{ failStep = 'tags' })
    Assert-True 'exit 2: a first write that fails (a lone tag) stays "nothing was written"' ($firstTagFails.ExitCode -eq 2 -and $firstTagFails.Output -notmatch 'PARTLY UPDATED')
    $refusedLate = Invoke-Edit "-Ticket DEV-390 -Parent DEV-284 -Order" -State (New-State)
    Assert-True 'exit 2: a refusal decided from reads stays exit 2 with zero writes' ($refusedLate.ExitCode -eq 2 -and $refusedLate.Output -notmatch 'PARTLY UPDATED' -and (Get-NonGetCall $refusedLate).Count -eq 0)

    # ---- -Ticket -DryRun: every planned write, zero non-GET ----
    $repairState = New-RepairState -Parent $null
    $editDry = Invoke-Edit "-Ticket DEV-290 -DryRun -Parent DEV-284 -Estimate 4h -Type Bug -Order -Sprint -Tag size:S -Summary 'Planned only'" -State $repairState
    Assert-True 'edit dryrun: exit 0 with zero non-GET calls through the seam' ($editDry.ExitCode -eq 0 -and $editDry.Output -match 'DRY RUN' -and (Get-NonGetCall $editDry).Count -eq 0 -and $editDry.Calls.Count -gt 0)
    Assert-True 'edit dryrun: changes nothing in the fake' (& $unchanged $editDry $repairState)
    Assert-True 'edit dryrun: prints the text POST, tag, parent command, estimate, Type and sprint' (
        $editDry.Output -match '"summary":"Planned only"' -and $editDry.Output -match '"id":"10-2"' -and $editDry.Output -match 'query: subtask of DEV-284' -and $editDry.Output -match '"minutes":240' -and
        $editDry.Output -match '"name":"Bug"' -and $editDry.Output -match 'Sprint: Sprint 1' -and $editDry.Output -match 'sprints/218-10/issues' -and $editDry.Output -match '"id":"3-290"')
    Assert-True 'edit dryrun: prints the slot and every renumber, highest first' (
        $editDry.Output -match 'slot 11' -and @($expectedShifts | Where-Object { $editDry.Output -match [regex]::Escape($_) }).Count -eq 3 -and
        $editDry.Output.IndexOf('DEV-370: 13 -> 14') -lt $editDry.Output.IndexOf('DEV-285: 12 -> 13') -and $editDry.Output.IndexOf('DEV-285: 12 -> 13') -lt $editDry.Output.IndexOf('DEV-310: 11 -> 12') -and $editDry.Output -match '(?m)^    DEV-290: Order 11')
    Assert-True 'edit dryrun: touches neither the text nor the comment store' ($editDry.State.issues['DEV-290'].summary -ceq 'Old summary' -and $editDry.State.comments.Count -eq 0)
    $dryRefused = Invoke-Edit "-Ticket DEV-390 -DryRun -Order" -State (New-State)
    Assert-True 'edit dryrun: refuses exactly like a real run (exit 2, zero writes)' ($dryRefused.ExitCode -eq 2 -and $dryRefused.Output -match 'already has Order 7' -and (Get-NonGetCall $dryRefused).Count -eq 0)
    $dryDifferent = Invoke-Edit "-Ticket DEV-290 -DryRun -Parent DEV-284" -State (New-RepairState)
    Assert-True 'edit dryrun: a different parent is refused too' ($dryDifferent.ExitCode -eq 2 -and $dryDifferent.Output -match 'already a subtask of DEV-93' -and (Get-NonGetCall $dryDifferent).Count -eq 0)
    $heldState = New-RepairState -Parent 'DEV-284' -Estimate 240
    $heldState.members['218-10'] = @('DEV-290')
    $dryHeld = Invoke-Edit "-Ticket DEV-290 -DryRun -Parent DEV-284 -Estimate 4h -Type Task -Sprint" -State $heldState
    Assert-True 'edit dryrun: everything already held prints that nothing needs writing (exit 0, zero writes)' ($dryHeld.ExitCode -eq 0 -and $dryHeld.Output -match 'nothing would be written' -and $dryHeld.Output -match 'already as requested, nothing to write: parent, estimate, type, sprint' -and (Get-NonGetCall $dryHeld).Count -eq 0)
    $dryText = Invoke-Edit "-Ticket DEV-290 -DryRun -Summary 'Renamed' -Tag size:S" -State (New-State)
    Assert-True 'edit dryrun: a plain text and tag edit is planned, not made' ($dryText.ExitCode -eq 0 -and (Get-NonGetCall $dryText).Count -eq 0 -and $dryText.State.issues['DEV-290'].summary -ceq 'Old summary' -and $dryText.Output -match 'planned POST /api/issues/DEV-290/tags')
    $dryNoTag = Invoke-Edit "-Ticket DEV-290 -DryRun -Tag nope" -State (New-State)
    Assert-True 'edit dryrun: an unknown tag is exit 2 with zero writes' ($dryNoTag.ExitCode -eq 2 -and (Get-NonGetCall $dryNoTag).Count -eq 0)
    Assert-True 'edit dryrun: with nothing to change is still exit 2' ((Invoke-Edit "-Ticket DEV-290 -DryRun").ExitCode -eq 2)

    # ---- the exit-3 banner: the exact runnable repair command per step not done ----
    $repairPattern = { param($Output, [string]$Tail) $Output -match ('(?m)^\s+pwsh ' + [regex]::Escape($edit) + ' -Ticket DEV-900 ' + [regex]::Escape($Tail) + '\s*$') }
    $bannerParent = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ failStep = 'parent' })
    Assert-True 'banner: a failed parent step names -Parent, -Tag, -Order and -Sprint repair commands, in that order' (
        $bannerParent.ExitCode -eq 3 -and (& $repairPattern $bannerParent.Output '-Parent DEV-284') -and (& $repairPattern $bannerParent.Output '-Tag size:M') -and (& $repairPattern $bannerParent.Output '-Order') -and (& $repairPattern $bannerParent.Output '-Sprint') -and
        $bannerParent.Output.IndexOf('-Parent DEV-284') -lt $bannerParent.Output.IndexOf('-Tag size:M') -and $bannerParent.Output.IndexOf('-Tag size:M') -lt $bannerParent.Output.IndexOf('-Order') -and $bannerParent.Output.IndexOf('-Order', $bannerParent.Output.IndexOf('-Tag size:M')) -lt $bannerParent.Output.IndexOf('-Sprint'))
    Assert-True 'banner: no longer tells the caller to hand parent, estimate, Order or sprint to Patron' ($bannerParent.Output -notmatch 'tell Patron\.' -and $bannerParent.Output -notmatch 'For parent, estimate')
    $bannerTags = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ failStep = 'tags' })
    Assert-True 'banner: a failed tags step lists -Tag, -Order and -Sprint, and not the parent link that is done' ($bannerTags.ExitCode -eq 3 -and (& $repairPattern $bannerTags.Output '-Tag size:M') -and (& $repairPattern $bannerTags.Output '-Order') -and (& $repairPattern $bannerTags.Output '-Sprint') -and -not (& $repairPattern $bannerTags.Output '-Parent DEV-284'))
    $bannerOrder = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ failStep = 'order' })
    Assert-True 'banner: a failed order step lists -Order and -Sprint only, and warns about a half-done renumber' ($bannerOrder.ExitCode -eq 3 -and (& $repairPattern $bannerOrder.Output '-Order') -and (& $repairPattern $bannerOrder.Output '-Sprint') -and -not (& $repairPattern $bannerOrder.Output '-Tag size:M') -and $bannerOrder.Output -match 'renumber stopped half way')
    $bannerSprint = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ failStep = 'sprint' })
    Assert-True 'banner: a failed sprint step lists -Sprint only' ($bannerSprint.ExitCode -eq 3 -and (& $repairPattern $bannerSprint.Output '-Sprint') -and -not (& $repairPattern $bannerSprint.Output '-Order') -and -not (& $repairPattern $bannerSprint.Output '-Parent DEV-284'))
    $bannerOther = Invoke-Edit "-Create $createArguments -Repository essay-reviewer" -State (New-State -Option @{ failStep = 'parent' })
    Assert-True 'banner: a repository with no Order gets no -Order command' ($bannerOther.ExitCode -eq 3 -and (& $repairPattern $bannerOther.Output '-Parent DEV-284') -and -not (& $repairPattern $bannerOther.Output '-Order'))
    $bannerNoTag = Invoke-Edit "-Create -Summary 'No tags here' -DescriptionFile '$description' -Parent DEV-284 -Estimate 1d" -State (New-State -Option @{ failStep = 'parent' })
    Assert-True 'banner: no -Tag command when no tag was asked for' ($bannerNoTag.ExitCode -eq 3 -and -not (& $repairPattern $bannerNoTag.Output '-Tag size:M') -and $bannerNoTag.Output -notmatch '(?m)^\s+pwsh .* -Tag')
    foreach ($case in @(
            @{ Option = 'wrongType'; Tail = '-Type Task' }
            @{ Option = 'dropEstimate'; Tail = '-Estimate 1d' }
            @{ Option = 'dropSprint'; Tail = '-Sprint' }
            @{ Option = 'dropTag'; Tail = '-Tag size:M' })) {
        $readBack = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ $case.Option = $true })
        Assert-True "banner: a read-back mismatch ($($case.Option)) prints its repair command $($case.Tail)" ($readBack.ExitCode -eq 3 -and (& $repairPattern $readBack.Output $case.Tail))
    }
    $bannerQuoted = Invoke-Edit "-Create -Summary 'Two parts' -DescriptionFile '$description' -Parent DEV-284 -Estimate '2h 30m'" -State (New-State -Option @{ dropEstimate = $true })
    Assert-True 'banner: an estimate with a space is single-quoted so the command runs as printed' ($bannerQuoted.ExitCode -eq 3 -and (& $repairPattern $bannerQuoted.Output "-Estimate '2h 30m'"))
    $bannerOrderMismatch = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ dropOrder = $true })
    Assert-True 'banner: an Order read-back mismatch has no command and is left to Patron' ($bannerOrderMismatch.ExitCode -eq 3 -and $bannerOrderMismatch.Output -match 'has no repair command: tell Patron' -and -not (& $repairPattern $bannerOrderMismatch.Output '-Order'))

    # The printed commands are runnable: feed them back, in order, and the ticket ends up as a clean -Create would leave it.
    $broken = Invoke-Edit "-Create $createArguments" -State (New-State -Option @{ failStep = 'parent' })
    $repairState = $broken.State
    $null = $repairState.options.Remove('failStep')
    $commands = @($broken.Output -split "`n" | ForEach-Object { if ($_ -match ('^\s+pwsh ' + [regex]::Escape($edit) + ' (-Ticket DEV-900 .+?)\s*$')) { $Matches[1] } })
    $repairExit = @()
    foreach ($command in $commands) {
        $step = Invoke-Edit $command -State $repairState
        $repairExit += $step.ExitCode
        $repairState = $step.State
    }
    Assert-True 'repair chain: the printed commands are four, and each exits 0' ($commands.Count -eq 4 -and (@($repairExit | Where-Object { $_ -ne 0 }).Count -eq 0))
    $clean = $create.State.issues['DEV-900']
    $mended = $repairState.issues['DEV-900']
    Assert-True 'repair chain: parent, tag, Order and sprint end up exactly as after a clean -Create' (
        $mended.parent -ceq $clean.parent -and $mended.tags -contains 'size:M' -and $mended.order -eq $clean.order -and $repairState.members['218-10'] -contains 'DEV-900' -and
        (Test-Contiguous @($repairState.issues.Values | Where-Object { $null -ne $_.order } | ForEach-Object { [int]$_.order } | Sort-Object)) -and $repairState.issues['DEV-310'].order -eq 12)

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

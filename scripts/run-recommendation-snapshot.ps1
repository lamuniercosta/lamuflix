#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Captures the For you and five Related recommendation lists from the running home
    instance into a deterministic JSON artifact, or diffs two compatible artifacts.

.DESCRIPTION
    Capture reads GET {BaseUrl}/api/recommendations/for-you?take=30 and, for each of the
    five ordered seed movies, GET {BaseUrl}/api/movies/{id}/recommendations?take=10. The
    returned order is preserved row for row, every reason kind and text is recorded, and
    an empty successful Related response is recorded as an empty list with status "empty".
    The artifact carries only the mode, the seed ids, the explicit take values and the
    lists/rows; no timestamp, host or elapsed time is recorded. Canonical compact UTF-8
    JSON without a byte-order mark and without a terminal newline is published atomically
    through a ".partial" sibling and a rename, so two captures of unchanged data are
    byte-identical. Any HTTP, JSON, schema or duplicate-id failure stops the run before
    anything is published.

    Diff accepts two same-mode artifacts (capture/capture or compare/compare), validates
    compatible mode, targets and take values, matches rows by movie id inside each list,
    and always prints the five groups moved up, moved down, entered, left and reasons
    changed, even when a group is empty.

    The five ordered seed titles are A Fistful of Dollars, About Time, A Few Good Men,
    Aladdin and The Wind Rises, with literal home-library ids 575, 518, 515, 519 and 831
    found by the one-time T019 lookup. Their defaults keep parameter-free Capture working
    against the home instance; -SeedIds still accepts explicit overrides.

.PARAMETER Mode
    Capture publishes a new artifact; Diff prints the five comparison groups.

.PARAMETER BaseUrl
    Home API origin. Default: http://localhost:3100

.PARAMETER SeedIds
    Five ordered positive home-library movie ids, validated before any HTTP request.
    Defaults come from the one-time T019 home-library lookup, never from a fixture id:
    A Fistful of Dollars 575, About Time 518, A Few Good Men 515, Aladdin 519,
    The Wind Rises 831. Explicit overrides still work. The fixture About Time id 90 is
    never a home-library id.

.PARAMETER OutputPath
    Published capture artifact. Default, resolved from the repository root:
    artifacts/recommendations/capture.json

.PARAMETER Before
    Diff operand published first (the earlier capture or compare artifact).

.PARAMETER After
    Diff operand published second (the later capture or compare artifact).

.PARAMETER Help
    Show this help.
#>
[CmdletBinding()]
param(
    [ValidateSet('Capture', 'Diff')]
    [string]$Mode = 'Capture',
    [string]$BaseUrl = 'http://localhost:3100',
    [string[]]$SeedIds = @('575', '518', '515', '519', '831'),
    [string]$OutputPath = '',
    [string]$Before = '',
    [string]$After = '',
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$captureMode = 'capture'
$compareMode = 'compare'
$forYouKind = 'for-you'
$relatedKind = 'related'
$compareKind = 'compare'
$okStatus = 'ok'
$emptyStatus = 'empty'
$structuralReason = 'structural'
$personalReason = 'personal'
$forYouTake = 30
$relatedTake = 10
$requiredSeedCount = 5
$defaultOutputRelative = 'artifacts/recommendations/capture.json'
$partialSuffix = '.partial'
$requestTimeout = [TimeSpan]::FromSeconds(100)
$seedTitles = @(
    'A Fistful of Dollars',
    'About Time',
    'A Few Good Men',
    'Aladdin',
    'The Wind Rises'
)
$seedDefaultIds = @(575, 518, 515, 519, 831)

function Format-SeedDefaultLines {
    $lines = [System.Collections.Generic.List[string]]::new()
    for ($index = 0; $index -lt $seedTitles.Count; $index++) {
        $lines.Add(('                        {0}. {1} = {2}' -f ($index + 1), $seedTitles[$index], $seedDefaultIds[$index]))
    }

    return ($lines -join [Environment]::NewLine)
}

function Write-Usage {
    Write-Output @"
Usage: pwsh -NoProfile -File scripts/run-recommendation-snapshot.ps1 [OPTIONS]

Capture publishes the deterministic For you plus five Related lists. Diff prints the five
comparison groups for two same-mode artifacts.

OPTIONS:
  -Mode <Capture|Diff>  Work to perform. Default: Capture
  -BaseUrl <uri>        Home API origin. Default: $BaseUrl
  -SeedIds <int[]>      Five ordered positive movie ids, validated before any HTTP
                        request. Ordered titles and home-library defaults:
$(Format-SeedDefaultLines)
                        Explicit overrides are accepted in the same order.
  -OutputPath <path>    Published capture artifact. Default, resolved from the
                        repository root: $defaultOutputRelative
  -Before <path>        Diff operand published first.
  -After <path>         Diff operand published second.
  -Help                 Show this help.

Capture always requests take=$forYouTake for For you and take=$relatedTake for each Related
list. Diff requires two capture artifacts or two compare artifacts; mixed modes, malformed
JSON, schema violations and duplicate movie ids are rejected. Exit code 0 means the capture
artifact was published or the diff completed.
"@
}

if ($Help) {
    Write-Usage
    exit 0
}

function Stop-Run {
    param([string]$Message)

    throw $Message
}

function Read-JsonDocument {
    param(
        [string]$Text,
        [string]$Description
    )

    try {
        return [System.Text.Json.JsonDocument]::Parse($Text)
    }
    catch {
        Stop-Run "The $Description is not valid JSON."
    }
}

function Read-FiniteNumber {
    param(
        [System.Text.Json.JsonElement]$Element,
        [string]$Name,
        [string]$Context
    )

    if ($Element.ValueKind -ne [System.Text.Json.JsonValueKind]::Number) {
        Stop-Run "The $Context '$Name' must be a JSON number."
    }

    $value = $Element.GetDouble()
    if (-not [double]::IsFinite($value)) {
        Stop-Run "The $Context '$Name' must be a finite number."
    }

    return $value
}

function Read-RequiredProperty {
    param(
        [System.Text.Json.JsonElement]$Element,
        [string]$Name,
        [string]$Context
    )

    $property = [System.Text.Json.JsonElement]::new()
    if (-not $Element.TryGetProperty($Name, [ref]$property)) {
        Stop-Run "The $Context is missing the '$Name' property."
    }

    return $property
}

function Read-IntegerValue {
    param(
        [System.Text.Json.JsonElement]$Element,
        [string]$Name,
        [string]$Context
    )

    if ($Element.ValueKind -ne [System.Text.Json.JsonValueKind]::Number) {
        Stop-Run "The $Context '$Name' must be a JSON integer."
    }

    $value = 0
    if (-not $Element.TryGetInt32([ref]$value)) {
        Stop-Run "The $Context '$Name' must be a JSON integer."
    }

    return $value
}

function Read-RequiredInteger {
    param(
        [System.Text.Json.JsonElement]$Element,
        [string]$Name,
        [string]$Context
    )

    return Read-IntegerValue `
        -Element (Read-RequiredProperty -Element $Element -Name $Name -Context $Context) `
        -Name $Name `
        -Context $Context
}

function Read-OptionalInteger {
    param(
        [System.Text.Json.JsonElement]$Element,
        [string]$Name,
        [string]$Context
    )

    $property = [System.Text.Json.JsonElement]::new()
    if (-not $Element.TryGetProperty($Name, [ref]$property) -or $property.ValueKind -eq [System.Text.Json.JsonValueKind]::Null) {
        return $null
    }

    return Read-IntegerValue -Element $property -Name $Name -Context $Context
}

function Read-RequiredString {
    param(
        [System.Text.Json.JsonElement]$Element,
        [string]$Name,
        [string]$Context
    )

    $property = Read-RequiredProperty -Element $Element -Name $Name -Context $Context
    if ($property.ValueKind -ne [System.Text.Json.JsonValueKind]::String) {
        Stop-Run "The $Context '$Name' must be a JSON string."
    }

    return $property.GetString()
}

function Read-Reasons {
    param(
        [System.Text.Json.JsonElement]$Element,
        [string]$Context
    )

    $reasons = Read-RequiredProperty -Element $Element -Name 'reasons' -Context $Context
    if ($reasons.ValueKind -ne [System.Text.Json.JsonValueKind]::Array) {
        Stop-Run "The $Context 'reasons' must be a JSON array."
    }

    $read = [System.Collections.Generic.List[object]]::new()
    foreach ($reason in $reasons.EnumerateArray()) {
        if ($reason.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
            Stop-Run "The $Context carries a reason that is not a JSON object."
        }

        $kind = Read-RequiredString -Element $reason -Name 'kind' -Context "$Context reason"
        if ($kind -ne $structuralReason -and $kind -ne $personalReason) {
            Stop-Run "The $Context carries the unrecognized reason kind '$kind'."
        }

        $text = Read-RequiredString -Element $reason -Name 'text' -Context "$Context reason"
        $read.Add([pscustomobject]@{ Kind = $kind; Text = $text })
    }

    return , $read.ToArray()
}

function Read-Row {
    param(
        [System.Text.Json.JsonElement]$Element,
        [int]$Position,
        [string]$Context
    )

    if ($Element.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
        Stop-Run "The $Context row $Position is not a JSON object."
    }

    $rank = Read-OptionalInteger -Element $Element -Name 'rank' -Context "$Context row"
    if ($null -eq $rank) {
        Stop-Run "The $Context row $Position is missing the 'rank' property."
    }

    if ($rank -ne $Position) {
        Stop-Run "The $Context ranks must be one-based and contiguous; found $rank at position $Position."
    }

    $movieId = Read-RequiredInteger -Element $Element -Name 'movieId' -Context "$Context row"
    if ($movieId -le 0) {
        Stop-Run "The $Context row $Position carries the non-positive movie id '$movieId'."
    }

    return [pscustomobject]@{
        Rank    = $rank
        MovieId = $movieId
        Title   = Read-RequiredString -Element $Element -Name 'title' -Context "$Context row"
        Score   = Read-FiniteNumber -Element (Read-RequiredProperty -Element $Element -Name 'score' -Context "$Context row") -Name 'score' -Context "$Context row"
        Reasons = Read-Reasons -Element $Element -Context "$Context row"
    }
}

function Read-Rows {
    param(
        [System.Text.Json.JsonElement]$Element,
        [string]$Context
    )

    $required = Read-RequiredProperty -Element $Element -Name 'rows' -Context $Context
    if ($required.ValueKind -ne [System.Text.Json.JsonValueKind]::Array) {
        Stop-Run "The $Context 'rows' must be a JSON array."
    }

    $rows = [System.Collections.Generic.List[object]]::new()
    $seen = [System.Collections.Generic.HashSet[int]]::new()
    $position = 0
    foreach ($item in $required.EnumerateArray()) {
        $position++
        $row = Read-Row -Element $item -Position $position -Context $Context
        if (-not $seen.Add($row.MovieId)) {
            Stop-Run "The $Context repeats movie id '$($row.MovieId)'."
        }

        $rows.Add($row)
    }

    return , $rows.ToArray()
}

function Read-ArtifactList {
    param(
        [System.Text.Json.JsonElement]$Element
    )

    if ($Element.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
        Stop-Run 'The artifact lists must be JSON objects.'
    }

    $kind = Read-RequiredString -Element $Element -Name 'kind' -Context 'list'
    if ($kind -ne $forYouKind -and $kind -ne $relatedKind -and $kind -ne $compareKind) {
        Stop-Run "The artifact list kind '$kind' is not recognized."
    }

    $targetId = Read-OptionalInteger -Element $Element -Name 'targetId' -Context 'list'
    $take = Read-OptionalInteger -Element $Element -Name 'take' -Context 'list'
    if ($null -ne $take -and $take -le 0) {
        Stop-Run 'The artifact list take must be a positive integer when declared.'
    }

    $status = Read-RequiredString -Element $Element -Name 'status' -Context 'list'
    if ($status -ne $okStatus -and $status -ne $emptyStatus) {
        Stop-Run "The artifact list status '$status' is not recognized."
    }

    $rows = Read-Rows -Element $Element -Context "list '$kind'"
    if ($status -eq $emptyStatus -and $rows.Count -gt 0) {
        Stop-Run "The artifact list '$kind' is marked empty but carries rows."
    }

    return [pscustomobject]@{
        Kind     = $kind
        TargetId = $targetId
        Take     = $take
        Status   = $status
        Rows     = $rows
    }
}

function Read-Artifact {
    param([string]$Path)

    if (-not [System.IO.File]::Exists($Path)) {
        Stop-Run "The artifact was not found: $Path"
    }

    $text = [System.IO.File]::ReadAllText($Path)
    $document = Read-JsonDocument -Text $text -Description "artifact '$Path'"
    try {
        $root = $document.RootElement
        if ($root.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
            Stop-Run 'The artifact root must be a JSON object.'
        }

        $artifactMode = Read-RequiredString -Element $root -Name 'mode' -Context 'artifact'
        if ($artifactMode -ne $captureMode -and $artifactMode -ne $compareMode) {
            Stop-Run "The artifact mode '$artifactMode' is not recognized."
        }

        $targetsElement = Read-RequiredProperty -Element $root -Name 'targets' -Context 'artifact'
        if ($targetsElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Array) {
            Stop-Run 'The artifact targets must be a JSON array.'
        }

        $targets = [System.Collections.Generic.List[int]]::new()
        $seenTargets = [System.Collections.Generic.HashSet[int]]::new()
        foreach ($target in $targetsElement.EnumerateArray()) {
            $targetId = Read-IntegerValue -Element $target -Name 'targets' -Context 'target'
            if ($targetId -le 0) {
                Stop-Run "The artifact target '$targetId' must be a positive integer."
            }

            if (-not $seenTargets.Add($targetId)) {
                Stop-Run "The artifact target '$targetId' appears more than once."
            }

            $targets.Add($targetId)
        }

        if ($targets.Count -eq 0) {
            Stop-Run 'The artifact must declare at least one target.'
        }

        $listsElement = Read-RequiredProperty -Element $root -Name 'lists' -Context 'artifact'
        if ($listsElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Array) {
            Stop-Run 'The artifact lists must be a JSON array.'
        }

        $lists = [System.Collections.Generic.List[object]]::new()
        foreach ($listElement in $listsElement.EnumerateArray()) {
            $lists.Add((Read-ArtifactList -Element $listElement))
        }

        if ($lists.Count -eq 0) {
            Stop-Run 'The artifact must declare at least one list.'
        }

        return [pscustomobject]@{
            Mode    = $artifactMode
            Targets = $targets.ToArray()
            Lists   = $lists.ToArray()
        }
    }
    finally {
        $document.Dispose()
    }
}

function Read-ApiList {
    param(
        [string]$Body,
        [uri]$Uri,
        [string]$Kind,
        [Nullable[int]]$TargetId,
        [int]$Take
    )

    $document = Read-JsonDocument -Text $Body -Description "$Kind response from $Uri"
    try {
        $root = $document.RootElement
        if ($root.ValueKind -ne [System.Text.Json.JsonValueKind]::Array) {
            Stop-Run "The $Kind response from $Uri must be a JSON array."
        }

        $rows = [System.Collections.Generic.List[object]]::new()
        $seen = [System.Collections.Generic.HashSet[int]]::new()
        $position = 0
        foreach ($item in $root.EnumerateArray()) {
            $position++
            if ($item.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) {
                Stop-Run "The $Kind response carries a row that is not a JSON object."
            }

            $movieId = Read-RequiredInteger -Element $item -Name 'id' -Context "$Kind row"
            if ($movieId -le 0) {
                Stop-Run "The $Kind response carries the non-positive movie id '$movieId'."
            }

            if (-not $seen.Add($movieId)) {
                Stop-Run "The $Kind response repeats movie id '$movieId'."
            }

            $rows.Add([pscustomobject]@{
                    Rank    = $position
                    MovieId = $movieId
                    Title   = Read-RequiredString -Element $item -Name 'title' -Context "$Kind row"
                    Score   = Read-FiniteNumber -Element (Read-RequiredProperty -Element $item -Name 'score' -Context "$Kind row") -Name 'score' -Context "$Kind row"
                    Reasons = Read-Reasons -Element $item -Context "$Kind row"
                })
        }

        return [pscustomobject]@{
            Kind     = $Kind
            TargetId = $TargetId
            Take     = $Take
            Status   = $(if ($rows.Count -eq 0) { $emptyStatus } else { $okStatus })
            Rows     = $rows.ToArray()
        }
    }
    finally {
        $document.Dispose()
    }
}

function Invoke-ListRequest {
    param(
        [System.Net.Http.HttpClient]$Client,
        [uri]$Uri,
        [string]$Kind,
        [Nullable[int]]$TargetId,
        [int]$Take
    )

    $response = $Client.GetAsync($Uri).GetAwaiter().GetResult()
    try {
        if (-not $response.IsSuccessStatusCode) {
            Stop-Run "The $Kind request to $Uri failed with HTTP status $([int]$response.StatusCode)."
        }

        $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    }
    finally {
        $response.Dispose()
    }

    return Read-ApiList -Body $body -Uri $Uri -Kind $Kind -TargetId $TargetId -Take $Take
}

function Write-Reasons {
    param(
        [System.Text.Json.Utf8JsonWriter]$Writer,
        [object[]]$Reasons
    )

    $Writer.WriteStartArray('reasons')
    foreach ($reason in $Reasons) {
        $Writer.WriteStartObject()
        $Writer.WriteString('kind', [string]$reason.Kind)
        $Writer.WriteString('text', [string]$reason.Text)
        $Writer.WriteEndObject()
    }

    $Writer.WriteEndArray()
}

function Write-List {
    param(
        [System.Text.Json.Utf8JsonWriter]$Writer,
        [object]$List
    )

    $Writer.WriteStartObject()
    $Writer.WriteString('kind', [string]$List.Kind)
    if ($null -eq $List.TargetId) {
        $Writer.WriteNull('targetId')
    }
    else {
        $Writer.WriteNumber('targetId', [int]$List.TargetId)
    }

    if ($null -eq $List.Take) {
        $Writer.WriteNull('take')
    }
    else {
        $Writer.WriteNumber('take', [int]$List.Take)
    }

    $Writer.WriteString('status', [string]$List.Status)
    $Writer.WriteStartArray('rows')
    foreach ($row in $List.Rows) {
        $Writer.WriteStartObject()
        $Writer.WriteNumber('rank', [int]$row.Rank)
        $Writer.WriteNumber('movieId', [int]$row.MovieId)
        $Writer.WriteString('title', [string]$row.Title)
        $Writer.WriteNumber('score', [double]$row.Score)
        Write-Reasons -Writer $Writer -Reasons $row.Reasons
        $Writer.WriteEndObject()
    }

    $Writer.WriteEndArray()
    $Writer.WriteEndObject()
}

function Write-EnvelopeBytes {
    param([object]$Envelope)

    $stream = [System.IO.MemoryStream]::new()
    try {
        $writer = [System.Text.Json.Utf8JsonWriter]::new(
            $stream,
            [System.Text.Json.JsonWriterOptions]@{ Indented = $false })
        try {
            $writer.WriteStartObject()
            $writer.WriteString('mode', [string]$Envelope.Mode)
            $writer.WriteStartArray('targets')
            foreach ($target in $Envelope.Targets) {
                $writer.WriteNumberValue([int]$target)
            }

            $writer.WriteEndArray()
            $writer.WriteStartArray('lists')
            foreach ($list in $Envelope.Lists) {
                Write-List -Writer $writer -List $list
            }

            $writer.WriteEndArray()
            $writer.WriteEndObject()
        }
        finally {
            $writer.Dispose()
        }

        return , $stream.ToArray()
    }
    finally {
        $stream.Dispose()
    }
}

function Publish-Artifact {
    param(
        [object]$Envelope,
        [string]$Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $directory = [System.IO.Path]::GetDirectoryName($fullPath)
    if ([string]::IsNullOrEmpty($directory)) {
        Stop-Run "The artifact path must name a file inside a directory: $Path"
    }

    [System.IO.Directory]::CreateDirectory($directory) | Out-Null
    $bytes = Write-EnvelopeBytes -Envelope $Envelope

    $partialPath = $fullPath + $partialSuffix
    try {
        [System.IO.File]::WriteAllBytes($partialPath, $bytes)
        [System.IO.File]::Move($partialPath, $fullPath, $true)
    }
    finally {
        if ([System.IO.File]::Exists($partialPath)) {
            [System.IO.File]::Delete($partialPath)
        }
    }
}

function Get-ListLabel {
    param([object]$List)

    if ($null -eq $List.TargetId) {
        return [string]$List.Kind
    }

    return "$($List.Kind) $($List.TargetId)"
}

function Get-ReasonSignature {
    param([object[]]$Reasons)

    return (@($Reasons | ForEach-Object { "$($_.Kind):$($_.Text)" }) -join [char]31)
}

function Write-DiffGroup {
    param(
        [string]$Heading,
        [System.Collections.Generic.List[string]]$Lines
    )

    Write-Output "$Heading`:"
    foreach ($line in $Lines) {
        Write-Output "  $line"
    }
}

function Assert-DiffPair {
    param(
        [object]$Before,
        [object]$After
    )

    if ($Before.Mode -ne $After.Mode) {
        Stop-Run "Diff requires same-mode artifacts; found '$($Before.Mode)' and '$($After.Mode)'."
    }

    $beforeTargets = ($Before.Targets -join ',')
    $afterTargets = ($After.Targets -join ',')
    if ($beforeTargets -ne $afterTargets) {
        Stop-Run "Diff requires identical targets; found [$beforeTargets] and [$afterTargets]."
    }

    if ($Before.Lists.Count -ne $After.Lists.Count) {
        Stop-Run "Diff requires the same list count; found $($Before.Lists.Count) and $($After.Lists.Count)."
    }

    for ($index = 0; $index -lt $Before.Lists.Count; $index++) {
        $beforeList = $Before.Lists[$index]
        $afterList = $After.Lists[$index]

        if ($beforeList.Kind -ne $afterList.Kind) {
            Stop-Run "Diff requires matching list kinds; found '$($beforeList.Kind)' and '$($afterList.Kind)'."
        }

        if ($beforeList.TargetId -ne $afterList.TargetId) {
            Stop-Run "Diff requires matching list targets inside '$($beforeList.Kind)'."
        }

        if ($null -ne $beforeList.Take -and $null -ne $afterList.Take -and $beforeList.Take -ne $afterList.Take) {
            Stop-Run "Diff requires matching take values inside '$($beforeList.Kind)'; found $($beforeList.Take) and $($afterList.Take)."
        }
    }
}

function Invoke-Diff {
    param(
        [string]$BeforePath,
        [string]$AfterPath
    )

    $before = Read-Artifact -Path $BeforePath
    $after = Read-Artifact -Path $AfterPath
    Assert-DiffPair -Before $before -After $after

    $movedUp = [System.Collections.Generic.List[string]]::new()
    $movedDown = [System.Collections.Generic.List[string]]::new()
    $entered = [System.Collections.Generic.List[string]]::new()
    $left = [System.Collections.Generic.List[string]]::new()
    $reasonsChanged = [System.Collections.Generic.List[string]]::new()

    for ($index = 0; $index -lt $before.Lists.Count; $index++) {
        $beforeList = $before.Lists[$index]
        $afterList = $after.Lists[$index]
        $label = Get-ListLabel -List $beforeList

        $beforeRows = @{}
        foreach ($row in $beforeList.Rows) {
            $beforeRows[$row.MovieId] = $row
        }

        $afterRows = @{}
        foreach ($row in $afterList.Rows) {
            $afterRows[$row.MovieId] = $row
        }

        foreach ($row in $afterList.Rows) {
            if (-not $beforeRows.ContainsKey($row.MovieId)) {
                $entered.Add("$label movie $($row.MovieId) `"$($row.Title)`" rank $($row.Rank)")
                continue
            }

            $prior = $beforeRows[$row.MovieId]
            if ($row.Rank -lt $prior.Rank) {
                $movedUp.Add("$label movie $($row.MovieId) `"$($row.Title)`" rank $($prior.Rank) -> $($row.Rank)")
            }
            elseif ($row.Rank -gt $prior.Rank) {
                $movedDown.Add("$label movie $($row.MovieId) `"$($row.Title)`" rank $($prior.Rank) -> $($row.Rank)")
            }

            if ((Get-ReasonSignature -Reasons $prior.Reasons) -ne (Get-ReasonSignature -Reasons $row.Reasons)) {
                $reasonsChanged.Add("$label movie $($row.MovieId) `"$($row.Title)`"")
            }
        }

        foreach ($row in $beforeList.Rows) {
            if (-not $afterRows.ContainsKey($row.MovieId)) {
                $left.Add("$label movie $($row.MovieId) `"$($row.Title)`" rank $($row.Rank)")
            }
        }
    }

    Write-DiffGroup -Heading 'moved up' -Lines $movedUp
    Write-DiffGroup -Heading 'moved down' -Lines $movedDown
    Write-DiffGroup -Heading 'entered' -Lines $entered
    Write-DiffGroup -Heading 'left' -Lines $left
    Write-DiffGroup -Heading 'reasons changed' -Lines $reasonsChanged
    Write-Output (('summary: moved up {0}, moved down {1}, entered {2}, left {3}, reasons changed {4}' -f `
                $movedUp.Count, $movedDown.Count, $entered.Count, $left.Count, $reasonsChanged.Count))
}

function Resolve-SeedIds {
    param([string[]]$Values)

    $resolved = [System.Collections.Generic.List[int]]::new()
    foreach ($value in $Values) {
        foreach ($part in ([string]$value).Split(',', [System.StringSplitOptions]::RemoveEmptyEntries)) {
            $trimmed = $part.Trim()
            $parsed = 0
            if (-not [int]::TryParse($trimmed, [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
                Stop-Run "SeedIds entries must be integers; found '$trimmed'."
            }

            $resolved.Add($parsed)
        }
    }

    if ($resolved.Count -ne $requiredSeedCount) {
        Stop-Run "SeedIds must contain exactly $requiredSeedCount ordered positive movie ids; found $($resolved.Count)."
    }

    $seen = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($value in $resolved) {
        if ($value -le 0) {
            Stop-Run "SeedIds must be positive integers; found '$value'."
        }

        if (-not $seen.Add($value)) {
            Stop-Run "SeedIds repeats the movie id '$value'."
        }
    }

    return , $resolved.ToArray()
}

function Invoke-Capture {
    param(
        [uri]$BaseAddress,
        [int[]]$Seeds,
        [string]$Path
    )

    $client = [System.Net.Http.HttpClient]::new()
    $client.BaseAddress = $BaseAddress
    $client.Timeout = $requestTimeout
    try {
        $lists = [System.Collections.Generic.List[object]]::new()
        $forYouUri = [uri]::new($BaseAddress, "api/recommendations/for-you?take=$forYouTake")
        $lists.Add((Invoke-ListRequest -Client $client -Uri $forYouUri -Kind $forYouKind -TargetId $null -Take $forYouTake))

        foreach ($seed in $Seeds) {
            $relatedUri = [uri]::new($BaseAddress, "api/movies/$seed/recommendations?take=$relatedTake")
            $lists.Add((Invoke-ListRequest -Client $client -Uri $relatedUri -Kind $relatedKind -TargetId $seed -Take $relatedTake))
        }
    }
    finally {
        $client.Dispose()
    }

    $envelope = [pscustomobject]@{
        Mode    = $captureMode
        Targets = $Seeds
        Lists   = $lists.ToArray()
    }

    Publish-Artifact -Envelope $envelope -Path $Path
    Write-Output "Capture published: $([System.IO.Path]::GetFullPath($Path))"
}

$callerLocation = (Get-Location).ProviderPath
$repoRoot = Split-Path -Parent $PSScriptRoot

$baseAddress = [uri]::new($BaseUrl.TrimEnd('/') + '/')
if (-not $baseAddress.IsAbsoluteUri -or ($baseAddress.Scheme -ne 'http' -and $baseAddress.Scheme -ne 'https')) {
    Stop-Run "BaseUrl must be an absolute http or https origin; found '$BaseUrl'."
}

if ($Mode -eq 'Diff') {
    if ([string]::IsNullOrWhiteSpace($Before) -or [string]::IsNullOrWhiteSpace($After)) {
        Stop-Run 'Diff requires both -Before and -After artifact paths.'
    }

    Invoke-Diff `
        -BeforePath ([System.IO.Path]::GetFullPath($Before, $callerLocation)) `
        -AfterPath ([System.IO.Path]::GetFullPath($After, $callerLocation))
    exit 0
}

$seeds = Resolve-SeedIds -Values $SeedIds
Invoke-Capture `
    -BaseAddress $baseAddress `
    -Seeds $seeds `
    -Path $(if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot $defaultOutputRelative))
    }
    else {
        [System.IO.Path]::GetFullPath($OutputPath, $callerLocation)
    })
exit 0

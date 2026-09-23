#!/usr/bin/env pwsh
<#
  Sets a YouTrack issue's State and reads it back.

    pwsh scripts/local/Set-YouTrackState.ps1 -Ticket DEV-289 -State Done

  Exit 0 only when the read-back State equals -State. Exit 1 when the command
  was accepted but the State did not change (a workflow rule refused it, or the
  value name is wrong). Exit 2 on configuration or HTTP errors.

  The pipeline used to "update YouTrack to Done" by free-form API calls and
  report success without checking; this is the one command that proves it.

  Credentials resolve as documented in _youtrack.ps1 (same order as
  scripts/get-task.ps1). The token is sent only as Authorization: Bearer and is never printed.

  Offline-test seams (production callers omit them):
    -EnvironmentReader  { param($Name, $Target) ... }   # Target is Process or User
    -RestMethodInvoker  { param($Method, $Uri, $Headers, $Body) ... }
    -DpapiFileReader    { param($Path) ... }
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Z][A-Z0-9]*-\d+$')]
    [string]$Ticket,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$State,

    [scriptblock]$EnvironmentReader,
    [scriptblock]$RestMethodInvoker,
    [scriptblock]$DpapiFileReader
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$YouTrackTool = 'Set-YouTrackState'
. (Join-Path $PSScriptRoot '_youtrack.ps1')

Connect-YouTrack

# Braces let multi-word values such as {In Progress} parse as one value.
$null = Send-YouTrack Post '/api/commands' @{ query = "State {$State}"; issues = @(@{ idReadable = $Ticket }) }
$issue = Send-YouTrack Get "/api/issues/$([uri]::EscapeDataString($Ticket))?fields=idReadable,customFields(name,value(name))"

$actual = Get-CustomFieldName $issue 'State'
if ($actual -and $actual.Equals($State, [StringComparison]::OrdinalIgnoreCase)) {
    Write-Output "$Ticket State: $actual (verified)"
    exit 0
}

$shown = if ($actual) { $actual } else { '<unset>' }
[Console]::Error.WriteLine("Set-YouTrackState: $Ticket State is '$shown' after the command, expected '$State'. Not done.")
exit 1

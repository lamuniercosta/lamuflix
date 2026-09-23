# Shared YouTrack client for Set-YouTrackState.ps1 and Edit-YouTrackIssue.ps1.
# Dot-source it after the caller's param block, which declares these
# offline-test seams (production callers omit them):
#   -EnvironmentReader  { param($Name, $Target) ... }   # Target is Process or User
#   -RestMethodInvoker  { param($Method, $Uri, $Headers, $Body) ... }
#   -DpapiFileReader    { param($Path) ... }
# Set $YouTrackTool to the caller's name first; errors are prefixed with it.
#
# Credentials, in the same order as scripts/get-task.ps1:
#   YOUTRACK_URL    process env, then (Windows) User-scope env
#   YOUTRACK_TOKEN  process env, then (Windows) the DPAPI file
#                   $env:USERPROFILE\.dotnet-agent-harness\youtrack-token,
#                   then (Windows) User-scope env
# The token is sent only as Authorization: Bearer and is never printed.

. (Join-Path $PSScriptRoot '_json-property.ps1')

$onWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [System.Runtime.InteropServices.OSPlatform]::Windows)
$YouTrackConnection = $null

function Stop-WithError {
    param([string]$Message)
    [Console]::Error.WriteLine("${YouTrackTool}: $Message")
    exit 2
}

function Read-Environment {
    param([string]$Name, [string]$Target)
    if ($EnvironmentReader) { return [string](& $EnvironmentReader $Name $Target) }
    return [Environment]::GetEnvironmentVariable($Name, $Target)
}

function Read-DpapiToken {
    if ([string]::IsNullOrWhiteSpace($env:USERPROFILE)) { return $null }
    $path = Join-Path $env:USERPROFILE (Join-Path '.dotnet-agent-harness' 'youtrack-token')
    try {
        if ($DpapiFileReader) { return [string](& $DpapiFileReader $path) }
        if (-not $onWindows -or -not (Test-Path -LiteralPath $path)) { return $null }
        $raw = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
        $secure = ConvertTo-SecureString -String $raw.Trim()
        return [System.Net.NetworkCredential]::new('', $secure).Password
    }
    catch {
        Write-Warning "Could not read or decrypt YouTrack token file '$path'."
        return $null
    }
}

function Resolve-Setting {
    param([string]$Name, [switch]$AllowTokenFile)
    $value = Read-Environment $Name 'Process'
    if (-not [string]::IsNullOrWhiteSpace($value)) { return $value.Trim() }
    if ($AllowTokenFile) {
        $value = Read-DpapiToken
        if (-not [string]::IsNullOrWhiteSpace($value)) { return $value.Trim() }
    }
    if ($onWindows -or $EnvironmentReader) {
        $value = Read-Environment $Name 'User'
        if (-not [string]::IsNullOrWhiteSpace($value)) { return $value.Trim() }
    }
    return $null
}

function Connect-YouTrack {
    # Exits 2 when a setting is missing, before any request is made.
    $url = Resolve-Setting 'YOUTRACK_URL'
    $token = Resolve-Setting 'YOUTRACK_TOKEN' -AllowTokenFile
    if (-not $url) { Stop-WithError 'YOUTRACK_URL is not set (process env or User-scope env).' }
    if (-not $token) { Stop-WithError 'YOUTRACK_TOKEN is not set (process env, the DPAPI token file, or User-scope env).' }
    $script:YouTrackConnection = @{
        Base    = $url.TrimEnd('/')
        Headers = @{ Accept = 'application/json'; Authorization = "Bearer $token" }
        Token   = $token
    }
}

function Invoke-YouTrack {
    param([string]$Method, [string]$Uri, [hashtable]$Headers, [string]$Body)
    if ($RestMethodInvoker) { return & $RestMethodInvoker $Method $Uri $Headers $Body }
    $request = @{ Method = $Method; Uri = $Uri; Headers = $Headers; TimeoutSec = 30 }
    if ($Body) {
        $request.Body = [Text.Encoding]::UTF8.GetBytes($Body)
        $request.ContentType = 'application/json; charset=utf-8'
    }
    return Invoke-RestMethod @request
}

function Send-YouTrack {
    # $Path starts with /api/. $Body is serialised to JSON. Exits 2 on failure,
    # with the token redacted from the message.
    param([string]$Method, [string]$Path, $Body)
    $connection = $script:YouTrackConnection
    $json = if ($null -ne $Body) { ConvertTo-Json -InputObject $Body -Compress -Depth 8 } else { $null }
    try {
        return Invoke-YouTrack -Method $Method -Uri "$($connection.Base)$Path" -Headers $connection.Headers -Body $json
    }
    catch {
        Stop-WithError ("YouTrack $Method $Path failed: " + $_.Exception.Message.Replace($connection.Token, '<redacted>'))
    }
}

function Get-CustomFieldName {
    # The value name of a single-value custom field such as State or Type.
    param($Issue, [string]$Field)
    foreach ($entry in @(Get-JsonPath -Object $Issue -Path 'customFields')) {
        if ((Get-JsonPath -Object $entry -Path 'name') -ne $Field) { continue }
        return [string](Get-JsonPath -Object $entry -Path 'value', 'name')
    }
    return $null
}

#Requires -Version 7.0
# Renders docker/api.settings.json from docker/api.settings.example.json and a dedicated
# test-key env file holding OMDB_API_KEY and TMDB_API_KEY. The test-key file is required:
# the script never falls back to the home deployment secret source, so home production
# keys cannot be rendered into Compose settings labeled as test keys. Existing
# non-provider settings are preserved; OMDb is required and TMDb renders an explicit
# empty key when the source omits it. Never prints secret values.
#
# Usage: pwsh -NoProfile -File scripts/New-ComposeApiSettings.ps1 -SecretSourcePath <test-keys-file> [-SettingsPath <file>] [-WhatIf]
# Exit 0 on success, 1 on failure.

[CmdletBinding()]
param(
    [string]$SecretSourcePath,
    [string]$SettingsPath,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'home/_home-common.ps1')

function Merge-ExistingSettings {
    param($Template, [string]$Path)
    $existing = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    foreach ($property in $existing.PSObject.Properties) {
        if ($Template.PSObject.Properties.Name -contains $property.Name) {
            $Template.($property.Name) = $property.Value
        }
        else {
            $Template | Add-Member -NotePropertyName $property.Name -NotePropertyValue $property.Value
        }
    }
    return $Template
}

$exitCode = 0
try {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $templatePath = Join-Path $repoRoot 'docker/api.settings.example.json'
    if ([string]::IsNullOrWhiteSpace($SettingsPath)) {
        $SettingsPath = Join-Path $repoRoot 'docker/api.settings.json'
    }
    if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf)) { throw "missing $templatePath" }
    if ([string]::IsNullOrWhiteSpace($SecretSourcePath)) {
        throw "missing -SecretSourcePath: pass a dedicated test-key file holding OMDB_API_KEY and TMDB_API_KEY"
    }
    if (-not (Test-Path -LiteralPath $SecretSourcePath -PathType Leaf)) {
        throw "secret source file not found: $SecretSourcePath"
    }

    $omdb = Read-ProviderKey -Path $SecretSourcePath -Key 'OMDB_API_KEY'
    if ([string]::IsNullOrWhiteSpace($omdb)) { throw 'OMDB_API_KEY not found in secret source file' }
    $tmdb = Read-ProviderKey -Path $SecretSourcePath -Key 'TMDB_API_KEY'

    $settings = Get-Content -LiteralPath $templatePath -Raw | ConvertFrom-Json
    if (($null -eq $settings.Omdb) -or ($null -eq $settings.Tmdb)) { throw "template is missing the Omdb or Tmdb section: $templatePath" }
    if (Test-Path -LiteralPath $SettingsPath -PathType Leaf) {
        $settings = Merge-ExistingSettings -Template $settings -Path $SettingsPath
    }
    $settings.Omdb.ApiKey = $omdb
    $settings.Tmdb.ApiKey = $tmdb
    $settings | Add-Member -NotePropertyName 'ProviderKeys' -NotePropertyValue ([pscustomobject]@{ KeySet = 'test' }) -Force

    if ($WhatIf) {
        Write-Host '[compose] WhatIf: no changes made. Would render the Compose API settings from the dedicated test-key source.'
        exit 0
    }
    $dir = Split-Path -Parent $SettingsPath
    if (-not [string]::IsNullOrWhiteSpace($dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    Write-TextFileUtf8 -Path $SettingsPath -Text ($settings | ConvertTo-Json -Depth 20)
    Write-Host '[compose] rendered Compose API settings (values not shown)'
}
catch {
    Write-Host "[compose] FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $exitCode = 1
}
exit $exitCode

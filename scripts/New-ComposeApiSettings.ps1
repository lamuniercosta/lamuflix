#Requires -Version 7.0
# Renders docker/api.settings.json from docker/api.settings.example.json and the shared
# provider-key source the home setup reads (secretSourcePath holding OMDB_API_KEY and
# TMDB_API_KEY). Existing non-provider settings are preserved; OMDb is required and
# TMDb renders an explicit empty key when the source omits it. Never prints secret values.
#
# Usage: pwsh -NoProfile -File scripts/New-ComposeApiSettings.ps1 [-SecretSourcePath <file>] [-SettingsPath <file>] [-WhatIf]
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

function Get-DefaultSecretSourcePath {
    param([string]$RepoRoot)
    $configPath = Get-HomeConfigPathDefault -RepoRoot $RepoRoot
    if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
        throw "home config not found: $configPath (pass -SecretSourcePath or create the config from deploy/home/home.config.example.json)"
    }
    $raw = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    $source = [string](Get-OptionalProperty $raw 'secretSourcePath' '')
    if ([string]::IsNullOrWhiteSpace($source)) {
        throw "home config sets no secretSourcePath: $configPath (pass -SecretSourcePath)"
    }
    return $source
}

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
        $SecretSourcePath = Get-DefaultSecretSourcePath -RepoRoot $repoRoot
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

    if ($WhatIf) {
        Write-Host '[compose] WhatIf: no changes made. Would render the Compose API settings from the shared provider-key source.'
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

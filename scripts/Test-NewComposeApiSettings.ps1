#Requires -Version 7.0
# Companion test for New-ComposeApiSettings.ps1. File fixtures only; no Docker or cluster is touched.
# Exit 0 when every case passes, 1 otherwise.

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'home/_home-test-lib.ps1')

$target = Join-Path $PSScriptRoot 'New-ComposeApiSettings.ps1'
$root = New-TestRoot
$repoRoot = Split-Path -Parent $PSScriptRoot
$templatePath = Join-Path (Join-Path $repoRoot 'docker') 'api.settings.example.json'

$sourceFull = Join-Path $root 'source.env'
Write-TestText -Path $sourceFull -Text "OMDB_API_KEY=omdb-test-value`nTMDB_API_KEY=tmdb-test-value`n"
$sourceNoOmdb = Join-Path $root 'source-no-omdb.env'
Write-TestText -Path $sourceNoOmdb -Text "TMDB_API_KEY=tmdb-test-value`n"
$sourceNoTmdb = Join-Path $root 'source-no-tmdb.env'
Write-TestText -Path $sourceNoTmdb -Text "OMDB_API_KEY=omdb-test-value`n"
$sourceBlankTmdb = Join-Path $root 'source-blank-tmdb.env'
Write-TestText -Path $sourceBlankTmdb -Text "OMDB_API_KEY=omdb-test-value`nTMDB_API_KEY=   `n"

function Invoke-ComposeSettingsCase {
    param([string]$SettingsPath, [string[]]$ExtraArgs = @())
    $output = @(& pwsh -NoProfile -File $target -SettingsPath $SettingsPath @ExtraArgs 2>&1 | ForEach-Object { [string]$_ })
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output -join "`n") }
}

# Case 1: fresh render from the template and the shared source.
$settings1 = Join-Path $root 'case-fresh/api.settings.json'
$r1 = Invoke-ComposeSettingsCase -SettingsPath $settings1 -ExtraArgs @('-SecretSourcePath', $sourceFull)
Assert-Case 'fresh render: exit 0' ($r1.ExitCode -eq 0) "exit $($r1.ExitCode)"
Assert-Case 'fresh render: settings file created' (Test-Path -LiteralPath $settings1)
if (Test-Path -LiteralPath $settings1) {
    $s1 = Get-Content -LiteralPath $settings1 -Raw | ConvertFrom-Json
    Assert-Case 'fresh render: OMDb key copied from source' ([string]$s1.Omdb.ApiKey -eq 'omdb-test-value')
    Assert-Case 'fresh render: TMDb key copied from source' ([string]$s1.Tmdb.ApiKey -eq 'tmdb-test-value')
    Assert-Case 'fresh render: TMDb enablement from the template' (([bool]$s1.Tmdb.Enabled) -eq $true)
    Assert-Case 'fresh render: compose key set is test' ([string]$s1.ProviderKeys.KeySet -eq 'test')
    Assert-Case 'fresh render: connection string from the template' ([string]$s1.ConnectionStrings.DefaultConnection -match '^Host=postgres')
    Assert-Case 'fresh render: template has no secret values' ((Get-Content -LiteralPath $templatePath -Raw) -notmatch 'omdb-test-value|tmdb-test-value')
}

# Case 2: existing settings keep their non-provider values; provider keys are replaced from the source.
$settings2 = Join-Path $root 'case-keep/api.settings.json'
$existing = Get-Content -LiteralPath $templatePath -Raw | ConvertFrom-Json
$existing.ConnectionStrings.DefaultConnection = 'Host=postgres;Database=lamuflix;Username=local-user;Password=fix-pw'
$existing.RabbitMq.Password = 'local-rabbit'
$existing.Library.RootPath = '/media/videos'
$existing.Subtitles.OpenSubtitles.ApiKey = 'subs-test-value'
$existing.Omdb.ApiKey = 'stale-omdb'
$existing.Tmdb.ApiKey = 'stale-tmdb'
New-Item -ItemType Directory -Force -Path (Split-Path $settings2) | Out-Null
[System.IO.File]::WriteAllText($settings2, ($existing | ConvertTo-Json -Depth 20), [System.Text.UTF8Encoding]::new($false))
$r2 = Invoke-ComposeSettingsCase -SettingsPath $settings2 -ExtraArgs @('-SecretSourcePath', $sourceFull)
Assert-Case 'existing settings: exit 0' ($r2.ExitCode -eq 0) "exit $($r2.ExitCode)"
if ($r2.ExitCode -eq 0) {
    $s2 = Get-Content -LiteralPath $settings2 -Raw | ConvertFrom-Json
    Assert-Case 'existing settings: OMDb key replaced from source' ([string]$s2.Omdb.ApiKey -eq 'omdb-test-value')
    Assert-Case 'existing settings: TMDb key replaced from source' ([string]$s2.Tmdb.ApiKey -eq 'tmdb-test-value')
    Assert-Case 'existing settings: connection string preserved' ([string]$s2.ConnectionStrings.DefaultConnection -eq 'Host=postgres;Database=lamuflix;Username=local-user;Password=fix-pw')
    Assert-Case 'existing settings: broker password preserved' ([string]$s2.RabbitMq.Password -eq 'local-rabbit')
    Assert-Case 'existing settings: library root preserved' ([string]$s2.Library.RootPath -eq '/media/videos')
    Assert-Case 'existing settings: subtitle key preserved' ([string]$s2.Subtitles.OpenSubtitles.ApiKey -eq 'subs-test-value')
    Assert-Case 'existing settings: compose key set is test' ([string]$s2.ProviderKeys.KeySet -eq 'test')
}

# Case 3: OMDb key absent from the source; fail closed without touching the settings file.
$before3 = Get-Content -LiteralPath $settings2 -Raw
$r3 = Invoke-ComposeSettingsCase -SettingsPath $settings2 -ExtraArgs @('-SecretSourcePath', $sourceNoOmdb)
Assert-Case 'missing OMDb key: exit 1' ($r3.ExitCode -eq 1) "exit $($r3.ExitCode)"
Assert-Case 'missing OMDb key: names the key, not a value' ($r3.Output -match 'OMDB_API_KEY')
Assert-Case 'missing OMDb key: settings file untouched' ((Get-Content -LiteralPath $settings2 -Raw) -eq $before3)

# Case 4: TMDb key absent; explicit empty key, exit 0.
$settings4 = Join-Path $root 'case-no-tmdb/api.settings.json'
$r4 = Invoke-ComposeSettingsCase -SettingsPath $settings4 -ExtraArgs @('-SecretSourcePath', $sourceNoTmdb)
Assert-Case 'missing TMDb key: exit 0' ($r4.ExitCode -eq 0) "exit $($r4.ExitCode)"
if ($r4.ExitCode -eq 0) {
    $s4 = Get-Content -LiteralPath $settings4 -Raw | ConvertFrom-Json
    Assert-Case 'missing TMDb key: explicit empty key' (([string]$s4.Tmdb.ApiKey) -eq '')
    Assert-Case 'missing TMDb key: key property present' ($s4.Tmdb.PSObject.Properties.Name -contains 'ApiKey')
}

# Case 5: blank TMDb key renders the same explicit empty key.
$settings5 = Join-Path $root 'case-blank-tmdb/api.settings.json'
$r5 = Invoke-ComposeSettingsCase -SettingsPath $settings5 -ExtraArgs @('-SecretSourcePath', $sourceBlankTmdb)
Assert-Case 'blank TMDb key: exit 0' ($r5.ExitCode -eq 0) "exit $($r5.ExitCode)"
if ($r5.ExitCode -eq 0) {
    $s5 = Get-Content -LiteralPath $settings5 -Raw | ConvertFrom-Json
    Assert-Case 'blank TMDb key: explicit empty key' (([string]$s5.Tmdb.ApiKey) -eq '')
}

# Case 6: missing source file fails closed.
$settings6 = Join-Path $root 'case-no-source/api.settings.json'
$r6 = Invoke-ComposeSettingsCase -SettingsPath $settings6 -ExtraArgs @('-SecretSourcePath', (Join-Path $root 'does-not-exist.env'))
Assert-Case 'missing source file: exit 1' ($r6.ExitCode -eq 1) "exit $($r6.ExitCode)"
Assert-Case 'missing source file: nothing written' (-not (Test-Path -LiteralPath $settings6))

# Case 7: omitting -SecretSourcePath fails closed without touching the settings file.
$settings7 = Join-Path $root 'case-no-source-arg/api.settings.json'
$r7 = Invoke-ComposeSettingsCase -SettingsPath $settings7
Assert-Case 'missing -SecretSourcePath: exit 1' ($r7.ExitCode -eq 1) "exit $($r7.ExitCode)"
Assert-Case 'missing -SecretSourcePath: names the argument, not a home fallback' ($r7.Output -match 'SecretSourcePath')
Assert-Case 'missing -SecretSourcePath: nothing written' (-not (Test-Path -LiteralPath $settings7))

# Case 8: blank -SecretSourcePath fails the same way.
$settings8 = Join-Path $root 'case-blank-source-arg/api.settings.json'
$r8 = Invoke-ComposeSettingsCase -SettingsPath $settings8 -ExtraArgs @('-SecretSourcePath', '   ')
Assert-Case 'blank -SecretSourcePath: exit 1' ($r8.ExitCode -eq 1) "exit $($r8.ExitCode)"
Assert-Case 'blank -SecretSourcePath: nothing written' (-not (Test-Path -LiteralPath $settings8))

# Case 9: the ignored Compose settings path is the script default and stays out of Git.
$gitignore = Get-Content -LiteralPath (Join-Path $repoRoot '.gitignore') -Raw
Assert-Case 'compose settings path stays git-ignored' ($gitignore -match '(?m)^/docker/api\.settings\.json\s*$')

$combined = $r1.Output + "`n" + $r2.Output + "`n" + $r3.Output + "`n" + $r4.Output + "`n" + $r5.Output + "`n" + $r6.Output + "`n" + $r7.Output + "`n" + $r8.Output
Assert-Case 'secret values are never printed' (-not ($combined.Contains('omdb-test-value') -or $combined.Contains('tmdb-test-value') -or $combined.Contains('subs-test-value') -or $combined.Contains('fix-pw') -or $combined.Contains('local-rabbit')))

Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
$failures = Get-CaseFailureCount
Write-Host ''
Write-Host "New-ComposeApiSettings companion: $failures failed"
if ($failures -gt 0) { exit 1 }
exit 0

param(
    [string]$Marker,
    [string]$ChildScript,
    [string]$ChildMarker,
    [int]$ChildDelaySeconds = 4,
    [int]$Seconds = 30
)

Start-Process -FilePath "pwsh" -ArgumentList @(
    "-NoProfile",
    "-NonInteractive",
    "-File",
    $ChildScript,
    $ChildMarker,
    $ChildDelaySeconds
) | Out-Null

Set-Content -Path $Marker -Value "started"
Start-Sleep -Seconds $Seconds

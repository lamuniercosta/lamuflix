param(
    [string]$Marker,
    [int]$Seconds = 30
)

Set-Content -Path $Marker -Value "started"
Start-Sleep -Seconds $Seconds

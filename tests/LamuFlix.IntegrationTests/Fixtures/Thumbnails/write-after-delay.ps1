param(
    [string]$Marker,
    [int]$Seconds = 4
)

Start-Sleep -Seconds $Seconds
Set-Content -Path $Marker -Value "child"

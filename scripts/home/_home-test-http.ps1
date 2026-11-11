#Requires -Version 7.0
# Test helper for Test-GetHomeStatus.ps1: a minimal HTTP responder on loopback.
# Answers every request with 200, except /api/genres when -GenresStatus is given.
# Runs until it is stopped by the caller. Not an operator script.

param(
    [Parameter(Mandatory)][int]$Port,
    [int]$GenresStatus = 200
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
$listener.Start()
try {
    while ($true) {
        $client = $listener.AcceptTcpClient()
        try {
            $stream = $client.GetStream()
            $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::ASCII, $false, 1024, $true)
            $requestLine = [string]$reader.ReadLine()
            while ($true) {
                $header = $reader.ReadLine()
                if ([string]::IsNullOrEmpty($header)) { break }
            }
            $status = 200
            if ($requestLine -match ' /api/genres ' -and $GenresStatus -ne 200) { $status = $GenresStatus }
            $reason = 'OK'
            if ($status -ne 200) { $reason = 'Internal Server Error' }
            $body = 'ok'
            $text = "HTTP/1.1 $status $reason`r`nContent-Type: text/plain`r`nContent-Length: $($body.Length)`r`nConnection: close`r`n`r`n$body"
            $bytes = [System.Text.Encoding]::ASCII.GetBytes($text)
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Flush()
        }
        catch {
            # A probe that connects and closes without a request is expected; keep serving.
        }
        finally {
            $client.Close()
        }
    }
}
finally {
    $listener.Stop()
}

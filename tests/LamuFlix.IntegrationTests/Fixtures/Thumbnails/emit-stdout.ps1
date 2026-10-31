param(
    [int]$Count
)

$stream = [Console]::OpenStandardOutput()
$bytes = [byte[]]::new($Count)
[Array]::Fill($bytes, [byte][char]'x')
$stream.Write($bytes, 0, $bytes.Length)
$stream.Flush()
exit 0

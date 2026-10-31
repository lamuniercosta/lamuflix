param(
    [int]$Count
)

$bytes = [byte[]]::new($Count)
[Array]::Fill($bytes, [byte][char]'x')

$stdout = [Console]::OpenStandardOutput()
$stdout.Write($bytes, 0, $bytes.Length)
$stdout.Flush()

$stderr = [Console]::OpenStandardError()
$stderr.Write($bytes, 0, $bytes.Length)
$stderr.Flush()

exit 0

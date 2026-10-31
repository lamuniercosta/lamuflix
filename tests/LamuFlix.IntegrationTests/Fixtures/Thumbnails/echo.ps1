param(
    [string]$Message = "hello"
)

[Console]::Out.WriteLine($Message)
[Console]::Error.WriteLine("err-" + $Message)
exit 0

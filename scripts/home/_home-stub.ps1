# Test double for kind, kubectl and docker. Launched by the stub files that New-StubBin writes.
# Every call is appended to $env:LAMUFLIX_STUB_LOG as "<tool> <arguments>".
# $env:LAMUFLIX_STUB_RULES points at a JSON array of rules, checked in order; the first match wins:
#   tool            "kind", "kubectl", "docker" or "*"
#   match           regex tested against the joined arguments
#   exit            exit code (default 0)
#   stdout          text written to stdout
#   stdoutSequence  answers returned in turn on successive matches (the last one repeats)
#   createFile      path to create, so a fake Job can leave a dump behind
# A call that matches no rule exits 0 with no output.

$tool = [string]$args[0]
$rest = @($args | Select-Object -Skip 1 | ForEach-Object { [string]$_ })
$line = $rest -join ' '

if ($env:LAMUFLIX_STUB_LOG) {
    Add-Content -LiteralPath $env:LAMUFLIX_STUB_LOG -Value "$tool $line"
}

$exitCode = 0
$out = $null
if ($env:LAMUFLIX_STUB_RULES -and (Test-Path -LiteralPath $env:LAMUFLIX_STUB_RULES)) {
    $rules = @(Get-Content -LiteralPath $env:LAMUFLIX_STUB_RULES -Raw | ConvertFrom-Json)
    for ($i = 0; $i -lt $rules.Count; $i++) {
        $rule = $rules[$i]
        if ($rule.tool -ne '*' -and $rule.tool -ne $tool) { continue }
        if (-not ($line -match [string]$rule.match)) { continue }
        if ($null -ne $rule.exit) { $exitCode = [int]$rule.exit }
        if ($rule.stdoutSequence) {
            $counter = "$($env:LAMUFLIX_STUB_LOG).seq.$i"
            $n = 0
            if (Test-Path -LiteralPath $counter) { $n = [int](Get-Content -LiteralPath $counter -Raw) }
            Set-Content -LiteralPath $counter -Value ($n + 1)
            $seq = @($rule.stdoutSequence)
            $out = [string]$seq[[Math]::Min($n, $seq.Count - 1)]
        }
        elseif ($null -ne $rule.stdout) {
            $out = [string]$rule.stdout
        }
        if ($rule.createFile) {
            Set-Content -LiteralPath ([string]$rule.createFile) -Value 'stub dump'
        }
        break
    }
}

if ($null -ne $out) { Write-Output $out }
exit $exitCode

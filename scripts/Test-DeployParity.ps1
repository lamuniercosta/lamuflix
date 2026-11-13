#!/usr/bin/env pwsh
# Parity check between docker-compose.yml and the rendered home overlay (build spec 7.6).
#
# The rendered overlay comes from `kubectl kustomize --load-restrictor LoadRestrictionsNone`,
# which runs offline. `kubectl apply --dry-run=client -o json -k` needs API discovery and
# cannot run without a cluster, so it is not used. Both inputs are read by the YAML subset
# reader in this file, so no YAML module is required.
#
# Exit 0 when every check passes. Exit 1 when a check fails or an input cannot be read.

[CmdletBinding()]
param(
    [string]$ComposePath,
    [string]$OverlayPath,
    [string]$RenderedPath,
    [string]$KubectlPath = 'kubectl'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $ComposePath) { $ComposePath = Join-Path $repoRoot 'docker-compose.yml' }
if (-not $OverlayPath) { $OverlayPath = Join-Path $repoRoot 'deploy/k8s/overlays/home' }

$script:Results = [System.Collections.Generic.List[object]]::new()

function Add-Check {
    param([string]$Name, [bool]$Ok, [string]$Detail = '')
    $script:Results.Add([pscustomobject]@{ Name = $Name; Ok = $Ok; Detail = $Detail })
}

function Get-YamlIndent {
    param([string]$Raw)
    return $Raw.Length - $Raw.TrimStart(' ').Length
}

function Find-YamlLine {
    param($Cur)
    while ($Cur.Pos -lt $Cur.Lines.Count) {
        $raw = $Cur.Lines[$Cur.Pos]
        if ($raw -match '^\s*\t') { throw "YAML: tab indentation at line $($Cur.Pos + 1); use spaces" }
        $t = $raw.Trim()
        if ($t -eq '' -or $t.StartsWith('#')) {
            $Cur.Pos = $Cur.Pos + 1
            continue
        }
        return $Cur.Pos
    }
    return -1
}

function ConvertFrom-YamlDoubleQuoted {
    param([string]$Token)
    $inner = $Token.Substring(1, $Token.Length - 2)
    $sb = [System.Text.StringBuilder]::new()
    for ($i = 0; $i -lt $inner.Length; $i++) {
        $c = $inner[$i]
        if ($c -eq '\') {
            $i++
            if ($i -ge $inner.Length) { throw "YAML: dangling escape in $Token" }
            switch ($inner[$i]) {
                'n' { [void]$sb.Append("`n") }
                't' { [void]$sb.Append("`t") }
                '"' { [void]$sb.Append('"') }
                '\' { [void]$sb.Append('\') }
                '/' { [void]$sb.Append('/') }
                default { throw "YAML: unsupported escape in $Token" }
            }
        }
        elseif ($c -eq '"') { throw "YAML: unescaped quote in $Token" }
        else { [void]$sb.Append($c) }
    }
    return $sb.ToString()
}

function ConvertFrom-YamlSingleQuoted {
    param([string]$Token)
    $inner = $Token.Substring(1, $Token.Length - 2)
    if ($inner -match "(?<!')'(?!')") { throw "YAML: unescaped quote in $Token" }
    return $inner.Replace("''", "'")
}

function Split-YamlFlow {
    param([string]$Inner)
    $parts = [System.Collections.Generic.List[string]]::new()
    if ($Inner.Trim() -eq '') { return ,$parts }
    $sb = [System.Text.StringBuilder]::new()
    $quote = [char]0
    for ($i = 0; $i -lt $Inner.Length; $i++) {
        $c = $Inner[$i]
        if ($quote -eq [char]0) {
            if ($c -eq ',') {
                $parts.Add($sb.ToString().Trim())
                [void]$sb.Clear()
                continue
            }
            if ($c -eq '"' -or $c -eq "'") { $quote = $c }
            [void]$sb.Append($c)
        }
        elseif ($quote -eq '"' -and $c -eq '\') {
            [void]$sb.Append($c)
            $i++
            if ($i -lt $Inner.Length) { [void]$sb.Append($Inner[$i]) }
        }
        elseif ($c -eq $quote) {
            $quote = [char]0
            [void]$sb.Append($c)
        }
        else { [void]$sb.Append($c) }
    }
    if ($quote -ne [char]0) { throw "YAML: unterminated quote in flow sequence: $Inner" }
    $parts.Add($sb.ToString().Trim())
    return ,$parts
}

function ConvertTo-YamlValue {
    param([string]$Text)
    $s = $Text.Trim()
    if ($s -eq '') { return $null }
    if ($s.StartsWith('"')) {
        if ($s.Length -lt 2 -or -not $s.EndsWith('"')) { throw "YAML: unterminated double-quoted scalar: $s" }
        return ConvertFrom-YamlDoubleQuoted $s
    }
    if ($s.StartsWith("'")) {
        if ($s.Length -lt 2 -or -not $s.EndsWith("'")) { throw "YAML: unterminated single-quoted scalar: $s" }
        return ConvertFrom-YamlSingleQuoted $s
    }
    if ($s.StartsWith('[')) {
        if (-not $s.EndsWith(']')) { throw "YAML: unterminated flow sequence: $s" }
        $items = [System.Collections.Generic.List[object]]::new()
        foreach ($part in (Split-YamlFlow $s.Substring(1, $s.Length - 2))) {
            $items.Add((ConvertTo-YamlValue $part))
        }
        return ,$items
    }
    if ($s -eq '{}') { return ,([ordered]@{}) }
    if ($s.StartsWith('{')) { throw "YAML: unsupported flow mapping: $s" }
    $hash = $s.IndexOf(' #')
    if ($hash -ge 0) { $s = $s.Substring(0, $hash).TrimEnd() }
    return $s
}

function Split-YamlEntry {
    param([string]$Text)
    if ($Text.StartsWith('"') -or $Text.StartsWith("'")) {
        $pattern = if ($Text.StartsWith('"')) { '^"(?:[^"\\]|\\.)*"' } else { "^'(?:[^']|'')*'" }
        $m = [regex]::Match($Text, $pattern)
        if (-not $m.Success) { return $null }
        $after = $Text.Substring($m.Length).TrimStart()
        if (-not $after.StartsWith(':')) { return $null }
        return [pscustomobject]@{ Key = [string](ConvertTo-YamlValue $m.Value); Rest = $after.Substring(1).Trim() }
    }
    if ($Text.StartsWith('[') -or $Text.StartsWith('{')) { return $null }
    $idx = $Text.IndexOf(': ')
    if ($idx -lt 0 -and $Text.EndsWith(':')) { $idx = $Text.Length - 1 }
    if ($idx -lt 1) { return $null }
    return [pscustomobject]@{ Key = $Text.Substring(0, $idx).Trim(); Rest = $Text.Substring($idx + 1).Trim() }
}

function Read-YamlBlockScalar {
    param($Cur, [int]$ParentIndent)
    $lines = [System.Collections.Generic.List[string]]::new()
    $blockIndent = -1
    while ($Cur.Pos -lt $Cur.Lines.Count) {
        $raw = $Cur.Lines[$Cur.Pos]
        if ($raw.Trim() -eq '') {
            $lines.Add('')
            $Cur.Pos = $Cur.Pos + 1
            continue
        }
        $ind = Get-YamlIndent $raw
        if ($blockIndent -lt 0) {
            if ($ind -le $ParentIndent) { break }
            $blockIndent = $ind
        }
        if ($ind -lt $blockIndent) { break }
        $lines.Add($raw.Substring($blockIndent))
        $Cur.Pos = $Cur.Pos + 1
    }
    while ($lines.Count -gt 0 -and $lines[$lines.Count - 1] -eq '') { $lines.RemoveAt($lines.Count - 1) }
    return (($lines -join "`n") + "`n")
}

function Read-YamlSequence {
    param($Cur, [int]$Indent)
    $list = [System.Collections.Generic.List[object]]::new()
    while ($true) {
        $i = Find-YamlLine $Cur
        if ($i -lt 0) { break }
        $raw = $Cur.Lines[$i]
        if ((Get-YamlIndent $raw) -ne $Indent) { break }
        $text = $raw.Trim()
        if ($text -ne '-' -and -not $text.StartsWith('- ')) { break }
        $after = $text.Substring(1)
        $rest = $after.Trim()
        if ($rest -eq '') {
            $Cur.Pos = $i + 1
            $j = Find-YamlLine $Cur
            $item = $null
            if ($j -ge 0 -and (Get-YamlIndent $Cur.Lines[$j]) -gt $Indent) { $item = Read-YamlNode $Cur ($Indent + 1) }
            $list.Add($item)
            continue
        }
        if ($rest -match '^[|>][-+]?$') { $Cur.Pos = $i + 1; $list.Add((Read-YamlBlockScalar $Cur $Indent)); continue }
        $childIndent = $Indent + 1 + ($after.Length - $after.TrimStart().Length)
        $Cur.Lines[$i] = (' ' * $childIndent) + $rest
        $list.Add((Read-YamlNode $Cur $childIndent))
    }
    return ,$list
}

function Read-YamlMapping {
    param($Cur, [int]$Indent)
    $map = [ordered]@{}
    while ($true) {
        $i = Find-YamlLine $Cur
        if ($i -lt 0) { break }
        $raw = $Cur.Lines[$i]
        if ((Get-YamlIndent $raw) -ne $Indent) { break }
        $text = $raw.Trim()
        if ($text -eq '-' -or $text.StartsWith('- ')) { break }
        $entry = Split-YamlEntry $text
        if ($null -eq $entry) { throw "YAML: expected 'key: value' at line $($i + 1): $text" }
        if ($map.Contains($entry.Key)) { throw "YAML: duplicate key '$($entry.Key)' at line $($i + 1)" }
        $Cur.Pos = $i + 1
        $value = $null
        if ($entry.Rest -eq '') {
            $j = Find-YamlLine $Cur
            if ($j -ge 0) {
                $childIndent = Get-YamlIndent $Cur.Lines[$j]
                $childText = $Cur.Lines[$j].Trim()
                if ($childIndent -gt $Indent) { $value = Read-YamlNode $Cur ($Indent + 1) }
                elseif ($childIndent -eq $Indent -and ($childText -eq '-' -or $childText.StartsWith('- '))) {
                    $value = Read-YamlSequence $Cur $Indent
                }
            }
        }
        elseif ($entry.Rest -match '^[|>][-+]?$') { $value = Read-YamlBlockScalar $Cur $Indent }
        else { $value = ConvertTo-YamlValue $entry.Rest }
        $map[$entry.Key] = $value
    }
    return ,$map
}

function Read-YamlNode {
    param($Cur, [int]$MinIndent)
    $i = Find-YamlLine $Cur
    if ($i -lt 0) { return $null }
    $raw = $Cur.Lines[$i]
    $indent = Get-YamlIndent $raw
    if ($indent -lt $MinIndent) { return $null }
    $text = $raw.Trim()
    if ($text -eq '-' -or $text.StartsWith('- ')) { return ,(Read-YamlSequence $Cur $indent) }
    if ($null -ne (Split-YamlEntry $text)) { return ,(Read-YamlMapping $Cur $indent) }
    $Cur.Pos = $i + 1
    return ConvertTo-YamlValue $text
}

function ConvertFrom-SubsetYaml {
    param([string]$Text)
    $documents = [System.Collections.Generic.List[object]]::new()
    $current = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($Text -split "`r?`n")) {
        if ($line.TrimEnd() -eq '---') {
            $documents.Add($current.ToArray())
            $current = [System.Collections.Generic.List[string]]::new()
            continue
        }
        $current.Add($line)
    }
    $documents.Add($current.ToArray())
    $parsed = [System.Collections.Generic.List[object]]::new()
    foreach ($docLines in $documents) {
        $cur = [pscustomobject]@{ Lines = [string[]]$docLines; Pos = 0 }
        $node = Read-YamlNode $cur 0
        $left = Find-YamlLine $cur
        if ($left -ge 0) { throw "YAML: unparsed content at line $($left + 1): $($cur.Lines[$left].Trim())" }
        if ($null -ne $node) { $parsed.Add($node) }
    }
    return ,$parsed
}

function Get-Field {
    param($Node, [string]$Name)
    if ($Node -is [System.Collections.IDictionary] -and $Node.Contains($Name)) { return $Node[$Name] }
    return $null
}

function Get-DocByKindName {
    param($Docs, [string[]]$Kinds, [string]$Name)
    foreach ($doc in $Docs) {
        if ($Kinds -contains (Get-Field $doc 'kind') -and [string](Get-Field (Get-Field $doc 'metadata') 'name') -eq $Name) {
            return $doc
        }
    }
    return $null
}

function Get-Containers {
    param($Doc)
    $pod = Get-Field (Get-Field (Get-Field $Doc 'spec') 'template') 'spec'
    $list = Get-Field $pod 'containers'
    if ($null -eq $list) { return }
    return @($list)
}

function Get-EnvMap {
    param($Container)
    $map = @{}
    $envs = Get-Field $Container 'env'
    if ($null -ne $envs) {
        foreach ($e in $envs) { $map[[string](Get-Field $e 'name')] = $e }
    }
    return $map
}

function Get-ComposeService {
    param($Compose, [string]$Name)
    return (Get-Field (Get-Field $Compose 'services') $Name)
}

function Get-ComposeEnv {
    param($Service)
    $result = [ordered]@{}
    $env = Get-Field $Service 'environment'
    if ($env -is [System.Collections.IDictionary]) {
        foreach ($k in $env.Keys) {
            $v = $env[$k]
            if ($null -eq $v) { $result[$k] = $null } else { $result[$k] = [string]$v }
        }
    }
    elseif ($null -ne $env) {
        foreach ($item in $env) {
            $s = [string]$item
            $eq = $s.IndexOf('=')
            if ($eq -lt 0) { $result[$s] = $null } else { $result[$s.Substring(0, $eq)] = $s.Substring($eq + 1) }
        }
    }
    return $result
}

function Get-ImageParts {
    param([string]$Ref)
    $r = [string]$Ref
    if ($r -match '^\$\{[A-Za-z_][A-Za-z0-9_]*:-(.+)\}$') { $r = $matches[1] }
    $digest = $null
    $at = $r.IndexOf('@')
    if ($at -ge 0) {
        $digest = $r.Substring($at + 1)
        $r = $r.Substring(0, $at)
    }
    $tag = $null
    $colon = $r.LastIndexOf(':')
    if ($colon -gt $r.LastIndexOf('/')) {
        $tag = $r.Substring($colon + 1)
        $r = $r.Substring(0, $colon)
    }
    return [pscustomobject]@{ Repo = $r; Tag = $tag; Digest = $digest }
}

function Invoke-Kustomize {
    if (-not (Get-Command $KubectlPath -ErrorAction SilentlyContinue)) { throw "kubectl not found: $KubectlPath" }
    $errFile = [System.IO.Path]::GetTempFileName()
    try {
        $out = & $KubectlPath kustomize --load-restrictor LoadRestrictionsNone $OverlayPath 2>$errFile
        $code = $LASTEXITCODE
        if ($code -ne 0) {
            $err = (Get-Content -LiteralPath $errFile -Raw)
            throw "kubectl kustomize failed with exit $code for $OverlayPath`: $err"
        }
        return (@($out | ForEach-Object { [string]$_ }) -join "`n")
    }
    finally {
        Remove-Item -LiteralPath $errFile -ErrorAction SilentlyContinue
    }
}

# Compose service name -> rendered workload name, where they differ.
$aliases = @{ 'ollama-model-init' = 'ollama' }
$workloadKinds = @('StatefulSet', 'Deployment')
$imageServices = @('postgres', 'rabbitmq', 'ollama')
$appImages = [ordered]@{
    api = 'ghcr.io/lamuniercosta/lamuflix-api'
    web = 'ghcr.io/lamuniercosta/lamuflix-web'
}
# Rendered workloads that exist only in the home cluster, with no Compose service by design.
$clusterOnlyWorkloads = @('aspire-dashboard')
# The API exports telemetry to the in-cluster Aspire dashboard, and only the API sets it.
$otelEndpointName = 'OTEL_EXPORTER_OTLP_ENDPOINT'
$otelEndpointValue = 'http://aspire-dashboard:18889'
$webNodePort = '30100'

try {
    $composeDocs = ConvertFrom-SubsetYaml (Get-Content -LiteralPath $ComposePath -Raw)
    if ($composeDocs.Count -lt 1) { throw "compose file is empty: $ComposePath" }
    $compose = $composeDocs[0]
    $composeServices = Get-Field $compose 'services'
    if ($null -eq $composeServices) { throw "compose file has no services section: $ComposePath" }
    $composeNames = @($composeServices.Keys)

    if ($RenderedPath) { $renderedText = Get-Content -LiteralPath $RenderedPath -Raw }
    else { $renderedText = Invoke-Kustomize }
    $docs = ConvertFrom-SubsetYaml $renderedText
    $workloads = @($docs | Where-Object { $workloadKinds -contains (Get-Field $_ 'kind') })
    $renderedNames = @($workloads | ForEach-Object { [string](Get-Field (Get-Field $_ 'metadata') 'name') })

    $targetOf = @{}
    foreach ($name in $composeNames) {
        $target = if ($aliases.ContainsKey($name)) { $aliases[$name] } else { $name }
        $targetOf[$name] = $target
        Add-Check "compose '$name' has a workload" ($null -ne (Get-DocByKindName $workloads $workloadKinds $target)) "no StatefulSet or Deployment named '$target' in the rendered output"
    }
    foreach ($n in $renderedNames) {
        if ($clusterOnlyWorkloads -contains $n) {
            Add-Check "workload '$n' is cluster-only (no compose service by design)" $true
            continue
        }
        Add-Check "workload '$n' has a compose service" ($targetOf.Values -contains $n) "rendered workload '$n' has no compose service"
    }

    if ($composeNames -contains 'ollama-model-init') {
        $ollamaWl = Get-DocByKindName $workloads $workloadKinds 'ollama'
        $pullText = ''
        if ($null -ne $ollamaWl) {
            $c0 = @(Get-Containers $ollamaWl)[0]
            $pullText = (@(Get-Field $c0 'command') + @(Get-Field $c0 'args') | ForEach-Object { [string]$_ }) -join ' '
        }
        Add-Check "model pull (ollama-model-init) runs in the ollama workload" ($pullText.Contains('ollama-init.sh')) 'the ollama container does not run ollama-init.sh'
    }

    $apiComposeEnv = Get-ComposeEnv (Get-ComposeService $compose 'api')
    $endpointRaw = [string]$apiComposeEnv['Ollama__Endpoint']
    $endpoint = [regex]::Match($endpointRaw, '^https?://([^/:]+):(\d+)')
    $ollamaHost = if ($endpoint.Success) { $endpoint.Groups[1].Value } else { '' }
    $ollamaPort = if ($endpoint.Success) { $endpoint.Groups[2].Value } else { '' }
    Add-Check "compose api Ollama__Endpoint names host 'ollama' with a port" ($ollamaHost -eq 'ollama' -and $ollamaPort -ne '') "compose Ollama__Endpoint is '$endpointRaw'"

    $webComposePorts = @(Get-Field (Get-ComposeService $compose 'web') 'ports')
    $webPort = if ($webComposePorts.Count -gt 0) { ([string]$webComposePorts[0] -split ':')[-1] } else { '' }

    $expectedPorts = [ordered]@{
        postgres = '5432'
        rabbitmq = '5672'
        ollama   = $ollamaPort
        api      = '8080'
        web      = $webPort
        'aspire-dashboard' = '18889'
    }

    foreach ($name in $expectedPorts.Keys) {
        $expected = $expectedPorts[$name]
        $svc = Get-DocByKindName $docs @('Service') $name
        Add-Check "Service '$name' exists" ($null -ne $svc) "no Service named '$name' in the rendered output"
        if ($null -ne $svc) {
            $svcPorts = @()
            foreach ($p in @(Get-Field (Get-Field $svc 'spec') 'ports')) { $svcPorts += [string](Get-Field $p 'port') }
            Add-Check "Service '$name' port is $expected" ([bool]($expected -and ($svcPorts -contains $expected))) "Service ports: $($svcPorts -join ', ')"
        }
        $wl = Get-DocByKindName $workloads $workloadKinds $name
        if ($null -ne $wl) {
            $containerPorts = @()
            foreach ($c in (Get-Containers $wl)) {
                foreach ($p in @(Get-Field $c 'ports')) { $containerPorts += [string](Get-Field $p 'containerPort') }
            }
            Add-Check "container of '$name' listens on $expected" ([bool]($expected -and ($containerPorts -contains $expected))) "container ports: $($containerPorts -join ', ')"
        }
    }

    $webSvc = Get-DocByKindName $docs @('Service') 'web'
    if ($null -ne $webSvc) {
        $webSpec = Get-Field $webSvc 'spec'
        $nodePorts = @(@(Get-Field $webSpec 'ports') | ForEach-Object { [string](Get-Field $_ 'nodePort') })
        $svcType = [string](Get-Field $webSpec 'type')
        Add-Check "Service 'web' is NodePort $webNodePort" ($svcType -eq 'NodePort' -and ($nodePorts -contains $webNodePort)) "type $svcType, nodePorts $($nodePorts -join ', ')"
    }

    foreach ($name in $imageServices) {
        $composeSvc = Get-ComposeService $compose $name
        $wl = Get-DocByKindName $workloads $workloadKinds $name
        if ($null -eq $composeSvc -or $null -eq $wl) { continue }
        $cImage = Get-ImageParts ([string](Get-Field $composeSvc 'image'))
        $containers = @(Get-Containers $wl)
        if ($containers.Count -eq 0) {
            Add-Check "image of '$name' present" $false 'workload has no containers'
            continue
        }
        $rImage = Get-ImageParts ([string](Get-Field $containers[0] 'image'))
        Add-Check "image repository of '$name' matches compose" ($rImage.Repo -eq $cImage.Repo) "compose $($cImage.Repo), manifests $($rImage.Repo)"
        if ($cImage.Tag) {
            Add-Check "image tag of '$name' matches compose when manifests set one" ((-not $rImage.Tag) -or ($rImage.Tag -eq $cImage.Tag)) "compose $($cImage.Tag), manifests $($rImage.Tag)"
        }
        if ($cImage.Digest) {
            Add-Check "image digest of '$name' matches compose" ($rImage.Digest -eq $cImage.Digest) "compose $($cImage.Digest), manifests $($rImage.Digest)"
        }
        else {
            Add-Check "image digest of '$name' pinned in manifests" ([bool]($rImage.Digest -match '^sha256:[0-9a-f]{64}$')) "manifests digest: $($rImage.Digest)"
        }
    }

    foreach ($app in $appImages.Keys) {
        $wl = Get-DocByKindName $workloads $workloadKinds $app
        if ($null -eq $wl) { continue }
        $containers = @(Get-Containers $wl)
        $img = if ($containers.Count -gt 0) { Get-ImageParts ([string](Get-Field $containers[0] 'image')) } else { Get-ImageParts '' }
        Add-Check "image repository of '$app' is $($appImages[$app])" ($img.Repo -eq $appImages[$app]) "manifests image $($img.Repo)"
        Add-Check "image digest of '$app' pinned in manifests" ([bool]($img.Digest -match '^sha256:[0-9a-f]{64}$')) "manifests digest: $($img.Digest)"
    }

    $apiWl = Get-DocByKindName $workloads $workloadKinds 'api'
    if ($null -ne $apiWl) {
        $apiContainers = @(Get-Containers $apiWl)
        if ($apiContainers.Count -gt 0) {
            $apiEnv = Get-EnvMap $apiContainers[0]
            $renderedEndpoint = if ($apiEnv.ContainsKey('Ollama__Endpoint')) { [string](Get-Field $apiEnv['Ollama__Endpoint'] 'value') } else { '' }
            Add-Check 'api Ollama__Endpoint in manifests matches compose' ($renderedEndpoint -eq $endpointRaw) "compose '$endpointRaw', manifests '$renderedEndpoint'"
        }
    }

    $otelOccurrences = ([regex]::Matches($renderedText, [regex]::Escape($otelEndpointName))).Count
    $otelValue = ''
    $apiOtelContainers = @(Get-Containers $apiWl)
    if ($apiOtelContainers.Count -gt 0) {
        $apiOtelEnv = Get-EnvMap $apiOtelContainers[0]
        if ($apiOtelEnv.ContainsKey($otelEndpointName)) { $otelValue = [string](Get-Field $apiOtelEnv[$otelEndpointName] 'value') }
    }
    Add-Check "$otelEndpointName is set once, on api, to the in-cluster dashboard" ($otelOccurrences -eq 1 -and $otelValue -eq $otelEndpointValue) "occurrences $otelOccurrences, api value '$otelValue'"
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$failed = 0
foreach ($r in $script:Results) {
    if ($r.Ok) {
        Write-Host "PASS  $($r.Name)"
    }
    else {
        $failed++
        Write-Host "FAIL  $($r.Name) - $($r.Detail)" -ForegroundColor Red
    }
}
Write-Host ''
Write-Host "$($script:Results.Count) checks, $failed failed"
if ($script:Results.Count -eq 0 -or $failed -gt 0) { exit 1 }
exit 0

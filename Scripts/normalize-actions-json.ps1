<#
.SYNOPSIS
  Normalizes GameData/Actions.json: deduplicates tags and repeated description phrases.
.DESCRIPTION
  - Tags: split on comma/semicolon, trim, deduplicate case-insensitively, rejoin as comma-separated.
  - Description: if the whole value is the same phrase repeated consecutively, replace with one occurrence.
#>

param(
    [string]$ActionsPath = "GameData\Actions.json",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$fullPath = Join-Path $rootDir $ActionsPath

if (-not (Test-Path $fullPath)) {
    Write-Error "File not found: $fullPath"
    exit 1
}

$jsonText = Get-Content -Raw -Path $fullPath -Encoding UTF8
$actions = $jsonText | ConvertFrom-Json

function Normalize-Tags {
    param([string]$tagsValue)
    if ([string]::IsNullOrWhiteSpace($tagsValue)) { return $tagsValue }
    $parts = $tagsValue -split '[,;]' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    # Deduplicate case-insensitively, output lowercase to match game usage
    $unique = $parts | ForEach-Object { $_.ToLower() } | Select-Object -Unique
    return ($unique -join ", ")
}

function Normalize-Description {
    param([string]$desc)
    if ([string]::IsNullOrWhiteSpace($desc) -or $desc.Length -lt 30) { return $desc }
    $maxPhraseLen = [Math]::Min(200, [int][Math]::Floor($desc.Length / 2))
    # Try shortest phrase first so we collapse to a single copy (e.g. "For the Next ACTION: +1 ACCURACY")
    # Pattern: (phrase + " ") repeated with space between: "A B A B" => (phrase + " ") * (n-1) + phrase => desc.Length = n*len + (n-1)
    for ($len = 15; $len -le $maxPhraseLen; $len++) {
        $total = $desc.Length + 1
        $unitLen = $len + 1
        if ($total % $unitLen -ne 0) { continue }
        $n = $total / $unitLen
        if ($n -lt 2) { continue }
        $phrase = $desc.Substring(0, $len)
        $built = ""
        for ($i = 0; $i -lt $n; $i++) {
            if ($i -gt 0) { $built += " " }
            $built += $phrase
        }
        if ($built -eq $desc) {
            return $phrase
        }
    }
    for ($len = 15; $len -le $maxPhraseLen; $len++) {
        $phrase = $desc.Substring(0, $len)
        $n = [int][Math]::Floor($desc.Length / $len)
        if ($n -lt 2) { continue }
        $fullRepeatLen = $n * $len
        $repeated = ""
        for ($i = 0; $i -lt $n; $i++) { $repeated += $phrase }
        # Exact repetition (no space between)
        if ($repeated -eq $desc) {
            return $phrase.Trim()
        }
        # Repetition plus a prefix of the phrase at the end
        $remainderLen = $desc.Length - $fullRepeatLen
        if ($remainderLen -gt 0 -and $remainderLen -lt $len -and $repeated -eq $desc.Substring(0, $fullRepeatLen)) {
            $remainder = $desc.Substring($fullRepeatLen)
            if ($phrase.StartsWith($remainder) -or $remainder -eq $phrase.Substring(0, [Math]::Min($remainderLen, $phrase.Length))) {
                return $phrase.Trim()
            }
        }
    }
    return $desc
}

$tagsNormalized = 0
$descriptionsNormalized = 0

foreach ($obj in $actions) {
    if ($obj.PSObject.Properties['tags']) {
        $oldTags = $obj.tags
        $newTags = Normalize-Tags -tagsValue $obj.tags
        if ($newTags -ne $oldTags) {
            $obj.tags = $newTags
            $tagsNormalized++
        }
    }
    if ($obj.PSObject.Properties['description']) {
        $oldDesc = $obj.description
        $newDesc = Normalize-Description -desc $obj.description
        if ($newDesc -ne $oldDesc) {
            $obj.description = $newDesc
            $descriptionsNormalized++
        }
    }
}

Write-Host "Tags normalized: $tagsNormalized"
Write-Host "Descriptions normalized: $descriptionsNormalized"

if (-not $WhatIf) {
    $options = @{
        Depth = 100
        Compress = $false
    }
    $newJson = $actions | ConvertTo-Json @options
    [System.IO.File]::WriteAllText($fullPath, $newJson, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Wrote $fullPath"
} else {
    Write-Host "WhatIf: no file written"
}

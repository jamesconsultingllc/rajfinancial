#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Post-processes an Expressium LivingDoc HTML report to set a custom <title>
    and inject a favicon as an embedded data URI.

.DESCRIPTION
    The Expressium.LivingDoc.ReqnrollPlugin generates a self-contained HTML
    report but hardcodes `<title>Expressium LivingDoc</title>` and does not
    include a favicon. This script rewrites the title and injects a base64-encoded
    favicon so the artifact stays a single self-contained file.

.PARAMETER HtmlPath
    Path to the generated LivingDoc.html file.

.PARAMETER FaviconPath
    Path to a .ico/.png file to embed as the favicon.

.PARAMETER Title
    The browser tab title to set.

.EXAMPLE
    ./scripts/postprocess-livingdoc.ps1 `
        -HtmlPath tests/IntegrationTests/bin/Release/net10.0/LivingDoc.html `
        -FaviconPath src/Client/public/favicon.ico `
        -Title 'RajFinancial Integration Tests'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $HtmlPath,
    [Parameter(Mandatory)] [string] $FaviconPath,
    [Parameter(Mandatory)] [string] $Title
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $HtmlPath)) {
    Write-Warning "LivingDoc HTML not found at '$HtmlPath' — skipping post-processing."
    return
}

if (-not (Test-Path -LiteralPath $FaviconPath)) {
    throw "Favicon not found at '$FaviconPath'."
}

$ext = [System.IO.Path]::GetExtension($FaviconPath).TrimStart('.').ToLowerInvariant()
$mime = switch ($ext) {
    'ico' { 'image/x-icon' }
    'png' { 'image/png' }
    'svg' { 'image/svg+xml' }
    default { throw "Unsupported favicon extension '.$ext'. Use .ico, .png, or .svg." }
}

$bytes = [System.IO.File]::ReadAllBytes($FaviconPath)
$b64 = [System.Convert]::ToBase64String($bytes)
$faviconLink = "<link rel=`"icon`" type=`"$mime`" href=`"data:$mime;base64,$b64`">"

$html = [System.IO.File]::ReadAllText($HtmlPath)

$encodedTitle = [System.Net.WebUtility]::HtmlEncode($Title)
$titlePattern = '<title>[^<]*</title>'
$replacementText = "<title>$encodedTitle</title>$faviconLink"

if ($html -notmatch $titlePattern) {
    throw "Could not locate <title> element in '$HtmlPath'."
}

$evaluator = [System.Text.RegularExpressions.MatchEvaluator] { param($m) $replacementText }
$regex = [regex]::new($titlePattern)
$updated = $regex.Replace($html, $evaluator, 1)
[System.IO.File]::WriteAllText($HtmlPath, $updated)

Write-Host "LivingDoc post-processed: title set to '$Title', favicon embedded from '$FaviconPath'."

#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pre-commit quality gate: runs JetBrains InspectCode on staged .cs files.

.DESCRIPTION
    Fails the commit when any warning-level Rider/Roslyn inspection fires in
    a staged file. Note-level suggestions are ignored.

    Bypass in an emergency with `git commit --no-verify`.
#>
[CmdletBinding()]
param(
    [string] $Solution = 'src/RajFinancial.sln',
    [string[]] $IgnoredRules = @()
)

$ErrorActionPreference = 'Stop'

$repoRoot = (& git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

$stagedFiles = & git diff --cached --name-only --diff-filter=ACMR | Where-Object { $_ -like '*.cs' }

if (-not $stagedFiles) {
    Write-Host "pre-commit: no staged .cs files - skipping inspection." -ForegroundColor DarkGray
    exit 0
}

if (-not (Get-Command jb -ErrorAction SilentlyContinue)) {
    Write-Warning "pre-commit: 'jb' (JetBrains CLI) not found on PATH."
    Write-Warning "Install with: dotnet tool install -g JetBrains.ReSharper.GlobalTools"
    Write-Warning "Skipping inspection."
    exit 0
}

Write-Host "pre-commit: inspecting $($stagedFiles.Count) staged .cs file(s)..." -ForegroundColor Cyan

$reportPath = Join-Path ([IO.Path]::GetTempPath()) "precommit-inspect-$([Guid]::NewGuid()).sarif"

# --include uses file-name wildcards; we use **/<leaf> so patterns match the solution layout.
$includeMask = ($stagedFiles | ForEach-Object { "**/" + (Split-Path $_ -Leaf) } | Sort-Object -Unique) -join ';'

$jbArgs = @(
    $Solution,
    "--output=$reportPath",
    "--include=$includeMask",
    '--no-build',
    '--verbosity=WARN'
)

$sw = [Diagnostics.Stopwatch]::StartNew()
& jb inspectcode @jbArgs | Out-Null
$sw.Stop()
Write-Host "pre-commit: inspection finished in $([int]$sw.Elapsed.TotalSeconds)s." -ForegroundColor DarkGray

if (-not (Test-Path $reportPath)) {
    Write-Warning "pre-commit: InspectCode did not produce a report; allowing commit."
    exit 0
}

$stagedSet = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$stagedFiles | ForEach-Object { [void]$stagedSet.Add(($_ -replace '\\', '/')) }

$sarif = Get-Content $reportPath -Raw | ConvertFrom-Json
Remove-Item $reportPath -Force -ErrorAction SilentlyContinue

$results = @($sarif.runs[0].results | Where-Object {
        $_.level -eq 'warning' -and
        $_.ruleId -notin $IgnoredRules
    })

# Only fail for issues in files that were actually staged (filename-wildcard
# include may match same-named files in other folders).
$failures = @($results | Where-Object {
        $uri = $_.locations[0].physicalLocation.artifactLocation.uri -replace '\\', '/'
        $stagedSet.Contains($uri)
    })

if ($failures.Count -eq 0) {
    Write-Host "pre-commit: no blocking issues in staged files. ✔" -ForegroundColor Green
    exit 0
}

Write-Host ""
Write-Host "pre-commit: $($failures.Count) blocking issue(s) in staged files:" -ForegroundColor Red
Write-Host ""

$failures | Sort-Object { $_.locations[0].physicalLocation.artifactLocation.uri } | ForEach-Object {
    $loc = $_.locations[0].physicalLocation
    $file = $loc.artifactLocation.uri
    $line = $loc.region.startLine
    "  {0}:{1}  [{2}]  {3}" -f $file, $line, $_.ruleId, $_.message.text
} | Write-Host

Write-Host ""
Write-Host "Fix the issues above and re-stage, or bypass with: git commit --no-verify" -ForegroundColor Yellow
exit 1

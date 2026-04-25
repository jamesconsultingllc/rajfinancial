#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pre-commit quality gate: runs ESLint --fix on staged TypeScript files in src/Client.

.DESCRIPTION
    Auto-fixes ESLint violations in staged .ts/.tsx files, then re-stages the
    corrected files. Fails the commit if any violations remain after auto-fix.

    Bypass in an emergency with `git commit --no-verify`.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = (& git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

$stagedFiles = & git diff --cached --name-only --diff-filter=ACMR |
    Where-Object { $_ -match '^src/Client/.*\.(ts|tsx)$' -and $_ -notmatch '\.d\.ts$' }

if (-not $stagedFiles) {
    Write-Host 'pre-commit: no staged TypeScript files in src/Client - skipping ESLint.' -ForegroundColor DarkGray
    exit 0
}

if (-not (Get-Command npx -ErrorAction SilentlyContinue)) {
    Write-Warning "pre-commit: 'npx' not found on PATH. Skipping ESLint."
    exit 0
}

Write-Host "pre-commit: linting $($stagedFiles.Count) staged TypeScript file(s)..." -ForegroundColor Cyan

# Absolute paths so ESLint finds files regardless of working directory
$absolutePaths = $stagedFiles | ForEach-Object { Join-Path $repoRoot $_ }

# Run from src/Client so ESLint resolves eslint.config.js and node_modules correctly
Push-Location (Join-Path $repoRoot 'src/Client')
try {
    # ESLint exit codes: 0 = clean, 1 = unfixable lint errors, 2 = config/fatal error
    & npx eslint --fix @absolutePaths
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($exitCode -eq 2) {
    Write-Host 'pre-commit: ESLint configuration error - cannot run lint check.' -ForegroundColor Red
    exit 1
}

# Re-stage files that ESLint auto-fixed
& git add @stagedFiles

if ($exitCode -eq 1) {
    Write-Host ''
    Write-Host 'pre-commit: ESLint found violations that could not be auto-fixed.' -ForegroundColor Red
    Write-Host 'Fix the issues above and re-stage, or bypass with: git commit --no-verify' -ForegroundColor Yellow
    exit 1
}

Write-Host 'pre-commit: ESLint passed. ✔' -ForegroundColor Green
exit 0

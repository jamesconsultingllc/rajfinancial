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

$stagedFiles = @(& git diff --cached --name-only --diff-filter=ACMR |
    Where-Object { $_ -match '^src/Client/.*\.(ts|tsx)$' -and $_ -notmatch '\.d\.ts$' })

if (-not $stagedFiles) {
    Write-Host 'pre-commit: no staged TypeScript files in src/Client - skipping ESLint.' -ForegroundColor DarkGray
    exit 0
}

if (-not (Get-Command npx -ErrorAction SilentlyContinue)) {
    Write-Warning "pre-commit: 'npx' not found on PATH. Skipping ESLint."
    exit 0
}

Write-Host "pre-commit: linting $($stagedFiles.Count) staged TypeScript file(s)..." -ForegroundColor Cyan

# Refuse to lint files with unstaged working-tree changes: ESLint --fix runs on
# the working tree, and `git add` afterwards would silently stage edits the
# developer never intended to include in this commit. Force them to be explicit.
$dirtyFiles = & git diff --name-only -- @stagedFiles
if ($LASTEXITCODE -ne 0) {
    Write-Host 'pre-commit: failed to inspect working-tree state via git diff.' -ForegroundColor Red
    exit 1
}
if ($dirtyFiles) {
    Write-Host ''
    Write-Host 'pre-commit: the following staged files also have UNSTAGED changes:' -ForegroundColor Red
    $dirtyFiles | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host ''
    Write-Host 'Auto-fix would re-stage those unstaged edits. Resolve before committing:' -ForegroundColor Yellow
    Write-Host '  - stash unstaged edits:  git stash push --keep-index -- <files>' -ForegroundColor Yellow
    Write-Host '  - or stage them:         git add <files>' -ForegroundColor Yellow
    Write-Host '  - or bypass entirely:    git commit --no-verify' -ForegroundColor Yellow
    exit 1
}

# Absolute paths so ESLint finds files regardless of working directory
$absolutePaths = @($stagedFiles | ForEach-Object { Join-Path $repoRoot $_ })

# Run from src/Client so ESLint resolves eslint.config.js and node_modules correctly
Push-Location (Join-Path $repoRoot 'src/Client')
try {
    # --no-install: never silently download ESLint from the registry during a
    # commit. Forces deterministic local execution and surfaces a clear error
    # when src/Client/node_modules hasn't been installed yet.
    # ESLint exit codes: 0 = clean, 1 = unfixable lint errors, 2 = config/fatal error
    & npx --no-install eslint --fix @absolutePaths
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

# npx exits non-zero (typically 1) when --no-install can't find the binary.
# Distinguish "ESLint is missing" from "ESLint ran and reported violations" by
# checking for node_modules/eslint before treating exitCode==1 as a lint error.
$eslintInstalled = Test-Path (Join-Path $repoRoot 'src/Client/node_modules/eslint/package.json')
if (-not $eslintInstalled) {
    Write-Host ''
    Write-Host 'pre-commit: ESLint is not installed under src/Client/node_modules.' -ForegroundColor Red
    Write-Host 'Run `npm ci` in src/Client to install dependencies, then retry the commit.' -ForegroundColor Yellow
    Write-Host '(Bypass in an emergency with: git commit --no-verify)' -ForegroundColor DarkGray
    exit 1
}

if ($exitCode -eq 2) {
    Write-Host 'pre-commit: ESLint configuration error - cannot run lint check.' -ForegroundColor Red
    exit 1
}

# Treat any non-zero, non-(1|2) exit code as a fatal failure. Examples include
# npx itself crashing, an OOM kill, or ESLint surfacing an internal error code
# we don't recognize. Do NOT `git add` in this case — the working tree state
# is unknown and we don't want to silently stage anything.
if ($exitCode -ne 0 -and $exitCode -ne 1) {
    Write-Host ''
    Write-Host "pre-commit: ESLint terminated unexpectedly (exit code $exitCode)." -ForegroundColor Red
    Write-Host 'Inspect the output above. Bypass in an emergency with: git commit --no-verify' -ForegroundColor Yellow
    exit 1
}

# Re-stage files that ESLint auto-fixed (only on success or recoverable lint
# violations — never on the unexpected-failure path above).
& git add @stagedFiles

if ($exitCode -eq 1) {
    Write-Host ''
    Write-Host 'pre-commit: ESLint found violations that could not be auto-fixed.' -ForegroundColor Red
    Write-Host 'Fix the issues above and re-stage, or bypass with: git commit --no-verify' -ForegroundColor Yellow
    exit 1
}

Write-Host 'pre-commit: ESLint passed. ✔' -ForegroundColor Green
exit 0

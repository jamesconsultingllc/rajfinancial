# scripts/dev-down.ps1 — tear down the local RAJ Financial dev stack.

[CmdletBinding()]
param(
    [switch]$Volumes
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $RepoRoot

# Compose still requires the env var to validate the file even on `down`.
if (-not $env:RAJFIN_DEV_MSSQL_SA_PASSWORD) {
    $env:RAJFIN_DEV_MSSQL_SA_PASSWORD = 'placeholder-for-down'
}

if ($Volumes) {
    Write-Host "==> Stopping stack AND removing volumes (data will be lost)..." -ForegroundColor Yellow
    docker compose -f docker-compose.dev.yml down --volumes
} else {
    Write-Host "==> Stopping stack (volumes preserved)..." -ForegroundColor Cyan
    docker compose -f docker-compose.dev.yml down
}

Write-Host "✅ Stack stopped." -ForegroundColor Green

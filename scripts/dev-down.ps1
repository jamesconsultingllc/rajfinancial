# scripts/dev-down.ps1 — tear down the local RAJ Financial dev stack.

[CmdletBinding()]
param(
    [switch]$Volumes
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $RepoRoot

# Compose still requires the env var to validate the file even on `down`.
# Set a placeholder ONLY for the duration of the compose invocation, then
# restore the prior value (or unset) so subsequent `dev-up` in the same
# shell still loads the real password from the secret store.
$hadSqlPassword = -not [string]::IsNullOrWhiteSpace($env:RAJFIN_DEV_MSSQL_SA_PASSWORD)
$originalSqlPassword = $env:RAJFIN_DEV_MSSQL_SA_PASSWORD

if (-not $hadSqlPassword) {
    $env:RAJFIN_DEV_MSSQL_SA_PASSWORD = 'placeholder-for-down'
}

try {
    if ($Volumes) {
        Write-Host "==> Stopping stack AND removing volumes (data will be lost)..." -ForegroundColor Yellow
        docker compose -f docker-compose.dev.yml down --volumes
    } else {
        Write-Host "==> Stopping stack (volumes preserved)..." -ForegroundColor Cyan
        docker compose -f docker-compose.dev.yml down
    }
} finally {
    if ($hadSqlPassword) {
        $env:RAJFIN_DEV_MSSQL_SA_PASSWORD = $originalSqlPassword
    } else {
        Remove-Item Env:\RAJFIN_DEV_MSSQL_SA_PASSWORD -ErrorAction SilentlyContinue
    }
}

Write-Host "✅ Stack stopped." -ForegroundColor Green

# scripts/dev-up.ps1 — bring up the local RAJ Financial dev stack.
# See docs/local-development.md for the full setup runbook.

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $RepoRoot

Write-Host "==> Checking prerequisites..." -ForegroundColor Cyan
& "$PSScriptRoot/check-prereqs.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Prereq check failed. Install missing tools and re-run."
    exit 1
}

Write-Host "==> Loading dev SA password from secrets store..." -ForegroundColor Cyan
if (-not $env:RAJFIN_DEV_MSSQL_SA_PASSWORD) {
    # Windows: prefer Credential Manager via cmdkey; fall back to env var
    # already set, otherwise prompt the user once and save it.
    $cred = $null
    try {
        $stored = cmdkey /list:rajfinancial-dev-mssql-sa 2>$null
        if ($stored -match 'rajfinancial-dev-mssql-sa') {
            # cmdkey doesn't expose the password — we use a small helper:
            # store the password in Windows Credential Manager via the
            # CredentialManager PowerShell module if available.
            if (Get-Module -ListAvailable -Name CredentialManager) {
                $cred = Get-StoredCredential -Target 'rajfinancial-dev-mssql-sa'
            }
        }
    } catch {
        $cred = $null
    }

    if ($cred) {
        $env:RAJFIN_DEV_MSSQL_SA_PASSWORD = $cred.GetNetworkCredential().Password
    } else {
        Write-Host @"
✗ No SA password found.

  Two options:

  1. Install the CredentialManager PowerShell module (recommended) and
     store the password once:

       Install-Module CredentialManager -Scope CurrentUser
       New-StoredCredential -Target rajfinancial-dev-mssql-sa ``
         -UserName sa -Password '<your-strong-dev-password>' ``
         -Persist LocalMachine

  2. Set the env var inline (for this shell only):

       `$env:RAJFIN_DEV_MSSQL_SA_PASSWORD = '<your-strong-dev-password>'

  SQL Server 2022 SA password rules: ≥8 chars, 3 of upper / lower / digit
  / non-alnum.
"@ -ForegroundColor Red
        exit 1
    }
}

Write-Host "==> Starting docker-compose stack (--wait for healthchecks)..." -ForegroundColor Cyan
docker compose -f docker-compose.dev.yml up -d --wait
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker compose up failed."
    exit $LASTEXITCODE
}

Write-Host "==> Stack health:" -ForegroundColor Cyan
docker compose -f docker-compose.dev.yml ps --format "table {{.Name}}`t{{.Status}}`t{{.Ports}}"

Write-Host "==> Running EF Core migrations against rajfin-sql..." -ForegroundColor Cyan
$apiDir = Join-Path $RepoRoot 'src/Api'
if ((Test-Path $apiDir) -and (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Push-Location $apiDir
    try {
        $efCheck = & dotnet ef --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            & dotnet ef database update
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "Migrations failed. Stack is up but DB is not ready."
                Write-Warning "Run manually: cd src/Api; dotnet ef database update"
            }
        } else {
            Write-Warning "dotnet-ef not installed; skipping migrations."
            Write-Warning "Install: dotnet tool install -g dotnet-ef"
        }
    } finally {
        Pop-Location
    }
}

Write-Host @"

✅ Local dev stack is ready.

Next steps:
  - Start API:    cd src/Api; func start
  - Start client: cd src/Client; npm run dev
  - Run tests:    dotnet test tests/IntegrationTests

To stop:           scripts/dev-down.ps1
To reset volumes:  scripts/dev-down.ps1 -Volumes
"@ -ForegroundColor Green

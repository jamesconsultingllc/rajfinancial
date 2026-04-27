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
    # Windows: try the CredentialManager PowerShell module first (if
    # installed). If neither the module nor an env var is set, print
    # setup instructions and exit — we deliberately don't prompt-and-save
    # here so the password choice/storage policy is explicit.
    $cred = $null
    try {
        if (Get-Module -ListAvailable -Name CredentialManager) {
            $cred = Get-StoredCredential -Target 'rajfinancial-dev-mssql-sa'
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
$migrationsFailed = $false
if ((Test-Path $apiDir) -and (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Push-Location $apiDir
    try {
        $efCheck = & dotnet ef --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            # IMPORTANT: DesignTimeDbContextFactory falls back to LocalDB
            # when no connection string is configured. LocalDB isn't
            # installed by default on a clean Windows box and doesn't
            # exist on macOS/Linux, so migrations would silently misroute
            # away from our docker container. Force the connection string
            # to point at the rajfin-sql container for the EF invocation.
            $efConn = "Server=localhost,1433;Database=RajFinancial_Dev;User Id=sa;Password=$($env:RAJFIN_DEV_MSSQL_SA_PASSWORD);TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true"
            $prevConn       = $env:ConnectionStrings__SqlConnectionString
            $prevValuesConn = $env:Values__SqlConnectionString
            $env:ConnectionStrings__SqlConnectionString = $efConn
            $env:Values__SqlConnectionString            = $efConn
            try {
                & dotnet ef database update
                if ($LASTEXITCODE -ne 0) {
                    $script:migrationsFailed = $true
                    Write-Warning "Migrations failed. Stack is up but DB is not ready."
                    Write-Warning "Run manually: cd src/Api; dotnet ef database update"
                }
            } finally {
                $env:ConnectionStrings__SqlConnectionString = $prevConn
                $env:Values__SqlConnectionString            = $prevValuesConn
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
  - Start API:    cd src/Api; func start --useHttps
  - Start client: cd src/Client; npm run dev
  - Run tests:    dotnet test tests/IntegrationTests

To stop:           scripts/dev-down.ps1
To reset volumes:  scripts/dev-down.ps1 -Volumes
"@ -ForegroundColor Green

if ($migrationsFailed) {
    Write-Host ""
    Write-Host "❌ Containers are up but database migrations failed." -ForegroundColor Red
    Write-Host "   Fix the migration error above before running integration tests." -ForegroundColor Red
    exit 2
}

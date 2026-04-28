# scripts/dev-up.ps1 — bring up the local RAJ Financial dev stack.
# See docs/local-development.md for the full setup runbook.

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $RepoRoot

Write-Host "==> Checking prerequisites..." -ForegroundColor Cyan
# Don't hard-exit on prereq fail — dev-up auto-installs the dotnet-ef
# and Azure Functions Core Tools gaps below. Surface the report so the
# contributor sees what was missing, but only abort if check-prereqs
# itself errored (exit code >= 2).
& "$PSScriptRoot/check-prereqs.ps1"
if ($LASTEXITCODE -ge 2) {
    Write-Error "Prereq check errored (exit $LASTEXITCODE). Aborting."
    exit $LASTEXITCODE
}

function Install-FuncCoreToolsIfMissing {
    if (Get-Command func -ErrorAction SilentlyContinue) {
        & func --version *>$null
        if ($LASTEXITCODE -eq 0) { return }
    }
    Write-Host "==> Azure Functions Core Tools (func) not found; installing..." -ForegroundColor Yellow

    if ($IsMacOS) {
        # Prefer brew (official path), fall back to npm if brew fails. The
        # brew formula needs a current Xcode; on stale-Xcode boxes the
        # build fails outright. npm install of azure-functions-core-tools
        # works fine on macOS in practice and avoids the Xcode dependency.
        $brewOk = $false
        if (Get-Command brew -ErrorAction SilentlyContinue) {
            & brew tap azure/functions *>&1 | Out-Host
            & brew install azure-functions-core-tools@4 *>&1 | Out-Host
            if ($LASTEXITCODE -eq 0) { $brewOk = $true }
        }
        if (-not $brewOk) {
            if (Get-Command npm -ErrorAction SilentlyContinue) {
                Write-Host "==> brew install failed or unavailable; falling back to npm." -ForegroundColor Yellow
                & npm install -g azure-functions-core-tools@4 --unsafe-perm true *>&1 | Out-Host
            } else {
                Write-Host "✗ Need brew or npm to install azure-functions-core-tools on macOS." -ForegroundColor Red
                exit 1
            }
        }
    }
    elseif ($IsWindows) {
        if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
            Write-Host "✗ winget is required to auto-install Functions Core Tools on Windows." -ForegroundColor Red
            Write-Host "  Install manually: https://learn.microsoft.com/azure/azure-functions/functions-run-local" -ForegroundColor Red
            exit 1
        }
        & winget install --silent --accept-source-agreements --accept-package-agreements Microsoft.Azure.FunctionsCoreTools *>&1 | Out-Host
    }
    else {
        # Linux fallback: npm-based install. npm is already a hard prereq.
        if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
            Write-Host "✗ npm is required to install azure-functions-core-tools on Linux." -ForegroundColor Red
            exit 1
        }
        & npm install -g azure-functions-core-tools@4 --unsafe-perm true *>&1 | Out-Host
    }

    if (-not (Get-Command func -ErrorAction SilentlyContinue)) {
        Write-Host "✗ Functions Core Tools install reported success but 'func' is still not on PATH." -ForegroundColor Red
        Write-Host "  Open a new shell or update PATH, then re-run dev-up." -ForegroundColor Red
        exit 1
    }
    & func --version *>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ 'func' installed but didn't execute cleanly. Investigate manually." -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Azure Functions Core Tools installed." -ForegroundColor Green
}

Install-FuncCoreToolsIfMissing

Write-Host "==> Loading dev SA password from secrets store..." -ForegroundColor Cyan
if (-not $env:RAJFIN_DEV_MSSQL_SA_PASSWORD) {
    $resolved = $null

    if ($IsMacOS) {
        # macOS Keychain via the security CLI.
        $sec = Get-Command security -ErrorAction SilentlyContinue
        if ($sec) {
            $pw = & security find-generic-password -s 'rajfinancial-dev-mssql-sa' -a $env:USER -w 2>$null
            if ($LASTEXITCODE -eq 0 -and $pw) { $resolved = $pw.Trim() }
        }
    }
    elseif ($IsLinux) {
        # Linux: prefer `pass`, fall back to `secret-tool` (libsecret).
        if (Get-Command pass -ErrorAction SilentlyContinue) {
            $pw = & pass show 'rajfinancial-dev-mssql-sa' 2>$null
            if ($LASTEXITCODE -eq 0 -and $pw) { $resolved = ($pw | Select-Object -First 1).Trim() }
        }
        if (-not $resolved -and (Get-Command secret-tool -ErrorAction SilentlyContinue)) {
            $pw = & secret-tool lookup service rajfinancial-dev-mssql-sa account sa 2>$null
            if ($LASTEXITCODE -eq 0 -and $pw) { $resolved = $pw.Trim() }
        }
    }
    else {
        # Windows: CredentialManager PowerShell module (if installed).
        try {
            if (Get-Module -ListAvailable -Name CredentialManager) {
                $cred = Get-StoredCredential -Target 'rajfinancial-dev-mssql-sa'
                if ($cred) { $resolved = $cred.GetNetworkCredential().Password }
            }
        } catch { }
    }

    if ($resolved) {
        $env:RAJFIN_DEV_MSSQL_SA_PASSWORD = $resolved
    } else {
        Write-Host @"
✗ No SA password found.

  Store it once in your OS secret store, or set the env var inline:

  macOS (Keychain):
    security add-generic-password ``
      -a `"`$USER`" -s rajfinancial-dev-mssql-sa -w '<password>' -U

  Windows (CredentialManager PowerShell module):
    Install-Module CredentialManager -Scope CurrentUser
    New-StoredCredential -Target rajfinancial-dev-mssql-sa ``
      -UserName sa -Password '<password>' -Persist LocalMachine

  Linux:
    pass insert rajfinancial-dev-mssql-sa
      (or)
    secret-tool store --label='RAJ Financial dev SA' ``
      service rajfinancial-dev-mssql-sa account sa

  Inline (any platform, this shell only):
    `$env:RAJFIN_DEV_MSSQL_SA_PASSWORD = '<password>'

  SQL Server 2022 SA password rules: ≥8 chars, 3 of upper / lower / digit / non-alnum.
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
if ((-not (Test-Path $apiDir)) -or (-not (Get-Command dotnet -ErrorAction SilentlyContinue))) {
    Write-Host "✗ dotnet SDK or src/Api directory missing — cannot apply migrations." -ForegroundColor Red
    exit 1
}
Push-Location $apiDir
try {
    $efCheck = & dotnet ef --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "==> dotnet-ef not found; installing as a global tool..." -ForegroundColor Yellow
        & dotnet tool install -g dotnet-ef *>&1 | Out-Host
        $installExit = $LASTEXITCODE
        # Make ~/.dotnet/tools visible to this process even on a clean box
        # where the user hasn't logged out/in since installing the SDK.
        $toolsPath = if ($IsWindows) {
            Join-Path $env:USERPROFILE '.dotnet\tools'
        } else {
            Join-Path $env:HOME '.dotnet/tools'
        }
        if ((Test-Path $toolsPath) -and ($env:PATH -notlike "*$toolsPath*")) {
            $sep = [System.IO.Path]::PathSeparator
            $env:PATH = "$toolsPath$sep$env:PATH"
        }
        & dotnet ef --version *>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "✗ Failed to install dotnet-ef (or it isn't on PATH after install)." -ForegroundColor Red
            Write-Host "  Try manually: dotnet tool install -g dotnet-ef" -ForegroundColor Red
            Write-Host "  Then ensure '$toolsPath' is on your PATH and re-run dev-up." -ForegroundColor Red
            exit 1
        }
        Write-Host "✓ dotnet-ef installed." -ForegroundColor Green
    }
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
} finally {
    Pop-Location
}

Write-Host @"

✅ Local dev stack is ready.

Next steps:
  - Start API:    cd src/Api; func start --useHttps
  - Start client: cd src/Client; npm run dev
  - Run tests:    dotnet test tests/IntegrationTests

To stop:           pwsh ./scripts/dev-down.ps1
To reset volumes:  pwsh ./scripts/dev-down.ps1 -Volumes
"@ -ForegroundColor Green

if ($migrationsFailed) {
    Write-Host ""
    Write-Host "❌ Containers are up but database migrations failed." -ForegroundColor Red
    Write-Host "   Fix the migration error above before running integration tests." -ForegroundColor Red
    exit 2
}

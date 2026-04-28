# Local Development

This is the authoritative setup runbook for running the RAJ Financial
stack on a developer workstation. If you can `git clone` and follow this
doc top-to-bottom, you should reach a green
`dotnet test tests/IntegrationTests` in well under 10 minutes (after
toolchain install).

> **Stack reference:** `docs/features/01-platform-infrastructure.md`
> is the source-of-truth document for what the platform _is_. This doc
> is the source-of-truth for how to _run it locally_.

---

## 1. Prerequisites

Install once per machine. Versions are minimums; newer is generally fine.

| Tool                              | Min version | Windows                                | macOS                              | Linux                                                       |
| --------------------------------- | ----------- | -------------------------------------- | ---------------------------------- | ----------------------------------------------------------- |
| **Docker Desktop**                | 24.0        | `winget install Docker.DockerDesktop`  | `brew install --cask docker`       | follow [docs.docker.com](https://docs.docker.com/engine/install/) |
| **.NET SDK**                      | 10.0        | `winget install Microsoft.DotNet.SDK.10` | `brew install --cask dotnet-sdk`   | `apt install dotnet-sdk-10.0`                               |
| **Node.js**                       | 22.x        | `winget install OpenJS.NodeJS.LTS`     | `brew install node@22`             | `nvm install 22`                                            |
| **Azure Functions Core Tools v4** _(auto-installed by `dev-up.ps1`)_ | 4.0.5413 | `winget install Microsoft.Azure.FunctionsCoreTools` | `brew tap azure/functions && brew install azure-functions-core-tools@4` | `npm i -g azure-functions-core-tools@4` |
| **PowerShell 7+**                 | 7.4         | preinstalled / `winget install Microsoft.PowerShell` | `brew install --cask powershell` | follow [aka.ms/powershell](https://aka.ms/powershell)       |
| **Azure CLI**                     | 2.60        | `winget install Microsoft.AzureCLI`    | `brew install azure-cli`           | `curl -sL https://aka.ms/InstallAzureCLIDeb | bash`         |
| **GitHub CLI**                    | 2.50        | `winget install GitHub.cli`            | `brew install gh`                  | follow [cli.github.com](https://cli.github.com)             |
| **EF Core CLI** _(auto-installed by `dev-up.ps1`)_ | 10.0 | `dotnet tool install -g dotnet-ef` | same                               | same                                                        |

Run the bundled validator any time:

```pwsh
pwsh ./scripts/check-prereqs.ps1
```

It prints a green/red matrix and exits non-zero on any required miss.

---

## 2. First-time setup

```bash
git clone https://github.com/jamesconsultingllc/rajfinancial
cd rajfinancial
dotnet restore src/RajFinancial.sln
cd src/Client && npm install && cd ../..
```

### 2a. Generate and store a dev SA password

The local SQL Server runs in Docker with a strong dev SA password.
Generate one and stash it in a supported secrets store so future sessions
can pick it up automatically. `dev-up.ps1` checks **Bitwarden CLI first**
(works on all platforms), then falls back to the OS-native store.

**Bitwarden CLI (cross-platform, recommended):**

```bash
# One-time setup (any platform):
bw login                            # interactive
export BW_SESSION=$(bw unlock --raw)   # bash / zsh
# pwsh:    $env:BW_SESSION = bw unlock --raw

# Generate a SQL-complexity-compliant password and store it.
PW=$(bw generate --passphrase --words 4 --separator '-')
PW="${PW}#A1z"   # guarantees ≥ 3 of 4 SQL complexity classes

# Create the item (login type, name = rajfinancial-dev-mssql-sa).
echo '{"type":1,"name":"rajfinancial-dev-mssql-sa","login":{"username":"sa","password":"'"$PW"'"}}' \
  | bw encode | bw create item
```

Each new shell only needs `BW_SESSION` re-set (`bw unlock --raw`).
`dev-up.ps1` reads the password via `bw get password rajfinancial-dev-mssql-sa`.

**macOS (Keychain):**

```bash
PW=$(LC_ALL=C tr -dc 'A-HJ-NP-Za-km-z2-9' </dev/urandom | head -c 28)
PW="${PW}#A1z"   # guarantees ≥ 3 of 4 SQL complexity classes
security add-generic-password \
  -a "$USER" -s rajfinancial-dev-mssql-sa -w "$PW" -U \
  -D "RAJ Financial dev SQL Server SA password (local docker-compose)"
```

**Windows (Credential Manager via PowerShell module):**

```powershell
Install-Module CredentialManager -Scope CurrentUser
$pw = -join ((48..57) + (65..90) + (97..122) + (33,35,36,37,42,43,45) |
  Get-Random -Count 28 | ForEach-Object { [char]$_ })
$pw = "${pw}#A1z"
New-StoredCredential -Target 'rajfinancial-dev-mssql-sa' `
  -UserName 'sa' -Password $pw -Persist LocalMachine
Write-Host "Stored. Length: $($pw.Length)"
```

**Linux:** use Bitwarden CLI (above), `pass`, or `secret-tool` (libsecret).
If you keep the secret in a manager `dev-up.ps1` doesn't natively support,
just export `RAJFIN_DEV_MSSQL_SA_PASSWORD` in your shell init.

### 2b. Configure `appsettings.local.json`

```bash
cp tests/IntegrationTests/appsettings.local.json.example \
   tests/IntegrationTests/appsettings.local.json
```

Open the new file and fill in:

- `ConnectionStrings:SqlConnectionString` — paste your dev SA password into
  the placeholder spot.
- `Entra:*` values — pull from the Entra portal or ask your team lead.

The real `appsettings.local.json` is gitignored.

---

## 3. Bring up the local services

Single command, idempotent, prints what it did:

```pwsh
pwsh ./scripts/dev-up.ps1
```

Behind the scenes this:

1. Runs `check-prereqs` — bails fast if your toolchain is wrong.
2. Loads the dev SA password from the OS secret store.
3. `docker compose -f docker-compose.dev.yml up -d --wait` — waits on
   container healthchecks before returning.
4. `dotnet ef database update` — applies migrations to the dev DB.
5. Prints a "you're ready, here's what to start next" summary.

The compose file defines two containers:

| Container         | Image                                                            | Ports         | Volume                  |
| ----------------- | ---------------------------------------------------------------- | ------------- | ----------------------- |
| `rajfin-sql`      | `mcr.microsoft.com/mssql/server:2022-CU24-GDR1-ubuntu-22.04`     | `1433`        | `rajfin-sqlserver-data` |
| `rajfin-azurite`  | `mcr.microsoft.com/azure-storage/azurite:3.35.0`                 | `10000-10002` | `rajfin-azurite-data`   |

Both have active healthchecks (SQL runs `SELECT 1` via `sqlcmd`, Azurite
TCP-probes its blob port) so `--wait` actually means "ready", not just
"started".

To stop:

```pwsh
pwsh ./scripts/dev-down.ps1            # keeps volumes (data persists)
pwsh ./scripts/dev-down.ps1 -Volumes   # also wipes volumes (clean slate)
```

---

## 4. Run the API

```bash
cd src/Api
func start --useHttps
```

The `--useHttps` flag is **important**: the integration tests in
`tests/IntegrationTests` default `FunctionsHost:BaseUrl` to
`https://localhost:7071`. Running plain `func start` (HTTP only) will
break those tests until you override the BaseUrl in your
`appsettings.local.json`.

With HTTPS the host listens on `https://localhost:7071`. The
function app exposes two health endpoints:

- `https://localhost:7071/api/health/live`  — liveness probe
- `https://localhost:7071/api/health/ready` — readiness (checks DB,
  Redis, etc.)

The first time you run `func start --useHttps`, the Functions host
generates a self-signed dev cert; accept it / trust it as needed for
your platform. `curl -k` is fine for spot-checks.

Environment variables come from `local.settings.json` (under `src/Api/`).
On a fresh checkout `pwsh ./scripts/dev-up.ps1` seeds it for you from the
checked-in template at `src/Api/local.settings.json.example` (it never
overwrites an existing file). If you edit `local.settings.json` while
`func start --useHttps` is already running, stop and restart the host so
the new settings load.

---

## 5. Run the client

```bash
cd src/Client
npm run dev
```

Vite serves on `http://localhost:5173` by default. MSAL is configured to
authenticate against the Entra External ID tenant defined in
`src/Client/.env.local`.

If MSAL gets stuck in a redirect loop, see
`docs/plans/2026-03-02-msal-redirect-loop-fix-design.md`.

---

## 6. Run tests

| Suite             | Command                                    | What it needs            |
| ----------------- | ------------------------------------------ | ------------------------ |
| Unit              | `dotnet test tests/Api.Tests`              | nothing external         |
| Architecture      | `dotnet test tests/Architecture.Tests`     | nothing external         |
| **Integration**   | `dotnet test tests/IntegrationTests`       | localhost: full Docker stack + `appsettings.local.json` (unsigned local JWTs); remote/deployed endpoint also requires Entra ROPC test users |
| Acceptance (e2e)  | `cd tests/e2e && npm ci && npm run playwright:install && npm test` | running API + client + Node deps + Playwright browsers |

**First-time Playwright setup:**

```bash
cd tests/e2e
npm ci
npm run playwright:install
```

---

## 7. Entra ROPC test users

Integration tests authenticate via ROPC against an Entra External ID
tenant. The tenant must have a public-client app registration with the
ROPC flow enabled, and three test users (Administrator / Client / Advisor)
created and password-set.

The provisioning is one-time, and the existing PowerShell scripts in
`scripts/` handle it:

- `scripts/setup-entra-oidc.ps1` — high-level orchestration.
- `scripts/configure-user-flows.ps1` — user-flow configuration.
- `scripts/configure-entra-app-roles.ps1` — app-role assignment.
- `scripts/check-user-flows.ps1` — verify a tenant is wired up correctly.
- `scripts/README-ENTRA-OIDC.md` — narrative walkthrough of the above.

If you don't already have access to the dev tenant, ask Rudy.

---

## 8. Troubleshooting

### Integration tests all fail with "Login failed for user 'sa'"

`RAJFIN_DEV_MSSQL_SA_PASSWORD` isn't set in your shell, or doesn't match
what's stored in the SQL container's volume. Either:

- Re-run `pwsh ./scripts/dev-up.ps1` (it re-loads the env var); or
- `pwsh ./scripts/dev-down.ps1 -Volumes` to wipe the volume, then `pwsh ./scripts/dev-up.ps1`
  again — the new password takes effect on a fresh init.

### `rajfin-sql` is `unhealthy`

Check the container logs:

```bash
docker logs rajfin-sql --tail 100
```

The most common causes are:

- Apple Silicon: first boot under emulation can take 60s+. The healthcheck
  has a `start_period: 60s`; if your hardware is slower, raise it.
- Password rejected for not meeting SQL Server complexity rules
  (≥8 chars, 3 of 4 of upper/lower/digit/non-alnum). The
  `2a` recipe satisfies this.

### `rajfin-azurite` is `unhealthy`

The Azurite TCP healthcheck uses a Node-based `net.connect` probe (Node
is always present in the official Azurite image). If your Azurite image
diverges (custom build, non-standard distro), update the healthcheck in
`docker-compose.dev.yml` accordingly.

### "Cannot resolve database 'RajFinancial_Dev'"

Migrations haven't run. From repo root:

```bash
cd src/Api
dotnet ef database update
```

### `func start` complains about `local.settings.json`

`src/Api/local.settings.json` is gitignored. On a fresh checkout
`pwsh ./scripts/dev-up.ps1` seeds it from
`src/Api/local.settings.json.example`; it does **not** overwrite an
existing file. If you ran `dev-up.ps1` and the file is still missing,
copy the template manually:

```bash
cp src/Api/local.settings.json.example src/Api/local.settings.json
```

Then replace the placeholder password / Entra GUIDs with real values
(at minimum: `ConnectionStrings__SqlConnectionString` and
`SqlConnectionString` should both contain your dev SA password).

---

## 9. CI parity

CI's `Build and Test` workflow runs Unit + Architecture tests only — it
does **not** run Integration tests. AB#662 leaves that as-is for now;
CI parity for integration tests is a separate (future) work item, since
it needs a hosted SQL + Azurite (or per-job Docker services in GHA).

If you want CI to fail on a regression that integration tests would catch,
gate the PR locally first.

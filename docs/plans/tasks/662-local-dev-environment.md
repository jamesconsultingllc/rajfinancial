# AB#662 — Local Development Environment

**Status:** In progress
**Owner:** Rudy James
**Branch:** `feature/662-local-dev-environment`
**Parent:** (TBD — operational/devex umbrella; not gated by Feature #396)

## Problem

A clean checkout of `develop` cannot run the integration test suite. All 77
tests in `tests/IntegrationTests/` fail wholesale because the project has
hard runtime dependencies on:

- A reachable SQL Server (port 1433) with an `RAJFinancial` (or test) database
- Azurite (Azure Storage emulator) on ports 10000/10001/10002
- An `appsettings.local.json` carrying the SQL connection string and Entra
  ROPC test-user metadata, which is intentionally **not** committed

Today, the only way to know any of this is to read `tests/IntegrationTests/appsettings.json`'s
comment that says "set it via `appsettings.local.json` (nested under
`ConnectionStrings`)" — and even that doesn't tell you to spin up SQL or
where to get one.

`tests/README.md` is also stack-stale (says .NET 9.0; we're on .NET 10) and
silent on the Docker/Azurite/Entra test-user dance.

## Goal

Make local development reproducible from a clean clone. A new contributor
(human or AI agent) should go from `git clone` → green integration test
run in **under 10 minutes**, by following one document.

## Non-goals

- Replacing CI's existing test runs. CI stays as-is; this is for local dev.
- Productionising the local Docker stack. Dev SA password is committed via
  Keychain reference; not for production.
- Auto-provisioning Entra test users. Existing `scripts/*.ps1` already do
  this; we just point at them.

## Deliverables

### 1. `docker-compose.dev.yml` (repo root)

Two services, healthchecked, persistent volumes:

| Service          | Image                                                              | Ports         | Volume                  |
| ---------------- | ------------------------------------------------------------------ | ------------- | ----------------------- |
| `rajfin-sql`     | Pinned `mcr.microsoft.com/mssql/server` tag (see `docker-compose.dev.yml`) | `1433`        | `rajfin-sqlserver-data` |
| `rajfin-azurite` | Pinned `mcr.microsoft.com/azure-storage/azurite` tag (see `docker-compose.dev.yml`) | `10000-10002` | `rajfin-azurite-data`   |

**Note:** The checked-in `docker-compose.dev.yml` is the source of truth for
exact image versions. Tags are pinned (no `:latest` / `:2022-latest`) so the
local environment is reproducible; bumping versions is a deliberate edit to
the compose file.

**SA password:** read from env var `RAJFIN_DEV_MSSQL_SA_PASSWORD` (no default).
Developers set it once via the bring-up script, which can pull it from a
supported secrets store. Lookup order:

1. **Bitwarden CLI (`bw`)** — cross-platform; requires `BW_SESSION` set
   (typically via `bw unlock --raw`).
2. OS-native fallback: macOS Keychain (`security`), Windows Credential
   Manager (`CredentialManager` PowerShell module), or Linux
   `pass` / `secret-tool`.

If you keep the secret in a manager not in the list above, export
`RAJFIN_DEV_MSSQL_SA_PASSWORD` manually before running the script. Never committed.

**Healthcheck (SQL):** invokes `sqlcmd` to actually execute `SELECT 1`
against `master`. The default mssql container ships without `/opt/mssql-tools18`
on path; we use the explicit path. (Lesson from `structura-sql` running
"unhealthy" 2 days straight: a passive TCP-listen healthcheck is not enough.)

**Healthcheck (Azurite):** TCP probe on 10000 — Azurite has no HTTP health
endpoint and shipping curl into the image is wasteful.

### 2. `scripts/dev-up.ps1`

Cross-platform bring-up via PowerShell 7+. Steps:

1. Source the SA password from secrets store, export as
   `RAJFIN_DEV_MSSQL_SA_PASSWORD` for the compose run.
2. Run `scripts/check-prereqs` first — bail if toolchain is wrong.
3. `docker compose -f docker-compose.dev.yml up -d --wait` (uses healthchecks).
4. `dotnet ef database update` against the dev SQL.
5. Optionally seed test-user mappings (idempotent script, no-op if present).
6. Print: "Stack ready. Start API with `cd src/Api && func start`. Start
   client with `cd src/Client && npm run dev`. Run integration tests with
   `dotnet test tests/IntegrationTests`."

`scripts/dev-down.ps1` — symmetric teardown
(`docker compose down` with optional `-Volumes` flag).

### 3. `scripts/check-prereqs.ps1`

Pinned-minimum validator. Checks:

| Tool                          | Min version  | Detection                                                |
| ----------------------------- | ------------ | -------------------------------------------------------- |
| Docker                        | 24+          | `docker --version`                                       |
| .NET SDK                      | 10.0         | `dotnet --version`                                       |
| Node.js                       | 22.x         | `node --version`                                         |
| Azure Functions Core Tools v4 | 4.0.5413+    | `func --version`                                         |
| PowerShell 7+                 | 7.4          | `pwsh --version`                                         |
| Azure CLI                     | 2.60+        | `az --version`                                           |
| GitHub CLI                    | 2.50+        | `gh --version`                                           |
| EF Core CLI                   | 10.0         | `dotnet ef --version`                                    |
| pwsh-flavored sqlcmd (opt)    | 18.0+        | `sqlcmd -?` (helpful for ad-hoc DB poking, not required) |

Output: green tick / red cross matrix + total pass/fail summary.
Exit 0 when all pass, non-zero otherwise. The bring-up script gates on this.

### 4. `tests/IntegrationTests/appsettings.local.json.example`

Committed template, copy to `appsettings.local.json` (already gitignored).
Contains placeholder values + comments mapping each field to where to find
the real one (Keychain service name, Entra portal section, etc.).

### 5. `docs/local-development.md` (new, authoritative)

The single document that ties it together. Sections:

1. Prerequisites — version-pinned matrix, install instructions per OS.
2. First-time setup — clone, npm install, dotnet restore, generate dev SA
   password (with explicit `security add-generic-password` / Windows
   Credential Manager equivalents).
3. Running the local stack — `scripts/dev-up`, what it does, troubleshooting.
4. Running the API — `func start`, environment variables, expected endpoints.
5. Running the client — `npm run dev`, MSAL config gotchas.
6. Running tests — Unit / Integration / Acceptance, with the integration
   tests being the exact reproduction target for AB#662.
7. ROPC test users — link to `scripts/configure-user-flows.ps1` etc.
8. Troubleshooting — top failures with fixes:
   - `structura-sql` unhealthy → use `rajfin-sql` from the new compose.
   - Integration tests fail "Login failed for user 'sa'" → SA password not
     set in env.
   - "Cannot resolve database" → ran `dotnet ef database update`?
   - MSAL redirect loops → `docs/plans/2026-03-02-msal-redirect-loop-fix-design.md`.

### 6. `readme.md` rewrite

Currently stack-stale (Blazor/Syncfusion era). Slim it to:

- One-paragraph project description (current stack: React 18 + .NET 10
  Functions + Azure SQL + Plaid + Anthropic).
- "Get started locally" → link to `docs/local-development.md`.
- "Architecture" → link to `docs/features/01-platform-infrastructure.md`.
- "Contributing" → AGENT.md / CLAUDE.md links.
- License + badges.

Strip everything else.

### 7. `tests/README.md` minor refresh

Bump .NET 9.0 references to 10.0; add a top-level pointer to
`docs/local-development.md` for the prerequisite setup. Keep the rest as is
— it's a valuable reference for how to write tests, just stack-stale at
the top.

## Acceptance criteria

1. `scripts/check-prereqs` passes on a stock dev box with the listed
   toolchain installed.
2. `scripts/dev-up` brings up two healthy containers and seeds a usable
   `RajFinancial_Dev` database.
3. `dotnet test tests/IntegrationTests` runs with 0 errors caused by missing
   environment (test failures from real bugs are still expected; this
   ticket fixes the *plumbing*, not pre-existing logic defects).
4. `docs/local-development.md` walks a new reader from `git clone` to
   green integration test run.
5. `readme.md` has no remaining references to Blazor or Syncfusion.

## Out of scope (parked)

- Healthcheck for Redis (we don't run it locally yet — local dev uses
  in-memory caching, per `docs/features/01-platform-infrastructure.md`).
- VS Code devcontainer (`.devcontainer/`) — would re-implement parts of
  this stack inside a container. Worth doing later for "open in Codespaces"
  but adds complexity now.
- Per-developer database seeding beyond what migrations + the existing
  `configure-user-flows.ps1` already do.

## Risks / things to watch

- **MSSQL_PID licensing.** We pin `MSSQL_PID=Developer`. The Developer
  edition is free for non-production; doc note: don't reuse this compose
  for any prod-leaning environment.
- **ARM64 (Apple Silicon) MSSQL image quirks.** The pinned `mssql/server`
  image runs under emulation on Apple Silicon and is known to be slow on
  startup; the bring-up script's healthcheck wait must give it ≥60s before
  giving up. Document the expected first-run latency.
- **Azurite default account key is well-known.** That's fine for dev (it's
  literally the "well-known dev account" on purpose). Make sure the doc
  flags that production must use real KeyVault-backed keys.

## References

- `docs/features/01-platform-infrastructure.md` — current stack (source of
  truth)
- `tests/README.md` — existing test-suite docs (will get a small refresh)
- `AB#398` — precedent for per-task planning conventions
- `AB#662` — Azure DevOps task context for this local development
  environment work

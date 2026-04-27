#!/usr/bin/env bash
# scripts/dev-up.sh — bring up the local RAJ Financial dev stack.
# See docs/local-development.md for the full setup runbook.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

echo "==> Checking prerequisites..."
if ! "$REPO_ROOT/scripts/check-prereqs.sh"; then
  echo "✗ Prereq check failed. Install missing tools and re-run." >&2
  exit 1
fi

echo "==> Loading dev SA password from secrets store..."
if [[ -z "${RAJFIN_DEV_MSSQL_SA_PASSWORD:-}" ]]; then
  if [[ "$OSTYPE" == "darwin"* ]]; then
    if ! RAJFIN_DEV_MSSQL_SA_PASSWORD=$(security find-generic-password \
        -a "$USER" -s rajfinancial-dev-mssql-sa -w 2>/dev/null); then
      cat >&2 <<EOF
✗ No SA password found in macOS Keychain (service: rajfinancial-dev-mssql-sa).

  Generate and store one:

    PW=\$(LC_ALL=C tr -dc 'A-HJ-NP-Za-km-z2-9' </dev/urandom | head -c 28)
    PW="\${PW}#A1z"
    security add-generic-password -a "\$USER" -s rajfinancial-dev-mssql-sa -w "\$PW" -U \\
      -D "RAJ Financial dev SQL Server SA password (local docker-compose)"

  Or set RAJFIN_DEV_MSSQL_SA_PASSWORD in your shell before running.
EOF
      exit 1
    fi
    export RAJFIN_DEV_MSSQL_SA_PASSWORD
  else
    echo "✗ RAJFIN_DEV_MSSQL_SA_PASSWORD is not set." >&2
    echo "  Linux: export it from a secret store (pass / age / 1Password CLI)." >&2
    exit 1
  fi
fi

echo "==> Starting docker-compose stack (--wait for healthchecks)..."
docker compose -f docker-compose.dev.yml up -d --wait

echo "==> Stack health:"
docker compose -f docker-compose.dev.yml ps --format \
  "table {{.Name}}\t{{.Status}}\t{{.Ports}}"

echo "==> Running EF Core migrations against rajfin-sql..."
if [[ -d "$REPO_ROOT/src/Api" ]] && command -v dotnet >/dev/null 2>&1; then
  pushd "$REPO_ROOT/src/Api" >/dev/null
  if dotnet ef --version >/dev/null 2>&1; then
    # IMPORTANT: DesignTimeDbContextFactory falls back to LocalDB when no
    # connection string is configured — LocalDB doesn't exist on macOS/Linux
    # and isn't installed by default on a clean Windows box either, which
    # would silently misroute migrations away from our docker container.
    # Force the connection string to point at the rajfin-sql container.
    EF_CONNSTR="Server=localhost,1433;Database=RajFinancial_Dev;User Id=sa;Password=${RAJFIN_DEV_MSSQL_SA_PASSWORD};TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true"
    ConnectionStrings__SqlConnectionString="$EF_CONNSTR" \
      Values__SqlConnectionString="$EF_CONNSTR" \
      dotnet ef database update || {
      echo "⚠ Migrations failed. Stack is up but DB is not ready." >&2
      echo "  Run manually: cd src/Api && dotnet ef database update" >&2
    }
  else
    echo "⚠ dotnet-ef not installed; skipping migrations." >&2
    echo "  Install: dotnet tool install -g dotnet-ef" >&2
  fi
  popd >/dev/null
fi

cat <<EOF

✅ Local dev stack is ready.

Next steps:
  - Start API:    cd src/Api && func start
  - Start client: cd src/Client && npm run dev
  - Run tests:    dotnet test tests/IntegrationTests

To stop:           scripts/dev-down.sh
To reset volumes:  scripts/dev-down.sh --volumes
EOF

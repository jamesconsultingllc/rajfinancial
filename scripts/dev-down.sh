#!/usr/bin/env bash
# scripts/dev-down.sh — tear down the local RAJ Financial dev stack.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

EXTRA_ARGS=()
if [[ "${1:-}" == "--volumes" || "${1:-}" == "-v" ]]; then
  echo "==> Stopping stack AND removing volumes (data will be lost)..."
  EXTRA_ARGS+=(--volumes)
else
  echo "==> Stopping stack (volumes preserved)..."
fi

# Compose still requires the env var to validate the file even on `down`.
export RAJFIN_DEV_MSSQL_SA_PASSWORD="${RAJFIN_DEV_MSSQL_SA_PASSWORD:-placeholder-for-down}"
docker compose -f docker-compose.dev.yml down "${EXTRA_ARGS[@]}"

echo "✅ Stack stopped."

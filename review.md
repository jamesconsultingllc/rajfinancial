# Code Review: Task #482 — EF Core Migration Pipeline

**Branch**: `feature/482-cicd-pipeline`
**Scope**: EF Core migration jobs added to `.github/workflows/azure-functions.yml` — idempotent SQL script generation, conditional migration execution, SqlConnectionString passed to integration tests

---

## Summary

This change adds EF Core migration support to the CI/CD pipeline with idempotent SQL script generation, conditional migration jobs for dev/prod, and passes `SqlConnectionString` to integration tests.

---

## ✅ What's Good

1. **Idempotent migration scripts** — Using `--idempotent` flag is the right choice for safe re-runs
2. **Conditional execution** — `migrations_changed` output with `dorny/paths-filter` avoids unnecessary migration runs
3. **OIDC auth for SQL** — Using `az account get-access-token` with Entra auth is secure (no connection string secrets for DDL)
4. **Artifact upload** — 30-day retention for audit trail
5. **`always()` + result checks** — Allows integration tests to run when migrations are skipped

---

## 🔴 Critical Issues

| Issue | Location | Problem |
|-------|----------|---------|
| **Migration runs AFTER deploy** | Lines 150, 246 (`needs: [build, deploy-dev]`) | Schema changes should apply **BEFORE** the app deploys, not after. If new code references new columns, app will fail until migration runs. |
| **`sqlcmd` not installed** | Lines 170-172, 266-268 | Ubuntu runners don't have `sqlcmd` by default. Need to install `mssql-tools18`. |
| **Token exposed in logs** | Lines 167, 263 | `${{ steps.token.outputs.sql_token }}` may be visible in logs. Use `::add-mask::` to hide it. |

---

## 🟡 Suggestions

| Issue | Recommendation |
|-------|----------------|
| **Hardcoded server names** | Extract `rajfinancial-dev.database.windows.net` and `rajfinancial-prod.database.windows.net` to env vars or secrets for maintainability |
| **No migration failure handling** | Consider adding `continue-on-error: false` explicitly and/or Slack/Teams notification on failure |
| **Missing `-N` flag for sqlcmd** | Add `-N` (encrypt connection) for Azure SQL: `sqlcmd -S ... -N ...` |
| **Duplicate Azure login** | `migrate-dev` and `integration-test-dev` both login — if migration runs, integration tests could reuse the session (minor) |

---

## 🛠️ Recommended Fixes

**1. Fix job ordering** — Migration should run BEFORE deploy:
```yaml
migrate-dev:
  needs: [build]  # Not deploy-dev

deploy-dev:
  needs: [build, migrate-dev]  # Deploy after migration
```

**2. Install sqlcmd**:
```yaml
- name: Install sqlcmd
  run: |
    curl https://packages.microsoft.com/keys/microsoft.asc | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc
    sudo add-apt-repository "$(curl https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/prod.list)"
    sudo apt-get update
    sudo apt-get install -y mssql-tools18 unixodbc-dev
    echo "/opt/mssql-tools18/bin" >> $GITHUB_PATH
```

**3. Mask token**:
```yaml
- name: Get access token for Azure SQL
  id: token
  run: |
    TOKEN=$(az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv)
    echo "::add-mask::$TOKEN"
    echo "sql_token=$TOKEN" >> $GITHUB_OUTPUT
```

---

## 📋 Pre-merge Checklist

- [ ] Fix migration job ordering (migrate → deploy → test)
- [ ] Add sqlcmd installation step
- [ ] Mask SQL token in logs
- [ ] Add `-N` flag for encrypted connections
- [ ] Verify `SQL_CONNECTION_STRING` secret exists in both GitHub environments
- [ ] Confirm OIDC SP has `db_ddladmin` role on dev database

---

## Verdict

**Request changes**. The critical issues (job ordering, missing sqlcmd, token exposure) must be fixed before merge. The migration-after-deploy ordering could cause production outages if new code depends on schema changes.

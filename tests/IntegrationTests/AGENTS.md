# IntegrationTests Agent Instructions (BDD API Integration)

> **Project:** `RajFinancial.IntegrationTests` — Reqnroll BDD tests against a running Azure
> Functions host (local `localhost:7071` or deployed dev).
> The repo-wide rules at the [repo root AGENTS.md](../../AGENTS.md) and the API rules at
> [`src/Api/AGENTS.md`](../../src/Api/AGENTS.md) apply.
> **Test Layer:** API Integration BDD (root §Testing).
> **Operational README:** [`README.md`](README.md) (config, LivingDoc, CI artifacts).

---

## Purpose

End-to-end **HTTP-level** verification of the API: real Functions host, real auth tokens
(ROPC against Entra External ID for deployed runs; unsigned local JWTs for `localhost:7071`),
real database. Every endpoint in `src/Api/Functions/` MUST have a corresponding
`@api`-tagged feature.

This is the **OWASP coverage gate** — A01 IDOR, A07 auth failures, A05 input validation,
A09 access logging — all live as Gherkin scenarios here.

---

## Required Packages (already wired)

| Package | Purpose |
|---------|---------|
| `Reqnroll` + `Reqnroll.xUnit` | Gherkin runner on top of xUnit |
| `Expressium.LivingDoc.ReqnrollPlugin` | Generates `LivingDoc.html` artifact for CI |
| `Microsoft.Identity.Client` | ROPC token acquisition for deployed runs |
| `System.IdentityModel.Tokens.Jwt` | Local unsigned JWT issuance for `localhost:7071` |
| `Microsoft.Data.SqlClient` | Direct DB seed/cleanup outside the API |
| `FluentAssertions` | Assertion DSL — **always use this** |

Do **not** add: NetArchTest, Moq, EF Core providers — this project drives the deployed
surface, it does not host one.

---

## Layout

```
tests/IntegrationTests/
├── Features/                      # Gherkin .feature files (organized by domain)
│   ├── Assets/
│   ├── Entities/
│   ├── Auth/
│   └── ClientManagement/
├── StepDefinitions/               # C# step definition classes (one per feature family)
├── Support/
│   ├── FunctionsHostFixture.cs    # Resolves base URL, waits for /health/ready
│   ├── TestAuthHelper.cs          # ROPC vs. unsigned-local JWT selection
│   └── (test data builders)
├── appsettings.json               # CI-default config
├── appsettings.local.json         # Gitignored; local overrides (passwords, base URL)
├── reqnroll.json                  # Reqnroll config + LivingDoc plugin
└── xunit.runner.json
```

`Features/` mirrors `src/Api/Functions/` structure. One `.feature` file per resource family,
one StepDefinitions class per `.feature` (or one shared class per family).

---

## Conventions

1. **BDD-first (root §Implementation Order step 1).** No new endpoint ships without a
   `.feature` file written *before* implementation. Step definitions stub first, then go red,
   then green when the implementation lands.

2. **Tag every scenario.** Standard tags:
   - `@api` — required on every scenario in this project.
   - `@{feature}` — `@assets`, `@entities`, `@auth`, etc.
   - `@security @A0X` — OWASP Top 10 mapping (`@A01` for IDOR, `@A07` for auth, etc.).
   - `@smoke` — happy-path subset that runs as a fast first gate.

3. **Background sets host invariant only.** `Given the API is running` — no per-scenario
   data seeding belongs in the Background; use Scenario `Given` steps for state.

4. **Every endpoint tests these scenarios** (root §Security Checklist):
   - 401 when no token / invalid token.
   - 403 when token lacks the required role.
   - 404 (not 403) when scoping a resource to a different user (IDOR via `RESOURCE_NOT_FOUND`).
   - 400 with a `VALIDATION_FAILED` code on each invalid field.
   - 200/201/204 on the happy path.

5. **Step definitions live in C#.** Use FluentAssertions in step bodies. Do not put
   assertions in `[Then]` step text — the Gherkin describes intent, the C# enforces it.

6. **Test data is per-scenario.** Either create+delete inside the scenario via the API, or
   seed via `Microsoft.Data.SqlClient` in `[Before]` hooks and reset via `[After]`. Tests
   must be re-runnable in any order.

7. **No shared mutable state across scenarios.** Reqnroll's `[BeforeFeature]` is allowed
   for read-only setup (e.g. acquiring a token); anything mutable goes in `[BeforeScenario]`.

8. **Localized error messages are not asserted.** API returns machine-readable codes
   (`AUTH_FORBIDDEN`, `RESOURCE_NOT_FOUND`, `VALIDATION_FAILED`); scenarios assert on the
   `code`, never the human-readable `message` (root §API Error Codes). Message text is
   localized client-side and may change; codes are contract.

9. **Tokens come from `TestAuthHelper`.** Never paste a real bearer token into a feature
   file or step. The helper picks unsigned-local vs. ROPC based on the configured base URL.

---

## Running

```bash
# All scenarios (against the host pointed to by appsettings.local.json)
dotnet test tests/IntegrationTests/RajFinancial.IntegrationTests.csproj

# Single domain
dotnet test tests/IntegrationTests --filter "Tag=entities"

# OWASP scenarios only
dotnet test tests/IntegrationTests --filter "Tag=security"

# Smoke pass (fast)
dotnet test tests/IntegrationTests --filter "Tag=smoke"
```

For local runs, start the Functions host first (`scripts/dev-up.ps1` then
`func start --csharp` from `src/Api`). For deployed dev runs, see
[`README.md`](README.md) for ROPC config.

After every run, `LivingDoc.html` lands in
`tests/IntegrationTests/bin/{Configuration}/net10.0/` — open it in a browser to view results.

---

## What does NOT belong here

| Concern | Goes here |
|---------|-----------|
| Service-layer logic in isolation | `tests/Api.Tests/` |
| Layering / dependency rules | `tests/Architecture.Tests/` |
| Browser navigation / a11y | `tests/e2e/` |
| React component tests | `src/Client/src/**/__tests__/` (Vitest) |
| Performance / load | (not yet established) |

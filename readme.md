# RAJ Financial

Personal financial planning platform for high-net-worth households.
Tracks assets, accounts, transactions, and beneficiaries; generates AI-powered
insights via Anthropic Claude; and links real-world accounts via Plaid (premium).

## Stack at a glance

- **Client:** React 18 + TypeScript + Vite + Tailwind + shadcn/ui + Radix
  + TanStack Query v5 + MSAL React
- **API:** .NET 10 Azure Functions (isolated worker)
- **Data:** Azure SQL + EF Core 10, Azure Redis, Key Vault, Blob Storage
- **AI:** Claude (Anthropic SDK), `claude-sonnet-4-5-20250929` (BYOK on free
  tier)
- **Account linking:** Plaid (premium tier only)
- **Infra:** Bicep → Azure Static Web Apps + Functions Consumption
- **Tests:** xUnit (Api.Tests, Architecture.Tests) + Reqnroll BDD
  (IntegrationTests + e2e Playwright)

For the deep stack reference, see
[`docs/features/01-platform-infrastructure.md`](docs/features/01-platform-infrastructure.md).

## Get started locally

See **[`docs/local-development.md`](docs/local-development.md)** for the
authoritative setup runbook (prerequisites, Docker stack, `appsettings.local.json`,
running tests, troubleshooting). Goal is `git clone` → green integration
tests in under 10 minutes.

Quick version once your toolchain is installed:

```bash
scripts/check-prereqs.sh   # or scripts/check-prereqs.ps1
scripts/dev-up.sh          # or scripts/dev-up.ps1
cd src/Api && func start   # in one terminal
cd src/Client && npm run dev   # in another
dotnet test tests/IntegrationTests   # in a third
```

## Repository layout

```
rajfinancial/
├── docker-compose.dev.yml      # Local SQL Server + Azurite stack
├── docs/
│   ├── local-development.md    # ← Start here for local setup
│   ├── features/               # Feature specs (source of truth for stack)
│   ├── adr/                    # Architecture decision records
│   ├── plans/                  # Active and archived design plans
│   └── archive/                # Legacy (Blazor-era) docs
├── infra/                      # Bicep IaC (modules + parameters)
├── scripts/                    # Operational + dev scripts
│   ├── check-prereqs.sh|ps1    # Toolchain validator
│   ├── dev-up.sh|ps1           # Bring up local stack
│   └── dev-down.sh|ps1
├── src/
│   ├── Api/                    # .NET 10 Azure Functions (isolated)
│   ├── Client/                 # React + Vite SPA
│   └── Shared/                 # Shared contracts & DTOs
├── tests/
│   ├── Api.Tests/              # xUnit unit tests
│   ├── Architecture.Tests/     # Architectural conventions
│   ├── IntegrationTests/       # Reqnroll BDD against live stack
│   └── e2e/                    # Playwright acceptance tests
└── tools/insomnia/             # Insomnia API collections
```

## Contributing

Agent workflows are documented in [`AGENT.md`](AGENT.md) and
[`CLAUDE.md`](CLAUDE.md). Per-task implementation plans land under
`docs/plans/tasks/<work-item-id>-<slug>.md` and are attached to their
ADO work item.

## License

Proprietary — © James Consulting LLC. All rights reserved.

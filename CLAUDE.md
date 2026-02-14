# Claude AI Assistant Instructions

> **Primary instructions are in [`AGENT.md`](AGENT.md).** Read that file first.
> This file contains Claude-specific workflow instructions only.

---

## Approval Required

**NEVER commit changes without explicit user approval.** Always:
1. Describe what changes you plan to make
2. Wait for user confirmation before committing
3. If in doubt, ask first

---

## When Starting Work

1. **Read [`AGENT.md`](AGENT.md)** for development methodology, coding standards, and tech stack
2. **Read the execution plans** to understand current progress:
   - `docs/RAJ_FINANCIAL_EXECUTION_PLAN.md` - Infrastructure/security tasks
   - `docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md` - API tasks
   - `docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md` - UI tasks
3. **Update execution plans** as you complete tasks (`⬜` -> `🟡` -> `✅`)
4. **Reference task numbers** from execution plans in commits and PRs

---

## Branch Plans

You may create a temporary `IMPLEMENTATION_PLAN.md` at repo root for the current feature branch:
- Reference tasks by number from the permanent execution plans
- Delete before merging to `develop`
- Always sync completed work back to the permanent execution plans

---

## Useful Commands

```bash
dotnet build src/RajFinancial.sln                # Build
dotnet test                                       # All tests
dotnet test --collect:"XPlat Code Coverage"       # Coverage
dotnet format src/RajFinancial.sln                # Format
cd src/Api && func start                          # Run API
cd src/Client && dotnet run                       # Run Client
dotnet list package --vulnerable                  # Audit deps
```

---

## Links

- **Agent Instructions (PRIMARY)**: [AGENT.md](AGENT.md)
- **Copilot Instructions**: [.github/copilot-instructions.md](.github/copilot-instructions.md)
- **Execution Plan**: [docs/RAJ_FINANCIAL_EXECUTION_PLAN.md](docs/RAJ_FINANCIAL_EXECUTION_PLAN.md)
- **API Tracking**: [docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md](docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md)
- **UI Tracking**: [docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md](docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md)
- **UI Design**: [docs/RAJ_FINANCIAL_UI.md](docs/RAJ_FINANCIAL_UI.md)

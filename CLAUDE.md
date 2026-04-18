# Claude AI Assistant Instructions

> **Global standards**: [`E:\AGENT.md`](file:///E:/AGENT.md) (methodology, security, a11y, i18n).
> **Project instructions**: [`AGENT.md`](AGENT.md) (tech stack, structure, conventions).
> This file contains **Claude-specific** workflow instructions only.

---

## Approval Required

**NEVER commit changes without explicit user approval.** Always:
1. Describe what changes you plan to make
2. Wait for user confirmation before committing
3. If in doubt, ask first

---

## When Starting Work

1. Read `session.md` to understand where we left off
2. Read the ADO work item for full task context
3. Check feature docs and plans for context:
   - `docs/features/` — Feature specification documents
   - `docs/plans/` — Implementation plans

---

## Branch Plans (Work-Item-Attached)

**Convention (2026-04-18 onward):** Implementation plans for a feature/bugfix branch are appended to the **Description field of the corresponding ADO work item** (not posted as a comment, not stored as `IMPLEMENTATION_PLAN.md` at repo root).

### Rules

1. Before starting work on a feature/bugfix branch, append an implementation plan section to the Description of the parent Feature or Task work item.
2. The plan section should contain:
   - A clear heading (e.g., `## Implementation Plan`)
   - Goal / scope (1–2 lines)
   - Numbered task checklist with checkboxes
   - Files to create/modify
   - Testing requirements
   - Acceptance criteria
3. As work progresses, **edit the Description** to check items off. Do not use comments for plan progress — keep the plan in one place.
4. **Do not** commit `IMPLEMENTATION_PLAN.md` or similar transient planning files to the repo.
5. Persistent design/architecture plans still live under `docs/plans/` (these describe multi-phase initiatives, not branch-level tasks).

### Why

- Plan is always visible at the top of the work item, not buried in a comment thread
- Checklist progress is visible at a glance
- No stale planning files lingering in branches or getting merged to `develop`
- Persistent architecture plans stay under `docs/plans/`; ephemeral branch plans stay on the work item

---

## Links

- **Global Standards**: [E:\AGENT.md](file:///E:/AGENT.md)
- **Project Instructions**: [AGENT.md](AGENT.md)
- **Copilot Instructions**: [.github/copilot-instructions.md](.github/copilot-instructions.md)
- **Feature Docs**: [docs/features/](docs/features/) (all feature specifications)
- **Implementation Plans**: [docs/plans/](docs/plans/) (phased implementation plans)
- **Entity Architecture**: [docs/features/12-entity-structure.md](docs/features/12-entity-structure.md)
- **Entity Implementation Plan**: [docs/plans/2026-03-08-entity-restructure.md](docs/plans/2026-03-08-entity-restructure.md)

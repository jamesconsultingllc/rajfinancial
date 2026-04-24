# Architecture Decision Records

This directory holds the project's Architecture Decision Records (ADRs). Each ADR captures a single decision, its context, and its consequences so future contributors can see *why* the code looks the way it does without having to reconstruct the reasoning from diffs.

## Format

Each ADR follows a short template adapted from [MADR](https://adr.github.io/madr/):

- **Status** — `Proposed` · `Accepted` · `Superseded by NNNN`
- **Date** — the date the decision was accepted
- **Deciders** — who signed off
- **Context** — the forces that drove the decision
- **Decision** — the chosen option, stated plainly
- **Alternatives considered** — other options and why they were rejected
- **Consequences** — what this decision commits us to, both good and painful
- **References** — links to the pattern doc, plan docs, code, work items

## Index

| ADR | Title | Status |
|---|---|---|
| [0001](0001-idor-returns-404.md) | Owner-scoped reads return 404 on deny (IDOR) | Accepted |
| [0002](0002-activity-naming-convention.md) | Activity naming convention (`<Domain>.<Op>` / `<Domain>.<Op>.Service`) | Accepted |
| [0003](0003-layered-exception-recording.md) | Layered exception recording on per-layer activities | Accepted |

## Relationship to the pattern doc

The canonical rules for how endpoints are built live in [`docs/patterns/service-function-pattern.md`](../patterns/service-function-pattern.md). That doc is the single normative standard for implementation; it tells reviewers what to reject. The ADRs here explain *why* the pattern says what it says. If the two ever disagree, the pattern doc wins for implementation, and a new ADR should be filed to record the change.

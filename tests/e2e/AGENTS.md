# E2E Tests Agent Instructions (BDD UI Acceptance)

> **Project:** `@rajfinancial/e2e-tests` — Cucumber.js + Playwright (TypeScript) acceptance
> tests against the deployed Static Web App + Functions host.
> The repo-wide rules at the [repo root AGENTS.md](../../AGENTS.md) and the client rules at
> [`src/Client/AGENTS.md`](../../src/Client/AGENTS.md) apply.
> **Test Layer:** E2E Acceptance BDD (root §Testing).

---

## Purpose

User-facing acceptance tests: real browser, real auth (Entra External ID via Playwright
flows or pre-acquired tokens), real network. This layer proves the **whole product** behaves
correctly — the UI, the API, the auth round-trip, and the navigation flow.

This is also the **a11y and mobile gate** (root §Accessibility Requirements). Every UI
flow that ships gets at least one `@accessibility` scenario hitting axe + keyboard nav.

---

## Required Packages (already wired in `package.json`)

| Package | Purpose |
|---------|---------|
| `@cucumber/cucumber` | Gherkin runner |
| `@playwright/test` | Cross-browser driver (Chromium / Firefox / WebKit) |
| `ts-node` | Run TypeScript step definitions without a build step |
| `dotenv` | Load `.env` for local credentials (gitignored) |
| `imapflow` + `mailparser` | Read OTP / signup emails when scenarios need them |
| `cross-env` | Cross-platform environment variable injection |

When adding accessibility checks, install `@axe-core/playwright` and call it from a step
that runs after the page is interactive — do **not** roll a custom a11y harness.

---

## Layout

```
tests/e2e/
├── features/              # Gherkin .feature files (one per top-level UI flow)
│   ├── HomePage.feature
│   ├── Authentication.feature
│   ├── Navigation.feature
│   ├── AdminDashboard.feature
│   └── Entities.feature
├── step-definitions/      # TypeScript step files (one per feature, plus shared.steps.ts)
├── support/               # World class, hooks, browser config, Entra auth helpers
├── cucumber.js            # Cucumber config (paths, tags, formatters)
├── tsconfig.json
└── package.json
```

`features/` files are top-level (not nested) because flows cross domains. Use **tags**
(below) for grouping, not folders.

---

## Conventions

1. **One feature = one user-visible flow**, not one component or one page. A scenario like
   "advisor invites a client and sees them in the dashboard" can span 4 pages — that's
   correct for E2E.

2. **Tag every scenario.** Standard tags:
   - `@{feature}` — `@authentication`, `@navigation`, `@admin-dashboard`, etc.
   - `@requires-auth` (or `@unauthenticated`) — auth state needed.
   - `@smoke` — fast critical-path subset.
   - `@mobile` — runs in a mobile viewport (Playwright device emulation).
   - `@accessibility` — runs an axe scan + keyboard-nav assertions on the rendered page.

3. **Test behavior, not markup.** Drive the page through user-visible actions
   (`getByRole`, `getByLabel`, `getByText`) — never `getByTestId` unless no semantic locator
   is available. Asserting on CSS class names or DOM structure is forbidden.

4. **Accessibility is non-negotiable** (root §Accessibility):
   - Every UI scenario family has at least one `@accessibility` companion scenario.
   - axe violations of severity `serious` or `critical` fail the scenario.
   - Keyboard-only flow (Tab / Shift-Tab / Enter / Escape) must complete the same task as
     mouse-driven flow for any interactive component shipped.
   - Color contrast and focus-visible are part of axe's default ruleset; do not disable
     them without an ADR.

5. **Localization is exercised, not hardcoded.** When asserting visible text:
   - Prefer `getByRole({ name: /pattern/i })` over exact-string matches when the UI string
     may be localized.
   - For copy that the test owns, look it up via the i18n key, not a literal string. The
     i18n catalog is the contract.

6. **Auth state is not shared across scenarios** unless explicitly tagged. Use Playwright's
   storage state per `@requires-auth` flow, reset between scenarios.

7. **No production-data writes in deployed runs.** Scenarios that mutate state must scope to
   a per-test seeded user/entity created via API at the top of the scenario and torn down at
   the end. The dev environment is shared; tests do not own the database.

8. **Timeouts are explicit.** Do not rely on Cucumber's default — set per-step timeouts in
   `support/hooks.ts`. A flaky scenario gets fixed (locator, wait condition), not retried.

9. **Mobile flows are real**, not just narrow viewports. `@mobile` scenarios use Playwright's
   `devices['iPhone 13']`-style emulation so touch events, viewport, and user agent all
   match.

---

## Running

```bash
# Install Playwright browsers once
cd tests/e2e
npm install
npm run playwright:install

# All scenarios (default browser)
npm test

# One browser
npm run test:chromium
npm run test:firefox
npm run test:webkit

# Headed (see the browser)
npm run test:headed

# By tag
npx cucumber-js --tags "@smoke"
npx cucumber-js --tags "@accessibility"
npx cucumber-js --tags "@mobile and not @flaky"
```

Local runs need the dev SWA + Functions host reachable; remote runs need the deployed dev
environment URL exported via `BASE_URL`.

---

## What does NOT belong here

| Concern | Goes here |
|---------|-----------|
| API-only behavior (no UI involvement) | `tests/IntegrationTests/` (BDD) |
| React component unit tests | `src/Client/src/**/__tests__/` (Vitest) |
| Service-layer business logic | `tests/Api.Tests/` |
| Layering / dependency rules | `tests/Architecture.Tests/` |
| Cross-browser visual regression | (out of scope; use Playwright snapshots only when justified) |

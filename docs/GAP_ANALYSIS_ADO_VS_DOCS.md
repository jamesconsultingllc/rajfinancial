# Gap Analysis: ADO Work Items vs Feature Documentation

> **Generated**: Auto-analysis comparing 46 Azure DevOps Feature work items against 10 feature documentation files.
>
> **ADO Org**: `jamesconsulting` | **Project**: `c21b4869-5c21-461b-9a0b-ab984e08a088`

---

## Summary Statistics

| Metric | Count |
|--------|-------|
| Total Epics | 10 |
| Total Features (all states) | 46 |
| Active Features (non-Removed) | 43 |
| Removed Features | 3 |
| **COVERED** | 34 (79%) |
| **PARTIALLY COVERED** | 4 (9%) |
| **NOT COVERED** | 5 (12%) |
| Orphan Features (no parent Epic) | 1 |

---

## Epic → Doc Mapping

| Epic # | Epic Title | State | Doc File | Features (active) |
|--------|-----------|-------|----------|-------------------|
| 265 | 01 - Platform Infrastructure | In Progress | `01-platform-infrastructure.md` | 6 |
| 288 | 02 - Identity & Authentication | New | `02-identity-authentication.md` | 5 |
| 433 | 03 - Authorization & Data Access | New | `03-authorization-data-access.md` | 4 |
| 352 | 04 - Contacts & Beneficiaries | New | `04-contacts-beneficiaries.md` | 3 |
| 331 | 05 - Assets & Portfolio Management | In Progress | `05-assets-portfolio.md` | 3 |
| 309 | 06 - Accounts & Transactions | New | `06-accounts-transactions.md` | 5 |
| 373 | 07 - Dashboard & Reporting | New | `07-dashboard-reporting.md` | 4 |
| 414 | 08 - Estate Planning | New | `08-estate-planning.md` | 4 |
| 393 | 09 - AI Insights & Document Processing | New | `09-ai-insights.md` | 4 |
| 451 | 10 - User Profile & Settings | New | `10-user-profile-settings.md` | 4 |

---

## Full Feature Coverage Matrix

### Epic #265 — 01 - Platform Infrastructure → `01-platform-infrastructure.md`

| Feature # | Title | State | Coverage | Matching Doc `##` Sections | Notes |
|-----------|-------|-------|----------|---------------------------|-------|
| 268 | Managed Identity & Azure Service Auth | In Progress | ✅ COVERED | Azure Resources, Security Headers | Doc covers Azure Managed Identity setup and service auth |
| 434 | Performance Optimization | New | 🟡 PARTIALLY | _(no dedicated section)_ | Doc mentions middleware/structure but lacks a dedicated performance optimization section |
| 436 | Accessibility & UX Polish | New | ❌ NOT COVERED | _(none)_ | Doc is backend/infra focused; no accessibility or UX section |
| 453 | Production Infrastructure | New | ✅ COVERED | Azure Resources, CI/CD Pipeline, Configuration Files | Production infra details well documented |
| 454 | Monitoring & Observability | New | ✅ COVERED | Observability | Dedicated `## Observability` section |
| 455 | User Documentation | New | ❌ NOT COVERED | _(none)_ | No end-user documentation section exists in any doc |

---

### Epic #288 — 02 - Identity & Authentication → `02-identity-authentication.md`

| Feature # | Title | State | Coverage | Matching Doc `##` Sections | Notes |
|-----------|-------|-------|----------|---------------------------|-------|
| 266 | Test Users & Security Policies | New | 🟡 PARTIALLY | MFA Configuration | Doc covers MFA/security policies but lacks dedicated test user provisioning section |
| 267 | Entra User Flows Configuration | New | ✅ COVERED | User Flows, Entra Tenant Configuration | Dedicated sections for User Flows and Entra config |
| 291 | JWT Validation Middleware | Done | ✅ COVERED | JWT Validation (API) | Dedicated `## JWT Validation (API)` section |
| 485 | Auth Functions & UserProfileService API | Done | ✅ COVERED | MSAL React Configuration, JWT Validation (API), OIDC Federated Credentials | Auth functions and API auth documented |
| 502 | ROPC-Based Auth for Integration & E2E Tests | In Progress | ❌ NOT COVERED | _(none)_ | ROPC authentication flow not documented in any feature doc |

**Removed Features (excluded from analysis):**
- ~~#289 — Enhanced Navigation Components~~ (Removed)
- ~~#290 — Fluxor State Management~~ (Removed)
- ~~#292 — ApiClient with Content Negotiation~~ (Removed)

---

### Epic #433 — 03 - Authorization & Data Access → `03-authorization-data-access.md`

| Feature # | Title | State | Coverage | Matching Doc `##` Sections | Notes |
|-----------|-------|-------|----------|---------------------------|-------|
| 452 | Security Hardening | New | 🟡 PARTIALLY | _(general RBAC/access content)_ | Doc covers RBAC and data access patterns but has no dedicated security hardening section (e.g., CSP, rate limiting, input sanitization) |
| 470 | Authorization Middleware & Resource Access Control | Done | ✅ COVERED | Data Access Control, App Roles, EF Core Tenant Isolation | Comprehensive auth middleware documentation |
| 520 | DataAccessGrant System | New | ✅ COVERED | Data Access Control, Sharing Flow | Dedicated `## Data Access Control` and `## Sharing Flow` sections |
| 521 | Audit Logging | New | ✅ COVERED | Audit Logging | Dedicated `## Audit Logging` section |

---

### Epic #352 — 04 - Contacts & Beneficiaries → `04-contacts-beneficiaries.md`

| Feature # | Title | State | Coverage | Matching Doc `##` Sections | Notes |
|-----------|-------|-------|----------|---------------------------|-------|
| 353 | Beneficiary Service & API | New | ✅ COVERED | API Endpoints, Service Interface, AssetContactLink, DTOs & Request/Response Contracts | Full API specification with DTOs and service interfaces |
| 354 | Beneficiaries Page UI | New | ✅ COVERED | UI Flow | Dedicated `## UI Flow` section describing the page |
| 355 | Beneficiary Assignment & Validation | New | ✅ COVERED | AssetContactLink, Validation Rules | Assignment entity model and validation rules fully documented |

---

### Epic #331 — 05 - Assets & Portfolio Management → `05-assets-portfolio.md`

| Feature # | Title | State | Coverage | Matching Doc `##` Sections | Notes |
|-----------|-------|-------|----------|---------------------------|-------|
| 332 | Assets Page UI | In Progress | ✅ COVERED | UI Patterns | Dedicated `## UI Patterns` section |
| 333 | Asset Form & CRUD UI | New | ✅ COVERED | Request/Response DTOs, UI Patterns, Validation Rules | DTOs, validation, and UI patterns document full CRUD |
| 334 | Asset Service & API | Done | ✅ COVERED | API Endpoints, Service Interface | Dedicated `## API Endpoints` and `## Service Interface` sections |

---

### Epic #309 — 06 - Accounts & Transactions → `06-accounts-transactions.md`

| Feature # | Title | State | Coverage | Matching Doc `##` Sections | Notes |
|-----------|-------|-------|----------|---------------------------|-------|
| 310 | Plaid Service Integration | New | ✅ COVERED | Plaid Sync Strategy, LinkedAccount Entity, Security & Privacy | Extensive Plaid integration documentation including sync, token handling |
| 311 | Account Management UI | New | ✅ COVERED | UI Design | Dedicated `## UI Design` section |
| 312 | Plaid Webhook Handling | New | ✅ COVERED | Plaid Sync Strategy | Webhook handling covered within Plaid sync strategy section |
| 522 | Transaction Service & API | New | ✅ COVERED | Transaction Entity, API Endpoints, Service Layer, DTOs & Contracts | Full service layer and API endpoint documentation |
| 524 | Spending & Transaction UI | New | ✅ COVERED | UI Design | Spending views documented in the UI Design section |

---

### Epic #373 — 07 - Dashboard & Reporting → `07-dashboard-reporting.md`

| Feature # | Title | State | Coverage | Matching Doc `##` Sections | Notes |
|-----------|-------|-------|----------|---------------------------|-------|
| 374 | Dashboard Widgets | New | ✅ COVERED | Dashboard Widgets | Dedicated section with full widget inventory table (8 widgets) |
| 375 | Dashboard Service & API | New | ✅ COVERED | Data Aggregation, Service Layer, API Endpoints, Azure Functions | Full service/API documentation with fan-out pattern |
| 376 | Dashboard Page UI | New | ✅ COVERED | UI Layout, React Query Hooks, TypeScript Types | Complete UI specification |
| 523 | Report Generation & Export | New | ✅ COVERED | Report Export | Dedicated `## Report Export` section |

---

### Epic #414 — 08 - Estate Planning → `08-estate-planning.md`

| Feature # | Title | State | Coverage | Matching Doc `##` Sections | Notes |
|-----------|-------|-------|----------|---------------------------|-------|
| 415 | Insurance Needs Calculator | New | ❌ NOT COVERED | _(none)_ | No insurance needs calculator in any doc; doc 08 focuses on beneficiary coverage and trust views |
| 525 | Trust Management Views | New | ✅ COVERED | Trust Overview, Service Interface | Detailed trust hierarchy views with TrustOverviewDto |
| 526 | Beneficiary Coverage Analysis | New | ✅ COVERED | Coverage Analysis, Allocation Validation | Dedicated coverage and allocation validation sections |
| 527 | Estate Planning UI | New | ✅ COVERED | UI Layout, React Query Hooks, TypeScript Types | Complete UI specification |

---

### Epic #393 — 09 - AI Insights & Document Processing → `09-ai-insights.md`

| Feature # | Title | State | Coverage | Matching Doc `##` Sections | Notes |
|-----------|-------|-------|----------|---------------------------|-------|
| 394 | AI Insights UI | New | ✅ COVERED | UI Layout, React Query Hooks, TypeScript Types, Statement Parse Review Flow | Full UI specification including parse review flow |
| 395 | Strategy Sources & RAG | New | 🟡 PARTIALLY | Prompt Engineering | Prompt engineering section contains some RAG context, but no dedicated RAG pipeline, vector DB, or strategy source management documentation |
| 396 | Claude AI Integration | New | ✅ COVERED | Architecture, Service Interfaces, Rate Limits, BYOK Key Management | Extensive Claude integration docs including BYOK model |
| 528 | Document Processing & Statement Parsing | New | ✅ COVERED | Entities (DocumentUpload), Statement Parse Review Flow, Blob Storage Configuration | Full document processing pipeline documented |

---

### Epic #451 — 10 - User Profile & Settings → `10-user-profile-settings.md`

| Feature # | Title | State | Coverage | Matching Doc `##` Sections | Notes |
|-----------|-------|-------|----------|---------------------------|-------|
| 529 | User Profile Service & API | New | ✅ COVERED | Service Interfaces, API Endpoints, Azure Functions, Entities | Full service/API with JIT provisioning |
| 530 | Data Export & GDPR Compliance | New | ✅ COVERED | Data Export Contents, Account Deletion Flow | Dedicated data export and deletion flow sections |
| 531 | Subscription Management | New | ✅ COVERED | Stripe Integration, Tier Gating Summary | Stripe integration and subscription tier documentation |
| 532 | Settings & Preferences UI | New | ✅ COVERED | UI Layout, React Query Hooks, TypeScript Types | Complete UI specification |

---

### Orphan Feature (No Parent Epic)

| Feature # | Title | State | Coverage | Notes |
|-----------|-------|-------|----------|-------|
| 416 | Debt Payoff Analysis | New | ❌ NOT COVERED | No parent Epic, not in any doc. Needs to be assigned to an Epic or removed. |

---

## Gaps Requiring Action

### 1. NOT COVERED Features — Need Documentation

These ADO Features have **no corresponding documentation** in any feature doc:

| # | Feature | Epic | Recommended Action |
|---|---------|------|--------------------|
| 436 | Accessibility & UX Polish | #265 (Platform Infrastructure) | Add `## Accessibility & UX Standards` section to `01-platform-infrastructure.md` covering WCAG 2.1 AA targets, component patterns, testing strategy |
| 455 | User Documentation | #265 (Platform Infrastructure) | Add `## User Documentation` section to `01-platform-infrastructure.md` or create a new `11-user-documentation.md` covering help system, tooltips, onboarding flows |
| 502 | ROPC-Based Auth for Integration & E2E Tests | #288 (Identity & Auth) | Add `## ROPC Test Authentication` section to `02-identity-authentication.md` covering ROPC flow, test user configuration, CI/CD integration |
| 415 | Insurance Needs Calculator | #414 (Estate Planning) | Add `## Insurance Needs Calculator` section to `08-estate-planning.md` covering calculation model, inputs/outputs, DTOs, UI |
| 416 | Debt Payoff Analysis | _(orphan)_ | Either: (a) assign to an Epic and create documentation, or (b) remove if out of scope. Consider adding to `06-accounts-transactions.md` or a new `11-financial-tools.md` |

### 2. PARTIALLY COVERED Features — Need Documentation Enhancement

These ADO Features have some relevant content but lack dedicated, complete documentation:

| # | Feature | Epic | Gap Details | Recommended Action |
|---|---------|------|-------------|-------------------|
| 434 | Performance Optimization | #265 (Platform Infrastructure) | No dedicated perf section | Add `## Performance Optimization` to `01-platform-infrastructure.md` covering: response time targets, caching strategy, lazy loading, bundle optimization, database query optimization |
| 266 | Test Users & Security Policies | #288 (Identity & Auth) | MFA covered, test user provisioning not | Add `## Test Users & Security Policies` to `02-identity-authentication.md` covering: test user accounts, password policies, conditional access policies, security testing |
| 395 | Strategy Sources & RAG | #393 (AI Insights) | Prompts exist, RAG pipeline missing | Add `## Strategy Sources & RAG Pipeline` to `09-ai-insights.md` covering: knowledge base sources, vector store (Azure AI Search), embedding strategy, retrieval flow, document indexing |
| 452 | Security Hardening | #433 (Authorization) | RBAC covered, hardening gaps | Add `## Security Hardening` to `03-authorization-data-access.md` covering: CSP headers, rate limiting, input sanitization, SQL injection prevention, CORS policy, dependency scanning |

### 3. Removed Features — Confirmation Needed

These 3 Features are in `Removed` state. Confirm they should remain removed:

| # | Feature | Parent Epic | Notes |
|---|---------|-------------|-------|
| 289 | Enhanced Navigation Components | #288 (Identity & Auth) | Likely superseded by React SPA nav |
| 290 | Fluxor State Management | #288 (Identity & Auth) | Blazor-era artifact; project moved to React |
| 292 | ApiClient with Content Negotiation | #288 (Identity & Auth) | Likely superseded by React API client |

### 4. Doc-Defined Features Not in User's Original List

The following Features **exist in ADO** and are listed in the docs' own tracking tables, but were **not in the user's original 39-ID list**. They were discovered by reading the docs and fetching from ADO:

| # | Feature | Parent Epic | Doc | State |
|---|---------|-------------|-----|-------|
| 434 | Performance Optimization | #265 | `01-platform-infrastructure.md` | New |
| 436 | Accessibility & UX Polish | #265 | `01-platform-infrastructure.md` | New |
| 520 | DataAccessGrant System | #433 | `03-authorization-data-access.md` | New |
| 521 | Audit Logging | #433 | `03-authorization-data-access.md` | New |
| 522 | Transaction Service & API | #309 | `06-accounts-transactions.md` | New |
| 523 | Report Generation & Export | #373 | `07-dashboard-reporting.md` | New |
| 525 | Trust Management Views | #414 | `08-estate-planning.md` | New |

---

## Coverage by Epic

| Epic # | Epic Title | Total Features | Covered | Partial | Not Covered | Removed | Coverage % |
|--------|-----------|---------------|---------|---------|-------------|---------|-----------|
| 265 | Platform Infrastructure | 6 | 3 | 1 | 2 | 0 | 50% |
| 288 | Identity & Authentication | 8 | 3 | 1 | 1 | 3 | 60%* |
| 433 | Authorization & Data Access | 4 | 3 | 1 | 0 | 0 | 75% |
| 352 | Contacts & Beneficiaries | 3 | 3 | 0 | 0 | 0 | 100% |
| 331 | Assets & Portfolio | 3 | 3 | 0 | 0 | 0 | 100% |
| 309 | Accounts & Transactions | 5 | 5 | 0 | 0 | 0 | 100% |
| 373 | Dashboard & Reporting | 4 | 4 | 0 | 0 | 0 | 100% |
| 414 | Estate Planning | 4 | 3 | 0 | 1 | 0 | 75% |
| 393 | AI Insights | 4 | 3 | 1 | 0 | 0 | 75% |
| 451 | User Profile & Settings | 4 | 4 | 0 | 0 | 0 | 100% |

_*Epic #288 coverage % calculated against active (non-Removed) features only: 3 Covered + 1 Partial out of 5 active = 60%._

---

## Feature State Summary

| State | Count | Features |
|-------|-------|----------|
| Done | 4 | #291, #334, #470, #485 |
| In Progress | 4 | #268, #332, #502 + Epic #265, #331 |
| New | 35 | All remaining active features |
| Removed | 3 | #289, #290, #292 |

---

## Recommendations

1. **Prioritize NOT COVERED gaps**: Features #415 (Insurance Calculator), #502 (ROPC Auth), #436 (Accessibility), #455 (User Docs), and #416 (Debt Payoff) need documentation before implementation begins.

2. **Resolve orphan #416**: Debt Payoff Analysis has no parent Epic. Assign to Epic #309 (Accounts & Transactions) or create a new "Financial Tools" Epic.

3. **Enhance PARTIALLY COVERED features**: Add dedicated `##` sections for Performance Optimization (#434), Test Users (#266), RAG Pipeline (#395), and Security Hardening (#452).

4. **Confirm Removed features**: Verify #289, #290, #292 should remain Removed (they appear to be Blazor-era artifacts from the tech stack migration to React).

5. **Keep doc tracking tables in sync**: Several docs list Features that were not in the original tracking list. Ensure the Execution Plan tracking docs reference all Feature IDs.

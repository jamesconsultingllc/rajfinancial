# RAJ Financial - Agent Instructions

> **This file is the single source of truth for all AI agents working in this repository.**
> It includes both universal standards and project-specific instructions.
> Tool-specific overrides: `CLAUDE.md` (Claude) · `.github/copilot-instructions.md` (Copilot).

---

## Core Principles (Priority Order)

1. **BDD/TDD** - Tests first, always
2. **Security First** - Designed into every feature from the start
3. **Accessibility** - WCAG 2.1 AA minimum, semantic HTML
4. **Localization** - All user-facing text localizable
5. **Mobile Responsiveness** - Mobile-first CSS approach
6. **Documentation** - Document all public APIs
7. **Observability** - Structured logging, metrics, telemetry
8. **SOLID Principles** - Clean architecture, dependency inversion
9. **DRY** - Extract reusable components, services, and utilities
10. **Script Everything** - Automate all infrastructure and environment setup

---

## Infrastructure & Environment: Script Everything

**If it can be scripted, it MUST be scripted.** Never rely on manual portal clicks for repeatable infra or environment setup.

### What Must Be Scripted

- **Entra app registrations** - App creation, API permissions, role definitions, redirect URIs
- **Test user provisioning** - User creation, password assignment, role assignment, MFA exclusions
- **Service principal configuration** - SP creation, credential management
- **User flow / policy linkage** - Linking apps to authentication flows
- **GitHub environment secrets** - Documenting which secrets are needed (actual values set manually for security)
- **Azure resource provisioning** - Bicep/ARM templates for all cloud resources
- **Key Vault population** - Secrets, certificates, configuration values

### Script Requirements

- **Idempotent** - Safe to run multiple times without side effects (check-before-create pattern)
- **Environment-aware** - Accept `-Environment dev|prod` parameter, load per-env config
- **Self-documenting** - Include `Write-Host` progress messages for every step
- **Error-handling** - Fail fast with clear error messages, don't silently continue
- **Temp file pattern** - Use temp files for `az rest --body` payloads instead of inline JSON
- **Located in `scripts/infra/`** - All infra scripts in one place

### Script Naming Convention

| Script | Purpose |
|--------|---------|
| `register-entra-apps.ps1` | App registrations, API permissions, user flow linkage |
| `create-test-users.ps1` | Test user provisioning, role assignment, MFA exclusion |
| `setup-entra-oidc.ps1` | OIDC federated credentials for GitHub Actions |
| `entra-config-{env}.json` | Per-environment configuration (generated output) |

### Anti-Patterns

```
❌ "Go to the portal and click..."
❌ "Manually create the app registration..."
❌ "Copy the client ID from the portal..."

✅ Script it with `az rest` / `az ad` / `az cli`
✅ Output config to JSON for downstream consumption
✅ Include verification steps that confirm success
```

---

## Workflow: Plan Before Coding

**STOP and PLAN before writing any code.**

1. **Understand the task** - Read the task/issue thoroughly
2. **Check ADO work items** - Read the work item description and acceptance criteria
3. **Review existing code** - Understand the current implementation and patterns
4. **Plan the approach** - Outline what files need changes and why
5. **Only then implement** - One task at a time, completely

---

## Session Management

### Maintain a `session.md` File

Every project should have a `session.md` tracking:
- **Last completed task** - Work item ID, title, commit hash
- **Current task** - What's in progress
- **Next tasks** - What's queued up
- **Blockers** - Any issues preventing progress
- **Notes** - Important decisions or context

### When Starting Work

1. **Read `session.md`** to understand where we left off
2. **Read the ADO work item** (if applicable) for full context
3. **Update status** as you progress

### When Finishing a Task

1. **Update `session.md`** - Mark task complete with commit hash
2. **Prompt for next work** - Always end with a clear prompt:
   > *Task 474 is complete. The next task is **Task 475 - [title]**. Ready to proceed?*
3. **Never silently finish** - The user should always know what comes next

---

## One Task at a Time

**Complete one task fully before starting another.**

- Finish implementation, tests, and documentation
- Ensure all tests pass
- Get the task to a committable state
- Then move to the next task

---

## Azure DevOps Integration

### Before Starting a Task

**Always do these steps BEFORE writing any code:**

1. **Assign the work item** to the user
2. **Move to In Progress** — update the state from "To Do" to "In Progress"
3. **Read the work item description** and acceptance criteria

### When Working with ADO Tasks

1. **Read the work item** before starting implementation
2. **Reference work item IDs** in commits and PRs
3. **Update work item status** as you progress
4. **Link commits/PRs** to work items
5. **Move to Done** when the task is complete

### Task Workflow

```
⬜ Not Started -> 🟡 In Progress -> ✅ Done
```

---

## Development Methodology: BDD/TDD First

**NO CODE WITHOUT TESTS FIRST.** This is non-negotiable.

### Red-Green-Refactor

```
Write failing test -> Write minimum code to pass -> Refactor
```

1. **BDD**: Write Gherkin `.feature` files defining expected behavior BEFORE implementation
2. **TDD**: Write unit tests that fail BEFORE writing production code
3. **Red-Green-Refactor**: Fail first, pass minimally, then clean up
4. **No exceptions**: Even "simple" changes get tests first

### Test Coverage

- **90% minimum** code coverage for all new code
- Unit tests for ALL business logic
- Integration tests for ALL API endpoints
- Security tests for EVERY endpoint
- Accessibility tests for EVERY UI component

---

## Implementation Order (Project-Specific)

For every feature, follow this exact sequence:

| Step | What | Gate |
|------|------|------|
| 1 | Write BDD feature file (Gherkin) | Scenarios cover happy path, errors, and security |
| 2 | Write step definitions (stubs) | All steps compile but fail |
| 3 | Write unit tests (security first) | Tests for auth, IDOR, injection, validation |
| 4 | Write unit tests (accessibility) | Tests for a11y compliance |
| 5 | Write unit tests (business logic) | Tests for core behavior |
| 6 | Implement security layer | Auth, validation, tenant scoping pass |
| 7 | Implement UI with a11y + i18n | Semantic HTML, localized strings, ARIA |
| 8 | Implement business logic | All tests green |
| 9 | Refactor | Tests still green, code is clean |

---

## Technology Stack

### Runtime & Frameworks

| Component | Technology | Version |
|-----------|-----------|---------|
| **API Runtime** | .NET (Isolated Worker) | net10.0 |
| **Client Runtime** | React + TypeScript + Vite | React 18.3, Vite 5.4 |
| **Shared Library** | .NET Class Library | net9.0 / net10.0 (multi-target) |
| **Hosting** | Azure Static Web Apps | v4 Functions |
| **Database** | SQL Server via EF Core | 10.0.2 |
| **Auth (API)** | Azure AD / MSAL | Azure.Identity 1.17.1 |
| **Auth (Client)** | MSAL React | @azure/msal-react 2.x |
| **Identity** | Microsoft Graph | 5.101.0 |

### Key NuGet Packages

**API (`src/Api`):**
- `Microsoft.Azure.Functions.Worker` 2.51.0 - Isolated worker model
- `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` 2.1.0 - HTTP triggers with ASP.NET Core integration
- `Microsoft.EntityFrameworkCore.SqlServer` 10.0.2 - SQL Server provider
- `Microsoft.EntityFrameworkCore.InMemory` 10.0.2 - In-memory provider (testing)
- `FluentValidation` 12.1.1 - Request validation
- `MemoryPack` 1.21.4 - High-performance binary serialization
- `Azure.Identity` 1.17.1 - Azure AD authentication
- `Microsoft.Graph` 5.101.0 - Microsoft Graph API
- `Microsoft.ApplicationInsights.WorkerService` 3.0.0 - Telemetry

**Client (`src/Client` - React/TypeScript):**
- `react` 18.3.1 + `react-dom` - React framework
- `vite` 5.4.19 - Build tool and dev server
- `typescript` 5.8.3 - Type safety
- `@azure/msal-react` 2.x + `@azure/msal-browser` 3.x - Azure AD authentication
- `@tanstack/react-query` 5.83.0 - Server state management
- `react-router-dom` 6.30.1 - Client-side routing
- `tailwindcss` 3.4.17 - Utility-first CSS
- Radix UI primitives - Accessible UI components
- `react-hook-form` 7.61.1 + `zod` 3.25.76 - Form handling and validation
- `recharts` 2.15.4 - Data visualization
- `lucide-react` 0.462.0 - Icons
- `date-fns` 3.6.0 - Date utilities

**Client Testing:**
- `vitest` 3.2.4 - Test runner (Vite-native)
- `@testing-library/react` 16.3.0 - React component testing
- `@testing-library/jest-dom` 6.6.3 - DOM matchers
- `@testing-library/user-event` 14.6.1 - User interaction simulation
- `jsdom` 26.1.0 - DOM environment for tests

**Testing (`tests/` - .NET):**
- `xunit` 2.9.3 + `xunit.runner.visualstudio` - Unit test framework
- `Reqnroll` 3.3.0 + `Reqnroll.xUnit` - BDD/Gherkin acceptance tests
- `Microsoft.Playwright` 1.57.0 - Browser automation / E2E
- `Deque.AxeCore.Playwright` 4.10.1 - Accessibility testing (axe-core)
- `FluentAssertions` 8.8.0 - Assertion library
- `Moq` 4.20.72 - Mocking framework
- `coverlet.collector` 6.0.4 - Code coverage
- `MailKit` 4.14.1 - Email testing

### Coding Standards

- **C# 13** / .NET 10 features enabled
- **Nullable reference types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Validation**: FluentValidation + Data Annotations (defense-in-depth)
- **ORM**: Entity Framework Core (parameterized queries only, never raw SQL concatenation)
- **Serialization**: MemoryPack for internal, System.Text.Json for public APIs
- **Logging**: Structured logging via `ILogger` + Application Insights
- **Auth pattern**: Azure AD B2C via MSAL, protected routes in React, `[Authorize]` on Functions
- **Localization**: React i18n (i18next or similar) for client, `.resx` for API
- **Error handling**: Structured `ApiError` responses with machine-readable codes

### Code Style (C# / ReSharper Conventions)

#### Naming Conventions

| Member Type | Convention | Example |
|-------------|-----------|---------|
| Private instance fields | camelCase, **no** underscore prefix | `logger`, `dbContext` |
| Private static readonly | camelCase | `ownerId`, `defaultTimeout` |
| Private const | SCREAMING_SNAKE_CASE | `OBJECT_ID_CLAIM`, `MAX_RETRIES` |
| Public/internal members | PascalCase | `GetUserProfile()`, `ConnectionString` |

#### Preferences

- Prefer **collection expressions** `[]` over `Array.Empty<T>()` or `new List<T>()`
- Remove **redundant namespace qualifiers** — use `using` directives
- Use **file-scoped namespaces** (`namespace X;`)
- Prefer **pattern matching** over type checks + casts
- Use **primary constructors** where appropriate

---

## Code Documentation

### Requirements

- All public methods/classes: purpose, parameters, return values, exceptions
- Complex logic: inline comments for non-obvious algorithms
- Public APIs: request/response examples
- Configuration: all environment variables documented

### Documentation Formats

- **C#**: XML documentation comments (`/// <summary>`)
- **TypeScript/JavaScript**: JSDoc comments (`/** ... */`)
- **Python**: Docstrings (Google or NumPy style)

---

## GitFlow Branching

**Always create feature branches from `develop`, never from `main`.**

| Branch Type | Create From | Merge To | Pattern |
|-------------|-------------|----------|---------|
| `feature/*` | `develop` | `develop` | `feature/descriptive-name` |
| `bugfix/*` | `develop` | `develop` | `bugfix/descriptive-name` |
| `release/*` | `develop` | `main` + `develop` | `release/x.y.z` |
| `hotfix/*` | `main` | `main` + `develop` | `hotfix/x.y.z` |

### Solution Structure

```
rajfinancial/
├── src/RajFinancial.sln
├── src/Api/RajFinancial.Api.csproj              # Azure Functions API (net10.0)
├── src/Client/                                   # React + TypeScript Client
│   ├── package.json                              # npm dependencies
│   ├── vite.config.ts                            # Vite build config
│   ├── src/
│   │   ├── components/                           # Reusable UI components
│   │   │   └── __tests__/                        # Component tests (colocated)
│   │   ├── pages/                                # Route pages
│   │   │   └── __tests__/                        # Page tests (colocated)
│   │   ├── auth/                                 # MSAL auth logic
│   │   ├── hooks/                                # Custom React hooks
│   │   ├── services/                             # API service layer
│   │   ├── types/                                # TypeScript types
│   │   ├── generated/memorypack/                 # Auto-generated MemoryPack TS types
│   │   └── test/                                 # Test setup and mocks
│   └── public/                                   # Static assets
├── src/Shared/RajFinancial.Shared.csproj        # Shared models (net9.0;net10.0)
├── tests/Api.Tests/RajFinancial.Api.Tests.csproj           # API unit tests (net10.0)
├── tests/IntegrationTests/RajFinancial.IntegrationTests.csproj  # Integration tests
└── tests/AcceptanceTests/RajFinancial.AcceptanceTests.csproj    # Reqnroll + Playwright (net9.0)
```

---

## Project Overview

**Raj Financial** is a financial services application built with:
- **Frontend**: React + TypeScript + Vite (Client)
- **Backend**: Azure Functions (.NET Isolated Worker)
- **Shared**: .NET Class Library for shared models and contracts
- **Hosting**: Azure Static Web Apps

### Project Structure

```
src/
├── Api/                    # Azure Functions API (backend)
│   ├── Functions/          # HTTP trigger functions
│   ├── Services/           # Business logic services
│   └── Middleware/         # Auth, validation, error handling
├── Client/                 # React + TypeScript (frontend)
│   ├── src/
│   │   ├── pages/          # Route page components
│   │   ├── components/     # Reusable UI components
│   │   │   └── ui/         # shadcn/ui primitives
│   │   ├── auth/           # MSAL authentication
│   │   ├── hooks/          # Custom React hooks
│   │   ├── services/       # API client services
│   │   ├── types/          # TypeScript types/interfaces
│   │   └── generated/      # Auto-generated code (MemoryPack)
│   └── public/             # Static assets
├── Shared/                 # Shared library
│   ├── Entities/           # Domain entities
│   ├── Models/             # DTOs and domain models
│   └── Contracts/          # Interfaces and DTOs
tests/
├── Api.Tests/              # API unit tests (xUnit)
├── IntegrationTests/       # Integration tests
└── AcceptanceTests/        # BDD acceptance tests (Reqnroll + Gherkin)
docs/                       # Documentation and planning
```

---

## Brand Identity

**Name**: RAJ Financial Software
**Logo**: RF monogram with wing motif (gold gradient)
**Font**: Nexa XBold (display), Inter (body)

### Brand Colors (Gold Palette)

| Color | Hex | Usage |
|-------|-----|-------|
| Lemon Chiffon | `#fffbcc` | Lightest backgrounds |
| Light Cream | `#fff7b3` | Light backgrounds |
| Soft Gold | `#f5e99a` | Subtle accents |
| Flax | `#eed688` | Secondary elements |
| Bright Gold | `#e8c94d` | Highlights |
| **Spanish Yellow** | `#ebbb10` | **PRIMARY** |
| Rich Gold | `#d4a80e` | Primary hover |
| UC Gold | `#c3922e` | Accent/depth |
| Deep Gold | `#a67c26` | Dark accents |
| Darkest Gold | `#8a661f` | Text on light |

### Assets Location

Brand assets source: `D:\OneDrive - RAJ Financial\RAJ Financial\Assets\All files`

Project assets:
```
src/Client/public/images/brand/
├── logo-icon.svg, logo-icon.png       # RF monogram
├── logo-vertical.svg                  # Logo with text below
├── logo-horizontal.svg                # Logo with text right (black)
├── logo-horizontal-color.svg          # Logo with text right (color)
├── logo-color.svg, logo.png           # Full color logo
```

---

## Testing (Project-Specific)

### Test File Organization

```
src/Client/src/                    # React tests (colocated with source)
├── components/__tests__/          # Component unit tests
├── pages/__tests__/               # Page component tests
├── auth/__tests__/                # Auth logic tests
├── hooks/__tests__/               # Custom hook tests
└── test/                          # Test setup, mocks, utilities

tests/                             # .NET tests
├── Api.Tests/
│   ├── Middleware/                # Middleware unit tests
│   ├── Services/                  # Service unit tests
│   └── Functions/                 # Function unit tests
├── IntegrationTests/              # API integration tests
└── AcceptanceTests/
    ├── Features/                  # Gherkin .feature files
    ├── StepDefinitions/           # Step definition classes
    └── Support/                   # Test hooks, helpers
```

### Running Tests

```bash
# .NET tests
dotnet test                                      # All .NET tests
dotnet test tests/Api.Tests                      # API tests only
dotnet test tests/AcceptanceTests                # BDD tests only
dotnet test --collect:"XPlat Code Coverage"      # With coverage
dotnet test --filter "Category=Security"         # Security tests only

# React tests
cd src/Client && npm test                        # Run all React tests
cd src/Client && npm run test:watch              # Watch mode
```

---

## Accessibility Requirements (a11y)

**WCAG 2.1 AA minimum** for all UI components.

### Rules

- Semantic HTML elements (`<button>`, `<nav>`, `<main>`, `<article>`)
- ARIA attributes where semantic HTML is insufficient
- Keyboard navigation for all interactive elements
- Visible focus indicators (never `outline: none` without replacement)
- Color contrast: 4.5:1 for text, 3:1 for large text
- Alt text for all images and meaningful icons
- Screen reader support with labels and live regions
- Skip links for main content
- Form labels associated with inputs
- Minimum 44x44px touch targets

### Accessibility Testing

Test with accessibility tools (axe, Lighthouse, jest-axe, Deque.AxeCore.Playwright).

---

## Localization Requirements (i18n)

**All user-facing text must be localizable.** Never hardcode strings.

### Rules

- Use localization frameworks (`IStringLocalizer<SharedResources>` + `.resx` files)
- API returns error codes; client localizes messages
- Support RTL layouts (CSS logical properties)
- Format dates/numbers/currencies per locale
- Account for text expansion (30-50% longer than English)
- Use ICU message format for pluralization

---

## Mobile Responsiveness

**Mobile-first CSS approach.** Design for mobile viewport first, enhance for desktop.

### Principles

- Mobile-first breakpoints
- Touch-friendly: minimum 44x44px interactive targets
- No horizontal scrolling on any viewport
- Responsive tables: card layout on mobile or horizontal scroll
- Minimum 16px body text

---

## Security

All code must follow **OWASP WSTG v4.2** and address **OWASP Top 10:2025**.

**References:**
- OWASP WSTG v4.2: https://owasp.org/www-project-web-security-testing-guide/v42/
- OWASP Top 10:2025: https://owasp.org/Top10/2025/
- OWASP Cheat Sheet Series: https://cheatsheetseries.owasp.org/

### OWASP Cheat Sheets (Consult Before Implementation)

When implementing security-sensitive features, **read the relevant OWASP Cheat Sheet** and apply its guidance:

| Feature Area | Cheat Sheet |
|--------------|-------------|
| **Authentication** | Authentication, Password Storage, Session Management, Multifactor Authentication |
| **Authorization** | Authorization, Access Control, Insecure Direct Object Reference Prevention |
| **Input Validation** | Input Validation, Injection Prevention |
| **SQL/Database** | SQL Injection Prevention, Query Parameterization, Database Security |
| **XSS Prevention** | Cross-Site Scripting Prevention, DOM-based XSS Prevention |
| **CSRF Protection** | Cross-Site Request Forgery Prevention |
| **API Security** | REST Security, Web Service Security |
| **Cryptography** | Cryptographic Storage, Key Management, Transport Layer Security |
| **Error Handling** | Error Handling |
| **Logging** | Logging, Logging Vocabulary |
| **HTTP Security** | HTTP Headers, HTTP Strict Transport Security, Content Security Policy |
| **Secrets** | Secrets Management |
| **CI/CD** | CI/CD Security, Software Supply Chain Security |
| **Cloud/IaC** | Secure Cloud Architecture, Infrastructure as Code Security, Serverless/FaaS Security |
| **.NET Specific** | DotNet Security |

### OWASP Top 10:2025 Compliance

| Rank | Vulnerability | Key Mitigations |
|------|---------------|-----------------|
| **A01** | Broken Access Control | Deny by default, verify ownership, log failures |
| **A02** | Security Misconfiguration | Security headers, remove unused features |
| **A03** | Software Supply Chain Failures | Verify packages, use lockfiles, audit deps |
| **A04** | Cryptographic Failures | AES-256, Argon2id, TLS 1.2+, no hardcoded secrets |
| **A05** | Injection | Parameterized queries, input validation |
| **A06** | Insecure Design | Threat modeling, secure design patterns |
| **A07** | Authentication Failures | MFA, rate limiting, secure sessions |
| **A08** | Software/Data Integrity | Verify signatures, validate serialized data |
| **A09** | Logging & Alerting Failures | Log security events, protect logs |
| **A10** | Mishandling Exceptions | No stack traces leaked, fail securely |

### Authorization - Frontend (UI)

1. **Hide, Don't Disable**: Unauthorized features must be **hidden entirely**
2. **Conditional Rendering**: Check permissions before rendering
3. **Route Guards**: Redirect unauthorized access
4. **No Client-Side Trust**: UI hiding is UX only; enforce server-side

### Authorization - Backend (API)

1. **Tenant Isolation**: Every request scoped to authenticated tenant
2. **Role Validation**: Return `403 Forbidden` for unauthorized access
3. **Deny by Default**: No implicit permissions
4. **Audit Logging**: Log all authorization failures and data modifications

### Security Checklist (Pre-Merge)

- [ ] All endpoints verify resource ownership (no IDOR)
- [ ] Security headers configured, no debug info in prod
- [ ] Dependencies audited, lockfiles used
- [ ] Strong encryption, no hardcoded secrets
- [ ] Parameterized queries, no XSS
- [ ] Threat model reviewed for new features
- [ ] Auth has rate limiting, secure session config
- [ ] Serialized data validated, signatures verified
- [ ] Security events logged (auth failures, access denied)
- [ ] Exceptions handled securely, no stack traces leaked

### Security Logging

```
Log security events (auth failures, access denied)
NEVER log sensitive data (passwords, tokens, PII)
```

### Error Handling (Security)

- Never expose stack traces to users
- Return generic error messages externally
- Log detailed errors internally with correlation IDs

### API Error Codes

All errors return structured responses with machine-readable codes for localization:

```csharp
public class ApiError
{
    public required string Code { get; set; }
    public required string Message { get; set; }
    public object? Details { get; set; }
    public string? TraceId { get; set; }
}
```

Standard codes: `AUTH_REQUIRED`, `AUTH_FORBIDDEN`, `RESOURCE_NOT_FOUND`, `VALIDATION_FAILED`, `RATE_LIMITED`, `SERVER_ERROR`

### HTTP Status Codes

| Code | Usage |
|------|-------|
| `200` | Successful GET, PUT, PATCH |
| `201` | Successful POST creating a resource |
| `204` | Successful DELETE, no response body |
| `400` | Validation errors, malformed request |
| `401` | Missing/invalid authentication |
| `403` | Authenticated but insufficient permissions |
| `404` | Resource not found |
| `409` | Conflict (duplicate, concurrent modification) |
| `422` | Business logic validation failure |
| `429` | Rate limited |
| `500` | Unexpected server error |

### Security Headers (staticwebapp.config.json)

```json
{
  "globalHeaders": {
    "Content-Security-Policy": "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self' https://*.azure.com; frame-ancestors 'none'",
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "Strict-Transport-Security": "max-age=31536000; includeSubDomains",
    "Referrer-Policy": "strict-origin-when-cross-origin",
    "Permissions-Policy": "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()"
  }
}
```

---

## Observability

### Application Insights

- All API functions emit structured telemetry
- Custom metrics for business events (account created, transaction processed)
- Exception tracking with correlation IDs
- Dependency tracking (SQL, external APIs)

### Logging Pattern

```csharp
// Structured logging with named parameters
_logger.LogInformation("Account {AccountId} created for user {UserId}", accountId, userId);
_logger.LogWarning("Authorization denied: {UserId} attempted {Action} on {Resource}", userId, action, resourceId);
```

---

## Serialization

### Dual Serialization Strategy

| Context | Format | Library |
|---------|--------|---------|
| Public APIs (browser clients) | JSON | System.Text.Json |
| Internal APIs (React client) | MemoryPack | MemoryPack 1.21.4 |
| Content negotiation | `Accept` header | `ContentNegotiationMiddleware` |

- **MemoryPack is the primary serialization format** — JSON exists only for development convenience and browser compatibility
- Production: MemoryPack for 7-8x faster serialization, 60% smaller payloads
- All shared DTOs decorated with `[MemoryPackable(GenerateType.VersionTolerant)]` with `[MemoryPackOrder(n)]`
- **TypeScript types auto-generated** from C# DTOs to `src/Client/src/generated/memorypack/`

### Type Mapping: EF Entities vs DTOs

MemoryPack does not reliably handle `decimal` or `DateTimeOffset`. Use this conversion strategy:

| Concern | EF Entity (DB layer) | DTO (API/display layer) | Why |
|---------|---------------------|------------------------|-----|
| Money | `decimal` | `double` | MemoryPack compatible; DB needs `decimal` precision |
| Timestamps | `DateTimeOffset` | `DateTime` (UTC) | MemoryPack compatible; DB needs offset-aware storage |

- **EF entities** use database-accurate types (`decimal`, `DateTimeOffset`)
- **DTOs** use MemoryPack-compatible types (`double`, `DateTime` UTC)
- **Conversion** happens in the service/mapping layer (entity → DTO)
- DTOs containing `decimal` or date fields that **cannot** be converted must use **JSON-only** serialization (no `[MemoryPackable]`)

### MemoryPack Testing Requirements

**Every `[MemoryPackable]` DTO must have a MemoryPack round-trip test.** This is mandatory because:
- `[MemoryPackOrder]` errors are silent at compile time but fail at runtime
- Constructor-less deserialization can skip required property validation
- Field ordering changes break binary compatibility

For every new or modified DTO (entities, requests, responses), add tests that verify:

1. **Round-trip fidelity** — Serialize → deserialize → assert all properties match
2. **Default/empty values** — Round-trip with nulls, empty strings, default enums
3. **Collection properties** — Round-trip with populated and empty collections

```csharp
[Fact]
public void CreateAssetRequest_MemoryPackRoundTrip_PreservesAllProperties()
{
    var original = new CreateAssetRequest { /* populate all fields */ };
    var bytes = MemoryPackSerializer.Serialize(original);
    var deserialized = MemoryPackSerializer.Deserialize<CreateAssetRequest>(bytes);

    deserialized.Should().BeEquivalentTo(original);
}
```

**Do not** rely on generic `TestDto` round-trip tests as a proxy for real DTO coverage. Each DTO has its own `[MemoryPackOrder]` layout and must be tested individually.

---

## UI Implementation

**All UI must follow [`docs/RAJ_FINANCIAL_UI.md`](docs/RAJ_FINANCIAL_UI.md).**

**Component Library**: shadcn/ui (Radix UI primitives + Tailwind CSS)
**Reference**: https://ui.shadcn.com/

Before creating any UI component:
1. Check if shadcn/ui has the component (`npx shadcn-ui@latest add <component>`)
2. Check `docs/RAJ_FINANCIAL_UI.md` for existing specifications
3. Follow component structure and design tokens specified
4. Use Radix UI primitives for accessibility
5. Apply glass morphism via GlassCard component where appropriate
6. Implement mobile-first responsive design with Tailwind
7. Adapt designs to RAJ Financial's gold brand palette

---

## Planning & Progress Tracking

### Execution Plans (Source of Truth)

| Document | Purpose |
|----------|---------|
| [`docs/RAJ_FINANCIAL_EXECUTION_PLAN.md`](docs/RAJ_FINANCIAL_EXECUTION_PLAN.md) | Master execution plan |
| [`docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md`](docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md) | API progress tracking |
| [`docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md`](docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md) | UI progress tracking |
| [`docs/RAJ_FINANCIAL_INTEGRATIONS_API.md`](docs/RAJ_FINANCIAL_INTEGRATIONS_API.md) | API integration specs |
| [`docs/RAJ_FINANCIAL_UI.md`](docs/RAJ_FINANCIAL_UI.md) | UI design specs |

---

## Common Tasks

### Adding a New API Endpoint

1. Write Gherkin feature file in `tests/AcceptanceTests/Features/`
2. Write step definitions (stubs)
3. Write unit tests (security scenarios first) in `tests/Api.Tests/`
4. Create function in `src/Api/Functions/`
5. Add service in `src/Api/Services/` if needed
6. Add DTOs in `src/Shared/Models/`
7. Verify all tests pass, coverage >= 90%
8. Update `docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md`

### Adding a New React Page

1. Write Gherkin feature file for user-facing behavior
2. Write Vitest tests (a11y + behavior) in `pages/__tests__/`
3. Create page component in `src/Client/src/pages/`
4. Add route in router configuration
5. Create child components in `src/Client/src/components/`
6. Add localization strings (i18next or similar)
7. Verify all tests pass
8. Update `docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md`

### Adding Localization (React)

1. Add string to localization JSON (e.g., `locales/en.json`)
2. Add translations to other locale files (`locales/{locale}.json`)
3. Use `useTranslation()` hook in component
4. Use `t('key')` for translated strings

---

## Useful Commands

```bash
# .NET
dotnet build src/RajFinancial.sln                # Build .NET projects
dotnet test                                       # All .NET tests
dotnet test --collect:"XPlat Code Coverage"       # Coverage
dotnet format src/RajFinancial.sln                # Format C# code
cd src/Api && func start                          # Run API locally
dotnet list package --vulnerable                  # Audit .NET deps

# React Client
cd src/Client && npm install                      # Install dependencies
cd src/Client && npm run dev                      # Run dev server
cd src/Client && npm run build                    # Production build
cd src/Client && npm test                         # Run tests
cd src/Client && npm run lint                     # Lint TypeScript
npm audit                                         # Audit npm deps
```

---

## Useful Reminders

- **Run tests before committing** - Never commit code that fails tests
- **Audit dependencies** - Check for vulnerabilities regularly
- **Use parameterized queries** - Never concatenate SQL
- **Validate all input** - Server-side validation is mandatory
- **Log structured data** - Use structured logging for observability
- **Never hardcode secrets** - Use environment variables or secret managers

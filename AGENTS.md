# RAJ Financial - Agent Instructions

> **This file is the single source of truth for all AI agents working in this repository.**
> It includes both universal standards and project-specific instructions.
> Tool-specific overrides: `CLAUDE.md` (Claude) · `.github/copilot-instructions.md` (Copilot).

---

## Variables

| Variable | Description | Example (Windows) | Example (macOS/Linux) |
|----------|-------------|--------------------|-----------------------|
| `${REPOS_ROOT}` | Root directory where tools/repos are cloned | `E:\tools` | `~/tools` |

> **Setup**: Set the `REPOS_ROOT` environment variable on your machine, or mentally substitute the correct path when reading these instructions.

---

## Vertical Slice Implementation

**Implement features as vertical slices — UI to datastore — not horizontal layers.**

When building a feature, complete the full stack for that feature before starting the next:

```
UI Component → API Client / Hook → API Endpoint → Service Layer → Data Access → Database Schema
```

### Rules

1. **One feature at a time** — Finish the entire vertical before moving on
2. **Start from the outside in** — Define the user-facing contract (UI/API shape) first, then work inward
3. **Tests at every layer** — Each slice includes tests for UI, API, service, and data access
4. **Commit per slice** — Each vertical slice should be a single, deployable commit
5. **No partial layers** — Never build "all the API endpoints" then "all the UI" — that's horizontal

### Workflow

```
1. Define the user story / acceptance criteria
2. Write BDD feature file (.feature) for the slice
3. Build UI component (with mock data / stub API)
4. Build API endpoint + service layer
5. Build data access + schema migration
6. Wire everything together
7. Run full vertical test suite (unit + integration + E2E)
8. Commit
```

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

**Project URL**: https://dev.azure.com/jamesconsulting/RAJ%20Financial%20Planner/

## GitHub Repository

**Repo URL**: https://github.com/jamesconsultingllc/rajfinancial
**Owner**: jamesconsultingllc
**Repo**: rajfinancial

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

**Testing (.NET — `tests/Api.Tests/` + `tests/IntegrationTests/`):**
- `xunit` 2.9.3 + `xunit.runner.visualstudio` - Unit test framework
- `Reqnroll` 3.3.0 + `Reqnroll.xUnit` - BDD/Gherkin API integration tests
- `FluentAssertions` 8.8.0 - Assertion library
- `Moq` 4.20.72 - Mocking framework
- `coverlet.collector` 6.0.4 - Code coverage
- `MailKit` 4.14.1 - Email testing

**Testing (E2E — `tests/e2e/`):**
- `@cucumber/cucumber` 11.2.0 - Cucumber.js BDD framework (Gherkin features)
- `@playwright/test` 1.52.0 - Browser automation (Chromium, Firefox, WebKit)
- TypeScript step definitions with `ts-node`
- Pre-authenticated storage states for Entra login (avoids per-scenario login)
- Screenshot on failure, HTML + JSON reports

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
| Private static readonly | PascalCase | `JsonOptions`, `ValidLocales`, `ActivitySource` |
| Private const | SCREAMING_SNAKE_CASE | `OBJECT_ID_CLAIM`, `MAX_RETRIES` |
| Public/internal members | PascalCase | `GetUserProfile()`, `ConnectionString` |

#### Preferences

- Prefer **collection expressions** `[]` over `Array.Empty<T>()` or `new List<T>()`
- Remove **redundant namespace qualifiers** — use `using` directives
- Use **file-scoped namespaces** (`namespace X;`)
- Prefer **pattern matching** over type checks + casts
- Use **primary constructors** where appropriate

#### No Magic Strings or Numbers

**Rule:** Any string or numeric literal that carries meaning and/or appears in more than one place **MUST** be declared as a `const` (or `static readonly` for reference types) and referenced by name. Never inline the literal.

**Applies to:**
- `FunctionContext.Items` keys (e.g., `"UserId"`, `"ClaimsPrincipal"`, `"RequestBodyBytes"`)
- HTTP header names (`"Authorization"`, `"Accept"`, `"Content-Type"`)
- Claim type URIs and claim names
- Cache keys, queue names, topic names, blob container names
- Content-type strings, error codes, status strings
- Magic numbers (limits, timeouts, retry counts, port numbers, buffer sizes)

**Organize by domain:**
- Group related constants into a dedicated static class (e.g., `FunctionContextKeys`, `ClaimTypes`, `HeaderNames`, `ErrorCodes`).
- Place the class next to its primary producer/consumer in the same folder.
- Public constants go in `Shared/`; internal-only go in the consuming project.

**✅ Correct:**
```csharp
public static class FunctionContextKeys
{
    public const string UserId = "UserId";
    public const string ClaimsPrincipal = "ClaimsPrincipal";
    public const string IsAuthenticated = "IsAuthenticated";
}

context.Items[FunctionContextKeys.UserId] = userId;
if (context.Items.TryGetValue(FunctionContextKeys.ClaimsPrincipal, out var p)) { ... }
```

**❌ Wrong:**
```csharp
context.Items["UserId"] = userId;                          // magic string
if (context.Items.TryGetValue("ClaimsPrincipal", out ...)) // typo risk, no rename safety
```

**Why:** Typos in string literals compile silently and fail at runtime (e.g., the EasyAuth `"HttpContext"` vs `"HttpRequestContext"` bug). Constants give compile-time safety, IDE rename refactoring, Find-All-References, and a single source of truth.

**PR review gate:** A reviewer finding any duplicated string/number literal that represents a domain concept **must** block the PR until extracted to a constant.

---

## Architecture Conventions (Enforced)

These rules are mechanically enforced via `SonarAnalyzer.CSharp` (build errors) + `NetArchTest` in `tests/Architecture.Tests/`. Violations fail the build or tests.

### Partial files

- `{ClassName}.Logging.cs` — **allowed.** Used exclusively for source-generated `[LoggerMessage]` methods. These must be partial instance methods on the owning class (mechanical necessity of the logging source generator). Co-locate in a `.Logging.cs` sibling file.
- `{ClassName}.Helper.cs` / `{ClassName}.Extensions.cs` / `{ClassName}.Utils.cs` — **not allowed.** Junk-drawer partials violate SRP by grouping unrelated methods under a meaningless name. Extract each concern to its own noun-named class.

### Pure utility code

- Pure static helpers (mappers, validators, slug generators, format converters, rule evaluators) belong in their own noun-named class, not as private statics on a service.
- **Good:** `EntityMapper`, `EntitySlug`, `EntityRoleRules`, `AssetDepreciation`, `MoneyConversion`.
- **Bad:** `EntityService.MapToDto(...)`, `EntityService.NormalizeSlug(...)` as private statics on the service.

### Service classes

- `*Service` classes may not contain private static methods. If you need a pure function, it's a utility — extract it to its own class.
- Enforced by `Services_ShouldNotHavePrivateStaticMethods` in `tests/Architecture.Tests/ServiceInvariantsTests.cs`.
- Services in the allow-list have pending SRP cleanup tasks cited in the test file (see ADO #623, #625).

### Mapper classes

- Any type named `*Mapper` must be a static class (`abstract sealed`).
- Enforced by `Mappers_ShouldBeStaticClasses` in `tests/Architecture.Tests/MapperInvariantsTests.cs`.

### Dependency boundaries

- `RajFinancial.Shared.Contracts.*` (wire DTOs) must **not** reference `RajFinancial.Shared.Entities.*` classes. Entity enums are allowed (wire protocol legitimately mirrors enum values).
- `RajFinancial.Api.Functions.*` must **not** reference `ApplicationDbContext` directly — always go through a service. `HealthCheckFunction` is the sole allow-listed exception (liveness probe).
- Enforced by `Contracts_ShouldNotDependOnEntityClasses` and `Functions_ShouldNotReferenceApplicationDbContext`.

### Sonar severity=error rules

The following Sonar rules are escalated to build errors in `.editorconfig`:

| Rule | Description |
|------|-------------|
| S138 | Method length limit |
| S1448 | Too many methods per class |
| S1200 | Too many type fan-out dependencies |
| S3776 | Cognitive complexity (threshold 15) |
| S2068 | Hardcoded credentials |
| S4830 | Server certificate validation disabled |
| S5542 | Weak cipher modes |
| S2259 | Null dereference |

Scoped suppressions apply to `tests/**`, EF migrations, generated code, and `Program.cs` (tracked under #624).

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

## Pull Request Review Workflow

**Every PR MUST go through Copilot review and reach zero unresolved comments before merge.**

### Required Loop

1. **Open the PR** targeting `develop` (or `main` for release/hotfix per GitFlow).
2. **Request a Copilot review immediately** — `gh pr edit <num> --add-reviewer Copilot` or use the GitHub UI ("Request review" → Copilot). Do not wait for human review first.
3. **Watch for review comments.** Poll with `gh pr view <num> --json reviews,comments` or wait for notifications. Copilot's review typically lands within a few minutes.
4. **Address every comment.** For each finding:
   - If valid: fix the code, push the commit, and **resolve** the conversation thread.
   - If invalid or out-of-scope: reply with a brief justification, then resolve the thread.
   - Never leave a comment unresolved without an explicit reply.
5. **Re-request Copilot review** after pushing fixes — `gh pr edit <num> --add-reviewer Copilot` again triggers a fresh pass on the latest commit.
6. **Iterate steps 3–5 until Copilot's review returns zero new comments** on the latest commit. A clean pass is the merge gate.

### Rules

- **Do not merge** while any Copilot comment is unresolved, even if CI is green.
- **Do not dismiss** Copilot reviews to bypass the gate — address findings or justify them in-thread.
- Human reviewers can be added in parallel; the Copilot loop is **additive**, not a substitute.
- If Copilot flags something already covered by an architecture test or analyzer, link to the rule in your reply and resolve.
- The loop ends only when a re-review produces **no new actionable comments** — zero is the target, not "low".

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
├── tests/IntegrationTests/RajFinancial.IntegrationTests.csproj  # BDD API integration (Reqnroll + Gherkin)
└── tests/e2e/                                               # BDD E2E acceptance (Cucumber.js + Playwright)
    ├── features/                                            # Gherkin .feature files
    ├── step-definitions/                                    # TypeScript step definitions
    └── support/                                             # Hooks, world, config, helpers
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
│   ├── Functions/          # HTTP trigger functions (one file per resource)
│   ├── Services/           # Business logic services (interface + implementation)
│   ├── Middleware/         # Auth, validation, error handling, content negotiation
│   ├── Validators/         # FluentValidation validators
│   └── Data/               # EF Core DbContext, configurations, migrations
├── Client/                 # React + TypeScript (frontend)
│   ├── src/
│   │   ├── pages/          # Route page components
│   │   ├── components/     # Reusable UI components
│   │   │   └── ui/         # shadcn/ui primitives
│   │   ├── auth/           # MSAL authentication
│   │   ├── hooks/          # Custom React hooks
│   │   ├── services/       # API client services (TanStack Query hooks)
│   │   ├── types/          # TypeScript types/interfaces
│   │   ├── locales/        # i18n translation JSON files
│   │   └── generated/      # Auto-generated code (MemoryPack)
│   └── public/             # Static assets
├── Shared/                 # Shared library (multi-target net9.0;net10.0)
│   ├── Entities/           # Domain entities (MemoryPackable, EF-mapped)
│   └── Contracts/          # DTOs, request models, error codes (by feature)
tests/
├── Api.Tests/              # Unit tests (xUnit + FluentAssertions + InMemoryDb)
├── IntegrationTests/       # BDD API integration tests (Reqnroll + Gherkin .feature)
└── e2e/                    # BDD E2E acceptance tests (Cucumber.js + Playwright + Gherkin .feature)
docs/
├── features/               # Feature specification documents
├── plans/                  # Implementation plans
└── lovable-prompts/        # UI generation prompts
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

### Three Test Layers

| Layer | Location | Framework | Purpose |
|-------|----------|-----------|---------|
| **Unit Tests** | `tests/Api.Tests/` | xUnit + FluentAssertions + InMemoryDb + Moq | Service logic, validators, middleware |
| **API Integration (BDD)** | `tests/IntegrationTests/` | Reqnroll + Gherkin `.feature` (C#) | HTTP endpoints against live Functions host |
| **E2E Acceptance (BDD)** | `tests/e2e/` | Cucumber.js + Playwright + Gherkin `.feature` (TypeScript) | Full browser flows, navigation, a11y |
| **Component Tests** | `src/Client/src/**/__tests__/` | Vitest + Testing Library + jsdom | React component behavior |

### Test File Organization

```
src/Client/src/                    # React tests (colocated with source)
├── components/__tests__/          # Component unit tests
├── pages/__tests__/               # Page component tests
├── auth/__tests__/                # Auth logic tests
├── hooks/__tests__/               # Custom hook tests
├── services/__tests__/            # Service hook tests
└── test/                          # Test setup, mocks, utilities

tests/                             # .NET + E2E tests
├── Api.Tests/
│   ├── Middleware/                # Middleware unit tests
│   ├── Services/                  # Service unit tests (per feature)
│   └── Serialization/            # MemoryPack round-trip tests
├── IntegrationTests/              # BDD API integration (Reqnroll)
│   ├── Features/                  # Gherkin .feature files (@api @security tags)
│   ├── StepDefinitions/           # C# step definition classes
│   └── Support/                   # FunctionsHostFixture, TestAuthHelper
└── e2e/                           # BDD E2E acceptance (Cucumber.js + Playwright)
    ├── features/                  # Gherkin .feature files (@requires-auth @mobile tags)
    ├── step-definitions/          # TypeScript step definitions
    └── support/                   # World, hooks, config, Entra auth helpers
```

### BDD Feature File Conventions

**API Integration features** (`tests/IntegrationTests/Features/`):
- Tag with `@api @{feature} @security`
- Background: `Given the API is running`
- Test auth guard (401), CRUD operations, validation (400), IDOR prevention (403/404)
- Use `@smoke` for happy path, `@security @A01` for OWASP scenarios

**E2E features** (`tests/e2e/features/`):
- Tag with `@{feature} @requires-auth` (or `@unauthenticated`)
- Test UI navigation, form interactions, mobile responsiveness, keyboard accessibility
- Use `@smoke` for critical paths, `@mobile` for mobile-specific, `@accessibility` for a11y

### Running Tests

```bash
# .NET unit tests
dotnet test tests/Api.Tests                      # API unit tests only
dotnet test tests/Api.Tests --filter "FullyQualifiedName~EntityService"  # Specific service

# .NET BDD integration tests (requires func start running)
dotnet test tests/IntegrationTests               # All integration scenarios
dotnet test tests/IntegrationTests --filter "Tag=entities"  # Entity scenarios only
dotnet test tests/IntegrationTests --filter "Tag=security"  # Security scenarios only

# All .NET tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# React component tests
cd src/Client && npm test                        # Run all React tests
cd src/Client && npm run test:watch              # Watch mode

# E2E acceptance tests (requires app running)
cd tests/e2e && npm test                         # All E2E scenarios
cd tests/e2e && npm run test:headed              # Headed mode (see browser)
cd tests/e2e && npm run test:chromium            # Chromium only
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

**All new code MUST be born instrumented.** Retrofitting observability is painful (see CA1873 migration). The full [.NET diagnostics stack](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/) is mandatory, not optional.

### The Three Pillars

| Pillar | Answers | Our API |
|---|---|---|
| **Logs** | *What happened?* | `ILogger<T>` via `[LoggerMessage]` source-gen partial methods |
| **Metrics** | *How much / how often?* | `System.Diagnostics.Metrics.Meter` → `Counter<T>`, `Histogram<T>`, `UpDownCounter<T>` |
| **Traces** | *Where did time go? What called what?* | `System.Diagnostics.ActivitySource` → `StartActivity(...)` |

### Stack: OpenTelemetry + Application Insights

**OpenTelemetry is the instrumentation API. Application Insights is the backend.** They are **not alternatives**; we use both.

- **Instrument with OpenTelemetry** (`OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.*`). Vendor-neutral, standard semantic conventions, auto-instruments EF Core / HttpClient / Azure SDK.
- **Export to Application Insights** via `Azure.Monitor.OpenTelemetry.Exporter` paired with `Microsoft.Azure.Functions.Worker.OpenTelemetry` (the Functions isolated worker correlation package). Do **not** use `Azure.Monitor.OpenTelemetry.AspNetCore` — the isolated worker doesn't run an ASP.NET Core server, and the AspNetCore meta-package adds instrumentation that never fires. Gives us KQL, Application Map, Live Metrics, alerting.
- **Set `"telemetryMode": "OpenTelemetry"` in `host.json`.** Without this, the Functions host emits telemetry through the legacy Application Insights pipeline and host↔worker correlation breaks. The `logging.applicationInsights` block is ignored in this mode and should be removed.
- **Never use the classic `TelemetryClient` / `TrackDependency()` API** — it's maintenance mode. Microsoft's direction is OpenTelemetry-first.

### Required Instrumentation (per new service/module)

Every new service, middleware, function group, or domain module must:

1. **Declare a named `ActivitySource`** scoped to the module (e.g., `RajFinancial.Api.Entities`, `RajFinancial.Api.Auth`). One per logical domain — not one per class.
2. **Wrap external boundaries** (`StartActivity`): DB calls, HTTP calls, message publishes, cache ops, auth checks. Internal code: trace hot paths only.
3. **Declare a named `Meter`** and emit at least one domain-relevant metric (counter of operations, histogram of duration, gauge of size). Name metrics with dotted lowercase: `entities.created.count`, `auth.failures.count`, `db.query.duration.ms`.
4. **Wire the `ActivitySource` and `Meter` into the OTel pipeline** in `Program.cs` (`.AddSource("...")`, `.AddMeter("...")`).
5. **Emit source-gen logs** per the Logging Pattern below — every log site gets an `EventId` in a reserved range.
6. **Tag activities with standard attributes** — prefer OpenTelemetry semantic conventions (`db.system`, `http.method`, `user.id`) over ad-hoc names.

### Reserved Domain Names

| Domain | `ActivitySource` / `Meter` | EventId range |
|---|---|---|
| Auth / Authentication | `RajFinancial.Api.Auth` | `1000–1999` |
| Assets | `RajFinancial.Api.Assets` | `2000–2999` |
| Entities | `RajFinancial.Api.Entities` | `3000–3999` |
| User Profile | `RajFinancial.Api.UserProfile` | `4000–4999` |
| Middleware | `RajFinancial.Api.Middleware` | `5000–5999` |
| Client Management | `RajFinancial.Api.ClientManagement` | `6000–6999` |
| Authorization | `RajFinancial.Api.Authorization` | `7000–7999` |
| AI | `RajFinancial.Api.Ai` | `8000–8999` |
| Testing / diagnostics | `RajFinancial.Api.Testing` | `9000–9999` |

### Business Counters: Centralized Interceptor Only

**Domain-event counters (created/updated/deleted/etc.) are emitted exclusively by `BusinessEventsInterceptor` (`src/Api/Data/Interceptors/`).** Services and functions MUST NOT call `Counter<long>.Add(...)` for these events directly.

- **Single source of truth** is `src/Api/Observability/TelemetryMeters.cs` — one `Meter` per domain, one `Counter<long>` per business event. Do **not** re-declare a duplicate `Meter` with the same name elsewhere; doing so causes the counter to double-emit.
- The interceptor snapshots `ChangeTracker` in `SavingChanges[Async]`, drains-and-emits in `SavedChanges[Async]` (success), and emits the userprofile-conflict counter in `SaveChangesFailed[Async]` (failure) using the two-arm rule (test `DbUpdateConcurrencyException` first because it inherits from `DbUpdateException`).
- Helpers like `AssetsTelemetry`, `EntityTelemetry`, `ClientManagementTelemetry`, and `UserProfileTelemetry` keep only an `ActivitySource`, tag-name constants, and validation-time / histogram instruments (e.g., `EnsureDuration`, `SelfAssignmentBlocked`). They do **not** declare per-domain `Counter<long>` fields.
- **To add a new domain event:** add the `Counter<long>` to `TelemetryMeters`, then extend `BusinessEventsInterceptor.Snapshot` to map the EF entity state transition to a `PendingBusinessEvent`. Do **not** sprinkle `RecordXxx` helpers across services.
- Span enrichment for `user.id` / `user.tenant_id` / route values is handled by `TelemetryEnrichmentMiddleware` (`src/Api/Middleware/`). Functions MUST NOT manually `SetTag("user.id", ...)` on every activity — the middleware tags the per-invocation `Activity.Current` once.

### Canonical Function / Service Pattern

See [`docs/patterns/service-function-pattern.md`](docs/patterns/service-function-pattern.md) for the single written standard covering function ↔ service ↔ middleware responsibilities, authorization modes, activity naming, layered exception recording, and IDOR handling. Reviewers should reject PRs that drift from that pattern.

### Log Level Policy by Environment

**Strict rule — enforced via `appsettings.{Environment}.json`:**

| Environment | Default | Microsoft.* | EF Core | Our code |
|---|---|---|---|---|
| **Development** | `Debug` | `Information` | `Information` | `Debug` / `Trace` allowed |
| **Staging** | `Information` | `Warning` | `Warning` | `Information` |
| **Production** | `Warning` | `Warning` | `Warning` | `Warning` or above |

**Rationale:** Verbose/Informational logs in prod bloat Application Insights ingestion (cost) and drown signal in noise. Anything needed in prod should be `Warning+` or a metric/trace — not a log.

### Local Development: File Logging, Not Cloud

**Development runs MUST log to a rolling file, not to Application Insights.** This avoids:
- Bloating Azure ingestion costs during day-to-day dev
- Polluting production dashboards and alerts with dev traffic
- Leaking local test data (fake PII, seed data) into cloud retention

**Implementation:**
- Add a file-logger provider (e.g., `NReco.Logging.File` or Serilog with file sink) wired **only** when `IHostEnvironment.IsDevelopment()`.
- Write to `logs/rajfinancial-{Date}.log` at the repo root (gitignored).
- Skip `AddOpenTelemetryExporter(Azure Monitor)` and `AddApplicationInsights*` in Development.
- Keep OTel's **Console** exporter on in Dev for traces/metrics — cheap, useful, and local-only.

### Health Checks

- **Every service MUST expose** `/health/live` (process alive) and `/health/ready` (can serve traffic — DB reachable, config loaded, required dependencies up).
- Azure Front Door / Container Apps / App Service probes wire to `/health/ready` so broken instances drop out of rotation automatically.
- Add checks via `AddHealthChecks()` with DB/Cosmos/Redis probes as appropriate.

### Profiling & Runtime Diagnostics

- Every deployable service must support `dotnet-counters` and `dotnet-trace` attach without redeploy. Document how in the service README.
- Enable [EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe) (on by default in .NET 10) — do not disable.
- For on-call incident response, prefer pulling a 30-second `dotnet-trace` over log-grepping.

### Logging Pattern (Source-Generated)

**All logging MUST use source-generated `[LoggerMessage]` partial methods** to eliminate boxing, `params object?[]` allocation, and pre-`IsEnabled` template parsing. This is enforced by analyzer rules **CA1873** (*Avoid potentially expensive logging*) and **CA1848** (*Use LoggerMessage delegates*), both treated as warnings.

**References:** [CA1873](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1873) · [High-performance logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/high-performance-logging) · [Source generation for logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation)

#### Rules

1. **Never call `logger.LogInformation`/`LogWarning`/`LogError` directly in new code.** Every log site must route through a `[LoggerMessage]`-annotated `partial` method.
2. **Declare the containing class `partial`** and place the logging methods at the bottom of the same file — not in a shared `LoggerExtensions.cs` or `CommonLogging` class. Co-location keeps call + template greppable together and preserves the `ILogger<T>` category for per-category filtering.
3. **Instance `private partial void LogXxx(...)`** methods (not static extensions). The source generator resolves the `ILogger` field from the class, including primary-constructor fields.
4. **Keep field names domain-specific** (`{AssetId}`, `{EntityId}`, `{ClientUserId}`, `{Function}`). Do **not** collapse them into a generic `{ResourceId}` just to share a method — that breaks Application Insights pivot queries.
5. **Assign `EventId` from the reserved range** for the domain (see table above).
6. **Exception overloads** take an `Exception` parameter — the generator automatically emits the `ex`-accepting overload.
7. **Do not share log methods across classes.** If two classes need the same shape, each keeps its own `[LoggerMessage]` — duplication is cheap, shared extensions are expensive (lost `ILogger<T>` category, awkward disambiguating names).

#### Pattern

```csharp
// ✅ Correct — source-generated, zero allocation when level is disabled
public partial class AssetFunctions(IAssetService assetService, ILogger<AssetFunctions> logger)
{
    private static readonly ActivitySource ActivitySource = new("RajFinancial.Api.Assets");
    private static readonly Meter Meter = new("RajFinancial.Api.Assets");
    private static readonly Counter<long> AssetsCreated = Meter.CreateCounter<long>("assets.created.count");

    public async Task<HttpResponseData> Create(...)
    {
        using var activity = ActivitySource.StartActivity("Assets.Create");
        activity?.SetTag("user.id", userId);

        // ... work ...

        AssetsCreated.Add(1);
        LogAssetCreated(asset.Id, userId);
        return response;
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Asset {AssetId} created for user {UserId}")]
    private partial void LogAssetCreated(Guid assetId, Guid userId);

    [LoggerMessage(EventId = 2099, Level = LogLevel.Error,
        Message = "Failed to create asset for user {UserId}")]
    private partial void LogAssetCreateFailed(Exception ex, Guid userId);
}
```

```csharp
// ❌ Do NOT use — boxes userId/accountId, allocates params array, parses template on every call
_logger.LogInformation("Account {AccountId} created for user {UserId}", accountId, userId);
```

#### Why

Even with structured templates, direct `logger.LogX(template, args)` calls:
- Allocate a `params object?[]` per call
- **Box** every value-type arg (`Guid`, `int`, `bool`, enums)
- Re-parse the template at runtime
- Pay all of the above **before** the `IsEnabled(level)` gate

Source-gen emits a cached delegate with an `IsEnabled` guard before argument evaluation. Disabled level ≈ zero allocation; enabled level = direct typed call, no boxing.

### PR Review Gate (Observability)

Reviewers **MUST** block PRs that:
- Introduce direct `logger.LogX(...)` calls in `src/Api` (enforced by CA1848/CA1873 analyzers too).
- Add a new service/module without declaring an `ActivitySource` and `Meter`.
- Add a per-domain `Counter<long>.Add(...)` for a business event from a service or function — these MUST go through `BusinessEventsInterceptor` + `TelemetryMeters`.
- Re-declare a `Meter` with a name already in `TelemetryMeters` (causes double-emit).
- Add an external call (DB, HTTP, queue, cache) without wrapping in `ActivitySource.StartActivity(...)`.
- Raise default log level in `appsettings.Production.json` above `Warning` without explicit justification.
- Wire Application Insights exporter for Development environment.
- Expose a new deployable without `/health/live` + `/health/ready` endpoints.

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
| Timestamps | `DateTimeOffset` | `DtoDateTime` | MemoryPack compatible wrapper; implicit conversions eliminate manual `.UtcDateTime` calls |

- **EF entities** use database-accurate types (`decimal`, `DateTimeOffset`)
- **DTOs** use MemoryPack-compatible types (`double`, `DtoDateTime`)
- **Conversion** happens automatically via implicit operators (`DtoDateTime` ↔ `DateTimeOffset`)
- DTOs containing `decimal` or date fields that **cannot** be converted must use **JSON-only** serialization (no `[MemoryPackable]`)

**DtoDateTime** (`src/Shared/DtoDateTime.cs`) is a wrapper struct that enables seamless entity ↔ DTO mapping:

```csharp
// Entity → DTO (implicit conversion)
dto.CreatedAt = entity.CreatedAt;   // DateTimeOffset → DtoDateTime

// DTO → Entity (implicit conversion)  
entity.CreatedAt = dto.CreatedAt;   // DtoDateTime → DateTimeOffset
```

See `docs/ASSET_TYPE_SPECIFICATIONS.md` §Serialization Strategy for full documentation.

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

**Component Library**: shadcn/ui (Radix UI primitives + Tailwind CSS)
**Reference**: https://ui.shadcn.com/

Before creating any UI component:
1. Check if shadcn/ui has the component (`npx shadcn-ui@latest add <component>`)
2. Check `docs/features/` for existing feature specifications
3. Follow component structure and design tokens specified
4. Use Radix UI primitives for accessibility
5. Apply glass morphism via GlassCard component where appropriate
6. Implement mobile-first responsive design with Tailwind
7. Adapt designs to RAJ Financial's gold brand palette

---

## Planning & Progress Tracking

### Planning Documents

| Document | Purpose |
|----------|---------|
| [`docs/features/`](docs/features/) | Feature design specifications |
| [`docs/plans/`](docs/plans/) | Implementation plans |
| [`docs/features/12-entity-structure.md`](docs/features/12-entity-structure.md) | Entity-First Architecture design |
| [`docs/plans/2026-03-08-entity-restructure.md`](docs/plans/2026-03-08-entity-restructure.md) | Entity restructure implementation plan |

---

## Common Tasks

### Adding a New API Endpoint

1. Write BDD feature file in `tests/IntegrationTests/Features/{Feature}.feature`
2. Write step definition stubs in `tests/IntegrationTests/StepDefinitions/{Feature}Steps.cs`
3. Write unit tests (security first) in `tests/Api.Tests/Services/{Feature}/`
4. Create error codes in `src/Shared/Contracts/{Feature}/{Feature}ErrorCodes.cs`
5. Create DTOs in `src/Shared/Contracts/{Feature}/`
6. Create validator in `src/Api/Validators/`
7. Create service interface + implementation in `src/Api/Services/{Feature}/`
8. Create Functions endpoint in `src/Api/Functions/{Feature}/`
9. Register service + validator in `src/Api/Program.cs`
10. Verify all tests pass, coverage >= 90%

### Adding a New React Page

1. Write E2E BDD feature file in `tests/e2e/features/{Feature}.feature`
2. Write step definitions in `tests/e2e/step-definitions/{feature}.steps.ts`
3. Write Vitest tests (a11y + behavior) in `src/Client/src/pages/__tests__/`
4. Create TypeScript types in `src/Client/src/types/{feature}.ts`
5. Create TanStack Query service hooks in `src/Client/src/services/{feature}-service.ts`
6. Create page component in `src/Client/src/pages/{Feature}.tsx`
7. Add route in `src/Client/src/App.tsx`
8. Create child components in `src/Client/src/components/{feature}/`
9. Add i18n translation keys in `src/Client/src/locales/en/{feature}.json`
10. Verify all tests pass

### Adding Localization (React)

1. Add translation keys to `src/Client/src/locales/en/{feature}.json`
2. Use `useTranslation()` hook: `const { t } = useTranslation();`
3. Use `t('feature.section.key')` for translated strings
4. Use `Intl.NumberFormat` for currency/number formatting
5. Use `Intl.DateTimeFormat` for date formatting
6. Account for text expansion (30-50% longer than English)

---

## Useful Commands

```bash
# .NET — Build & Run
dotnet build src/RajFinancial.sln                # Build all .NET projects
cd src/Api && func start                          # Run API locally
dotnet format src/RajFinancial.sln                # Format C# code
dotnet list package --vulnerable                  # Audit .NET deps

# .NET — Tests
dotnet test tests/Api.Tests                       # Unit tests
dotnet test tests/IntegrationTests                # BDD API integration tests
dotnet test --collect:"XPlat Code Coverage"       # All tests with coverage
dotnet test tests/Api.Tests --filter "FullyQualifiedName~EntityService"  # Specific service

# React Client — Build & Run
cd src/Client && npm install                      # Install dependencies
cd src/Client && npm run dev                      # Run dev server
cd src/Client && npm run build                    # Production build
cd src/Client && npm run lint                     # Lint TypeScript
npm audit                                         # Audit npm deps

# React Client — Tests
cd src/Client && npm test                         # Component tests (Vitest)
cd src/Client && npm run test:watch              # Watch mode

# E2E Acceptance Tests (Cucumber.js + Playwright)
cd tests/e2e && npm test                          # All E2E BDD scenarios
cd tests/e2e && npm run test:headed               # Headed mode (see browser)
cd tests/e2e && npm run test:chromium             # Chromium only
cd tests/e2e && npm run playwright:install        # Install browser binaries

# GitHub PR review iteration
gh pr edit <PR#> --add-reviewer copilot-pull-request-reviewer  # Re-request Copilot review on a PR
```

---

## Copilot CLI Plugins

This repo standardizes on the following Microsoft-authored plugins from the
[`awesome-copilot`](https://github.com/github/awesome-copilot) marketplace
(included with Copilot CLI by default). Plugin contents are **not** vendored
into this repo — each contributor (and the Copilot cloud agent, via
`.github/workflows/copilot-setup-steps.yml`) installs them at the user level.

| Plugin | Purpose |
|--------|---------|
| `dotnet` | .NET development skills |
| `dotnet-diag` | .NET diagnostics skills |
| `azure` | Azure MCP / Functions / infra skills |
| `modernize-dotnet` | .NET modernization workflows |
| `microsoft-docs` | Microsoft Learn docs lookup |
| `advanced-security` (from `copilot-plugins`) | Local pre-commit secret scanning |

One-time local install (idempotent; safe to re-run if a transient network error occurs):

```bash
for plugin in dotnet dotnet-diag azure modernize-dotnet microsoft-docs; do
  copilot plugin install "$plugin@awesome-copilot"
done
copilot plugin install advanced-security@copilot-plugins
copilot plugin list
```

Restart the Copilot CLI session (`/restart`) after install so the new skills load.

---

## AI Automations (Claude Code & Copilot)

The following automations are configured for this project. Both Claude Code and GitHub Copilot should understand and use these workflows.

### Hooks (Claude Code — `.claude/settings.json`)

Hooks run automatically on every AI file edit. They are Node.js scripts in `.claude/hooks/` and run cross-platform (Windows + macOS).

| Hook | Trigger | Script | What It Does |
|------|---------|--------|-------------|
| Block secrets | PreToolUse (Edit/Write) | `block-secrets.js` | Blocks any AI edit to `local.settings.json` — contains Azure connection strings |
| Auto-lint | PostToolUse (Edit/Write) | `lint-client.js` | Runs `eslint --fix` on any `.ts`/`.tsx` file edited inside `src/Client/` |

### Skills (Claude Code slash commands + Copilot workflows)

Skills are defined in `.claude/skills/`. Claude Code users invoke them as `/skill-name`. Copilot users ask for the same workflow by name — Copilot follows the same procedure documented below.

#### `/gen-migration` — Generate EF Core Migration

**Full procedure:** `.claude/skills/gen-migration/SKILL.md`

When asked to create a migration (any phrasing: "add migration", "scaffold migration", "create DB migration"):

1. Validate the migration name is PascalCase and describes the schema change
2. Run: `cd src/Api && dotnet ef migrations add <Name> --project . --startup-project .`
3. Run: `dotnet build src/RajFinancial.sln --nologo -v:q` — build must pass
4. Present a checklist: confirm `Up()`/`Down()` are correct, no destructive operations, indexes added where needed
5. Report the generated file path, operation count, and build status

**Architecture context:** DbContext at `src/Api/Data/ApplicationDbContext.cs`, entity configurations at `src/Api/Data/Configurations/`, entities at `src/Shared/Entities/`.

#### `/new-api-function` — Scaffold Azure Function Endpoint

**Full procedure:** `.claude/skills/new-api-function/SKILL.md`

When asked to create a new API endpoint (any phrasing: "add endpoint", "create function", "scaffold resource"):

1. Read the reference implementations in `src/Api/Functions/ClientManagementFunctions.cs` and `src/Api/Services/ClientManagement/`
2. Create all four layers following the established patterns:
   - `src/Api/Functions/<Resource>Functions.cs` — `partial class`, primary constructor, `[RequireRole]`, XML doc
   - `src/Api/Functions/<Resource>Functions.Logging.cs` — `[LoggerMessage]` partial methods
   - `src/Api/Services/<Resource>/I<Resource>Service.cs` — interface
   - `src/Api/Services/<Resource>/<Resource>Service.cs` — implementation (no private static methods — architecture rule)
   - `src/Api/Validators/<Resource>RequestValidator.cs` — `AbstractValidator<T>`
   - `src/Shared/Contracts/<Resource>/` — request/response records (no entity references — architecture rule)
3. Register in `src/Api/Configuration/ApplicationServicesRegistration.cs`
4. Create BDD tests first: `tests/IntegrationTests/Features/<Resource>.feature` (Gherkin), then unit tests
5. Run `dotnet build` and `dotnet test tests/Api.Tests` — both must pass

### Subagent (Claude Code — `.claude/agents/`)

#### `security-reviewer`

**Full definition:** `.claude/agents/security-reviewer.md`

Invoke this agent before any PR that touches auth, middleware, financial data services, or DTOs. It checks:

- JWT/OIDC token validation parameters
- `[RequireRole]` coverage and IDOR risks
- EF Core parameterized queries and user-identity scoping
- DTO PII exposure and error response safety
- Frontend token handling and XSS patterns

**Copilot:** Run a security review by asking "review this code for security issues using the security-reviewer checklist in `.claude/agents/security-reviewer.md`".

### MCP Server (Claude Code — `.mcp.json`)

#### GitHub MCP

Enables Claude Code to query GitHub directly: PR status, workflow run results, branch protection, issues. Uses GitHub's official **remote** MCP server (`https://api.githubcopilot.com/mcp/`) — no Docker or local binary required.

**Setup (one-time per developer):** export a PAT with `repo` + `workflow` scopes:

```bash
# Easiest: reuse the gh CLI's token (added to ~/.zshrc so every new shell picks it up)
export GITHUB_PERSONAL_ACCESS_TOKEN="$(gh auth token)"
```

The server config is in `.mcp.json` and is checked into the repo so all contributors get the same MCP setup automatically. The `Authorization: Bearer ${GITHUB_PERSONAL_ACCESS_TOKEN}` header is substituted at runtime — no token is stored in the file.

> Note: The Copilot CLI ships its own GitHub MCP server using its own auth, so this `.mcp.json` is primarily for Claude Code and other MCP hosts.

**Use cases:**
- "Check if the CI workflows passed on this branch"
- "List open PRs targeting develop"
- "Show me the failing step in the azure-functions workflow"

#### Azure DevOps MCP

Enables Claude Code to query Azure DevOps directly: work items, build pipelines, repos, wikis. Uses Microsoft's official remote MCP server (`https://mcp.dev.azure.com/jamesconsulting`) — no local install. Authenticates via the Azure CLI's signed-in user, so you must `az login` once.

**Setup (one-time per developer):**

```bash
az login
# Confirm you can resolve the org:
az account show
```

The server config is in `.mcp.json` and is checked into the repo. The Microsoft Learn docs for this MCP are at https://learn.microsoft.com/azure/devops/integrate/mcp/.

> ADO project for this repo is always **"RAJ Financial Planner"** (org: `jamesconsulting`). Don't prompt for project on `ado-wit_*` calls.

**Use cases:**
- "Read AB#545 and tell me the acceptance criteria"
- "Create a child Task under Feature #523 for the Ollama provider"
- "Show me the latest build for the develop branch pipeline"

---

## Pre-Commit / Pre-Merge Test Gate (MANDATORY)

**Before any `git commit`, `git push`, or `gh pr merge`, run the FULL local test pipeline. CI green is not a substitute — it skips the deployed integration tier on PR builds.**

Required sequence on the branch you're about to commit/merge:

1. **Bring up the dev stack** (idempotent; safe to re-run):
   ```bash
   pwsh scripts/dev-up.ps1
   ```
   This starts SQL Server + Azurite via `docker-compose.dev.yml`, applies pending EF migrations, and seeds `local.settings.json` if missing.

2. **Run unit + architecture tests** (scoped — do **not** run the whole solution here, since `src/RajFinancial.sln` also includes `RajFinancial.IntegrationTests`, which requires the Functions host from step 3):
   ```bash
   dotnet test tests/Api.Tests --nologo -v:q
   dotnet test tests/Architecture.Tests --nologo -v:q
   ```
   Required: 0 failures across both projects.

3. **Start the Functions host over HTTPS** (in a second shell, leave running). HTTPS is required because `tests/IntegrationTests` defaults its base URL to `https://localhost:7071`:
   ```bash
   cd src/Api && func start --useHttps
   ```
   Wait for `Host lock lease acquired` and confirm `curl -k https://localhost:7071/api/health/live` returns 200.

4. **Run BDD integration tests against the live host:**
   ```bash
   dotnet test tests/IntegrationTests --nologo -v:q
   ```
   Required: 0 failures. CI's `Integration Tests (Dev)` job is **skipped** on PRs against `develop` (it only runs after the post-merge Deploy to Dev), so this local run is the *only* pre-merge integration coverage that exists.

5. **Stop the host** (`Ctrl+C`) and optionally `pwsh scripts/dev-down.ps1`.

**Exceptions (documentation-only changes):** if the diff touches only `*.md`, `docs/`, `.github/copilot-instructions.md`, `CLAUDE.md`, or `AGENTS.md`, the integration tier may be skipped. Unit + architecture tests still required.

**Never `gh pr merge --admin` to bypass a failing or unrun integration tier.** If integration tests can't be run locally (e.g., Docker unavailable), state that explicitly and stop — do not merge.

## Useful Reminders

- **Run tests before committing** - Never commit code that fails tests (see Pre-Commit Test Gate above for the required sequence)
- **Audit dependencies** - Check for vulnerabilities regularly
- **Use parameterized queries** - Never concatenate SQL
- **Validate all input** - Server-side validation is mandatory
- **Log structured data** - Use structured logging for observability
- **Never hardcode secrets** - Use environment variables or secret managers

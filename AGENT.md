# RAJ Financial - Agent Instructions

> **This is the primary instruction file for all AI agents (Claude, Copilot, etc.).**
> Tool-specific overrides live in `CLAUDE.md` (Claude) and `.github/copilot-instructions.md` (Copilot).

---

## Development Methodology: BDD/TDD First

**NO CODE WITHOUT TESTS FIRST.** This is non-negotiable.

Every feature follows this strict implementation order:

### 1. Tests First (BDD/TDD)

```
Write failing acceptance test (Gherkin) -> Write failing unit tests -> Write minimum code to pass -> Refactor
```

- **BDD**: Write Gherkin `.feature` files (Reqnroll) defining the expected behavior BEFORE any implementation
- **TDD**: Write xUnit/bUnit unit tests that fail BEFORE writing production code
- **Red-Green-Refactor**: Fail first, pass minimally, then clean up
- **No exceptions**: Even "simple" changes get tests first. If you think it's too simple to test, test it anyway.

#### BDD Workflow (Acceptance Tests)

```gherkin
# 1. Write the feature file FIRST
Feature: Account Balance Retrieval
  As an authenticated user
  I want to view my account balance
  So that I can track my finances

  Scenario: User retrieves their own account balance
    Given I am authenticated as user "user@example.com"
    And I have an account with balance 1500.00
    When I request my account balance
    Then the response status should be 200
    And the balance should be 1500.00

  Scenario: User cannot access another user's account
    Given I am authenticated as user "user@example.com"
    When I request account balance for another user's account
    Then the response status should be 403
    And the error code should be "AUTH_FORBIDDEN"
```

```csharp
// 2. Write the step definitions
[Binding]
public class AccountBalanceSteps
{
    [Given("I am authenticated as user {string}")]
    public void GivenIAmAuthenticatedAs(string email) { /* ... */ }

    [When("I request my account balance")]
    public async Task WhenIRequestMyAccountBalance() { /* ... */ }

    [Then("the balance should be {decimal}")]
    public void ThenTheBalanceShouldBe(decimal expected) { /* ... */ }
}
```

```csharp
// 3. Write unit tests BEFORE the implementation
[Fact]
public async Task GetBalance_AuthenticatedUser_ReturnsBalance()
{
    // Arrange - setup mocks
    // Act - call the method
    // Assert - verify the result
}

[Fact]
public async Task GetBalance_UnauthorizedUser_Returns403()
{
    // Arrange - setup unauthorized context
    // Act - call the method
    // Assert - verify 403 with AUTH_FORBIDDEN code
}
```

```csharp
// 4. ONLY NOW write the production code to make tests pass
```

#### TDD Workflow (Unit Tests)

```
1. Write a test that describes the desired behavior
2. Run it - confirm it FAILS (Red)
3. Write the MINIMUM code to make it pass (Green)
4. Refactor while keeping tests green (Refactor)
5. Repeat for next behavior
```

### 2. Security (Designed In, Not Bolted On)

Security is part of the design, not an afterthought. Every feature must include security from the first test:

- Write security test scenarios in your Gherkin features (unauthorized access, injection, IDOR)
- Write unit tests for authorization checks BEFORE implementing the endpoint
- Threat model new features before coding
- See [Security Requirements](#security-requirements) below

### 3. Accessibility (a11y)

After security tests pass, add accessibility:

- Write accessibility tests (jest-axe or equivalent) BEFORE building UI
- Semantic HTML, ARIA attributes, keyboard navigation
- See [Accessibility Requirements](#accessibility-requirements) below

### 4. Localization (i18n)

After accessibility is validated, add localization:

- All user-facing strings go through `IStringLocalizer` / `.resx` files
- Never hardcode user-facing text
- See [Localization Requirements](#localization-requirements) below

### 5. Business Logic

Only after tests, security, accessibility, and localization scaffolding are in place do you write the actual business logic to make all tests pass.

### Implementation Order Summary

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
| **Client Runtime** | Blazor WebAssembly | net9.0 |
| **Shared Library** | .NET Class Library | net9.0 / net10.0 (multi-target) |
| **Hosting** | Azure Static Web Apps | v4 Functions |
| **Database** | SQL Server via EF Core | 10.0.2 |
| **Auth** | Azure AD / MSAL | Microsoft.Authentication.WebAssembly.Msal 9.0.0 |
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

**Client (`src/Client`):**
- `Microsoft.AspNetCore.Components.WebAssembly` 9.0.12 - Blazor WASM runtime
- `Microsoft.AspNetCore.Components.Authorization` 9.0.10 - Auth components (`<AuthorizeView>`)
- `Microsoft.Authentication.WebAssembly.Msal` 9.0.0 - MSAL authentication
- `Microsoft.Extensions.Localization` 9.0.12 - IStringLocalizer support
- `JetBrains.Annotations` 2025.2.4 - Code annotations

**Testing (`tests/`):**
- `xunit` 2.9.3 + `xunit.runner.visualstudio` - Unit test framework
- `bunit` 2.5.3 - Blazor component testing
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
- **Auth pattern**: Azure AD B2C via MSAL, `<AuthorizeView>` in Blazor, `[Authorize]` on Functions
- **Localization**: `IStringLocalizer<SharedResources>` + `.resx` resource files
- **Error handling**: Structured `ApiError` responses with machine-readable codes

### Solution Structure

```
src/RajFinancial.sln
├── src/Api/RajFinancial.Api.csproj              # Azure Functions API (net10.0)
├── src/Client/RajFinancial.Client.csproj        # Blazor WASM Client (net9.0)
├── src/Shared/RajFinancial.Shared.csproj        # Shared models (net9.0;net10.0)
├── tests/UnitTests/RajFinancial.UnitTests.csproj           # xUnit + bUnit (net10.0)
└── tests/AcceptanceTests/RajFinancial.AcceptanceTests.csproj  # Reqnroll + Playwright (net9.0)
```

---

## Project Overview

**Raj Financial** is a financial services application built with:
- **Frontend**: Blazor WebAssembly (Client)
- **Backend**: Azure Functions (.NET Isolated Worker)
- **Shared**: .NET Class Library for shared models and contracts
- **Hosting**: Azure Static Web Apps

### Project Structure

```
src/
├── Api/                    # Azure Functions API (backend)
│   ├── Functions/          # HTTP trigger functions
│   ├── Services/           # Business logic services
│   └── Components/         # Blazor SSR components (if applicable)
├── Client/                 # Blazor WebAssembly (frontend)
│   ├── Pages/              # Routable page components
│   ├── Shared/             # Shared layout components
│   └── wwwroot/            # Static assets
├── Shared/                 # Shared library
│   ├── Entities/           # Domain entities
│   ├── Models/             # DTOs and domain models
│   └── Contracts/          # Interfaces and DTOs
tests/
├── UnitTests/              # xUnit unit tests
└── AcceptanceTests/        # BDD acceptance tests (Reqnroll + Gherkin)
docs/                       # Documentation and planning
```

### Quick Start

```bash
# Run API locally
cd src/Api && func start

# Run Blazor client (separate terminal)
cd src/Client && dotnet run

# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
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
src/Client/wwwroot/images/brand/
├── logo-icon.svg, logo-icon.png       # RF monogram
├── logo-vertical.svg                  # Logo with text below
├── logo-horizontal.svg                # Logo with text right (black)
├── logo-horizontal-color.svg          # Logo with text right (color)
├── logo-color.svg, logo.png           # Full color logo
```

---

## Core Principles (Priority Order)

1. **BDD/TDD** - Tests first, always. No exceptions.
2. **Security First** - Designed into every feature from the start
3. **Accessibility** - WCAG 2.1 AA minimum, semantic HTML
4. **Localization** - All user-facing text localizable via IStringLocalizer
5. **Mobile Responsiveness** - Mobile-first CSS approach
6. **Documentation** - XML docs on all public APIs, JSDoc where applicable
7. **Observability** - Structured logging, metrics, telemetry
8. **SOLID Principles** - Clean architecture, dependency inversion
9. **DRY** - Extract reusable components, services, and utilities

---

## Testing Requirements

### Frameworks

| Framework | Purpose |
|-----------|---------|
| **xUnit** | Unit tests |
| **bUnit** | Blazor component tests |
| **Reqnroll** | BDD acceptance tests (Gherkin `.feature` files) |
| **Playwright** | Browser automation / E2E tests |
| **jest-axe** (or equivalent) | Accessibility testing |

### Coverage

- **90% minimum** code coverage for all new code
- Unit tests for ALL business logic
- Integration tests for ALL API endpoints
- BDD acceptance tests for ALL user-facing features
- Security tests for EVERY endpoint (auth, IDOR, injection)
- Accessibility tests for EVERY UI component

### Test File Organization

```
tests/
├── UnitTests/
│   ├── Api/
│   │   ├── Functions/         # Tests for Azure Functions
│   │   └── Services/          # Tests for business services
│   ├── Client/
│   │   ├── Components/        # bUnit component tests
│   │   └── Pages/             # bUnit page tests
│   └── Shared/
│       └── Models/            # Model validation tests
└── AcceptanceTests/
    ├── Features/              # Gherkin .feature files
    ├── StepDefinitions/       # Step definition classes
    └── Support/               # Test hooks, helpers
```

### Running Tests

```bash
dotnet test                                      # All tests
dotnet test tests/UnitTests                      # Unit tests only
dotnet test tests/AcceptanceTests                # BDD tests only
dotnet test --collect:"XPlat Code Coverage"      # With coverage
dotnet test --filter "Category=Security"         # Security tests only
```

---

## Security Requirements

All code must follow **OWASP WSTG v4.2** and address **OWASP Top 10:2025**.

**References:**
- OWASP WSTG v4.2: https://owasp.org/www-project-web-security-testing-guide/v42/
- OWASP Top 10:2025: https://owasp.org/Top10/2025/

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

```razor
@* CORRECT: Hide unauthorized features *@
<AuthorizeView Policy="users:manage">
    <Authorized>
        <MenuItem>Manage Users</MenuItem>
    </Authorized>
</AuthorizeView>

@* WRONG: Don't just disable *@
<MenuItem disabled="@(!HasPermission("users:manage"))">Manage Users</MenuItem>
```

### Authorization - Backend (API)

1. **Tenant Isolation**: Every request scoped to authenticated tenant
2. **Role Validation**: Return `403 Forbidden` for unauthorized access
3. **Deny by Default**: No implicit permissions
4. **Audit Logging**: Log all authorization failures and data modifications

```csharp
// Always verify resource ownership (prevent IDOR)
var account = await _context.Accounts
    .Where(a => a.Id == accountId && a.UserId == userId)
    .FirstOrDefaultAsync();

if (account == null)
{
    _logger.LogWarning("User {UserId} attempted unauthorized access to account {AccountId}",
        userId, accountId);
    return null;
}
```

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

### XSS Prevention

```razor
@* SAFE: Blazor auto-encodes *@
<p>@userInput</p>

@* DANGEROUS: Never with untrusted input *@
@((MarkupString)untrustedHtml)

@* SAFE: Sanitize first if HTML is required *@
@((MarkupString)HtmlSanitizer.Sanitize(trustedHtml))
```

### SQL Injection Prevention

```csharp
// CORRECT: Parameterized via EF Core
var accounts = await _context.Accounts
    .Where(a => a.UserId == userId && a.Name == searchName)
    .ToListAsync();

// NEVER: String concatenation
var sql = $"SELECT * FROM Accounts WHERE UserId = '{userId}'"; // SQL INJECTION!
```

### Input Validation

```csharp
public class CreateAccountRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Invalid characters")]
    public required string Name { get; set; }

    [Range(0, double.MaxValue)]
    public decimal InitialBalance { get; set; }
}
```

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

### Cryptography Standards

- Passwords: Argon2id or bcrypt (NEVER MD5/SHA1)
- Encryption: AES-256-GCM (NEVER DES/3DES/RC4/ECB)
- TLS 1.2+ required for all connections
- Secrets in Azure Key Vault, never hardcoded

### Security Logging

```csharp
// Log security events
_logger.LogWarning("Authentication failed for user {Email} from IP {IP}", email, ip);
_logger.LogWarning("Authorization denied: {UserId} attempted {Action} on {Resource}", userId, action, resourceId);

// NEVER log sensitive data
_logger.LogInformation("Password: {Password}", password); // NEVER!
```

### Error Handling (No Leaks)

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Payment processing failed for {RequestId}", request.Id);
    return StatusCode(500, new ApiError
    {
        Code = "PAYMENT_FAILED",
        Message = "Payment processing failed. Please try again."
    });
}

// NEVER: return StatusCode(500, ex.ToString());
```

### OWASP Security Checklist (Pre-Merge)

- [ ] **A01**: All endpoints verify resource ownership (no IDOR)
- [ ] **A02**: Security headers configured, no debug info in prod
- [ ] **A03**: Dependencies audited, lockfiles used
- [ ] **A04**: Strong encryption, no hardcoded secrets
- [ ] **A05**: Parameterized queries, no XSS
- [ ] **A06**: Threat model reviewed for new features
- [ ] **A07**: Auth has rate limiting, secure session config
- [ ] **A08**: Serialized data validated, signatures verified
- [ ] **A09**: Security events logged (auth failures, access denied)
- [ ] **A10**: Exceptions handled securely, no stack traces leaked
- [ ] Cookies: HttpOnly, Secure, SameSite
- [ ] TLS 1.2+ for all connections

---

## Accessibility Requirements

WCAG 2.1 AA minimum for all UI components.

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

### Accessible Interactive Elements

```razor
<button @onclick="HandleSubmit"
        aria-label="Submit contact form"
        aria-busy="@isSubmitting"
        disabled="@isSubmitting">
    @(isSubmitting ? "Sending..." : "Send Message")
</button>

<button aria-label="Close dialog" @onclick="OnClose">
    <span class="icon-x" aria-hidden="true"></span>
</button>
```

### Accessibility Testing

```csharp
// Every UI component gets an accessibility test
[Fact]
public async Task Component_ShouldHaveNoAccessibilityViolations()
{
    var cut = RenderComponent<ProjectCard>(p => p.Add(x => x.Project, mockProject));
    // Validate semantic HTML, ARIA attributes, keyboard navigation
}
```

---

## Localization Requirements

All user-facing text must be localizable. Never hardcode strings.

### Blazor Localization

```razor
@inject IStringLocalizer<SharedResources> Localizer

<label for="name">@Localizer["Contact_Form_Name_Label"]</label>
<input id="name" placeholder="@Localizer["Contact_Form_Name_Placeholder"]" />
<button type="submit">@Localizer["Contact_Form_Submit"]</button>
```

### Error Code Localization

API returns error codes; the client localizes them:

```csharp
// API returns code
return BadRequest(new ApiError { Code = "PROJECT_NOT_FOUND" });

// Client maps code to localized string
var message = Localizer[$"Errors_{error.Code}"];
```

### Localization Checklist

- [ ] Never hardcode user-facing strings
- [ ] Use `IStringLocalizer` / `.resx` files
- [ ] Support RTL layouts (CSS logical properties)
- [ ] Format dates/numbers/currencies per locale
- [ ] Account for text expansion (30-50% longer than English)
- [ ] Use ICU message format for pluralization

---

## Mobile Responsiveness

Mobile-first CSS approach. Design for mobile viewport first, enhance for desktop.

### Principles

- Mobile-first breakpoints (base styles for mobile, `md:` and up for desktop)
- Touch-friendly: minimum 44x44px interactive targets
- No horizontal scrolling on any viewport
- Collapsible sidebar for admin layouts
- Responsive tables: card layout on mobile or horizontal scroll
- Minimum 16px body text

### Responsive Admin Layout

```razor
@* Mobile header with hamburger *@
<header class="sticky top-0 z-50 flex items-center justify-between p-4 md:hidden">
    <Logo />
    <button @onclick="ToggleSidebar" aria-label="Toggle menu">
        <span class="icon-menu h-6 w-6"></span>
    </button>
</header>

<div class="flex">
    @* Sidebar: hidden on mobile, visible on desktop *@
    <aside class="@sidebarClasses">
        <Navigation />
    </aside>

    <main class="flex-1 p-4 md:p-6 lg:p-8">
        @ChildContent
    </main>
</div>
```

---

## Code Documentation

### C# (XML Documentation)

```csharp
/// <summary>
/// Retrieves the account balance for the specified account.
/// </summary>
/// <param name="accountId">The unique identifier of the account.</param>
/// <returns>The current account balance.</returns>
/// <exception cref="NotFoundException">Thrown when account is not found.</exception>
public async Task<decimal> GetAccountBalanceAsync(Guid accountId) { ... }
```

### Requirements

- All public methods/classes: purpose, parameters, return values, exceptions
- Complex logic: inline comments for non-obvious algorithms
- Public APIs: request/response examples
- Configuration: all environment variables documented

---

## Observability

### Structured Logging

```csharp
_logger.LogInformation("Project created: {ProjectId} by {UserId}", project.Id, userId);

_logger.LogWarning("Authorization failed: {UserId} attempted {Action} on {Resource}",
    userId, action, resourceId);
```

### Audit Logging

Log all data modifications and sensitive operations with structured context.

---

## Serialization

Use **MemoryPack** for internal serialization (caching, queues, inter-service). JSON for external/public APIs only.

```csharp
[MemoryPackable]
public partial class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

Use `[MemoryPackable(GenerateType.VersionTolerant)]` for evolving schemas.

---

## GitFlow Branching

**Always create feature branches from `develop`, never from `main`.**

| Branch Type | Create From | Merge To | Pattern |
|-------------|-------------|----------|---------|
| `feature/*` | `develop` | `develop` | `feature/descriptive-name` |
| `bugfix/*` | `develop` | `develop` | `bugfix/descriptive-name` |
| `release/*` | `develop` | `main` + `develop` | `release/x.y.z` |
| `hotfix/*` | `main` | `main` + `develop` | `hotfix/x.y.z` |

---

## UI Implementation

**All UI must follow [`docs/RAJ_FINANCIAL_UI.md`](docs/RAJ_FINANCIAL_UI.md).**

**Syncfusion Essential UI Kit**: https://blazor.syncfusion.com/essential-ui-kit/

Before creating any UI component:
1. Check Syncfusion Essential UI Kit for design inspiration
2. Check `docs/RAJ_FINANCIAL_UI.md` for existing specifications
3. Follow component structure and design tokens specified
4. Use Syncfusion Blazor v24+ for complex elements
5. Apply glass morphism via GlassCard component
6. Implement mobile-first responsive design
7. Adapt designs to RAJ Financial's gold brand palette

---

## Planning & Progress Tracking

### Permanent Execution Plans (Source of Truth)

| Document | Purpose |
|----------|---------|
| [`docs/RAJ_FINANCIAL_EXECUTION_PLAN.md`](docs/RAJ_FINANCIAL_EXECUTION_PLAN.md) | Master execution plan |
| [`docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md`](docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md) | API progress tracking |
| [`docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md`](docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md) | UI progress tracking |
| [`docs/RAJ_FINANCIAL_INTEGRATIONS_API.md`](docs/RAJ_FINANCIAL_INTEGRATIONS_API.md) | API integration specs |
| [`docs/RAJ_FINANCIAL_UI.md`](docs/RAJ_FINANCIAL_UI.md) | UI design specs |

### When Starting Work

1. Read execution plans to understand current progress
2. Update status as tasks progress (`⬜ Not Started` -> `🟡 In Progress` -> `✅ Complete`)
3. Reference task numbers in commits and PRs

### When Finishing a Task

After completing a task (tests green, code committed, work item closed):

1. **Update `session.md`** - mark the task completed and record the commit hash
2. **Prompt for the next work item** - always end your response with a clear prompt asking whether to continue to the next task in the current feature, e.g.:

   > *Task 474 is done. The next task in Feature 470 is **Task 475 - [title]**. Ready to proceed?*

3. If the feature has no remaining tasks, note that the feature is complete and suggest next steps (e.g., PR creation, branch push).

**Never silently finish.** The user should always know what comes next.

### Temporary Branch Plans

Optional `IMPLEMENTATION_PLAN.md` at repo root for current feature branch. Delete before merging to `develop`.

---

## Common Tasks

### Adding a New API Endpoint

1. Write Gherkin feature file in `tests/AcceptanceTests/Features/`
2. Write step definitions (stubs)
3. Write unit tests (security scenarios first) in `tests/UnitTests/Api/`
4. Create function in `src/Api/Functions/`
5. Add service in `src/Api/Services/` if needed
6. Add DTOs in `src/Shared/Models/`
7. Verify all tests pass, coverage >= 90%
8. Update `docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md`

### Adding a New Blazor Page

1. Write Gherkin feature file for user-facing behavior
2. Write bUnit tests (a11y + localization + behavior)
3. Create page in `src/Client/Pages/` with `@page` directive
4. Create child components in `src/Client/Components/`
5. Add localization strings to `.resx` files
6. Verify all tests pass, coverage >= 90%
7. Update `docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md`

### Adding Localization

1. Add string to `Resources/SharedResources.resx` (English)
2. Add translations to `Resources/SharedResources.{culture}.resx`
3. Inject `IStringLocalizer<SharedResources>` in component
4. Use `@Localizer["Key"]` in Razor markup

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

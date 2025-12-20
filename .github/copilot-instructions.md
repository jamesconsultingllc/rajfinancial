---
applyTo: '**'
---

# Raj Financial - Development Instructions

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

Brand assets (logos, icons, images) source folder:
```
D:\OneDrive - RAJ Financial\RAJ Financial\Assets\All files
```

**Project assets** (copied to repo):
```
src/Client/wwwroot/images/brand/
├── logo-icon.svg              # RF monogram only (black)
├── logo-icon.png              # RF monogram only (color)
├── logo-vertical.svg          # Logo with text below (black)
├── logo-horizontal.svg        # Logo with text to the right (black)
├── logo-horizontal-color.svg  # Logo with text to the right (color)
├── logo-color.svg             # Full color logo
└── logo.png                   # Full color logo (PNG)

src/Client/wwwroot/
├── favicon.ico
├── favicon-16x16.png
├── favicon-32x32.png
├── apple-touch-icon.png
├── android-chrome-192x192.png
├── android-chrome-512x512.png
├── safari-pinned-tab.svg
└── site.webmanifest
```

---

## Core Principles

Follow these principles in order of priority:

1. **Security First** - All code must be secure by default
2. **Mobile Responsiveness** - All UI must be mobile-friendly (mobile-first approach)
3. **Accessibility** - All UI must be accessible (WCAG 2.1 AA)
4. **Localization** - All user-facing text must be localizable
5. **Documentation** - All code must be fully documented
6. **Observability** - Add logging, metrics, and telemetry
7. **SOLID Principles** - Follow Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion
8. **DRY (Don't Repeat Yourself)** - Avoid code duplication; extract reusable components, services, and utilities

---

## Security Requirements

### Authorization - Frontend (UI)

1. **Hide, Don't Disable**: Unauthorized features must be **hidden entirely**, not shown as disabled
2. **Conditional Rendering**: Check permissions before rendering menu items, buttons, pages
3. **Route Guards**: Redirect unauthorized route access attempts
4. **No Client-Side Trust**: UI hiding is for UX only; always enforce server-side

```razor
@* ✅ Correct: Hide unauthorized features *@
<AuthorizeView Policy="users:manage">
    <Authorized>
        <MenuItem>Manage Users</MenuItem>
    </Authorized>
</AuthorizeView>

@* ❌ Incorrect: Don't just disable *@
<MenuItem disabled="@(!HasPermission("users:manage"))">Manage Users</MenuItem>
```

### Authorization - Backend (API)

1. **Tenant Isolation**: Every request scoped to authenticated tenant
2. **Role Validation**: Return `403 Forbidden` for unauthorized access
3. **Deny by Default**: No implicit permissions
4. **Audit Logging**: Log all authorization failures and data modifications

```csharp
[Authorize(Policy = "RequireAdminRole")]
[TenantScoped]
public async Task<IActionResult> ManageUsers() { ... }
```

### API Error Codes and HTTP Status Codes

All API endpoints must return **proper HTTP status codes** along with **structured error responses** for localization:

#### HTTP Status Code Standards

| Status Code | Name | Usage |
|-------------|------|-------|
| `200` | OK | Successful GET, PUT, PATCH requests |
| `201` | Created | Successful POST that creates a resource |
| `204` | No Content | Successful DELETE or update with no response body |
| `400` | Bad Request | Validation errors, malformed request |
| `401` | Unauthorized | Missing or invalid authentication |
| `403` | Forbidden | Authenticated but insufficient permissions |
| `404` | Not Found | Resource does not exist |
| `409` | Conflict | Duplicate resource, concurrent modification |
| `422` | Unprocessable Entity | Business logic validation failure |
| `429` | Too Many Requests | Rate limiting |
| `500` | Internal Server Error | Unexpected server error |
| `503` | Service Unavailable | Dependent service down, maintenance |

#### Standardized Error Response Format

All error responses must use this structure:

```csharp
/// <summary>
/// Standard API error response.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Machine-readable error code for client-side localization.
    /// </summary>
    public required string Code { get; set; }
    
    /// <summary>
    /// Human-readable message (default English, clients localize by Code).
    /// </summary>
    public required string Message { get; set; }
    
    /// <summary>
    /// Optional additional details (field errors, resource IDs, etc.).
    /// </summary>
    public object? Details { get; set; }
    
    /// <summary>
    /// Trace ID for debugging and support tickets.
    /// </summary>
    public string? TraceId { get; set; }
}
```

#### Returning Errors with Correct Status Codes

```csharp
// ✅ Correct: Return proper status code with structured error
return req.CreateResponse(HttpStatusCode.NotFound, new ApiError
{
    Code = "ACCOUNT_NOT_FOUND",
    Message = "Account not found",
    Details = new { AccountId = id },
    TraceId = Activity.Current?.Id
});

// ✅ Correct: 400 for validation errors
return req.CreateResponse(HttpStatusCode.BadRequest, new ApiError
{
    Code = "VALIDATION_FAILED",
    Message = "Invalid request data",
    Details = new { Field = "email", Error = "Invalid email format" }
});

// ✅ Correct: 403 for authorization failures
return req.CreateResponse(HttpStatusCode.Forbidden, new ApiError
{
    Code = "AUTH_FORBIDDEN",
    Message = "Insufficient permissions to access this resource"
});

// ❌ Incorrect: Wrong status code or unstructured response
return req.CreateResponse(HttpStatusCode.OK, "Account not found"); // Should be 404
return req.CreateResponse(HttpStatusCode.BadRequest, "Error occurred"); // No structure
```

#### Standard Error Codes

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| `AUTH_REQUIRED` | 401 | Authentication required |
| `AUTH_FORBIDDEN` | 403 | Insufficient permissions |
| `AUTH_TOKEN_EXPIRED` | 401 | Token has expired |
| `RESOURCE_NOT_FOUND` | 404 | Generic resource not found |
| `ACCOUNT_NOT_FOUND` | 404 | Account does not exist |
| `USER_NOT_FOUND` | 404 | User does not exist |
| `VALIDATION_FAILED` | 400 | Input validation error |
| `INVALID_INPUT` | 400 | Malformed request data |
| `INSUFFICIENT_FUNDS` | 422 | Business rule: not enough balance |
| `ACCOUNT_LOCKED` | 422 | Business rule: account is locked |
| `DUPLICATE_REQUEST` | 409 | Idempotency conflict |
| `RATE_LIMITED` | 429 | Too many requests |
| `SERVER_ERROR` | 500 | Internal server error |
| `SERVICE_UNAVAILABLE` | 503 | Downstream service unavailable |

---

## Code Documentation

All code must be fully documented:

### C# (.NET XML Documentation)

```csharp
/// <summary>
/// Retrieves all published projects for public display.
/// </summary>
/// <remarks>
/// Projects are filtered by IsPublished=true and sorted by CompletionDate descending.
/// This endpoint is publicly accessible without authentication.
/// </remarks>
/// <returns>A list of published projects with public-safe fields only.</returns>
/// <response code="200">Returns the list of projects</response>
/// <response code="500">If an internal error occurs</response>
[HttpGet]
[ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetPublishedProjects() { ... }
```

### Blazor Component Documentation

```razor
@*
    AccountSummary.razor
    
    Displays a summary of the user's financial accounts with balances.
    
    Parameters:
    - AccountId (Guid): The unique identifier for the account
    - ShowTransactions (bool): Whether to display recent transactions
    
    Usage:
    <AccountSummary AccountId="@selectedAccountId" ShowTransactions="true" />
*@

@code {
    /// <summary>
    /// The unique identifier for the account to display.
    /// </summary>
    [Parameter]
    public Guid AccountId { get; set; }
    
    /// <summary>
    /// Whether to show recent transactions in the summary.
    /// </summary>
    [Parameter]
    public bool ShowTransactions { get; set; } = false;
}
```

### Documentation Requirements

- **Functions/Methods**: Purpose, parameters, return values, exceptions
- **Classes/Interfaces**: Purpose and usage patterns
- **Complex Logic**: Inline comments for non-obvious algorithms
- **Public APIs**: Request/response examples
- **Configuration**: All environment variables documented

---

## Mobile Responsiveness

All UI must be mobile-friendly using a **mobile-first** design approach:

### Design Principles

1. **Mobile-First CSS**: Write styles for mobile viewports first, then add complexity for larger screens
2. **Touch-Friendly**: All interactive elements must be easily tappable (minimum 44x44px touch targets)
3. **Responsive Layouts**: Use CSS Grid and Flexbox for fluid layouts that adapt to all screen sizes
4. **No Horizontal Scroll**: Content must fit within viewport width on all devices

### CSS Breakpoints (Mobile-First)

Use CSS breakpoints consistently with a mobile-first approach:

```razor
@* ✅ Correct: Mobile-first approach with Blazor *@
<div class="flex flex-col md:flex-row gap-4">
    <aside class="w-full md:w-64 lg:w-80">
        @* Sidebar - full width on mobile, fixed width on desktop *@
    </aside>
    <main class="flex-1">
        @* Main content *@
    </main>
</div>

@* ✅ Correct: Responsive grid *@
<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
    @foreach (var item in Items)
    {
        <Card Item="@item" />
    }
</div>

@* ❌ Incorrect: Desktop-first (requires overrides for mobile) *@
<div class="flex flex-row md:flex-col">
    @* This is harder to maintain *@
</div>
```

### Admin Layout Requirements

- **Collapsible Sidebar**: Sidebar must collapse to hamburger menu on mobile
- **Bottom Navigation**: Consider bottom nav for frequently-used admin actions on mobile
- **Responsive Tables**: Use horizontal scroll or card layout for data tables on mobile
- **Touch-Optimized Forms**: Larger form inputs and adequate spacing for mobile

```razor
@* ✅ Correct: Responsive admin layout in Blazor *@
<div class="min-h-screen bg-background">
    @* Mobile header with hamburger *@
    <header class="sticky top-0 z-50 flex items-center justify-between p-4 md:hidden">
        <Logo />
        <button @onclick="ToggleSidebar" aria-label="Toggle menu" class="ghost">
            <span class="oi oi-menu h-6 w-6"></span>
        </button>
    </header>

    <div class="flex">
        @* Sidebar - hidden on mobile, visible on desktop *@
        <aside class="@SidebarClass">
            <Navigation />
        </aside>

        @* Overlay for mobile sidebar *@
        @if (IsSidebarOpen)
        {
            <div class="fixed inset-0 z-30 bg-black/50 md:hidden" 
                 @onclick="() => IsSidebarOpen = false">
            </div>
        }

        <main class="flex-1 p-4 md:p-6 lg:p-8">
            @ChildContent
        </main>
    </div>
</div>

@code {
    private bool IsSidebarOpen { get; set; }
    
    private string SidebarClass => 
        $"fixed inset-y-0 left-0 z-40 w-64 transform bg-sidebar transition-transform md:relative md:translate-x-0 {(IsSidebarOpen ? "translate-x-0" : "-translate-x-full")}";
    
    private void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;
}
```

### Responsive Data Tables

```razor
@* ✅ Correct: Card layout on mobile, table on desktop *@
<div class="hidden md:block">
    <table>@* Full table for desktop *@</table>
</div>
<div class="md:hidden space-y-4">
    @foreach (var item in Items)
    {
        <div class="card">
            @* Card layout for mobile *@
        </div>
    }
</div>

@* ✅ Alternative: Horizontal scroll for complex tables *@
<div class="overflow-x-auto -mx-4 px-4">
    <table class="min-w-[600px]">
        @* Table with minimum width *@
    </table>
</div>
```

### Requirements Checklist

- [ ] Mobile-first CSS approach (base styles for mobile, add complexity with breakpoints)
- [ ] All interactive elements have minimum 44x44px touch targets
- [ ] Navigation is accessible on all screen sizes (hamburger menu for mobile)
- [ ] Forms are usable on mobile (appropriate input sizes, spacing)
- [ ] Tables adapt to mobile (card layout or horizontal scroll)
- [ ] Images are responsive (`max-w-full h-auto` or `object-fit`)
- [ ] Text is readable without zooming (minimum 16px body text)
- [ ] No horizontal scrolling on any viewport
- [ ] Test on actual mobile devices, not just browser dev tools
- [ ] Admin section is fully functional on mobile devices

---

## Accessibility (a11y)

All UI components must be accessible (WCAG 2.1 AA minimum):

### Semantic HTML

```razor
@* ✅ Correct: Use semantic elements *@
<nav aria-label="Main navigation">
    <ul>
        <li><a href="/portfolio">Portfolio</a></li>
    </ul>
</nav>

<main>
    <article>
        <h1>Project Title</h1>
        <p>Description...</p>
    </article>
</main>

@* ❌ Incorrect: Divs for everything *@
<div class="nav">
    <div class="link">Portfolio</div>
</div>
```

### Interactive Elements

```razor
@* ✅ Correct: Accessible button with keyboard support *@
<button @onclick="HandleSubmit"
        aria-label="Submit contact form"
        aria-busy="@IsSubmitting"
        disabled="@IsSubmitting">
    @(IsSubmitting ? "Sending..." : "Send Message")
</button>

@* ✅ Correct: Icon button with label *@
<button aria-label="Close dialog" @onclick="OnClose">
    <span class="oi oi-x" aria-hidden="true"></span>
</button>
```

### Requirements Checklist

- [ ] Semantic HTML elements (`<button>`, `<nav>`, `<main>`, `<article>`)
- [ ] Proper ARIA attributes where semantic HTML is insufficient
- [ ] Keyboard navigation for all interactive elements
- [ ] Focus management for modals, dropdowns, dynamic content
- [ ] Visible focus indicators (never `outline: none` without replacement)
- [ ] Sufficient color contrast (4.5:1 for text, 3:1 for large text)
- [ ] Alt text for all images and meaningful icons
- [ ] Screen reader support with labels and live regions
- [ ] Skip links for main content
- [ ] Form labels associated with inputs

---

## Localization (i18n)

All user-facing text must be localizable:

### Using .NET Localization with IStringLocalizer

```csharp
// In a Blazor component or service
@inject IStringLocalizer<SharedResources> Localizer

<form>
    <label for="name">@Localizer["Contact.Form.Name.Label"]</label>
    <input id="name" placeholder="@Localizer["Contact.Form.Name.Placeholder"]" />
    <button type="submit">@Localizer["Contact.Form.Submit"]</button>
</form>
```

```csharp
// ✅ Correct: Use resource files for localization
public class ContactService
{
    private readonly IStringLocalizer<ContactService> _localizer;
    
    public ContactService(IStringLocalizer<ContactService> localizer)
    {
        _localizer = localizer;
    }
    
    public string GetValidationMessage(string key)
    {
        return _localizer[key];
    }
}

// ❌ Incorrect: Hardcoded strings
public string GetValidationMessage()
{
    return "Your Name is required"; // Not localizable!
}
```

### Error Messages from API

```csharp
// ✅ Correct: Localize error codes from API
public void HandleApiError(ApiError error, IStringLocalizer<ErrorMessages> localizer)
{
    // Map error codes to localized messages
    var message = localizer[$"Errors.{error.Code}"];
    
    // Show toast or notification
    NotificationService.ShowError(message);
}
```

### Resource File Structure

```
Resources/
├── SharedResources.resx           # Default (English)
├── SharedResources.es.resx        # Spanish
├── SharedResources.fr.resx        # French
├── ErrorMessages.resx             # Error messages (English)
└── ErrorMessages.es.resx          # Error messages (Spanish)
```

### Requirements Checklist

- [ ] Never hardcode user-facing strings
- [ ] Use IStringLocalizer for all display text
- [ ] Support RTL layouts (CSS logical properties)
- [ ] Format dates/numbers/currencies per locale using CultureInfo
- [ ] Account for text expansion (30-50% longer than English)
- [ ] Externalize strings to .resx resource files
- [ ] Use plural forms where appropriate

---

## Logging and Metrics

### Blazor Client Telemetry

```csharp
@inject TelemetryClient TelemetryClient

// ✅ Track user actions
private void HandleProjectView(Guid projectId)
{
    TelemetryClient.TrackEvent("ProjectViewed", 
        new Dictionary<string, string> { ["ProjectId"] = projectId.ToString() });
}

// ✅ Track errors
private void HandleError(Exception error, string context)
{
    TelemetryClient.TrackException(error, 
        new Dictionary<string, string> { ["Context"] = context });
}
```

### Backend Logging

```csharp
// ✅ Structured logging with context
_logger.LogInformation(
    "Project created: {ProjectId} by {UserId}", 
    project.Id, 
    userId
);

// ✅ Audit logging for sensitive operations
_auditLogger.LogDataModification(
    action: "ProjectDeleted",
    resourceType: "Project",
    resourceId: projectId,
    userId: userId,
    details: new { Reason = reason }
);

// ✅ Authorization failure logging
_logger.LogWarning(
    "Authorization failed: {UserId} attempted {Action} on {Resource}",
    userId, action, resourceId
);
```

---

## Testing Requirements

### Coverage Requirements

- **Minimum 90% code coverage** for all new code
- Unit tests for all business logic
- Integration tests for API endpoints
- E2E/Acceptance tests for critical user flows

### Testing Frameworks

| Framework | Purpose |
|-----------|---------|
| **xUnit** | Unit testing framework |
| **bUnit** | Blazor component testing |
| **Reqnroll** | BDD acceptance tests (Gherkin `.feature` files) |
| **Playwright** | Browser automation for E2E tests |

### Blazor Component Testing with bUnit

```csharp
using Bunit;
using Xunit;

public class ProjectCardTests : TestContext
{
    [Fact]
    public void ProjectCard_RendersProjectName()
    {
        // Arrange
        var project = new ProjectDto { Name = "Test Project" };
        
        // Act
        var cut = RenderComponent<ProjectCard>(parameters => 
            parameters.Add(p => p.Project, project));
        
        // Assert
        cut.Find("h2").MarkupMatches("<h2>Test Project</h2>");
    }
}
```

### Accessibility Testing

```csharp
// Use Playwright for accessibility testing
[Fact]
public async Task Page_ShouldHaveNoAccessibilityViolations()
{
    await Page.GotoAsync("/projects");
    
    var accessibilityResults = await Page.RunAxeCoreAsync();
    
    Assert.Empty(accessibilityResults.Violations);
}
```

### Authorization Testing

```csharp
// Test unauthorized access returns 403
[Fact]
public async Task ManageProjects_ReturnsUnauthorized_ForNonAdminUsers()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", _userToken);
    
    // Act
    var response = await client.GetAsync("/api/admin/projects");
    
    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Assert.Equal("AUTH_FORBIDDEN", error?.Code);
}

// Test feature hiding with bUnit
[Fact]
public void Header_HidesAdminLink_ForNonAdminUsers()
{
    // Arrange
    var authContext = this.AddTestAuthorization();
    authContext.SetAuthorized("testuser");
    // Note: Not adding admin role
    
    // Act
    var cut = RenderComponent<Header>();
    
    // Assert
    Assert.Throws<ElementNotFoundException>(() => cut.Find("[data-testid='admin-link']"));
}
```

### BDD Acceptance Testing with Reqnroll

```gherkin
# Features/AccountManagement.feature
Feature: Account Management
    As a user
    I want to view my account balance
    So that I can track my finances

Scenario: User views account balance
    Given I am logged in as "testuser@example.com"
    And I have an account with balance $1,500.00
    When I navigate to the account summary page
    Then I should see the balance "$1,500.00"
```

```csharp
// StepDefinitions/AccountManagementSteps.cs
[Binding]
public class AccountManagementSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly IPage _page;

    public AccountManagementSteps(ScenarioContext scenarioContext, IPage page)
    {
        _scenarioContext = scenarioContext;
        _page = page;
    }

    [Given(@"I am logged in as ""(.*)""")]
    public async Task GivenIAmLoggedInAs(string email)
    {
        await _page.GotoAsync("/login");
        await _page.FillAsync("[data-testid='email']", email);
        await _page.FillAsync("[data-testid='password']", "TestPassword123!");
        await _page.ClickAsync("[data-testid='login-button']");
    }

    [When(@"I navigate to the account summary page")]
    public async Task WhenINavigateToAccountSummary()
    {
        await _page.GotoAsync("/accounts/summary");
    }

    [Then(@"I should see the balance ""(.*)""")]
    public async Task ThenIShouldSeeBalance(string expectedBalance)
    {
        var balance = await _page.TextContentAsync("[data-testid='account-balance']");
        Assert.Equal(expectedBalance, balance);
    }
}
```

---

## File Structure for New Features

```
src/
├── Client/                          # Blazor WebAssembly Client
│   ├── Pages/
│   │   └── FeatureName/
│   │       ├── FeaturePage.razor    # Page component with XML docs
│   │       └── FeaturePage.razor.cs # Code-behind (if needed)
│   ├── Components/
│   │   └── FeatureName/
│   │       ├── FeatureCard.razor    # Reusable component
│   │       └── FeatureList.razor    # List component
│   ├── Services/
│   │   └── FeatureService.cs        # Client-side service
│   └── wwwroot/
│       └── css/                     # Component-specific styles
│
├── Api/                             # Azure Functions API
│   ├── Functions/
│   │   └── FeatureFunction.cs       # XML documented endpoint
│   ├── Services/
│   │   └── FeatureService.cs        # Business logic with logging
│   └── Models/
│       └── FeatureDto.cs            # Data transfer objects
│
├── Shared/                          # Shared library
│   ├── Models/
│   │   └── Feature.cs               # Shared domain models
│   └── Contracts/
│       └── IFeatureService.cs       # Shared interfaces
│
└── Resources/                       # Localization resources
    ├── Features.resx                # English strings
    └── Features.es.resx             # Spanish strings

tests/
├── UnitTests/
│   ├── Client/
│   │   └── FeatureComponentTests.cs # bUnit component tests
│   └── Api/
│       └── FeatureServiceTests.cs   # Unit tests
└── AcceptanceTests/
    └── FeatureAcceptanceTests.cs    # E2E tests
```

---

## GitFlow Branch Management

This project uses **GitFlow** for branch management. **Always follow these rules when creating branches:**

### Branch Creation Rules

| Branch Type | Create From | Merge To | Naming Pattern |
|-------------|-------------|----------|----------------|
| `feature/*` | `develop` | `develop` | `feature/descriptive-name` |
| `release/*` | `develop` | `main` + `develop` | `release/x.y.z` |
| `hotfix/*` | `main` | `main` + `develop` | `hotfix/x.y.z` |
| `bugfix/*` | `develop` | `develop` | `bugfix/descriptive-name` |

### Critical Rules

1. **NEVER create feature branches from `main`** - Always branch from `develop`
2. **Feature branches merge back to `develop`** - Not directly to `main`
3. **Only `release/*` and `hotfix/*` branches touch `main`**
4. **Hotfixes must be merged to both `main` AND `develop`**

### Branch Commands

```bash
# Start a new feature (always from develop)
git checkout develop
git pull origin develop
git checkout -b feature/my-feature

# Or using git-flow CLI
git flow feature start my-feature

# Finish feature (merges to develop)
git flow feature finish my-feature

# Start a release
git flow release start 1.2.0

# Finish release (merges to main + develop, creates tag)
git flow release finish 1.2.0

# Emergency hotfix from main
git flow hotfix start 1.2.1
git flow hotfix finish 1.2.1
```

### When Creating Branches Programmatically

When using GitHub API or MCP tools to create branches:

```csharp
// ✅ Correct: Feature branch from develop
create_branch(
    branch: "feature/admin-mobile-responsive",
    from_branch: "develop"  // ALWAYS develop for features
)

// ❌ Incorrect: Feature branch from main
create_branch(
    branch: "feature/admin-mobile-responsive",
    from_branch: "main"  // NEVER do this for features
)
```

---

## Planning & Progress Tracking

### ⚠️ IMPORTANT: Approval Required

**NEVER commit changes without explicit user approval.** Always:
1. Describe what changes you plan to make
2. Wait for user confirmation before committing
3. If in doubt, ask first

### Permanent Execution Plans (Source of Truth)

All project planning and progress tracking is maintained in the `docs/` folder. **These are the authoritative documents** - always keep them up to date:

| Document | Purpose |
|----------|---------|
| `docs/RAJ_FINANCIAL_EXECUTION_PLAN.md` | **Master execution plan** - phases, infrastructure, security tasks |
| `docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md` | API implementation progress and task tracking |
| `docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md` | UI implementation progress and task tracking |

### When Starting Work

1. **Read the execution plans** to understand current progress
2. **Update the permanent execution plans** as you complete tasks:
   - Change `⬜ Not Started` to `🟡 In Progress` or `✅ Complete`
   - Add notes with relevant details (dates, PRs, etc.)

### Temporary Branch Plan (Optional)

You may create a temporary `IMPLEMENTATION_PLAN.md` at the repository root for the current feature branch to:
- Track which specific tasks from the execution plan you're working on
- Make it easier to follow immediate progress
- Reference task numbers from the permanent execution plans

**Requirements for temp plans:**
1. Reference tasks by number from the permanent execution plans
2. Delete before merging the feature branch to `develop`
3. Always sync completed work back to the permanent execution plans

### Template for Temporary Plan

```markdown
# Implementation Plan: [Feature Name]

## Branch Info
- **Branch**: `feature/xxx`
- **Created From**: `develop`

## Tasks (from Execution Plan)
References tasks from `docs/RAJ_FINANCIAL_EXECUTION_PLAN.md`:

- [ ] Task 0.2.1.3: Create DataAccessGrant migration
- [ ] Task 0.2.1.4: Implement IDataAccessService
- [ ] Task 0.2.2.5: Add "Sign Up" button to LoginDisplay

## Files to Modify
- `path/to/file.cs` - [what changes]

## Notes
[Any branch-specific notes]
```

### Workflow Summary

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Read permanent execution plans in docs/                      │
│ 2. (Optional) Create temp IMPLEMENTATION_PLAN.md for branch     │
│ 3. Work on tasks, checking them off in temp plan                │
│ 4. Update permanent execution plans with completed work         │
│ 5. Delete temp plan before merging to develop                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Serialization

### Use MemoryPack for High-Performance Serialization

Use **MemoryPack** for all .NET serialization needs (caching, message queues, inter-service communication):

```csharp
using MemoryPack;

[MemoryPackable]
public partial class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public byte[] ThumbnailData { get; set; } = [];
}

// Serialize
byte[] bytes = MemoryPackSerializer.Serialize(project);

// Deserialize
ProjectDto? result = MemoryPackSerializer.Deserialize<ProjectDto>(bytes);
```

### MemoryPack Best Practices

1. **Mark classes as `partial`** - Required for source generation
2. **Use `[MemoryPackable]` attribute** - Enables compile-time serialization
3. **Prefer MemoryPack over JSON** for internal communication (10-100x faster)
4. **Use JSON only for external APIs** - Browser/third-party compatibility
5. **Handle versioning** with `[MemoryPackable(GenerateType.VersionTolerant)]` for evolving schemas

```csharp
// Version-tolerant for cache/queue data that may evolve
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class CachedUserSession
{
    [MemoryPackOrder(0)]
    public Guid UserId { get; set; }
    
    [MemoryPackOrder(1)]
    public string DisplayName { get; set; } = string.Empty;
    
    [MemoryPackOrder(2)]
    public List<string> Permissions { get; set; } = [];
}
```

### When to Use Each Serialization Format

| Use Case | Format | Reason |
|----------|--------|--------|
| Redis/Cache | MemoryPack | Speed + compact size |
| Message Queues | MemoryPack | High throughput |
| Inter-service RPC | MemoryPack | Low latency |
| Public REST APIs | JSON | Browser compatibility |
| Config files | JSON/YAML | Human readable |
| Logs | JSON | Tooling support |

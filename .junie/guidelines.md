---
applyTo: '**'
---

# Development Instructions

## Core Principles

Follow these principles in order of priority:

1. **Security First** - All code must be secure by default
2. **Mobile Responsiveness** - All UI must be mobile-friendly (mobile-first approach)
3. **Accessibility** - All UI must be accessible (WCAG 2.1 AA)
4. **Localization** - All user-facing text must be localizable
5. **Documentation** - All code must be fully documented
6. **Observability** - Add logging, metrics, and telemetry
7. **SOLID Principles** - Follow Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion
8. **DRY (Don't Repeat Yourself)** - Avoid code duplication; extract reusable components and services

---

## Security Requirements

### Authorization - Frontend (UI)

1. **Hide, Don't Disable**: Unauthorized features must be **hidden entirely**, not shown as disabled
2. **Conditional Rendering**: Check permissions before rendering menu items, buttons, pages
3. **Route Guards**: Redirect unauthorized route access attempts
4. **No Client-Side Trust**: UI hiding is for UX only; always enforce server-side

```razor
@* ✅ Correct: Hide unauthorized features *@
<AuthorizeView Policy="RequireUserManagement">
    <SfMenuItem Text="Manage Users" />
</AuthorizeView>

@* ❌ Incorrect: Don't just disable *@
<SfMenuItem Text="Manage Users" Disabled="@(!HasPermission("users:manage"))" />
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

### API Error Codes

Always return structured error responses with error codes, NOT hardcoded messages:

```csharp
// ✅ Correct: Use error codes for localization
return new ApiError 
{
    Code = "PROJECT_NOT_FOUND",
    Message = "Project not found", // Default English, client localizes by code
    Details = new { ProjectId = id }
};

// ❌ Incorrect: Hardcoded messages that can't be localized
return BadRequest("The project you requested was not found");
```

Standard error codes:
- `AUTH_REQUIRED` - Authentication required
- `AUTH_FORBIDDEN` - Insufficient permissions
- `RESOURCE_NOT_FOUND` - Resource does not exist
- `VALIDATION_FAILED` - Input validation error
- `RATE_LIMITED` - Too many requests
- `SERVER_ERROR` - Internal server error

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

### Syncfusion Blazor Responsive Components

Use Syncfusion's built-in responsive features and CSS utilities:

```razor
@* ✅ Correct: Mobile-first approach with Syncfusion Dashboard Layout *@
<SfDashboardLayout CellSpacing="@(new double[] { 10, 10 })" 
                   Columns="12" 
                   MediaQuery="@MediaQueries">
    <DashboardLayoutPanels>
        <DashboardLayoutPanel SizeX="12" SizeY="1" Row="0" Col="0"
                              Id="sidebar" CssClass="e-panel-sidebar">
            @* Sidebar - full width on mobile, adapts on desktop *@
            <ContentTemplate>
                <SfSidebar @bind-IsOpen="@IsSidebarOpen" 
                           Type="SidebarType.Responsive"
                           MediaQuery="(min-width: 768px)">
                    <ChildContent>
                        <SfMenu Items="@MenuItems" />
                    </ChildContent>
                </SfSidebar>
            </ContentTemplate>
        </DashboardLayoutPanel>
    </DashboardLayoutPanels>
</SfDashboardLayout>

@* ✅ Correct: Responsive grid with Syncfusion *@
<div class="row">
    @foreach (var item in Items)
    {
        <div class="col-12 col-sm-6 col-lg-4 col-xl-3">
            <SfCard>
                <CardContent>@item.Name</CardContent>
            </SfCard>
        </div>
    }
</div>
```

### Admin Layout Requirements

- **Collapsible Sidebar**: Sidebar must collapse to hamburger menu on mobile
- **Bottom Navigation**: Consider bottom nav for frequently-used admin actions on mobile
- **Responsive Tables**: Use horizontal scroll or card layout for data tables on mobile
- **Touch-Optimized Forms**: Larger form inputs and adequate spacing for mobile

```razor
@* ✅ Correct: Responsive admin layout with Syncfusion *@
<div class="e-main-wrapper">
    @* Mobile header with hamburger *@
    <SfAppBar ColorMode="AppBarColor.Primary" CssClass="d-md-none sticky-top">
        <AppBarSpacer />
        <span class="logo">@Logo</span>
        <AppBarSpacer />
        <SfButton IconCss="e-icons e-menu" 
                  CssClass="e-inherit" 
                  @onclick="ToggleSidebar"
                  aria-label="Toggle menu" />
    </SfAppBar>

    <div class="d-flex">
        @* Sidebar - responsive with Syncfusion *@
        <SfSidebar @bind-IsOpen="@IsSidebarOpen" 
                   Type="SidebarType.Responsive"
                   Width="250px"
                   MediaQuery="(min-width: 768px)"
                   EnableGestures="true">
            <ChildContent>
                <SfMenu TValue="MenuItem" Items="@NavigationItems" Orientation="Orientation.Vertical" />
            </ChildContent>
        </SfSidebar>

        @* Overlay handled automatically by SfSidebar *@

        <main class="flex-grow-1 p-3 p-md-4 p-lg-5">
            @ChildContent
        </main>
    </div>
</div>

@code {
    private bool IsSidebarOpen { get; set; } = true;
    
    private void ToggleSidebar() => IsSidebarOpen = !IsSidebarOpen;
}
```

### Responsive Data Tables

```razor
@* ✅ Correct: Syncfusion Grid with built-in responsive modes *@
<SfGrid DataSource="@Items" 
        AllowPaging="true" 
        AllowSorting="true"
        EnableAdaptiveUI="true"
        RowRenderingMode="RowDirection.Horizontal">
    <GridColumns>
        <GridColumn Field="@nameof(Project.Name)" HeaderText="Name" Width="200" />
        <GridColumn Field="@nameof(Project.Status)" HeaderText="Status" Width="120" />
        <GridColumn Field="@nameof(Project.CreatedAt)" HeaderText="Created" Format="d" Width="150" />
    </GridColumns>
</SfGrid>

@* ✅ Alternative: Card-based view for mobile using Row Template *@
<SfGrid DataSource="@Items" EnableAdaptiveUI="true">
    <GridTemplates>
        <RowTemplate>
            @{
                var item = (context as Project);
            }
            <SfCard CssClass="d-md-none mb-3">
                <CardHeader Title="@item.Name" />
                <CardContent>
                    <p>Status: @item.Status</p>
                    <p>Created: @item.CreatedAt.ToShortDateString()</p>
                </CardContent>
            </SfCard>
        </RowTemplate>
    </GridTemplates>
    <GridColumns>
        <GridColumn Field="@nameof(Project.Name)" HeaderText="Name" />
        <GridColumn Field="@nameof(Project.Status)" HeaderText="Status" />
    </GridColumns>
</SfGrid>
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
@* ✅ Correct: Use semantic elements with Syncfusion *@
<nav aria-label="Main navigation">
    <SfMenu TValue="MenuItem" Items="@MenuItems" />
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
@* ✅ Correct: Accessible button with keyboard support using Syncfusion *@
<SfButton @onclick="HandleSubmit"
          aria-label="Submit contact form"
          aria-busy="@IsSubmitting"
          Disabled="@IsSubmitting"
          IsPrimary="true">
    @(IsSubmitting ? "Sending..." : "Send Message")
</SfButton>

@* ✅ Correct: Icon button with label *@
<SfButton IconCss="e-icons e-close" 
          CssClass="e-flat" 
          aria-label="Close dialog" 
          @onclick="OnClose" />
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

### Translation with IStringLocalizer

```razor
@* ✅ Correct: Use IStringLocalizer for translations *@
@inject IStringLocalizer<ContactForm> Localizer

<EditForm Model="@Model" OnValidSubmit="HandleSubmit">
    <div class="mb-3">
        <label for="name">@Localizer["contact.form.name.label"]</label>
        <SfTextBox @bind-Value="Model.Name" 
                   ID="name" 
                   Placeholder="@Localizer["contact.form.name.placeholder"]" />
    </div>
    <SfButton Type="submit" IsPrimary="true">
        @Localizer["contact.form.submit"]
    </SfButton>
</EditForm>

@* ❌ Incorrect: Hardcoded strings *@
<EditForm Model="@Model">
    <label>Your Name</label>
    <SfTextBox Placeholder="Enter your name" />
    <SfButton Type="submit">Send Message</SfButton>
</EditForm>
```

### Error Messages from API

```csharp
// ✅ Correct: Localize error codes from API
public class ApiErrorHandler
{
    private readonly IStringLocalizer<SharedResources> _localizer;

    public ApiErrorHandler(IStringLocalizer<SharedResources> localizer)
    {
        _localizer = localizer;
    }

    public string GetLocalizedError(ApiError error)
    {
        // Map error codes to localized messages
        var key = $"Errors.{error.Code}";
        var message = _localizer[key];
        
        return message.ResourceNotFound 
            ? _localizer["Errors.UNKNOWN"] 
            : message.Value;
    }
}

// Resource file (Resources/SharedResources.en.resx)
// Key: Errors.PROJECT_NOT_FOUND     Value: Project not found
// Key: Errors.AUTH_REQUIRED         Value: Please sign in to continue
// Key: Errors.AUTH_FORBIDDEN        Value: You don't have permission to do that
// Key: Errors.VALIDATION_FAILED     Value: Please check your input and try again
// Key: Errors.UNKNOWN               Value: Something went wrong. Please try again.
```

### Requirements Checklist

- [ ] Never hardcode user-facing strings
- [ ] Use `IStringLocalizer<T>` for component-specific translations
- [ ] Use `IStringLocalizer<SharedResources>` for shared translations
- [ ] Support RTL layouts (CSS logical properties)
- [ ] Format dates/numbers/currencies per locale using `CultureInfo`
- [ ] Account for text expansion (30-50% longer than English)
- [ ] Externalize strings to `.resx` resource files
- [ ] Configure supported cultures in `Program.cs`

---

## Logging and Metrics

### Blazor Telemetry

```csharp
// ✅ Inject and use Application Insights in Blazor components
@inject TelemetryClient TelemetryClient

// ✅ Track user actions
private void HandleProjectView(Guid projectId)
{
    TelemetryClient.TrackEvent("ProjectViewed", new Dictionary<string, string>
    {
        { "ProjectId", projectId.ToString() }
    });
}

// ✅ Track errors
private void HandleError(Exception exception, string context)
{
    TelemetryClient.TrackException(exception, new Dictionary<string, string>
    {
        { "Context", context }
    });
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
- E2E tests for critical user flows

### Accessibility Testing

```csharp
using Bunit;
using Xunit;

public class ProjectCardAccessibilityTests : TestContext
{
    [Fact]
    public void ProjectCard_ShouldHaveAccessibleMarkup()
    {
        // Arrange
        var project = new Project { Id = Guid.NewGuid(), Name = "Test Project" };
        
        // Act
        var cut = RenderComponent<ProjectCard>(parameters => 
            parameters.Add(p => p.Project, project));
        
        // Assert - Check for proper ARIA attributes and semantic HTML
        Assert.NotNull(cut.Find("article"));
        Assert.NotNull(cut.Find("[aria-label]"));
        
        // Verify images have alt text
        var images = cut.FindAll("img");
        foreach (var img in images)
        {
            Assert.True(img.HasAttribute("alt"));
        }
    }
}
```

### Authorization Testing

```csharp
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Xunit;

// Test unauthorized access returns 403
[Fact]
public async Task AdminEndpoint_Returns403_ForNonAdminUsers()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", _userToken);
    
    // Act
    var response = await client.GetAsync("/api/admin/projects");
    
    // Assert
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    var content = await response.Content.ReadFromJsonAsync<ApiError>();
    Assert.Equal("AUTH_FORBIDDEN", content?.Code);
}

// Test feature hiding in Blazor with bUnit
[Fact]
public void Header_ShouldHideAdminLink_ForNonAdminUsers()
{
    // Arrange
    var authContext = this.AddTestAuthorization();
    authContext.SetAuthorized("regularuser@example.com");
    authContext.SetRoles("User"); // Not Admin
    
    // Act
    var cut = RenderComponent<Header>();
    
    // Assert
    Assert.Throws<ElementNotFoundException>(() => cut.Find("[data-testid='admin-link']"));
}
```

### C# Testing Standards

#### Unit Testing with Moq

Use **Moq** for mocking dependencies in unit tests:

```csharp
using Moq;
using Xunit;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _mockRepository;
    private readonly Mock<ILogger<ProjectService>> _mockLogger;
    private readonly ProjectService _service;

    public ProjectServiceTests()
    {
        _mockRepository = new Mock<IProjectRepository>();
        _mockLogger = new Mock<ILogger<ProjectService>>();
        _service = new ProjectService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetProjectById_ReturnsProject_WhenProjectExists()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var expectedProject = new Project { Id = projectId, Name = "Test Project" };
        _mockRepository
            .Setup(r => r.GetByIdAsync(projectId))
            .ReturnsAsync(expectedProject);

        // Act
        var result = await _service.GetProjectByIdAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedProject.Name, result.Name);
        _mockRepository.Verify(r => r.GetByIdAsync(projectId), Times.Once);
    }
}
```

#### BDD Testing with Reqnroll

Use **Reqnroll** (SpecFlow successor) for Behavior-Driven Development (BDD) tests. Write feature files in Gherkin syntax:

```gherkin
# Features/ProjectManagement.feature
Feature: Project Management
    As a portfolio administrator
    I want to manage projects
    So that I can showcase my work to potential clients

    Background:
        Given I am logged in as an admin user

    Scenario: Create a new project
        Given I am on the project management page
        When I click the "Add Project" button
        And I fill in the project details:
            | Field       | Value                    |
            | Name        | New Construction Project |
            | Description | A modern office building |
            | Status      | In Progress              |
        And I click "Save"
        Then I should see a success message "Project created successfully"
        And the project "New Construction Project" should appear in the project list

    Scenario: Publish a draft project
        Given a draft project "Renovation Project" exists
        When I publish the project "Renovation Project"
        Then the project status should be "Published"
        And the project should be visible on the public portfolio
```

Step definitions in C#:

```csharp
using Reqnroll;
using Microsoft.Playwright;

[Binding]
public class ProjectManagementSteps
{
    private readonly IPage _page;
    private readonly ScenarioContext _scenarioContext;

    public ProjectManagementSteps(IPage page, ScenarioContext scenarioContext)
    {
        _page = page;
        _scenarioContext = scenarioContext;
    }

    [Given(@"I am logged in as an admin user")]
    public async Task GivenIAmLoggedInAsAnAdminUser()
    {
        await _page.GotoAsync("/login");
        await _page.FillAsync("[data-testid=email]", "admin@example.com");
        await _page.FillAsync("[data-testid=password]", "password");
        await _page.ClickAsync("[data-testid=login-button]");
        await _page.WaitForURLAsync("/admin/dashboard");
    }

    [When(@"I fill in the project details:")]
    public async Task WhenIFillInTheProjectDetails(Table table)
    {
        foreach (var row in table.Rows)
        {
            var field = row["Field"];
            var value = row["Value"];
            await _page.FillAsync($"[data-testid=project-{field.ToLower()}]", value);
        }
    }

    [Then(@"the project ""(.*)"" should appear in the project list")]
    public async Task ThenTheProjectShouldAppearInTheProjectList(string projectName)
    {
        var projectElement = await _page.QuerySelectorAsync($"text={projectName}");
        Assert.NotNull(projectElement);
    }
}
```

#### BDD & Unit Test Guidelines

1. **Use BDD (Reqnroll) for**:
   - User-facing acceptance tests
   - End-to-end workflow validation
   - Cross-functional team collaboration (stakeholder-readable scenarios)

2. **Use Unit Tests (xUnit + Moq) for**:
   - Business logic validation
   - Service layer testing
   - Edge cases and error handling
   - Fast feedback during development

3. **Test Project Structure**:
   ```
   tests/
   ├── UnitTests/
   │   ├── Services/
   │   │   └── ProjectServiceTests.cs
   │   └── Validators/
   │       └── ProjectValidatorTests.cs
   ├── IntegrationTests/
   │   └── Api/
   │       └── ProjectsControllerTests.cs
   └── AcceptanceTests/
       ├── Features/
       │   └── ProjectManagement.feature
       ├── StepDefinitions/
       │   └── ProjectManagementSteps.cs
       └── Hooks/
           └── TestSetup.cs
   ```

---

## File Structure for New Features

```
src/
├── Client/                          # Blazor WebAssembly or Server project
│   ├── Pages/
│   │   └── FeatureName/
│   │       ├── FeatureName.razor         # Page component with XML docs
│   │       └── FeatureName.razor.cs      # Code-behind (partial class)
│   ├── Components/
│   │   └── FeatureName/
│   │       ├── FeatureComponent.razor    # Reusable component
│   │       └── FeatureComponent.razor.cs # Code-behind
│   ├── Services/
│   │   └── FeatureService.cs             # Client-side service
│   └── Resources/
│       ├── Pages.FeatureName.en.resx     # English translations
│       └── Pages.FeatureName.es.resx     # Spanish translations
├── Server/                          # ASP.NET Core API project
│   ├── Controllers/
│   │   └── FeatureController.cs          # XML documented endpoints
│   ├── Services/
│   │   └── FeatureService.cs             # Business logic with logging
│   └── Models/
│       └── FeatureDto.cs                 # Data transfer objects
├── Shared/                          # Shared library
│   ├── Models/
│   │   └── Feature.cs                    # Shared models
│   └── Interfaces/
│       └── IFeatureService.cs            # Service contracts
└── Tests/
    ├── UnitTests/
    │   └── FeatureServiceTests.cs        # xUnit + Moq tests
    ├── IntegrationTests/
    │   └── FeatureControllerTests.cs     # API integration tests
    └── AcceptanceTests/
        ├── Features/
        │   └── Feature.feature           # Reqnroll BDD specs
        └── StepDefinitions/
            └── FeatureSteps.cs           # Step definitions
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

```typescript
// ✅ Correct: Feature branch from develop
create_branch({
  branch: "feature/admin-mobile-responsive",
  from_branch: "develop"  // ALWAYS develop for features
})

// ❌ Incorrect: Feature branch from main
create_branch({
  branch: "feature/admin-mobile-responsive",
  from_branch: "main"  // NEVER do this for features
})
```

---

## RJA Financial Execution Plan Workflow

**Before starting work on any new feature branch**, create execution and tracking documents:

### Required Documents

| Document | Purpose |
|----------|---------|
| `RJA_FINANCIAL_EXECUTION_PLAN.md` | Master execution plan with tasks, files, and acceptance criteria |
| `RJA_FINANCIAL_TRACKING.md` | Progress tracking, status updates, and change log |

### Required Steps

1. **Create `RJA_FINANCIAL_EXECUTION_PLAN.md`** at the repository root with:
    - Feature description and goals
    - Numbered task checklist with checkboxes
    - Files to be created/modified
    - Testing requirements
    - Acceptance criteria

2. **Create `RJA_FINANCIAL_TRACKING.md`** for ongoing progress:
    - Current status and blockers
    - Daily/session progress notes
    - Change log for plan deviations

3. **Follow the plan** - Check off each item as progress is made:
   ```markdown
   ## Tasks
   - [x] 1. Install dependencies
   - [x] 2. Create utility function
   - [ ] 3. Add unit tests
   - [ ] 4. Update documentation
   ```

4. **Keep both documents synchronized with code changes**:
   - Update the execution plan **immediately** when adding new files or modifying different files than originally planned
   - Add newly discovered tasks as they arise during implementation
   - Document any deviations in the tracking doc with brief explanations
   - Update the "Files to Modify" section when the scope changes
   ```markdown
   ## Files to Modify
   - `path/to/file.cs` - [what changes]
   - ~~`path/to/removed.cs`~~ - [no longer needed - reason]
   - `path/to/new-file.cs` - [added: discovered need during implementation]
   ```

5. **Resume work easily** - When returning to a branch, read both `RJA_FINANCIAL_EXECUTION_PLAN.md` and `RJA_FINANCIAL_TRACKING.md` to see where you left off

6. **Remove before merging** - Delete both documents before merging the feature branch to `develop`

### Execution Plan Template (`RJA_FINANCIAL_EXECUTION_PLAN.md`)

```markdown
# RJA Financial Execution Plan: [Feature Name]

## Overview
[Brief description of the feature and its goals]

## Tasks
- [ ] 1. [First task]
- [ ] 2. [Second task]
- [ ] 3. [Third task]

## Files to Modify
- `path/to/file.cs` - [what changes]
- `path/to/another.cs` - [what changes]

## Testing Requirements
- [ ] Unit tests for [component/function]
- [ ] BDD scenarios for [user flow]
- [ ] Accessibility tests

## Acceptance Criteria
- [ ] [Criterion 1]
- [ ] [Criterion 2]
```

### Tracking Document Template (`RJA_FINANCIAL_TRACKING.md`)

```markdown
# RJA Financial Tracking: [Feature Name]

## Current Status
**Status**: 🟡 In Progress | 🟢 Complete | 🔴 Blocked
**Last Updated**: YYYY-MM-DD
**Current Task**: [Task number and description]

## Progress Log
### YYYY-MM-DD
- Completed: [what was done]
- Next: [what's planned next]
- Blockers: [any issues]

## Change Log
| Date | Change | Reason |
|------|--------|--------|
| YYYY-MM-DD | Added task X | Discovered during implementation |
| YYYY-MM-DD | Removed file Y | No longer needed because... |

## Notes
[Any additional context, decisions made, or reference links]
```

### Benefits

- **Continuity**: Both Copilot and Claude can read the RJA Financial documents to understand context
- **Progress tracking**: Know exactly where work left off with detailed session logs
- **Change visibility**: All plan modifications are documented with reasons
- **Clean merges**: No plan files in develop/main branches

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

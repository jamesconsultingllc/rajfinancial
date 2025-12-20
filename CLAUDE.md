﻿# Claude AI Assistant Instructions

## Project Overview

**Raj Financial** is a financial services application built with:
- **Frontend**: Blazor WebAssembly (Client)
- **Backend**: Azure Functions (.NET Isolated Worker)
- **Shared**: .NET Class Library for shared models and contracts
- **Hosting**: Azure Static Web Apps

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

## Quick Start

```bash
# Navigate to API project
cd src/Api

# Run the Azure Functions locally
func start

# In another terminal, run the Blazor client
cd src/Client
dotnet run
```

## Project Structure

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
│   └── Models/             # DTOs and domain models
tests/
├── UnitTests/              # xUnit unit tests
└── AcceptanceTests/        # E2E acceptance tests
docs/                       # Documentation and planning
```

---

## 📋 Planning & Progress Tracking

### ⚠️ IMPORTANT: Approval Required

**NEVER commit changes without explicit user approval.** Always:
1. Describe what changes you plan to make
2. Wait for user confirmation before committing
3. If in doubt, ask first

### Permanent Execution Plans (Source of Truth)

**All project planning and progress tracking is maintained in the `docs/` folder.** These are the authoritative documents - always keep them up to date:

| Document | Purpose |
|----------|---------|
| [`docs/RAJ_FINANCIAL_EXECUTION_PLAN.md`](docs/RAJ_FINANCIAL_EXECUTION_PLAN.md) | **Master execution plan** - phases, infrastructure, security tasks |
| [`docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md`](docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md) | API implementation progress and task tracking |
| [`docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md`](docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md) | UI implementation progress and task tracking |
| [`docs/RAJ_FINANCIAL_INTEGRATIONS_API.md`](docs/RAJ_FINANCIAL_INTEGRATIONS_API.md) | API integration specifications |
| [`docs/RAJ_FINANCIAL_UI.md`](docs/RAJ_FINANCIAL_UI.md) | UI design and component specifications |

### When Starting Work

1. **Read the execution plans** to understand current progress:
   - Check `docs/RAJ_FINANCIAL_EXECUTION_PLAN.md` for infrastructure/security tasks (Part 0)
   - Check `docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md` for API tasks (Part 2)
   - Check `docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md` for UI tasks (Part 1)

2. **Update the permanent execution plans** as you complete tasks:
   - Change `⬜ Not Started` to `🟡 In Progress` or `✅ Complete`
   - Add notes with relevant details (dates, PRs, etc.)

3. **Reference task numbers** from the execution plan in commits and PRs

### Temporary Branch Plans (Optional)

You may create a temporary `IMPLEMENTATION_PLAN.md` at the repo root for the current feature branch to make it easier to follow immediate progress. Requirements:
- Reference tasks by number from the permanent execution plans
- Delete before merging to `develop`
- Always sync completed work back to the permanent execution plans

---

## Development Guidelines

### Follow the Copilot Instructions

All coding standards are documented in [`.github/copilot-instructions.md`](.github/copilot-instructions.md). Key principles:

1. **Security First** - Hide unauthorized features, enforce server-side authorization
2. **Mobile Responsiveness** - Mobile-first CSS, touch-friendly UI
3. **Accessibility** - WCAG 2.1 AA compliance, semantic HTML
4. **Localization** - Use IStringLocalizer, never hardcode strings
5. **Documentation** - XML docs on all public APIs
6. **Observability** - Structured logging with ILogger
7. **SOLID Principles** - Clean architecture patterns
8. **DRY** - Extract reusable components and services

### Code Documentation

Always use XML documentation for C# code:

```csharp
/// <summary>
/// Retrieves the account balance for the specified account.
/// </summary>
/// <param name="accountId">The unique identifier of the account.</param>
/// <returns>The current account balance.</returns>
/// <exception cref="NotFoundException">Thrown when account is not found.</exception>
public async Task<decimal> GetAccountBalanceAsync(Guid accountId) { ... }
```

### Error Handling

Return structured error responses with error codes:

```csharp
return new ApiError 
{
    Code = "ACCOUNT_NOT_FOUND",
    Message = "Account not found",
    Details = new { AccountId = id }
};
```

---

## GitFlow Branching

This project uses GitFlow. **Always create feature branches from `develop`**, not `main`.

```bash
# Start a new feature
git checkout develop
git pull origin develop
git checkout -b feature/my-feature-name

# Finish and merge back to develop
git checkout develop
git merge --no-ff feature/my-feature-name
```

| Branch Type | Create From | Merge To |
|-------------|-------------|----------|
| `feature/*` | `develop` | `develop` |
| `bugfix/*` | `develop` | `develop` |
| `release/*` | `develop` | `main` + `develop` |
| `hotfix/*` | `main` | `main` + `develop` |

---

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test tests/UnitTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Requirements

- **90% minimum coverage** for new code
- Use **xUnit** for unit tests
- Use **bUnit** for Blazor component tests
- Use **Reqnroll** for BDD acceptance tests (Gherkin feature files)
- Use **Playwright** for browser automation in E2E tests

---

## Common Tasks

### Adding a New API Endpoint

1. Create function in `src/Api/Functions/`
2. Add service in `src/Api/Services/` if needed
3. Add DTOs in `src/Shared/Models/`
4. Add unit tests in `tests/UnitTests/`
5. Update API tracking doc: `docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md`

### Adding a New Blazor Page

1. Create page in `src/Client/Pages/`
2. Add route with `@page "/route"` directive
3. Create child components in `src/Client/Components/`
4. Add localization strings to `.resx` files
5. Add bUnit tests in `tests/UnitTests/Client/`
6. Update UI tracking doc: `docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md`

### Adding Localization

1. Add string to `Resources/SharedResources.resx` (English)
2. Add translations to `Resources/SharedResources.{culture}.resx`
3. Inject `IStringLocalizer<SharedResources>` in component
4. Use `@Localizer["Key"]` in Razor markup

---

## Useful Commands

```bash
# Build the solution
dotnet build src/RajFinancial.sln

# Run API locally
cd src/Api && func start

# Run Client locally  
cd src/Client && dotnet run

# Add a new package
dotnet add src/Api package PackageName

# Format code
dotnet format src/RajFinancial.sln
```

---

## Links

- **Copilot Instructions**: [.github/copilot-instructions.md](.github/copilot-instructions.md)
- **Execution Plan**: [docs/RAJ_FINANCIAL_EXECUTION_PLAN.md](docs/RAJ_FINANCIAL_EXECUTION_PLAN.md)
- **API Tracking**: [docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md](docs/RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md)
- **UI Tracking**: [docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md](docs/RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md)


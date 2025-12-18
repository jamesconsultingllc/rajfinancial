# RAJ Financial Execution Plan: Blazor Web Setup

## Overview
Initialize the project structure for RAJ Financial using Blazor Web App (Auto render mode) as per the decision matrix and guidelines.

## Tasks
- [x] 1. Create GitFlow branches (`develop` and `feature/blazor-web-setup`)
- [x] 2. Cleanup legacy src content
- [x] 3. Initialize Blazor Web App solution and projects
    - [x] Create `RajFinancial.sln`
    - [x] Create `src/Client` (Blazor WebAssembly project)
    - [x] Create `src/Server` (ASP.NET Core Server project)
    - [x] Create `src/Shared` (Shared Class Library)
- [x] 4. Initialize Test projects
    - [x] Create `tests/UnitTests` (xUnit + Moq)
    - [x] Create `tests/IntegrationTests` (xUnit)
    - [x] Create `tests/AcceptanceTests` (Reqnroll BDD)
- [ ] 5. Configure Syncfusion Blazor components
- [ ] 6. Set up Localization (`IStringLocalizer`)
- [ ] 7. Set up MemoryPack serialization
- [x] 8. Update Deployment Workflow (GitHub Actions)
    - [x] Align target frameworks to net9.0
    - [x] Update SWA workflow for new project structure and GitFlow
- [ ] 9. Document API and UI in tracking files

## Files to Modify/Create
- `RAJ_FINANCIAL_EXECUTION_PLAN.md` - [updated]
- `RAJ_FINANCIAL_TRACKING.md` - [updated]
- `.github/workflows/azure-static-web-apps-*.yml` - [updated]
- `src/Client/RajFinancial.Client.csproj` - [updated]
- `src/Server/RajFinancial.Server.csproj` - [updated]
- `src/Shared/RajFinancial.Shared.csproj` - [updated]
- `tests/UnitTests/RajFinancial.UnitTests.csproj` - [created]
- `tests/IntegrationTests/RajFinancial.IntegrationTests.csproj` - [created]
- `tests/AcceptanceTests/RajFinancial.AcceptanceTests.csproj` - [created]

## Testing Requirements
- [ ] Ensure all projects compile
- [ ] Verify basic routing
- [ ] Verify localization middleware
- [ ] Verify Syncfusion license activation

## Acceptance Criteria
- [ ] Clean GitFlow branch structure
- [ ] Compiling solution with Client, Server, and Shared projects
- [ ] Adherence to mobile-first and accessibility guidelines in initial layout
- [ ] MemoryPack and Syncfusion integrated

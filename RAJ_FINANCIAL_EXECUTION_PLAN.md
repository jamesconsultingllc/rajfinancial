﻿# RAJ Financial Execution Plan: OIDC Deployment and Infrastructure

## Overview
Implement OIDC (Federated Credentials) for secure GitHub Actions deployment and prepare infrastructure scripts for CORS and storage.

## Tasks
- [x] 1. Create feature branch `feature/oidc-deployment-setup` from `develop`
- [x] 2. Update GitHub Actions workflow to use OIDC
    - [x] Add `permissions` for `id-token: write`
    - [x] Add Azure Login step using Client ID, Tenant ID, and Subscription ID
    - [x] Update SWA deployment to use federated credentials if supported or continue with token while authenticated via Azure Login
- [x] 3. Add Infrastructure preparation steps
    - [x] Create script for Blob Storage CORS configuration
    - [x] Integrate script into workflow (optional/commented)
- [x] 4. Synchronize project settings with reference (texas-build-pros)
    - [x] Implement separate unit tests job
    - [x] Add environment-based deployment
    - [x] Add settings synchronization for preview environments
    - [x] Add E2E tests job
    - [x] Implement comprehensive PR cleanup
    - [x] Create test project stubs
    - [x] Create setup documentation

## Files to Modify
- `RAJ_FINANCIAL_EXECUTION_PLAN.md` - [updated]
- `RAJ_FINANCIAL_TRACKING.md` - [updated]
- `.github/workflows/azure-static-web-apps-*.yml` - [updated]
- `tests/UnitTests/UnitTests.csproj` - [created]
- `tests/AcceptanceTests/AcceptanceTests.csproj` - [created]
- `tests/IntegrationTests/IntegrationTests.csproj` - [created]
- `AZURE_FEDERATED_CREDENTIALS_SETUP.md` - [created]
- `WORKFLOW_UPDATE_SUMMARY.md` - [created]

## Testing Requirements
- [x] Verify GitHub Actions workflow syntax
- [ ] (Manual) Verify OIDC connection in GitHub Repo (requires secret setup)
- [ ] (Manual) Test deployment to development environment
- [ ] (Manual) Test preview environment creation
- [ ] (Manual) Test PR cleanup process

## Acceptance Criteria
- [x] Workflow uses Azure Login with OIDC
- [x] Prepared scripts for future Blob Storage CORS settings
- [x] Aligned with texas-build-pros deployment pattern
- [x] Separate unit tests job runs before deployment
- [x] Environment-based deployment configured
- [x] Settings synchronization for preview environments
- [x] E2E tests run after deployment
- [x] Comprehensive PR cleanup implemented
- [x] Test project stubs created
- [x] Setup documentation provided

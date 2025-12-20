# Implementation Plan: Security & Infrastructure

## Overview
Set up the foundational security and infrastructure components for RAJ Financial, including:
- Microsoft Entra External ID configuration
- Data access and sharing model
- Bicep infrastructure as code
- CI/CD pipeline setup

## Branch Info
- **Branch**: `feature/security-infrastructure`
- **Created From**: `develop`
- **Created**: December 18, 2025

---

## Tasks

### Phase 1: Entra External ID Setup
- [x] 1. Create dev Entra External ID tenant (`rajfinancialdev.onmicrosoft.com`)
- [x] 2. Create prod Entra External ID tenant (`rajfinancialprod.onmicrosoft.com`)
- [x] 3. Document multi-tenant strategy in execution plan
- [x] 4. Create app registrations in dev tenant (API + SPA)
- [ ] 5. Configure SWA environment variables in Azure Portal (see below)
- [x] 6. Create `staticwebapp.config.json` with auth routes
- [ ] 7. Configure user flows (Sign-up, Sign-in) in Entra External ID portal
- [x] 8. Create Entra branding assets and configuration
- [ ] 9. Apply branding in Entra External ID portal
- [ ] 10. Test authentication flow

**App Registrations Created (Dev Tenant):**
| App | Client ID | Purpose |
|-----|-----------|---------|
| API | `211438af-9f00-47be-a367-796dd7770113` | Azure Functions token validation |
| SPA | `2d6a08c7-b142-4d53-a307-9ac75bae75eb` | Blazor WASM authentication |

**SWA Environment Variables to Configure:**

In Azure Portal → Static Web App → Settings → Environment Variables:

| Variable | Value | Notes |
|----------|-------|-------|
| `AZURE_CLIENT_ID` | `2d6a08c7-b142-4d53-a307-9ac75bae75eb` | SPA app registration |
| `AZURE_CLIENT_SECRET` | `(generate in Entra portal)` | Create in App Registration → Certificates & secrets |
| `AZURE_TENANT_ID` | `496527a2-41f8-4297-a979-c916e7255a22` | Dev tenant ID |

**To generate client secret:**
1. Go to Entra External ID portal → App registrations → `rajfinancial-spa-dev`
2. Certificates & secrets → New client secret
3. Copy value immediately (shown only once)
4. Add to SWA environment variables as `AZURE_CLIENT_SECRET`

### Phase 2: Data Access Model
- [x] 9. Document data access & sharing model in execution plan
- [x] 10. Create `DataAccessGrant` entity (src/Shared/Entities/)
- [ ] 11. Create `User` entity
- [ ] 12. Create `IDataAccessService` interface
- [ ] 13. Document sharing API endpoints

### Phase 3: Infrastructure as Code (Bicep)
- [x] 14. Create `infra/` folder structure
- [x] 15. Create `main.bicep` orchestrator
- [x] 16. Create `parameters/dev.bicepparam`
- [x] 17. Create `parameters/prod.bicepparam`
- [x] 18. Create `modules/monitoring.bicep`
- [x] 19. Create `modules/keyvault.bicep`
- [x] 20. Create `modules/storage.bicep`
- [ ] 21. Create `modules/sql.bicep`
- [ ] 22. Create `modules/functions.bicep`
- [ ] 23. Create `modules/staticwebapp.bicep`
- [ ] 24. Create `modules/redis.bicep`
- [ ] 25. Create `modules/identity.bicep`

### Phase 4: Local Development Settings
- [x] 26. Update `Api/local.settings.json` with dev tenant config
- [x] 27. Update `Api/appsettings.Development.json` with dev tenant config
- [x] 28. Update `Client/appsettings.Development.json` with dev tenant config
- [ ] 29. Add MemoryPack package to Shared project
- [ ] 30. Verify solution builds

### Phase 5: CI/CD Pipeline (Future)
- [ ] 31. Create `.github/workflows/ci.yml`
- [ ] 32. Create `.github/workflows/deploy-dev.yml`
- [ ] 33. Create `.github/workflows/deploy-prod.yml`
- [ ] 34. Configure GitHub OIDC for Azure

---

## Files Created/Modified

### Created
| File | Purpose |
|------|---------|
| `infra/main.bicep` | Bicep orchestrator |
| `infra/parameters/dev.bicepparam` | Dev environment parameters |
| `infra/parameters/prod.bicepparam` | Prod environment parameters |
| `infra/modules/monitoring.bicep` | App Insights + Log Analytics |
| `infra/modules/keyvault.bicep` | Key Vault |
| `infra/modules/storage.bicep` | Blob Storage |
| `infra/scripts/register-entra-apps.ps1` | Entra app registration script |
| `infra/entra-branding/README.md` | Entra branding configuration guide |
| `infra/entra-branding/custom-styles.css` | Custom CSS for Entra login pages |
| `infra/entra-branding/banner-logo.png` | Horizontal logo for sign-in page |
| `infra/entra-branding/square-logo.png` | Square logo for tiles/loading |
| `infra/entra-branding/favicon.ico` | Favicon for Entra pages |
| `src/Shared/Entities/DataAccessGrant.cs` | Data sharing entity |
| `src/Client/wwwroot/staticwebapp.config.json` | SWA auth & routing config |
| `src/Client/wwwroot/images/brand/*` | Brand logos (7 files) |
| `src/Client/wwwroot/favicon*.png` | Favicon assets |
| `.github/actions/manage-entra-redirect-uris/action.yml` | GitHub Action for managing Entra redirect URIs |
| `.github/actions/manage-entra-redirect-uris/README.md` | Documentation for manage-entra-redirect-uris |
| `.github/actions/find-swa-slot/action.yml` | GitHub Action for finding SWA environment slots |
| `.github/actions/find-swa-slot/README.md` | Documentation for find-swa-slot |

### Modified
| File | Changes |
|------|---------|
| `.github/copilot-instructions.md` | Added Blazor-specific patterns |
| `.github/workflows/azure-static-web-apps-gray-cliff-072f3b510.yml` | Added Entra redirect URI management |
| `docs/RAJ_FINANCIAL_EXECUTION_PLAN.md` | Added multi-tenant strategy, data access model |
| `docs/RAJ_FINANCIAL_INTEGRATIONS_API.md` | Added multi-tenant environment strategy |
| `src/Api/local.settings.json` | Added Entra dev tenant config |
| `src/Api/appsettings.Development.json` | Added Entra dev tenant config |
| `src/Client/appsettings.json` | Added placeholder auth config |
| `src/Client/appsettings.Development.json` | Added Entra dev tenant config |

---

## GitHub Secrets Required

| Secret | Value | Notes |
|--------|-------|-------|
| `ENTRA_SPA_APP_OBJECT_ID` | `fe98922f-d169-44fb-b4d1-c550fc2ede60` | Object ID of SPA app registration (not Client ID) |
| `ENTRA_DEV_TENANT_ID` | `496527a2-41f8-4297-a979-c916e7255a22` | Entra External ID dev tenant |
| `AZURE_CLIENT_ID` | *(existing)* | For OIDC login to main Azure subscription |
| `AZURE_TENANT_ID` | *(existing)* | Main Azure tenant |
| `AZURE_SUBSCRIPTION_ID` | *(existing)* | Azure subscription |

> **Note**: The workflow logs into the Entra External ID tenant using `az login --tenant` to update app registrations. The GitHub runner uses OIDC for the main Azure subscription, then re-authenticates to the Entra tenant for redirect URI management.

### Modified
| File | Changes |
|------|---------|
| `docs/RAJ_FINANCIAL_EXECUTION_PLAN.md` | Added multi-tenant strategy, data access model |
| `docs/RAJ_FINANCIAL_INTEGRATIONS_API.md` | Added multi-tenant environment strategy |
| `src/Api/local.settings.json` | Added Entra dev tenant config |
| `src/Api/appsettings.Development.json` | Added Entra dev tenant config |
| `src/Client/appsettings.json` | Added placeholder auth config |
| `src/Client/appsettings.Development.json` | Added Entra dev tenant config |

---

## Tenant Information

| Environment | Tenant ID | Domain |
|-------------|-----------|--------|
| Development | `496527a2-41f8-4297-a979-c916e7255a22` | `rajfinancialdev.onmicrosoft.com` |
| Production | `cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6` | `rajfinancialprod.onmicrosoft.com` |

## Static Web App

| Environment | URL |
|-------------|-----|
| Development | `https://gray-cliff-072f3b510.azurestaticapps.net` |

**Redirect URI configured in SPA app registration:**
`https://gray-cliff-072f3b510.azurestaticapps.net/.auth/login/entraExternalId/callback`

---

## Next Steps
1. Run `register-entra-apps.ps1 -Environment dev` to create app registrations
2. Update config files with actual client IDs
3. Add MemoryPack NuGet package to Shared project
4. Test solution builds

---

## Acceptance Criteria
- [ ] Dev Entra tenant has SPA and API app registrations
- [ ] Local dev can authenticate against dev tenant
- [ ] Bicep templates are valid (pass `az bicep build`)
- [ ] Solution compiles without errors
- [ ] Data access model is fully documented


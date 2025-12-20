# Implementation Plan: Security & Infrastructure - Phase 2

## Overview
Continue the foundational security and infrastructure setup for RAJ Financial.
This branch continues from the work completed in `feature/security-infrastructure`.

## Branch Info
- **Branch**: `feature/security-infrastructure-phase2`
- **Created From**: `develop`
- **Created**: December 20, 2025
- **Previous Branch**: `feature/security-infrastructure` (merged via PRs #20-24)

---

## Completed in Phase 1
The following was completed and merged:
- ✅ Entra External ID dev/prod tenants created
- ✅ App registrations (API + SPA) created
- ✅ staticwebapp.config.json created
- ✅ Entra branding assets created
- ✅ Bicep modules: main, monitoring, keyvault, storage
- ✅ Local development settings configured
- ✅ DataAccessGrant entity created
- ✅ CI/CD workflow with preview environments
- ✅ GitHub Actions for Entra redirect URI management

---

## Phase 2 Tasks

### Data Model (Current Focus)
- [x] 1. Create `UserProfile` entity (supports multiple advisor relationships via DataAccessGrant)
- [ ] 2. Create `IDataAccessService` interface
- [ ] 3. Document sharing API endpoints

### Entra Portal Configuration (Manual)
- [ ] 4. Configure SWA environment variables in Azure Portal
- [ ] 5. Configure user flows (Sign-up, Sign-in) in Entra External ID portal
- [ ] 6. Apply branding in Entra External ID portal
- [ ] 7. Test authentication flow end-to-end

### Remaining Bicep Modules
- [ ] 8. Create `modules/sql.bicep`
- [ ] 9. Create `modules/functions.bicep`
- [ ] 10. Create `modules/staticwebapp.bicep`
- [ ] 11. Create `modules/redis.bicep`
- [ ] 12. Create `modules/identity.bicep`

### Package Dependencies
- [ ] 13. Add MemoryPack package to Shared project
- [ ] 14. Verify solution builds

---

## Files to Create/Modify

### To Create
| File | Purpose |
|------|---------|
| `src/Shared/Entities/UserProfile.cs` | User profile entity (supports multiple advisors) |
| `src/Shared/Contracts/IDataAccessService.cs` | Data access service interface |
| `infra/modules/sql.bicep` | Azure SQL Database module |
| `infra/modules/functions.bicep` | Azure Functions module |
| `infra/modules/staticwebapp.bicep` | Static Web App module |
| `infra/modules/redis.bicep` | Redis Cache module |
| `infra/modules/identity.bicep` | Managed Identity module |

---

## Notes on User/Advisor Relationships
- A **Client** can grant access to **multiple Advisors** via `DataAccessGrant` entity
- The `UserProfile` entity does NOT have a single `AdvisorId` field
- Advisor relationships are managed through the data access grants system
- This provides flexibility for clients to work with multiple advisors

---

## Acceptance Criteria
- [ ] UserProfile entity is complete and documented
- [ ] IDataAccessService interface defined
- [ ] Solution compiles with MemoryPack
- [ ] All Bicep modules pass validation
- [ ] Authentication flow tested end-to-end

# Implementation Plan: UserProfile JIT Provisioning

## Overview

Implement Just-In-Time (JIT) provisioning of `UserProfile` records via middleware. On every authenticated request, check if a local `UserProfile` exists for the JWT's `oid` claim. If not, create one from claims. Always update `LastLoginAt`. This is the **P0 blocker** preventing Asset CRUD integration tests from passing (`FK_Assets_UserProfiles_UserId`).

## Branch

`feature/userprofile-provisioning` (from `develop`)

## Approach

**BDD + TDD** — Tests first, implementation after.

## Tasks

- [x] 1. Create IMPLEMENTATION_PLAN.md (this file)
- [x] 2. Write BDD feature file (`tests/IntegrationTests/Features/UserProfileProvisioning.feature`) — 8 scenarios
- [x] 3. Write BDD step definitions (`tests/IntegrationTests/StepDefinitions/UserProfileProvisioningSteps.cs`) — 381 lines
- [x] 4. Write TDD unit tests for UserProfileService (`tests/Api.Tests/Services/UserProfiles/UserProfileServiceTests.cs`) — 24 tests
- [x] 5. Write TDD unit tests for middleware (`tests/Api.Tests/Middleware/UserProfileProvisioningMiddlewareTests.cs`) — 10 tests
- [x] 6. Write TDD unit tests for ProfileFunctions (`tests/Api.Tests/Functions/ProfileFunctionsTests.cs`) — 8 tests
- [x] 7. Implement `UserProfileService` (`src/Api/Services/UserProfile/UserProfileService.cs`)
- [x] 8. Implement `UserProfileProvisioningMiddleware` (`src/Api/Middleware/UserProfileProvisioningMiddleware.cs`)
- [x] 9. Implement `ProfileFunctions` (`src/Api/Functions/ProfileFunctions.cs`) — GET /api/profile/me
- [x] 10. Wire up DI + middleware pipeline in `src/Api/Program.cs`
- [x] 11. Run all tests and verify green — **221/221 pass, 0 failures, 0 regressions**

## Files Created

| File | Purpose | Tests |
|------|---------|-------|
| `tests/IntegrationTests/Features/UserProfileProvisioning.feature` | 8 BDD scenarios | Integration (requires live host) |
| `tests/IntegrationTests/StepDefinitions/UserProfileProvisioningSteps.cs` | BDD step definitions (381 lines) | Integration |
| `tests/Api.Tests/Services/UserProfiles/UserProfileServiceTests.cs` | 24 unit tests | ✅ All pass |
| `tests/Api.Tests/Middleware/UserProfileProvisioningMiddlewareTests.cs` | 10 unit tests | ✅ All pass |
| `tests/Api.Tests/Functions/ProfileFunctionsTests.cs` | 8 unit tests | ✅ All pass |
| `src/Api/Services/UserProfile/UserProfileService.cs` | JIT provisioning + profile retrieval | — |
| `src/Api/Middleware/UserProfileProvisioningMiddleware.cs` | Pipeline middleware | — |
| `src/Api/Functions/ProfileFunctions.cs` | GET /api/profile/me endpoint | — |

## Files Modified

| File | Change |
|------|--------|
| `src/Api/Program.cs` | DI registration + middleware pipeline (6-step) |
| `src/Api/Services/UserProfile/IUserProfileService.cs` | Namespace → `UserProfiles` (plural) |

## Key Design Decisions

- **Middleware position**: after `AuthenticationMiddleware`, before `AuthorizationMiddleware`
- **Upsert pattern**: `FindAsync(userId)` → create if null, update claims if changed, always stamp `LastLoginAt`
- **Role mapping**: highest-priority JWT role → `UserRole` enum (Administrator > Advisor > Client)
- **TenantId**: from JWT `tid` claim or default `Guid.Empty`
- **Concurrency**: handle unique constraint `{TenantId, Email}` with try/catch
- **Namespace**: `RajFinancial.Api.Services.UserProfiles` (PLURAL) — renamed from `UserProfile` to avoid C# namespace hierarchy conflict with `RajFinancial.Shared.Entities.UserProfile` entity class
- **Exception catch**: `catch (System.Exception ex)` in middleware — `Exception` alone conflicts with `RajFinancial.Api.Middleware.Exception` sub-namespace
- **Failure mode**: provisioning failures logged as warnings, never block the pipeline

## Acceptance Criteria

- [x] First authenticated request creates a UserProfile row with correct Id, Email, DisplayName, Role
- [x] Subsequent requests update LastLoginAt without creating duplicates
- [x] Unauthenticated requests skip provisioning entirely
- [x] Claims changes (email, name, role) are synced on next request
- [x] All unit tests pass — 44 new tests (24 + 10 + 8 + 2 GetById), **221/221 total pass**
- [ ] BDD integration scenarios pass — requires live Functions host + database

## Integration Test Status

BDD feature file and step definitions are complete but **cannot run** until:
1. Functions host is started locally (`func start`) or deployed
2. Database is available with EF Core migrations applied
3. This branch is merged — Asset integration tests on `feature/334-asset-service` depend on provisioning

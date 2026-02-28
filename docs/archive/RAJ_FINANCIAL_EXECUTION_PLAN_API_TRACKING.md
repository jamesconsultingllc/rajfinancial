# RAJ Financial Software - Execution Plan (API Tracking)

This document contains the API implementation tracking tables extracted from [RAJ_FINANCIAL_EXECUTION_PLAN.md](RAJ_FINANCIAL_EXECUTION_PLAN.md).

---

## Part 2: API Development Tracking

### 2.0 Security & Identity (from Part 0)

#### Entities
| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| UserProfile entity | ✅ Complete | P0 | `src/Shared/Entities/UserProfile.cs` |
| DataAccessGrant entity | ✅ Complete | P0 | `src/Shared/Entities/DataAccessGrant.cs` |
| AccessType enum | ✅ Complete | P0 | Owner, Full, Read, Limited |
| GrantStatus enum | ✅ Complete | P0 | Pending, Active, Expired, Revoked |

#### Services
| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| IAuthorizationService interface | ✅ Complete | P0 | Three-tier resource authorization |
| AuthorizationService implementation | ✅ Complete | P0 | Owner > DataAccessGrant > Administrator |
| UserProfileService (API) | ⬜ Not Started | P0 | Sync Entra claims with profile |

#### Data Access Grant Functions
| Endpoint | Function | Status | Priority |
|----------|----------|--------|----------|
| POST /api/access/grants | CreateAccessGrant | ⬜ Not Started | P0 |
| GET /api/access/grants | GetMyGrants | ⬜ Not Started | P0 |
| GET /api/access/grants/received | GetReceivedGrants | ⬜ Not Started | P0 |
| POST /api/access/grants/{id}/accept | AcceptGrant | ⬜ Not Started | P0 |
| DELETE /api/access/grants/{id} | RevokeGrant | ⬜ Not Started | P0 |
| PATCH /api/access/grants/{id} | UpdateGrant | ⬜ Not Started | P1 |

---

### 2.1 Project Setup

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| Create solution structure | ✅ Complete | P0 | Api, Client, Shared projects |
| Configure Azure Functions project | ✅ Complete | P0 | Isolated worker, .NET 9 |
| Add MemoryPack package | ✅ Complete | P0 | High-performance serialization |
| Set up EF Core with Azure SQL | ✅ Complete | P0 | DbContext, migrations, MI auth |
| Configure Key Vault integration | ⬜ Not Started | P0 | Managed Identity access |
| Set up Application Insights | ⬜ Not Started | P1 | Logging, telemetry |
| Configure Redis caching | ⬜ Not Started | P1 | IDistributedCache, AAD auth |
| Configure Entra External ID validation | ⬜ Not Started | P0 | JWT Bearer middleware |

### 2.2 Shared Library (RAJFinancial.Shared)

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| AssetDto + AssetDetailDto | ✅ Complete | P0 | MemoryPack attributes, detail includes depreciation |
| CreateAssetRequest + UpdateAssetRequest | ✅ Complete | P0 | MemoryPack attributes |
| LinkedAccountDto | ⬜ Not Started | P0 | MemoryPack attributes |
| ContactDto (polymorphic) | ⬜ Not Started | P0 | Individual, Trust, Organization subtypes |
| TrustRoleDto | ⬜ Not Started | P0 | Trust-to-Contact role mapping |
| AssetContactLinkDto | ⬜ Not Started | P0 | Contact-to-Asset link (beneficiary, co-owner, etc.) |
| AIInsightDto | ⬜ Not Started | P1 | MemoryPack attributes |
| DebtPayoffAnalysisDto | ⬜ Not Started | P1 | Strategy results |
| InsuranceCoverageDto | ⬜ Not Started | P1 | Breakdown included |
| ApiErrorResponse | ✅ Complete | P0 | Standardized errors with Code, Message, Details |
| ErrorCodes constants | ✅ Complete | P0 | NotFoundException, ForbiddenException, etc. |
| AddressDto | ⬜ Not Started | P1 | Value object |
| Enums (all) | ✅ Complete | P0 | AssetType, DepreciationMethod done; Contact enums remaining |

### 2.3 Core Domain (RAJFinancial.Core)

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| User entity | ⬜ Not Started | P0 | Entra External ID linked |
| LinkedAccount entity | ⬜ Not Started | P0 | |
| Asset entity | ✅ Complete | P0 | Includes depreciation, disposal, valuation fields |
| Contact entity (base) | ⬜ Not Started | P0 | Abstract base for all contact types |
| IndividualContact entity | ⬜ Not Started | P0 | Person: name, DOB, SSN, relationship |
| TrustContact entity | ⬜ Not Started | P0 | Trust: name, EIN, category, purpose, specificType |
| OrganizationContact entity | ⬜ Not Started | P0 | Org: name, EIN, type (Charity, Business, etc.) |
| TrustRole entity | ⬜ Not Started | P0 | Links Contact to Trust with role |
| AssetContactLink entity | ⬜ Not Started | P0 | Links Contact to Asset (Beneficiary, CoOwner, etc.) |
| ClientRelationship entity | ⬜ Not Started | P0 | Professional-to-client mapping |
| AuditLog entity | ⬜ Not Started | P1 | |
| IAccountService interface | ⬜ Not Started | P0 | |
| IAssetService interface | ✅ Complete | P0 | |
| IContactService interface | ⬜ Not Started | P0 | Replaces IBeneficiaryService |
| IAnalysisService interface | ⬜ Not Started | P1 | |
| IPlaidService interface | ⬜ Not Started | P0 | |
| IClaudeAIService interface | ⬜ Not Started | P1 | |
| Value objects (Money, Percentage) | ⬜ Not Started | P2 | |

### 2.4 Infrastructure (RAJFinancial.Infrastructure)

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| ApplicationDbContext | ✅ Complete | P0 | Tenant isolation, UserProfile + DataAccessGrant |
| Entity configurations | ✅ Complete | P0 | Fluent API, enums as strings, JSON categories |
| Initial migration | ✅ Complete | P0 | 20260205040602_InitialCreate |
| PlaidService implementation | ⬜ Not Started | P0 | Full Plaid integration |
| ClaudeAIService implementation | ⬜ Not Started | P1 | Insight generation |
| EncryptionService | ⬜ Not Started | P0 | Key Vault integration |
| AuditLogger | ⬜ Not Started | P1 | Change tracking |
| AssetRepository | N/A | P0 | Not needed — AssetService uses EF Core directly |
| LinkedAccountRepository | ⬜ Not Started | P0 | |
| BeneficiaryRepository | ⬜ Not Started | P0 | |

### 2.5 Application Services (RAJFinancial.Application)

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| AccountService | ⬜ Not Started | P0 | Plaid orchestration |
| AssetService | ✅ Complete | P0 | CRUD + validation + depreciation calc |
| DepreciationCalculator | ✅ Complete | P0 | Pure computation: StraightLine, DecliningBalance, MACRS |
| ContactService | ⬜ Not Started | P0 | CRUD + trust roles + asset links |
| AnalysisService | ⬜ Not Started | P1 | Net worth, debt, insurance |
| CreateAssetValidator | ✅ Complete | P0 | FluentValidation |
| CreateContactValidator | ⬜ Not Started | P0 | FluentValidation (Individual, Trust, Org) |
| MappingProfile | ⬜ Not Started | P1 | AutoMapper config |

### 2.6 API Functions (RAJFinancial.Api)

#### Middleware
| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| ContentNegotiationMiddleware | ✅ Complete | P0 | JSON/MemoryPack |
| ExceptionMiddleware | ✅ Complete | P0 | Global error handling |
| AuthenticationMiddleware | ✅ Complete | P0 | JWT validation + EasyAuth |
| AuthorizationMiddleware | ✅ Complete | P0 | Attribute-based [RequireAuthentication]/[RequireRole] |
| ValidationMiddleware | ✅ Complete | P0 | FluentValidation pipeline |

#### Serialization
| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| SerializationFactory | ✅ Complete | P0 | ISerializationFactory |
| MemoryPackSerializer helper | ✅ Complete | P0 | Content negotiation via Accept header |

#### Account Functions
| Endpoint | Function | Status | Priority |
|----------|----------|--------|----------|
| GET /api/accounts | GetAccounts | ⬜ Not Started | P0 |
| POST /api/accounts/link-token | CreateLinkToken | ⬜ Not Started | P0 |
| POST /api/accounts/exchange | ExchangePublicToken | ⬜ Not Started | P0 |
| POST /api/accounts/{id}/refresh | RefreshAccount | ⬜ Not Started | P1 |
| DELETE /api/accounts/{id} | UnlinkAccount | ⬜ Not Started | P1 |

#### Asset Functions
| Endpoint | Function | Status | Priority |
|----------|----------|--------|----------|
| GET /api/assets | GetAssets | ✅ Complete | P0 |
| GET /api/assets/{id} | GetAssetById | ✅ Complete | P0 |
| POST /api/assets | CreateAsset | ✅ Complete | P0 |
| PUT /api/assets/{id} | UpdateAsset | ✅ Complete | P0 |
| DELETE /api/assets/{id} | DeleteAsset | ✅ Complete | P0 |
| GET /api/assets/summary | GetAssetSummary | ⬜ Not Started | P1 |
| POST /api/assets/{id}/dispose | DisposeAsset | ⬜ Not Started | P1 |
| GET /api/assets/{id}/depreciation | GetDepreciationSchedule | ⬜ Not Started | P2 |

#### Contact Functions (formerly Beneficiary Functions)
> **Note**: "Beneficiary" is now a *role* a Contact plays when linked to an Asset. See [RAJ_FINANCIAL_INTEGRATIONS_API.md](RAJ_FINANCIAL_INTEGRATIONS_API.md) for details.

| Endpoint | Function | Status | Priority |
|----------|----------|--------|----------|
| GET /api/contacts | GetContacts | ⬜ Not Started | P0 |
| GET /api/contacts/{id} | GetContactById | ⬜ Not Started | P0 |
| POST /api/contacts | CreateContact | ⬜ Not Started | P0 |
| PUT /api/contacts/{id} | UpdateContact | ⬜ Not Started | P0 |
| DELETE /api/contacts/{id} | DeleteContact | ⬜ Not Started | P0 |
| GET /api/contacts/{id}/roles | GetTrustRoles | ⬜ Not Started | P1 |
| POST /api/contacts/{id}/roles | AddTrustRole | ⬜ Not Started | P1 |
| PUT /api/contacts/{id}/roles/{roleId} | UpdateTrustRole | ⬜ Not Started | P1 |
| DELETE /api/contacts/{id}/roles/{roleId} | RemoveTrustRole | ⬜ Not Started | P1 |
| GET /api/assets/{id}/contacts | GetAssetContacts | ⬜ Not Started | P0 |
| POST /api/assets/{id}/contacts | LinkContactToAsset | ⬜ Not Started | P0 |
| PUT /api/asset-links/{linkId} | UpdateAssetContactLink | ⬜ Not Started | P1 |
| DELETE /api/asset-links/{linkId} | RemoveAssetContactLink | ⬜ Not Started | P1 |
| GET /api/contacts/coverage | GetBeneficiaryCoverage | ⬜ Not Started | P1 |

#### Analysis Functions
| Endpoint | Function | Status | Priority |
|----------|----------|--------|----------|
| GET /api/analysis/net-worth | CalculateNetWorth | ⬜ Not Started | P1 |
| POST /api/analysis/debt-payoff | AnalyzeDebtPayoff | ⬜ Not Started | P1 |
| POST /api/analysis/insurance | AnalyzeInsurance | ⬜ Not Started | P1 |
| GET /api/analysis/insights | GetAIInsights | ⬜ Not Started | P1 |

#### Webhook Functions
| Endpoint | Function | Status | Priority |
|----------|----------|--------|----------|
| POST /api/plaid/webhook | PlaidWebhook | ⬜ Not Started | P1 |

#### Auth Functions (Feature #485)
| Endpoint | Function | Status | Priority | BDD | Unit Tests | ADO Task |
|----------|----------|--------|----------|-----|------------|----------|
| GET /api/auth/me | AuthMe | ✅ Complete | P0 | ✅ 11 scenarios | ✅ 19 tests | #489 |
| GET /api/auth/roles | AuthRoles | ✅ Complete | P0 | ✅ 11 scenarios | ✅ 8 tests | #489 |
| POST /api/auth/clients | AssignClient | ✅ Complete | P1 | ✅ 18 scenarios | ✅ 14 tests | #492 |
| GET /api/auth/clients | GetAssignedClients | ✅ Complete | P1 | ✅ 18 scenarios | ✅ 10 tests | #492 |
| DELETE /api/auth/clients/{id} | RemoveClientAccess | ✅ Complete | P1 | ✅ 18 scenarios | ✅ 16 tests | #492 |

**Branch**: `feature/485-auth-functions` | **Feature**: [#485](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/485)

**Child Tasks**:
- #486 BDD Feature Files (✅ Done) — 29 scenarios across AuthUserProfile.feature + ClientManagement.feature
- #487 Auth DTOs & Contracts (✅ Done) — UserProfileResponse, UserRolesResponse, AssignClientRequest, ClientAssignmentResponse, AssignClientRequestValidator
- #488 Auth Unit Tests – Security-First TDD (✅ Done) — 27 tests in AuthFunctionsTests.cs, all passing
- #489 Auth Functions Implementation (✅ Done) — AuthFunctions.cs with GetMe + GetRoles endpoints
- #490 Client Management Unit Tests (✅ Done) — 40 tests in ClientManagementFunctionsTests.cs, all passing
- #491 IClientManagementService + Implementation (✅ Done) — IClientManagementService interface + ClientManagementService EF Core implementation
- #492 Client Management Functions (✅ Done) — ClientManagementFunctions.cs with AssignClient + GetClients + RemoveClient endpoints

> **Note**: Registration, login, password reset, and MFA are handled by Microsoft Entra External ID. These Auth Functions manage user profile and professional relationships within the application.

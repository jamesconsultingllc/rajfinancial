th# RAJ Financial Software - Execution Plan (API Tracking)

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
| IDataAccessService interface | ⬜ Not Started | P0 | Authorization logic |
| DataAccessService implementation | ⬜ Not Started | P0 | Check user access permissions |
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
| Add MemoryPack package | ⬜ Not Started | P0 | High-performance serialization |
| Set up EF Core 9 with Azure SQL | ⬜ Not Started | P0 | DbContext, migrations, MI auth |
| Configure Key Vault integration | ⬜ Not Started | P0 | Managed Identity access |
| Set up Application Insights | ⬜ Not Started | P1 | Logging, telemetry |
| Configure Redis caching | ⬜ Not Started | P1 | IDistributedCache, AAD auth |
| Configure Entra External ID validation | ⬜ Not Started | P0 | JWT Bearer middleware |

### 2.2 Shared Library (RAJFinancial.Shared)

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| AssetDto + CreateAssetRequest | ⬜ Not Started | P0 | MemoryPack attributes |
| LinkedAccountDto | ⬜ Not Started | P0 | MemoryPack attributes |
| BeneficiaryDto + requests | ⬜ Not Started | P0 | MemoryPack attributes |
| AIInsightDto | ⬜ Not Started | P1 | MemoryPack attributes |
| DebtPayoffAnalysisDto | ⬜ Not Started | P1 | Strategy results |
| InsuranceCoverageDto | ⬜ Not Started | P1 | Breakdown included |
| ApiErrorResponse | ⬜ Not Started | P0 | Standardized errors |
| ErrorCodes constants | ⬜ Not Started | P0 | All error codes |
| AddressDto | ⬜ Not Started | P1 | Value object |
| Enums (all) | ⬜ Not Started | P0 | AssetType, AccountType, etc. |

### 2.3 Core Domain (RAJFinancial.Core)

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| User entity | ⬜ Not Started | P0 | Entra External ID linked |
| LinkedAccount entity | ⬜ Not Started | P0 | |
| Asset entity | ⬜ Not Started | P0 | |
| Beneficiary entity | ⬜ Not Started | P0 | |
| BeneficiaryAssignment entity | ⬜ Not Started | P0 | |
| ClientRelationship entity | ⬜ Not Started | P0 | Professional-to-client mapping |
| AuditLog entity | ⬜ Not Started | P1 | |
| IAccountService interface | ⬜ Not Started | P0 | |
| IAssetService interface | ⬜ Not Started | P0 | |
| IBeneficiaryService interface | ⬜ Not Started | P0 | |
| IAnalysisService interface | ⬜ Not Started | P1 | |
| IPlaidService interface | ⬜ Not Started | P0 | |
| IClaudeAIService interface | ⬜ Not Started | P1 | |
| Value objects (Money, Percentage) | ⬜ Not Started | P2 | |

### 2.4 Infrastructure (RAJFinancial.Infrastructure)

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| ApplicationDbContext | ⬜ Not Started | P0 | Tenant isolation |
| Entity configurations | ⬜ Not Started | P0 | Fluent API |
| Initial migration | ⬜ Not Started | P0 | |
| PlaidService implementation | ⬜ Not Started | P0 | Full Plaid integration |
| ClaudeAIService implementation | ⬜ Not Started | P1 | Insight generation |
| EncryptionService | ⬜ Not Started | P0 | Key Vault integration |
| AuditLogger | ⬜ Not Started | P1 | Change tracking |
| AssetRepository | ⬜ Not Started | P0 | |
| LinkedAccountRepository | ⬜ Not Started | P0 | |
| BeneficiaryRepository | ⬜ Not Started | P0 | |

### 2.5 Application Services (RAJFinancial.Application)

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| AccountService | ⬜ Not Started | P0 | Plaid orchestration |
| AssetService | ⬜ Not Started | P0 | CRUD + validation |
| BeneficiaryService | ⬜ Not Started | P0 | Assignments included |
| AnalysisService | ⬜ Not Started | P1 | Net worth, debt, insurance |
| CreateAssetValidator | ⬜ Not Started | P0 | FluentValidation |
| CreateBeneficiaryValidator | ⬜ Not Started | P0 | FluentValidation |
| MappingProfile | ⬜ Not Started | P1 | AutoMapper config |

### 2.6 API Functions (RAJFinancial.Api)

#### Middleware
| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| ContentNegotiationMiddleware | ⬜ Not Started | P0 | JSON/MemoryPack |
| ExceptionMiddleware | ⬜ Not Started | P0 | Global error handling |
| TenantMiddleware | ⬜ Not Started | P0 | User context |
| AuthenticationMiddleware | ⬜ Not Started | P0 | JWT validation |

#### Serialization
| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| SerializationFactory | ⬜ Not Started | P0 | ISerializationFactory |
| MemoryPackSerializer helper | ⬜ Not Started | P0 | |

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
| GET /api/assets | GetAssets | ⬜ Not Started | P0 |
| GET /api/assets/{id} | GetAssetById | ⬜ Not Started | P0 |
| POST /api/assets | CreateAsset | ⬜ Not Started | P0 |
| PUT /api/assets/{id} | UpdateAsset | ⬜ Not Started | P0 |
| DELETE /api/assets/{id} | DeleteAsset | ⬜ Not Started | P0 |
| GET /api/assets/summary | GetAssetSummary | ⬜ Not Started | P1 |

#### Beneficiary Functions
| Endpoint | Function | Status | Priority |
|----------|----------|--------|----------|
| GET /api/beneficiaries | GetBeneficiaries | ⬜ Not Started | P0 |
| GET /api/beneficiaries/{id} | GetBeneficiaryById | ⬜ Not Started | P0 |
| POST /api/beneficiaries | CreateBeneficiary | ⬜ Not Started | P0 |
| PUT /api/beneficiaries/{id} | UpdateBeneficiary | ⬜ Not Started | P0 |
| DELETE /api/beneficiaries/{id} | DeleteBeneficiary | ⬜ Not Started | P0 |
| POST /api/beneficiaries/assign | AssignBeneficiary | ⬜ Not Started | P0 |
| PUT /api/assignments/{id} | UpdateAssignment | ⬜ Not Started | P1 |
| DELETE /api/assignments/{id} | RemoveAssignment | ⬜ Not Started | P1 |
| GET /api/beneficiaries/coverage | GetCoverageSummary | ⬜ Not Started | P1 |

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

#### Auth Functions
| Endpoint | Function | Status | Priority |
|----------|----------|--------|----------|
| GET /api/auth/me | GetCurrentUser | ⬜ Not Started | P0 |
| GET /api/auth/roles | GetUserRoles | ⬜ Not Started | P0 |
| POST /api/auth/clients | AssignClient | ⬜ Not Started | P1 |
| GET /api/auth/clients | GetAssignedClients | ⬜ Not Started | P1 |
| DELETE /api/auth/clients/{id} | RemoveClientAccess | ⬜ Not Started | P1 |

> **Note**: Registration, login, password reset, and MFA are handled by Microsoft Entra External ID. These Auth Functions manage user profile and professional relationships within the application.

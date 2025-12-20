# RAJ Financial Software - Execution Plan
## Development Tracking & Implementation Roadmap

---

## Executive Summary

**Product Name**: RAJ Financial  
**Vision**: Comprehensive financial planning tools platform - the "Plaid for comprehensive financial planning data"  
**Approach**: Tools-first (not advice) to minimize regulatory burden initially

### Phase Strategy
- **Phase 1 (MVP)**: Consumer web app with account aggregation, manual assets, beneficiary management, AI insights
- **Phase 2**: Professional API platform (FDX-compliant) for insurance agents, attorneys, advisors

### Core Differentiator
No existing platform combines: linked accounts + manual assets + beneficiary management + AI planning tools in one place.

---

## Technology Stack Summary

| Layer | Technology |
|-------|------------|
| **Frontend** | Blazor WebAssembly (.NET 9) |
| **UI Components** | Syncfusion Blazor v24+ |
| **State Management** | Fluxor (Redux pattern) |
| **Backend** | Azure Functions (.NET 9 Isolated Worker) |
| **Database** | Azure SQL + EF Core 9 |
| **Caching** | Azure Redis Cache |
| **Secrets** | Azure Key Vault |
| **Hosting** | Azure Static Web Apps |
| **Serialization** | MemoryPack (prod) / JSON (dev) |
| **Account Aggregation** | Plaid |
| **AI** | Claude API (Anthropic) |
| **Identity** | Microsoft Entra External ID |
| **Authentication** | Microsoft.Authentication.WebAssembly.Msal + Azure Functions Token Validation |
| **Authorization** | Role-Based Access Control (RBAC) |
| **Documents** | Azure Blob Storage (user strategy sources) |
| **Vector Search** | Azure AI Search (vector index for retrieval) |

---

## User Strategy Sources (Uploads + URLs)

Allow users to upload documents (e.g., books/notes they own) and add reference URLs so the platform can generate suggestions grounded in those strategies and the user’s current situation.

Scope (MVP):
- Upload file and/or add URL
- Background ingestion (extract text, chunk, embed)
- Retrieval-augmented planning/insights with citations
- Per-user isolation and delete-at-will

Safety & copyright notes:
- Treat uploaded content as private user-provided data.
- When responding, prefer summaries + short excerpts only; do not reproduce large portions of copyrighted text.
- For URLs, respect site terms/robots and store only what’s needed for retrieval.

## Part 0: Identity, Security & Infrastructure

### 0.1 Microsoft Entra External ID Configuration

> **Strategy**: Use **separate tenants** for Development and Production to ensure complete isolation of user data, credentials, and configuration. See `RAJ_FINANCIAL_INTEGRATIONS_API.md` for detailed configuration.

| Environment | Tenant Domain | Tenant ID | Purpose |
|-------------|---------------|-----------|---------|
| Development | `rajfinancialdev.onmicrosoft.com` | `496527a2-41f8-4297-a979-c916e7255a22` | Local dev, CI/CD, testing |
| Production | `rajfinancialprod.onmicrosoft.com` | `cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6` | Live users |

#### Development Tenant Tasks

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| Create Dev Entra External ID Tenant | ✅ Complete | P0 | `rajfinancialdev.onmicrosoft.com` |
| Configure Dev User Flows | ⬜ Not Started | P0 | Sign-up, Sign-in, Password Reset |
| Register Dev SPA Application | ⬜ Not Started | P0 | Redirect: localhost:5001 |
| Register Dev API Application | ⬜ Not Started | P0 | Expose `user_impersonation` scope |
| Create Test Users (all roles) | ⬜ Not Started | P1 | user, advisor, attorney, accountant, admin |
| Store Dev Tenant IDs in Key Vault | ⬜ Not Started | P0 | Dev Key Vault only |

#### Production Tenant Tasks

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| Create Prod Entra External ID Tenant | ⬜ Not Started | P0 | `rajfinancial.onmicrosoft.com` |
| Configure Prod User Flows | ⬜ Not Started | P0 | Sign-up, Sign-in, Password Reset |
| Register Prod SPA Application | ⬜ Not Started | P0 | Redirect: rajfinancial.com |
| Register Prod API Application | ⬜ Not Started | P0 | Expose `user_impersonation` scope |
| Configure Custom Branding | ⬜ Not Started | P1 | Gold theme, RAJ logo |
| Set up MFA Policies | ⬜ Not Started | P0 | Require MFA for all users |
| Configure Session Policies | ⬜ Not Started | P1 | 24-hour token lifetime |
| Social Identity Providers | ⬜ Not Started | P2 | Google, Microsoft, Apple (optional) |
| Store Prod Tenant IDs in Key Vault | ⬜ Not Started | P0 | Prod Key Vault only |

### 0.2 Role Definitions & RBAC

**User Roles:**

| Role | Code | Description | Permissions |
|------|------|-------------|-------------|
| **User** | `user` | Primary consumer account holder | Full access to own data, link accounts, manage beneficiaries |
| **Advisor** | `advisor` | Financial advisor/planner | Read access to assigned clients' data, propose plans |
| **Attorney** | `attorney` | Estate planning attorney | Read access to assigned clients' beneficiaries, estate planning data |
| **Accountant** | `accountant` | CPA/Tax professional | Read access to assigned clients' financial accounts, tax-relevant data |
| **Admin** | `admin` | Platform administrator | Full platform access, user management |

**Implementation Tasks:**

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| Define custom claims in Entra | ⬜ Not Started | P0 | `role`, `permissions` claims |
| Create App Roles in manifest | ✅ Complete | P0 | In register-entra-apps.ps1 script |
| Implement role assignment API | ⬜ Not Started | P1 | Assign professionals to clients |
| Create Authorization Policies | ⬜ Not Started | P0 | [Authorize(Roles = "...")] |
| Build DataAccessGrant entity | ⬜ Not Started | P0 | User-to-user data sharing |
| Consent flow for data sharing | ⬜ Not Started | P1 | User grants access to another user |

### 0.2.1 Data Access & Sharing Model

> **Principle**: Every user can only access their own data by default. Access to another user's data requires an explicit grant from the data owner.

#### Access Types

| Access Type | Code | Description | Use Case |
|-------------|------|-------------|----------|
| **Owner** | `owner` | Full control - read, write, delete, share | The user's own data |
| **Full Access** | `full` | Read and write, but cannot delete or share | Spouse, trusted family member |
| **Read Only** | `read` | View data only, no modifications | Attorney reviewing estate plan |
| **Limited** | `limited` | Specific data categories only | CPA seeing only tax-relevant accounts |

#### Data Categories (for Limited Access)

| Category | Code | Includes |
|----------|------|----------|
| Accounts | `accounts` | Linked bank accounts, balances, transactions |
| Assets | `assets` | Manual assets (property, vehicles, valuables) |
| Liabilities | `liabilities` | Debts, loans, mortgages |
| Beneficiaries | `beneficiaries` | Beneficiary assignments, estate planning |
| Documents | `documents` | Uploaded strategy documents |
| All | `all` | Everything (used with `read` or `full` access) |

#### DataAccessGrant Entity

```csharp
/// <summary>
/// Represents a grant of access from one user (Grantor) to another user (Grantee).
/// </summary>
[MemoryPackable]
public partial class DataAccessGrant
{
    /// <summary>Unique identifier for this grant.</summary>
    public Guid Id { get; set; }
    
    /// <summary>The user who owns the data and is granting access.</summary>
    public Guid GrantorUserId { get; set; }
    
    /// <summary>The user receiving access to the data.</summary>
    public Guid GranteeUserId { get; set; }
    
    /// <summary>The email used to invite the grantee (before they accept).</summary>
    public string? GranteeEmail { get; set; }
    
    /// <summary>Type of access granted (owner, full, read, limited).</summary>
    public AccessType AccessType { get; set; }
    
    /// <summary>Data categories accessible (for limited access).</summary>
    public List<string> Categories { get; set; } = new();
    
    /// <summary>Optional label for the relationship (e.g., "Spouse", "CPA").</summary>
    public string? RelationshipLabel { get; set; }
    
    /// <summary>When the grant was created.</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>When the grantee accepted the invitation (null if pending).</summary>
    public DateTime? AcceptedAt { get; set; }
    
    /// <summary>When the grant expires (null for no expiration).</summary>
    public DateTime? ExpiresAt { get; set; }
    
    /// <summary>When the grant was revoked (null if active).</summary>
    public DateTime? RevokedAt { get; set; }
    
    /// <summary>Status: Pending, Active, Expired, Revoked.</summary>
    public GrantStatus Status { get; set; }
}

public enum AccessType { Owner, Full, Read, Limited }
public enum GrantStatus { Pending, Active, Expired, Revoked }
```

#### Sharing Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│ 1. INVITE: User A grants access to user@email.com                       │
│    - Creates DataAccessGrant with Status = Pending                      │
│    - Sends email invitation with secure link                            │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ 2. ACCEPT: User B clicks link, logs in (or registers)                   │
│    - GranteeUserId is set to User B's ID                                │
│    - Status = Active, AcceptedAt = now                                  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ 3. ACCESS: User B can now see User A's data (per AccessType)            │
│    - API checks DataAccessGrant before returning data                   │
│    - UI shows "Viewing as [User A]" indicator                           │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│ 4. REVOKE: User A can revoke access at any time                         │
│    - Status = Revoked, RevokedAt = now                                  │
│    - User B immediately loses access                                    │
└─────────────────────────────────────────────────────────────────────────┘
```

#### API Endpoints for Sharing

```
POST   /api/access/grants              → CreateAccessGrant (invite someone)
GET    /api/access/grants              → GetMyGrants (who I've shared with)
GET    /api/access/grants/received     → GetReceivedGrants (who shared with me)
POST   /api/access/grants/{id}/accept  → AcceptGrant (accept invitation)
DELETE /api/access/grants/{id}         → RevokeGrant (revoke access)
PATCH  /api/access/grants/{id}         → UpdateGrant (change access level)
```

#### Authorization Check Pattern

```csharp
/// <summary>
/// Checks if the current user can access the specified user's data.
/// </summary>
public async Task<DataAccessResult> CanAccessUserDataAsync(
    Guid currentUserId, 
    Guid targetUserId, 
    string category = "all",
    AccessType minimumAccess = AccessType.Read)
{
    // Owner always has access
    if (currentUserId == targetUserId)
        return DataAccessResult.Allowed(AccessType.Owner);
    
    // Check for active grant
    var grant = await _db.DataAccessGrants
        .Where(g => g.GrantorUserId == targetUserId 
                 && g.GranteeUserId == currentUserId
                 && g.Status == GrantStatus.Active
                 && (g.ExpiresAt == null || g.ExpiresAt > DateTime.UtcNow))
        .FirstOrDefaultAsync();
    
    if (grant == null)
        return DataAccessResult.Denied("No access grant found");
    
    // Check access type
    if (grant.AccessType < minimumAccess)
        return DataAccessResult.Denied("Insufficient access level");
    
    // Check category for limited access
    if (grant.AccessType == AccessType.Limited 
        && category != "all" 
        && !grant.Categories.Contains(category))
        return DataAccessResult.Denied($"No access to {category}");
    
    return DataAccessResult.Allowed(grant.AccessType);
}
```

#### Implementation Tasks

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| Create DataAccessGrant entity | ⬜ Not Started | P0 | EF Core model |
| Create DataAccessGrant migration | ⬜ Not Started | P0 | Database schema |
| Implement IDataAccessService | ⬜ Not Started | P0 | Authorization logic |
| Create sharing API endpoints | ⬜ Not Started | P0 | CRUD for grants |
| Email invitation service | ⬜ Not Started | P1 | SendGrid integration |
| UI: Sharing management page | ⬜ Not Started | P1 | Manage who has access |
| UI: Account switcher | ⬜ Not Started | P1 | Switch between own/shared data |
| Audit logging for access | ⬜ Not Started | P0 | Log all data access |

**Blazor WASM Auth Configuration:**

```csharp
// Program.cs - Entra External ID Configuration
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add(
        "https://rajfinancial.onmicrosoft.com/api/user_impersonation");
    options.ProviderOptions.LoginMode = "redirect";
});

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("RequireUser", policy => 
        policy.RequireRole("user", "advisor", "attorney", "accountant", "admin"));
    options.AddPolicy("RequireAdvisor", policy => 
        policy.RequireRole("advisor", "admin"));
    options.AddPolicy("RequireAttorney", policy => 
        policy.RequireRole("attorney", "admin"));
    options.AddPolicy("RequireAccountant", policy => 
        policy.RequireRole("accountant", "admin"));
    options.AddPolicy("RequireAdmin", policy => 
        policy.RequireRole("admin"));
});
```

**Azure Functions Auth Validation:**

```csharp
// Startup - Token Validation
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        configuration.Bind("AzureAdB2C", options);
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "extension_Role";
    }, options => configuration.Bind("AzureAdB2C", options));
```

### 0.3 Managed Identities

**Supported Services (System-Assigned Managed Identity):**

| Service | Managed Identity Support | Target Resource | Status |
|---------|-------------------------|-----------------|--------|
| Azure Functions | ✅ Yes | Key Vault, Azure SQL, Redis, Storage | ⬜ Configure |
| Azure SQL | ✅ Yes (AAD Auth) | N/A | ⬜ Configure |
| Azure Key Vault | ✅ Yes | N/A | ⬜ Configure |
| Azure Redis Cache | ✅ Yes (AAD Auth) | N/A | ⬜ Configure |
| Azure Static Web Apps | ❌ No* | N/A | Use SWA auth integration |

> **Note**: Azure Static Web Apps does not support Managed Identity for runtime operations. The Blazor WASM app runs in the browser and authenticates users via `Microsoft.Authentication.WebAssembly.Msal` (C# MSAL library) to Entra External ID. Backend Azure Functions use Managed Identity.

**Implementation Tasks:**

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| Enable MI on Azure Functions | ⬜ Not Started | P0 | System-assigned identity |
| Grant Key Vault access (MI) | ⬜ Not Started | P0 | Secrets User role |
| Configure Azure SQL AAD Auth | ⬜ Not Started | P0 | Add MI as db_datareader/writer |
| Configure Redis AAD Auth | ⬜ Not Started | P1 | Data Contributor role |
| Remove connection string secrets | ⬜ Not Started | P0 | Use DefaultAzureCredential |
| Configure Blob Storage access | ⬜ Not Started | P1 | Storage Blob Data Contributor |

**DefaultAzureCredential Pattern:**

```csharp
// Recommended pattern for all Azure SDK usage
var credential = new DefaultAzureCredential();

// Key Vault
var secretClient = new SecretClient(
    new Uri("https://raj-kv.vault.azure.net/"), 
    credential);

// Azure SQL with Managed Identity
"Server=raj-sql.database.windows.net;Database=RajFinancial;Authentication=Active Directory Default;"

// Redis with AAD
var configOptions = await ConfigurationOptions.Parse(redisConnectionString)
    .ConfigureForAzureWithTokenCredentialAsync(credential);
```

### 0.4 Infrastructure Setup

> **Strategy**: Use **Bicep** templates for all Azure resource provisioning. Separate parameter files per environment (dev/prod). Store in `infra/` folder.

#### Resource Naming Convention

| Resource Type | Dev Name | Prod Name |
|---------------|----------|-----------|
| Resource Group | `rg-rajfinancial-dev` | `rg-rajfinancial-prod` |
| Azure SQL Server | `sql-rajfinancial-dev` | `sql-rajfinancial-prod` |
| Azure SQL Database | `sqldb-rajfinancial-dev` | `sqldb-rajfinancial-prod` |
| Azure Functions | `func-rajfinancial-dev` | `func-rajfinancial-prod` |
| Static Web App | `stapp-rajfinancial-dev` | `stapp-rajfinancial-prod` |
| Key Vault | `kv-rajfinancial-dev` | `kv-rajfinancial-prod` |
| Redis Cache | `redis-rajfinancial-dev` | `redis-rajfinancial-prod` |
| Storage Account | `strajfinancialdev` | `strajfinancialprod` |
| App Insights | `appi-rajfinancial-dev` | `appi-rajfinancial-prod` |
| Log Analytics | `log-rajfinancial-dev` | `log-rajfinancial-prod` |

#### Bicep File Structure

```
infra/
├── main.bicep                    # Orchestrator - deploys all modules
├── main.bicepparam               # Shared parameters
├── parameters/
│   ├── dev.bicepparam            # Development environment parameters
│   └── prod.bicepparam           # Production environment parameters
├── modules/
│   ├── keyvault.bicep            # Key Vault + secrets
│   ├── sql.bicep                 # Azure SQL Server + Database
│   ├── functions.bicep           # Azure Functions + App Service Plan
│   ├── staticwebapp.bicep        # Static Web Apps
│   ├── redis.bicep               # Azure Redis Cache
│   ├── storage.bicep             # Blob Storage (documents)
│   ├── monitoring.bicep          # App Insights + Log Analytics
│   └── identity.bicep            # Managed Identities + RBAC assignments
└── scripts/
    ├── deploy-dev.ps1            # Deploy to dev environment
    ├── deploy-prod.ps1           # Deploy to prod environment
    └── teardown.ps1              # Remove all resources
```

#### Infrastructure Tasks

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| Create `infra/` folder structure | ✅ Complete | P0 | Structure created |
| Create `main.bicep` orchestrator | ✅ Complete | P0 | Imports all modules |
| Create `modules/keyvault.bicep` | ✅ Complete | P0 | Standard tier, RBAC access |
| Create `modules/sql.bicep` | ⬜ Not Started | P0 | Create when ready to deploy |
| Create `modules/functions.bicep` | ⬜ Not Started | P0 | Create when ready to deploy |
| Create `modules/staticwebapp.bicep` | ⬜ Not Started | P0 | Create when ready to deploy |
| Create `modules/redis.bicep` | ⬜ Not Started | P1 | Create when ready to deploy |
| Create `modules/storage.bicep` | ✅ Complete | P1 | Standard LRS, Blob containers |
| Create `modules/monitoring.bicep` | ✅ Complete | P0 | App Insights + Log Analytics |
| Create `modules/identity.bicep` | ⬜ Not Started | P0 | Create when ready to deploy |
| Create `parameters/dev.bicepparam` | ✅ Complete | P0 | Tenant ID: 496527a2-... |
| Create `parameters/prod.bicepparam` | ✅ Complete | P0 | Tenant ID: cc4d96fb-... |
| Create deploy scripts (PowerShell) | ⬜ Not Started | P1 | Create when ready to deploy |
| Setup GitHub Actions for IaC | ⬜ Not Started | P1 | Auto-deploy on infra changes |

#### Deployment Commands

```powershell
# Deploy to Development
az deployment sub create `
  --location southcentralus `
  --template-file infra/main.bicep `
  --parameters infra/parameters/dev.bicepparam

# Deploy to Production
az deployment sub create `
  --location southcentralus `
  --template-file infra/main.bicep `
  --parameters infra/parameters/prod.bicepparam

# What-if (preview changes)
az deployment sub what-if `
  --location southcentralus `
  --template-file infra/main.bicep `
  --parameters infra/parameters/dev.bicepparam
```

#### Key Vault Secrets to Provision

| Secret Name | Source | Notes |
|-------------|--------|-------|
| `Plaid--ClientId` | Plaid Dashboard | Per environment |
| `Plaid--Secret` | Plaid Dashboard | Per environment |
| `Claude--ApiKey` | Anthropic Console | Per environment |
| `AzureAd--TenantId` | Entra Portal | Per environment tenant |
| `AzureAd--ClientId` | Entra Portal | SPA app registration |
| `AzureAd--ApiClientId` | Entra Portal | API app registration |
| `ConnectionStrings--Redis` | Bicep output | Auto-provisioned |
| `ConnectionStrings--SqlServer` | Bicep output | AAD auth string |

> **Note**: Connection strings for SQL and Redis use Managed Identity authentication, not secrets. Only external service credentials (Plaid, Claude, Entra) need Key Vault secrets.

### 0.5 CI/CD Pipeline Setup

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| Create `.github/workflows/` folder | ⬜ Not Started | P0 | GitHub Actions |
| Create `ci.yml` (build/test) | ⬜ Not Started | P0 | On PR to develop |
| Create `deploy-dev.yml` | ⬜ Not Started | P0 | On push to develop |
| Create `deploy-prod.yml` | ⬜ Not Started | P0 | On push to main |
| Create `infra-deploy.yml` | ⬜ Not Started | P1 | On changes to `infra/` |
| Configure GitHub Environments | ⬜ Not Started | P0 | dev, prod with approvals |
| Setup OIDC for Azure auth | ⬜ Not Started | P0 | Federated credentials |
| Configure branch protection | ⬜ Not Started | P0 | Require PR reviews |

---

## Part 1: UI Development Tracking

The UI tracking tables are maintained in [RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md](RAJ_FINANCIAL_EXECUTION_PLAN_UI_TRACKING.md).

---

## Part 2: API Development Tracking

The API tracking tables are maintained in [RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md](RAJ_FINANCIAL_EXECUTION_PLAN_API_TRACKING.md).

---

## Part 3: Blazor Client Setup

### 3.1 Project Configuration

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| Create Blazor WASM project | ⬜ Not Started | P0 | .NET 9 Standalone |
| Install Syncfusion packages | ⬜ Not Started | P0 | All required components |
| Install Fluxor | ⬜ Not Started | P0 | State management |
| Install MemoryPack | ⬜ Not Started | P0 | Serialization |
| Configure Program.cs | ⬜ Not Started | P0 | All services |
| Configure MSAL Authentication (C#) | ⬜ Not Started | P0 | AddMsalAuthentication() |
| Set up Authorization policies | ⬜ Not Started | P0 | Role-based access |
| Set up Syncfusion license | ⬜ Not Started | P0 | License key |
| Configure appsettings.json | ⬜ Not Started | P0 | Dev/Prod configs |
| Set up localization | ⬜ Not Started | P1 | Resource files |

### 3.2 Client Services

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| ApiClient.cs | ⬜ Not Started | P0 | Content negotiation |
| PlaidLinkService.cs | ⬜ Not Started | P0 | JS interop |

### 3.3 Fluxor State

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| AppState | ⬜ Not Started | P0 | Global state |
| AuthState | ⬜ Not Started | P0 | User, roles, claims |
| DashboardState | ⬜ Not Started | P0 | Actions, reducers, effects |
| AccountState | ⬜ Not Started | P0 | Actions, reducers, effects |
| AssetState | ⬜ Not Started | P0 | Actions, reducers, effects |
| BeneficiaryState | ⬜ Not Started | P0 | Actions, reducers, effects |
| DebtPayoffState | ⬜ Not Started | P1 | Actions, reducers, effects |
| InsuranceState | ⬜ Not Started | P1 | Actions, reducers, effects |

### 3.4 Static Assets

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| index.html | ⬜ Not Started | P0 | Base HTML |
| raj-theme.css | ⬜ Not Started | P0 | Design tokens |
| logo.svg | ⬜ Not Started | P0 | Brand assets |
| logo_only.svg | ⬜ Not Started | P0 | Icon version |
| logo_horizontal.svg | ⬜ Not Started | P1 | Wide version |
| Nexa-XBold.woff2 | ⬜ Not Started | P1 | Brand font |

---

## Part 4: Infrastructure & DevOps

### 4.1 Azure Resources

| Resource | Status | Priority | Notes |
|----------|--------|----------|-------|
| Resource Group | ⬜ Not Started | P0 | |
| Azure Static Web App | ⬜ Not Started | P0 | Blazor hosting |
| Azure Functions App | ⬜ Not Started | P0 | API hosting |
| Azure SQL Database | ⬜ Not Started | P0 | |
| Azure Key Vault | ⬜ Not Started | P0 | Secrets |
| Azure Redis Cache | ⬜ Not Started | P1 | Caching |
| Application Insights | ⬜ Not Started | P1 | Monitoring |
| Azure Storage Account | ⬜ Not Started | P1 | Blob storage |
| Azure AI Search | ⬜ Not Started | P1 | Vector search for strategy sources |
| Azure Storage Queue | ⬜ Not Started | P1 | Background ingestion pipeline |

### 4.2 CI/CD

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| GitHub Actions - Build | ⬜ Not Started | P1 | .NET build |
| GitHub Actions - Test | ⬜ Not Started | P1 | Unit tests |
| GitHub Actions - Deploy (Dev) | ⬜ Not Started | P1 | Auto-deploy |
| GitHub Actions - Deploy (Prod) | ⬜ Not Started | P2 | Manual approval |
| Bicep templates | ⬜ Not Started | P2 | IaC |

### 4.3 External Services

| Service | Status | Priority | Notes |
|---------|--------|----------|-------|
| Plaid account setup | ⬜ Not Started | P0 | Sandbox first |
| Anthropic API key | ⬜ Not Started | P1 | Claude access |
| Syncfusion license | ⬜ Not Started | P0 | Community or paid |

---

## Part 5: Testing

### 5.1 Unit Tests

| Area | Status | Priority | Coverage Target |
|------|--------|----------|-----------------|
| AccountService tests | ⬜ Not Started | P1 | 90% |
| AssetService tests | ⬜ Not Started | P1 | 90% |
| BeneficiaryService tests | ⬜ Not Started | P1 | 90% |
| AnalysisService tests | ⬜ Not Started | P1 | 90% |
| Validator tests | ⬜ Not Started | P1 | 100% |
| Serialization tests | ⬜ Not Started | P1 | 100% |

### 5.2 Integration Tests

| Area | Status | Priority | Notes |
|------|--------|----------|-------|
| API endpoint tests | ⬜ Not Started | P2 | In-memory DB |
| Plaid integration tests | ⬜ Not Started | P2 | Sandbox |
| Auth flow tests | ⬜ Not Started | P2 | |

### 5.3 Security Tests

| Area | Status | Priority | Notes |
|------|--------|----------|-------|
| Tenant isolation tests | ⬜ Not Started | P1 | Critical |
| Authorization tests | ⬜ Not Started | P1 | |
| Input validation tests | ⬜ Not Started | P1 | SQL injection, XSS |

### 5.4 Accessibility Tests

| Area | Status | Priority | Notes |
|------|--------|----------|-------|
| axe-core integration | ⬜ Not Started | P2 | Automated |
| Keyboard navigation | ⬜ Not Started | P2 | Manual |
| Screen reader testing | ⬜ Not Started | P3 | Manual |

---

## Part 6: Sprint Planning

### Sprint 1: Foundation (Week 1-2)
**Goal**: Project setup and core infrastructure

- [ ] Create solution structure
- [ ] Configure Entra External ID tenant
- [ ] Register SPA and API applications in Entra
- [ ] Set up Azure resources (SQL, Functions, Static Web App)
- [ ] Enable Managed Identity on Azure Functions
- [ ] Configure Key Vault access via Managed Identity
- [ ] Configure Azure SQL AAD authentication
- [ ] Implement shared DTOs with MemoryPack
- [ ] Create domain entities (including ClientRelationship)
- [ ] Set up EF Core 9 with migrations
- [ ] Configure Blazor WASM project with .NET 9
- [ ] Implement CSS design tokens

### Sprint 2: Authentication & Layout (Week 3-4)
**Goal**: Entra External ID integration and app shell

- [ ] Configure MSAL Authentication (AddMsalAuthentication) in Blazor WASM
- [ ] Set up Authorization policies (user, advisor, attorney, accountant, admin)
- [ ] Create role definitions in Entra manifest
- [ ] Implement JWT validation middleware in Azure Functions
- [ ] Create AuthRedirect and AccessDenied pages
- [ ] Implement MainLayout with auth state
- [ ] Create DesktopSidebar
- [ ] Create MobileBottomNav
- [ ] Set up Fluxor state management
- [ ] Implement ApiClient with content negotiation

### Sprint 3: Account Linking (Week 5-6)
**Goal**: Plaid integration

- [ ] Implement PlaidService
- [ ] Create account endpoints (link-token, exchange, get)
- [ ] Build PlaidLinkModal component
- [ ] Create Accounts page
- [ ] Implement AccountCard component
- [ ] Handle webhooks

### Sprint 4: Assets Management (Week 7-8)
**Goal**: Manual asset CRUD

- [ ] Implement AssetService
- [ ] Create asset endpoints (CRUD)
- [ ] Build Assets page with grid/cards
- [ ] Create AssetForm component
- [ ] Implement filter tabs
- [ ] Add delete confirmation

### Sprint 5: Beneficiaries (Week 9-10)
**Goal**: Beneficiary management

- [ ] Implement BeneficiaryService
- [ ] Create beneficiary endpoints
- [ ] Build Beneficiaries page
- [ ] Create assignment dialog
- [ ] Implement coverage warnings
- [ ] Add allocation validation

### Sprint 6: Dashboard (Week 11-12)
**Goal**: Main dashboard with insights

- [ ] Implement CalculateNetWorth
- [ ] Create Dashboard page
- [ ] Build NetWorthHero component
- [ ] Create QuickStatCards
- [ ] Implement HealthScoreCard
- [ ] Add recent activity

### Sprint 7: AI Insights (Week 13-14)
**Goal**: Claude AI integration

- [ ] Implement ClaudeAIService
- [ ] Create insights endpoint
- [ ] Build InsightsPanel
- [ ] Create InsightCard component
- [ ] Set up caching
- [ ] Add fallback insights

**User Strategy Sources (Uploads + URLs)**

- [ ] Create StrategySources endpoints (upload + add URL + list + delete)
- [ ] Store raw content in Blob Storage (private, per-user)
- [ ] Background ingestion: extract text, chunk, embed
- [ ] Create Azure AI Search vector index and push embeddings
- [ ] Retrieval-augmented suggestions with citations

### Sprint 8: Planning Tools (Week 15-16)
**Goal**: Debt payoff and insurance calculators

- [ ] Implement debt payoff analysis
- [ ] Create DebtPayoff page
- [ ] Build strategy comparison
- [ ] Implement insurance analysis
- [ ] Create InsuranceCalculator page
- [ ] Build CoverageGauge

### Sprint 9: Polish & Testing (Week 17-18)
**Goal**: Quality and performance

- [ ] Write unit tests (90% coverage)
- [ ] Integration testing
- [ ] Security testing
- [ ] Accessibility audit
- [ ] Performance optimization
- [ ] Bug fixes

### Sprint 10: Launch Prep (Week 19-20)
**Goal**: Production readiness

- [ ] Production environment setup
- [ ] CI/CD pipelines
- [ ] Monitoring and alerting
- [ ] Documentation
- [ ] User acceptance testing
- [ ] Launch checklist

---

## Status Legend

| Symbol | Meaning |
|--------|---------|
| ⬜ | Not Started |
| 🟡 | In Progress |
| ✅ | Complete |
| ❌ | Blocked |
| 🔄 | Needs Review |

## Priority Legend

| Priority | Meaning |
|----------|---------|
| P0 | Must have for MVP |
| P1 | Should have for MVP |
| P2 | Nice to have |
| P3 | Future enhancement |

---

## Notes & Decisions

### Architecture Decisions
1. **.NET 9** - Latest framework with performance improvements
2. **Blazor WASM over React** - Maximize C# expertise, code sharing
3. **MemoryPack over JSON** - 7-8x faster serialization in production
4. **Fluxor over simple state** - Predictable state management at scale
5. **Azure Functions Isolated** - Better performance, future-proof
6. **Syncfusion over custom** - Enterprise-ready components, reduce dev time
7. **Entra External ID over custom auth** - Enterprise-grade security, MFA, passwordless
8. **Managed Identities** - Eliminate secrets for Azure service-to-service auth

### Security Considerations
1. **Entra External ID** for all user authentication
2. **Role-based access control** (user, advisor, attorney, accountant, admin)
3. **Managed Identity** on Azure Functions for Key Vault, SQL, Redis, Storage
4. All Plaid access tokens encrypted at rest in Key Vault
5. Tenant isolation enforced at database level with user claims
6. PII sanitized before AI processing
7. Audit logging for all mutations
8. Rate limiting on all endpoints
9. **Client consent flow** for professionals accessing user data
10. **User strategy sources**: private by default, delete-at-will; avoid reproducing large copyrighted excerpts

### Compliance Notes
1. "Tools not advice" framing for all AI outputs
2. Disclaimers required on all analysis pages
3. No personalized recommendations
4. Professional consultation encouraged

---

*Last Updated: December 17, 2025*

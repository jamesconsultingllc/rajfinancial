# 01 — Platform Infrastructure

> Tech stack, Azure resources, project structure, middleware, error handling, CI/CD, environments.

**ADO Tracking:** [Epic #265 — 01 - Platform Infrastructure](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/265)

| # | Feature | State |
|---|---------|-------|
| [268](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/268) | Managed Identity & Azure Service Auth | In Progress |
| [434](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/434) | Performance Optimization | New |
| [436](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/436) | Accessibility & UX Polish | New |
| [453](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/453) | Production Infrastructure | New |
| [454](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/454) | Monitoring & Observability | New |
| [455](https://dev.azure.com/jamesconsulting/c21b4869-5c21-461b-9a0b-ab984e08a088/_workitems/edit/455) | User Documentation | New |

---

## Technology Stack

### Runtime & Frameworks

| Component | Technology | Version |
|-----------|-----------|---------|
| **API Runtime** | .NET (Isolated Worker) | net10.0 |
| **Client Runtime** | React + TypeScript | React 18, TS 5.x |
| **Build Tool** | Vite | 5.4.x |
| **CSS** | Tailwind CSS | 3.x |
| **UI Components** | shadcn/ui + Radix | Latest |
| **State/Data** | TanStack Query | v5 |
| **Auth (Client)** | MSAL React | @azure/msal-react |
| **Auth (API)** | Microsoft.Identity.Web | Latest |
| **Database** | Azure SQL + EF Core | EF Core 10.x |
| **Caching** | Azure Redis Cache | AAD Auth |
| **Secrets** | Azure Key Vault | Managed Identity |
| **Hosting (Client)** | Azure Static Web Apps | Free tier |
| **Hosting (API)** | Azure Functions (Standalone) | Consumption plan |
| **Serialization** | System.Text.Json + MemoryPack | Built-in + source-generated |
| **AI** | Claude API (Anthropic) | claude-sonnet-4-5-20250929 |
| **Account Linking** | Plaid | Premium tier only |
| **IaC** | Bicep | Latest |
| **Observability** | Application Insights | WorkerService SDK |

> **Architecture**: Standalone Azure Functions (not SWA-linked) for Managed Identity support.  
> **Serialization**: System.Text.Json only. No MemoryPack.  
> **UI Framework**: React + Vite + Tailwind. No Blazor, Syncfusion, or Fluxor.

### Key NuGet Packages (API)

| Package | Purpose |
|---------|---------|
| `Microsoft.Azure.Functions.Worker` | Isolated worker model |
| `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` | HTTP triggers |
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server provider |
| `FluentValidation` | Request validation |
| `Azure.Identity` | Azure AD / Managed Identity |
| `Azure.Security.KeyVault.Secrets` | Key Vault access |
| `Going.Plaid` | Plaid account aggregation |
| `Anthropic.SDK` | Claude AI integration |
| `Microsoft.ApplicationInsights.WorkerService` | Telemetry |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | Redis caching |

### Key npm Packages (Client)

| Package | Purpose |
|---------|---------|
| `react`, `react-dom` | UI framework |
| `@azure/msal-browser`, `@azure/msal-react` | Entra auth |
| `@tanstack/react-query` | Server state management |
| `tailwindcss` | Utility-first CSS |
| `react-i18next`, `i18next` | Localization |
| `react-router-dom` | Client-side routing |
| `lucide-react` | Icons |
| `recharts` | Charts and data visualization |

---

## Solution Structure

> **Architecture**: Simplified 2-layer structure (`Api` + `Shared`) — no separate Core or Infrastructure projects.

```
src/
├── Api/                        # Azure Functions API (net10.0)
│   ├── Configuration/          # App configuration helpers
│   ├── Data/                   # EF Core DbContext, migrations, interceptors
│   │   └── Migrations/         # Database migrations
│   ├── Functions/              # HTTP trigger functions (one folder per feature)
│   │   └── Assets/             # Asset CRUD endpoints
│   ├── Middleware/              # Auth, validation, error handling
│   │   ├── Authorization/      # RequireAuthentication, RequireRole attributes
│   │   ├── Content/            # Content negotiation & serialization
│   │   └── Exception/          # Custom exception types
│   ├── Services/               # Business logic (interface + impl per feature)
│   │   ├── AssetService/
│   │   ├── Authorization/
│   │   ├── ClientManagement/
│   │   └── UserProfile/
│   ├── Validators/             # FluentValidation validators
│   └── Program.cs              # Host configuration
├── Shared/                     # Shared entities, DTOs, contracts (multi-target net9.0;net10.0)
│   ├── Entities/               # EF Core entities & domain enums
│   └── Contracts/              # Request/response DTOs & error codes (per feature)
│       ├── Assets/
│       └── Auth/
├── Client/                     # React + Vite + TypeScript
│   ├── src/
│   │   ├── assets/             # Static assets (images, fonts)
│   │   ├── auth/               # MSAL auth config, AuthProvider, ProtectedRoute
│   │   ├── components/         # Reusable UI components
│   │   │   ├── dashboard/      # Dashboard-specific components
│   │   │   └── ui/             # shadcn/ui primitives
│   │   ├── data/               # Mock data (temporary, pre-API integration)
│   │   ├── generated/          # Auto-generated code (MemoryPack readers/writers)
│   │   ├── hooks/              # Custom React hooks
│   │   ├── lib/                # Utilities
│   │   ├── pages/              # Route-level page components
│   │   ├── services/           # API client services
│   │   ├── test/               # Test setup & utilities
│   │   └── types/              # TypeScript interfaces
│   └── public/                 # Static assets & staticwebapp.config.json
tests/
├── Api.Tests/                  # API unit tests (xUnit, net10.0)
│   ├── Configuration/
│   ├── Contracts/
│   ├── Functions/
│   ├── Middleware/
│   ├── Services/
│   └── Validators/
├── IntegrationTests/           # BDD integration tests (Reqnroll, net10.0)
│   ├── Features/               # Gherkin feature files
│   ├── StepDefinitions/
│   └── Support/
└── AcceptanceTests/            # E2E acceptance tests (Reqnroll + Playwright)
    ├── Features/               # Gherkin feature files
    ├── StepDefinitions/
    ├── Pages/                  # Page object models
    ├── Helpers/
    ├── Hooks/
    └── Accessibility/
docs/
├── features/                   # Feature documentation (this folder)
├── ASSET_TYPE_SPECIFICATIONS.md # Master asset type reference
├── TRANSACTION_STORAGE_SPECIFICATION.md # Master transaction reference
└── archive/                    # Archived specs (integrated into feature docs)
infra/
├── main.bicep                  # Orchestrator
├── parameters/
│   ├── dev.bicepparam          # Dev environment
│   └── prod.bicepparam         # Prod environment
└── modules/                    # Individual resource modules
scripts/
└── infra/                      # Infrastructure automation scripts (PowerShell)
```

---

## Tier Model

RAJ Financial uses a **freemium** model with two tiers:

| Limit | Free | Premium |
|-------|------|---------|
| Assets tracked | 10 | Unlimited |
| Document uploads | 5/month | Unlimited |
| Accounts (manual) | 3 | Unlimited |
| Accounts (Plaid live-linked) | — | ✅ Unlimited |
| AI insights | BYOK, 3/month | Platform key, Unlimited |
| Statement parsing | BYOK, 5/month | Platform key, Unlimited |
| Storage | 100 MB | 5 GB |
| Contacts | 5 | Unlimited |
| Data sharing | — | ✅ |
| Historical snapshots | Last 3 months | Full history |

> **BYOK** = Bring Your Own Key. Free-tier users supply their own Claude API key for AI features. Premium users use the platform's key. See [09-ai-insights.md](09-ai-insights.md) for architecture.

---

## Error Handling

### Exception Middleware

All API functions use centralized exception handling via `ExceptionMiddleware`:

```csharp
// RAJFinancial.Api/Middleware/ExceptionMiddleware.cs
namespace RAJFinancial.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions and returns structured ApiError responses.
/// Logs detailed errors internally while returning safe messages externally.
/// </summary>
public class ExceptionMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed for {Function}", context.FunctionDefinition.Name);
            await WriteErrorResponse(context, 400, ErrorCodes.VALIDATION_FAILED, ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteErrorResponse(context, 404, ErrorCodes.RESOURCE_NOT_FOUND, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access in {Function}", context.FunctionDefinition.Name);
            await WriteErrorResponse(context, 403, ErrorCodes.AUTH_FORBIDDEN, "Insufficient permissions");
        }
        catch (ConflictException ex)
        {
            await WriteErrorResponse(context, 409, ErrorCodes.RESOURCE_CONFLICT, ex.Message);
        }
        catch (RateLimitException ex)
        {
            _logger.LogWarning("Rate limit exceeded for user in {Function}", context.FunctionDefinition.Name);
            await WriteErrorResponse(context, 429, ErrorCodes.RATE_LIMITED, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in {Function}", context.FunctionDefinition.Name);
            await WriteErrorResponse(context, 500, ErrorCodes.SERVER_ERROR, "An unexpected error occurred");
        }
    }
}
```

### Custom Exception Types

```csharp
namespace RAJFinancial.Core.Exceptions;

public class NotFoundException : Exception
{
    public string ResourceType { get; }
    public object ResourceId { get; }

    public NotFoundException(string resourceType, object resourceId)
        : base($"{resourceType} with ID '{resourceId}' was not found.")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

public class RateLimitException : Exception
{
    public RateLimitException(string message) : base(message) { }
}
```

### Error Codes

```csharp
namespace RAJFinancial.Core.Constants;

/// <summary>
/// Machine-readable error codes for API responses.
/// Frontend maps these to localized messages.
/// </summary>
public static class ErrorCodes
{
    // Auth
    public const string AUTH_REQUIRED = "AUTH_REQUIRED";
    public const string AUTH_FORBIDDEN = "AUTH_FORBIDDEN";
    public const string AUTH_TOKEN_EXPIRED = "AUTH_TOKEN_EXPIRED";
    public const string AUTH_INVALID_TOKEN = "AUTH_INVALID_TOKEN";

    // Validation
    public const string VALIDATION_FAILED = "VALIDATION_FAILED";
    public const string VALIDATION_REQUIRED_FIELD = "VALIDATION_REQUIRED_FIELD";
    public const string VALIDATION_INVALID_FORMAT = "VALIDATION_INVALID_FORMAT";

    // Resources
    public const string RESOURCE_NOT_FOUND = "RESOURCE_NOT_FOUND";
    public const string RESOURCE_CONFLICT = "RESOURCE_CONFLICT";
    public const string RESOURCE_DELETED = "RESOURCE_DELETED";

    // Rate limiting
    public const string RATE_LIMITED = "RATE_LIMITED";

    // Plaid
    public const string PLAID_LINK_FAILED = "PLAID_LINK_FAILED";
    public const string PLAID_SYNC_FAILED = "PLAID_SYNC_FAILED";
    public const string PLAID_ITEM_ERROR = "PLAID_ITEM_ERROR";
    public const string PLAID_REAUTH_REQUIRED = "PLAID_REAUTH_REQUIRED";

    // AI
    public const string AI_SERVICE_UNAVAILABLE = "AI_SERVICE_UNAVAILABLE";
    public const string AI_RATE_LIMITED = "AI_RATE_LIMITED";
    public const string AI_INVALID_KEY = "AI_INVALID_KEY";

    // Tier
    public const string TIER_LIMIT_REACHED = "TIER_LIMIT_REACHED";
    public const string TIER_UPGRADE_REQUIRED = "TIER_UPGRADE_REQUIRED";

    // Server
    public const string SERVER_ERROR = "SERVER_ERROR";
}
```

### API Error Response

```csharp
namespace RAJFinancial.Core.Models;

/// <summary>
/// Structured error response returned by all API endpoints.
/// </summary>
public class ApiErrorResponse
{
    public required string Code { get; set; }
    public required string Message { get; set; }
    public object? Details { get; set; }
    public string? TraceId { get; set; }
}
```

### HTTP Status Code Usage

| Code | Usage |
|------|-------|
| `200` | Successful GET, PUT, PATCH |
| `201` | Successful POST creating a resource |
| `204` | Successful DELETE |
| `400` | Validation errors, malformed request |
| `401` | Missing/invalid authentication |
| `403` | Authenticated but insufficient permissions |
| `404` | Resource not found |
| `409` | Conflict (duplicate, concurrent modification) |
| `422` | Business logic validation failure |
| `429` | Rate limited / tier limit exceeded |
| `500` | Unexpected server error |

---

## Azure Resources

### Resource Naming Convention

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

### Managed Identity

All Azure-to-Azure authentication uses **System-Assigned Managed Identity** with `DefaultAzureCredential`:

```csharp
var credential = new DefaultAzureCredential();

// Key Vault
var secretClient = new SecretClient(
    new Uri("https://kv-rajfinancial-dev.vault.azure.net/"),
    credential);

// Azure SQL (connection string)
"Server=sql-rajfinancial-dev.database.windows.net;Database=sqldb-rajfinancial-dev;Authentication=Active Directory Default;"

// Redis with AAD
var configOptions = await ConfigurationOptions.Parse(redisConnectionString)
    .ConfigureForAzureWithTokenCredentialAsync(credential);
```

| Service | Target Resource | Role Assignment |
|---------|-----------------|-----------------|
| Azure Functions | Key Vault | Key Vault Secrets User |
| Azure Functions | Azure SQL | db_datareader, db_datawriter |
| Azure Functions | Redis | Redis Data Contributor |
| Azure Functions | Blob Storage | Storage Blob Data Contributor |

### Key Vault Secrets

| Secret Name | Source | Notes |
|-------------|--------|-------|
| `Plaid--ClientId` | Plaid Dashboard | Per environment |
| `Plaid--Secret` | Plaid Dashboard | Per environment |
| `Claude--ApiKey` | Anthropic Console | Platform key for premium users |
| `AzureAd--TenantId` | Entra Portal | Per environment tenant |
| `AzureAd--ClientId` | Entra Portal | SPA app registration |
| `AzureAd--ApiClientId` | Entra Portal | API app registration |

> SQL and Redis use Managed Identity authentication — no connection string secrets needed.

### Bicep Infrastructure

```
infra/
├── main.bicep                    # Orchestrator
├── parameters/
│   ├── dev.bicepparam            # Dev: tenant 496527a2-...
│   └── prod.bicepparam           # Prod: tenant cc4d96fb-...
└── modules/
    ├── keyvault.bicep            # Key Vault + secrets
    ├── sql.bicep                 # Azure SQL Server + Database
    ├── functions.bicep           # Azure Functions + App Service Plan
    ├── staticwebapp.bicep        # Static Web Apps
    ├── redis.bicep               # Azure Redis Cache
    ├── storage.bicep             # Blob Storage (documents, strategy sources)
    ├── monitoring.bicep          # App Insights + Log Analytics
    └── identity.bicep            # Managed Identities + RBAC assignments
```

Deploy commands:

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
```

---

## CI/CD Pipeline

GitHub Actions workflows:

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `azure-swa.yml` | Push to `develop`, PR | Build + deploy SWA + preview environments |
| `infra-deploy.yml` | Changes to `infra/` | Deploy Bicep templates |

Features:
- Preview environments auto-created on PR, auto-cleaned on merge
- OIDC federated credentials for Azure auth (no stored secrets)
- Entra redirect URI management for preview URLs
- `staticwebapp.config.json` settings sync

---

## Configuration Files

### API — `local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AZURE_FUNCTIONS_ENVIRONMENT": "Development",
    "SqlConnection": "Server=localhost;Database=RAJFinancial;Trusted_Connection=True;TrustServerCertificate=True;",
    "Plaid:ClientId": "your_client_id",
    "Plaid:Secret": "your_secret",
    "Plaid:Environment": "sandbox",
    "Claude:ApiKey": "your_api_key",
    "Claude:Model": "claude-sonnet-4-5-20250929"
  },
  "Host": {
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

### API — `host.json`

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      },
      "enableLiveMetricsFilters": true
    },
    "logLevel": {
      "default": "Information",
      "Host.Results": "Error",
      "Function": "Information",
      "Host.Aggregator": "Trace"
    }
  },
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  }
}
```

---

## Observability

### Structured Logging

```csharp
// Named parameters — never string interpolation
_logger.LogInformation("Account {AccountId} created for user {UserId}", accountId, userId);
_logger.LogWarning("Authorization denied: {UserId} attempted {Action} on {Resource}", userId, action, resourceId);
```

### Application Insights

- All API functions emit structured telemetry
- Custom metrics for business events (account created, asset added)
- Exception tracking with correlation IDs
- Dependency tracking (SQL, Redis, Plaid, Claude)

---

## Brand Identity

| Element | Value |
|---------|-------|
| **Name** | RAJ Financial Software |
| **Logo** | RF monogram with wing motif (gold gradient) |
| **Display Font** | Nexa XBold |
| **Body Font** | Inter |
| **Primary Color** | Spanish Yellow `#ebbb10` |
| **Hover** | Rich Gold `#d4a80e` |
| **Accent** | UC Gold `#c3922e` |
| **Dark Accent** | Deep Gold `#a67c26` |

---

## Security Headers

```json
{
  "globalHeaders": {
    "Content-Security-Policy": "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self' https://*.azure.com; frame-ancestors 'none'",
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "Strict-Transport-Security": "max-age=31536000; includeSubDomains",
    "Referrer-Policy": "strict-origin-when-cross-origin",
    "Permissions-Policy": "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()"
  }
}
```

---

## Cross-References

- Auth setup: [02-identity-authentication.md](02-identity-authentication.md)
- Role-based access: [03-authorization-data-access.md](03-authorization-data-access.md)
- Contacts & beneficiaries: [04-contacts-beneficiaries.md](04-contacts-beneficiaries.md)
- Assets & portfolio: [05-assets-portfolio.md](05-assets-portfolio.md)
- AI/BYOK architecture: [09-ai-insights.md](09-ai-insights.md)
- Tier management UI: [10-user-profile-settings.md](10-user-profile-settings.md)
- Asset type master reference: [../ASSET_TYPE_SPECIFICATIONS.md](../ASSET_TYPE_SPECIFICATIONS.md)
- Transaction storage reference: [../TRANSACTION_STORAGE_SPECIFICATION.md](../TRANSACTION_STORAGE_SPECIFICATION.md)
- Contact model (archived — integrated into 04): [../archive/CONTACT_MODEL_SPECIFICATION.md](../archive/CONTACT_MODEL_SPECIFICATION.md)

---

*Last Updated: February 2026*

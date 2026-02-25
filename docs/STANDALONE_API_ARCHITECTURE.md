# Standalone API Architecture Decision
## Moving from SWA Linked Functions to Standalone Azure Functions

---

## Decision Summary

**Date**: December 23, 2024
**Status**: ✅ Approved
**Decision**: Use **Standalone Azure Functions** instead of SWA (Static Web Apps) linked API

---

## The Problem

Azure Static Web Apps with linked Azure Functions has a critical limitation:

> **Static Web Apps linked APIs do NOT support Managed Identity.**

This means we **cannot** use Managed Identity to:
- Access Azure Key Vault (would need connection strings)
- Connect to Azure SQL (would need SQL auth credentials)
- Access Azure Redis (would need connection string)
- Access Azure Blob Storage (would need account keys)
- Call Microsoft Graph API (would need client secrets)

**All secrets would need to be stored as environment variables or in Key Vault with access keys**, which defeats the purpose of using Managed Identity for security.

---

## The Solution

Deploy **Standalone Azure Functions** separately from the Static Web App.

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                   Azure Static Web Apps                          │
│                 (Blazor WebAssembly Frontend)                    │
│                                                                   │
│  • Hosts compiled Blazor WASM (.dll files as static assets)      │
│  • Serves index.html, CSS, JS, images                            │
│  • NO backend functions (API moved to standalone)                │
└─────────────────────────────────────────────────────────────────┘
                                │
                                │ HTTPS (CORS configured)
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│             Standalone Azure Functions App                       │
│              (Hosted on Consumption or Premium Plan)             │
│                                                                   │
│  ✅ System-Assigned Managed Identity ENABLED                     │
│                                                                   │
│  Functions:                                                       │
│  • AssignRoleDuringSignup (Entra API Connector)                  │
│  • GetAccounts, CreateAsset, etc. (API endpoints)                │
│  • PlaidWebhook (external webhook handler)                       │
└─────────────────────────────────────────────────────────────────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        │                       │                       │
        ▼                       ▼                       ▼
┌───────────────┐     ┌─────────────────┐     ┌─────────────────┐
│ Key Vault     │     │  Azure SQL      │     │  Azure Redis    │
│               │     │                 │     │                 │
│ ✅ MI Access  │     │ ✅ AAD Auth     │     │ ✅ AAD Auth     │
└───────────────┘     └─────────────────┘     └─────────────────┘
        │                       │                       │
        └───────────────────────┼───────────────────────┘
                                │
                                ▼
                    ✅ NO SECRETS IN CODE!
```

---

## Benefits of Standalone API

| Benefit | Description |
|---------|-------------|
| ✅ **Managed Identity Support** | System-assigned MI can access Key Vault, SQL, Redis, Storage without secrets |
| ✅ **Better Security** | No connection strings or secrets in environment variables |
| ✅ **Compliance** | Meets security requirements for financial applications |
| ✅ **Separate Scaling** | API can scale independently from static frontend |
| ✅ **Better Monitoring** | Separate App Insights for frontend vs backend |
| ✅ **Flexible Deployment** | Can deploy API updates without redeploying frontend |
| ✅ **Custom Domain** | Easier to configure custom domain for API |
| ✅ **APIM Integration** | Can place Azure API Management in front of Functions |

---

## Tradeoffs

| Aspect | SWA Linked API | Standalone Functions |
|--------|----------------|---------------------|
| **Cost** | Included with SWA | ~$13/month (Consumption) |
| **Setup Complexity** | Simpler (automatic linking) | Requires CORS configuration |
| **Managed Identity** | ❌ Not supported | ✅ Fully supported |
| **Secrets Management** | Env vars or Key Vault with keys | ✅ MI with Key Vault |
| **Deployment** | Single deployment | Two separate deployments |
| **Scaling** | Tied to SWA | Independent scaling |
| **Monitoring** | Single App Insights | Separate App Insights |

**Verdict**: The security and compliance benefits outweigh the small cost increase and complexity.

---

## Configuration Changes Required

### 1. CORS Configuration

The standalone Functions app must allow requests from the Static Web App:

**Function App `host.json`:**
```json
{
  "version": "2.0",
  "extensions": {
    "http": {
      "customHeaders": {
        "Access-Control-Allow-Origin": "https://your-swa-domain.azurestaticapps.net",
        "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, OPTIONS",
        "Access-Control-Allow-Headers": "Content-Type, Authorization",
        "Access-Control-Allow-Credentials": "true"
      }
    }
  }
}
```

**Or use Azure Portal:**
1. Go to Function App → CORS
2. Add allowed origins:
   - Dev: `https://your-dev-swa.azurestaticapps.net`
   - Prod: `https://app.rajfinancial.net`

### 2. API Base URL in Blazor Client

**appsettings.json:**
```json
{
  "ApiBaseUrl": "https://func-rajfinancial-dev.azurewebsites.net"
}
```

**appsettings.Production.json:**
```json
{
  "ApiBaseUrl": "https://func-rajfinancial-prod.azurewebsites.net"
}
```

**Program.cs:**
```csharp
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!)
});
```

### 3. Managed Identity Setup

**Enable System-Assigned Managed Identity:**

```bash
# Enable MI on Function App
az functionapp identity assign \
  --name func-rajfinancial-dev \
  --resource-group rg-rajfinancial-dev

# Get the Managed Identity Object ID
az functionapp identity show \
  --name func-rajfinancial-dev \
  --resource-group rg-rajfinancial-dev \
  --query principalId -o tsv
```

**Grant Key Vault Access:**
```bash
# Grant Secrets User role to MI
az keyvault set-policy \
  --name kv-rajfinancial-dev \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

**Grant SQL Database Access:**
```sql
-- Connect to Azure SQL as admin
CREATE USER [func-rajfinancial-dev] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [func-rajfinancial-dev];
ALTER ROLE db_datawriter ADD MEMBER [func-rajfinancial-dev];
```

**Connection String (using AAD auth):**
```
Server=sql-rajfinancial-dev.database.windows.net;
Database=RajFinancial;
Authentication=Active Directory Default;
```

### 4. Remove SWA API Folder

Since we're not using linked functions:

```bash
# Remove api folder from SWA repo (if it exists)
rm -rf api/

# Update staticwebapp.config.json
# Remove "api" configuration section
```

---

## Deployment Strategy

### Development Environment

**Function App:**
```bash
# Deploy from local machine
func azure functionapp publish func-rajfinancial-dev
```

**Static Web App:**
```bash
# Deployed via GitHub Actions (already configured)
git push origin develop
```

### Production Environment

**GitHub Actions Workflow:**

```yaml
# .github/workflows/deploy-api.yml
name: Deploy API

on:
  push:
    branches: [main]
    paths:
      - 'src/Api/**'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'

      - name: Build
        run: dotnet build src/Api/RajFinancial.Api.csproj --configuration Release

      - name: Publish
        run: dotnet publish src/Api/RajFinancial.Api.csproj -c Release -o ./publish

      - name: Deploy to Azure Functions
        uses: Azure/functions-action@v1
        with:
          app-name: func-rajfinancial-prod
          package: ./publish
```

---

## Migration Checklist

### Phase 1: Setup (Current)
- [x] ✅ Create standalone Azure Functions App in Azure Portal
- [x] ✅ Enable System-Assigned Managed Identity
- [ ] ⬜ Configure CORS for SWA domain
- [ ] ⬜ Grant Key Vault access to MI
- [ ] ⬜ Grant Azure SQL access to MI
- [ ] ⬜ Update connection strings to use AAD auth

### Phase 2: Code Changes
- [ ] ⬜ Update Blazor Client API base URL
- [ ] ⬜ Remove SWA linked API folder
- [ ] ⬜ Update GitHub Actions for separate API deployment
- [ ] ⬜ Test CORS configuration

### Phase 3: Testing
- [ ] ⬜ Test API calls from local Blazor to deployed Functions
- [ ] ⬜ Verify Managed Identity access to Key Vault
- [ ] ⬜ Verify AAD auth to Azure SQL
- [ ] ⬜ Test end-to-end authentication flow

### Phase 4: Production
- [ ] ⬜ Deploy standalone Functions to production
- [ ] ⬜ Update production SWA API base URL
- [ ] ⬜ Configure production CORS
- [ ] ⬜ Monitor logs and errors

---

## Cost Analysis

### Before (SWA Linked API)
| Service | Monthly Cost |
|---------|--------------|
| Static Web Apps (Standard) | $9 |
| **Total** | **$9** |

### After (Standalone Functions)
| Service | Monthly Cost |
|---------|--------------|
| Static Web Apps (Free tier) | $0 |
| Azure Functions (Consumption) | ~$13 |
| **Total** | **~$13** |

**Cost Increase**: $4/month
**Security Value**: Priceless ✅

---

## Security Compliance Benefits

### Before: ❌ Secrets Everywhere
```bash
# Environment variables with secrets (bad!)
AZURE_SQL_CONNECTION_STRING="Server=...;User=sa;Password=P@ssw0rd"
PLAID_SECRET="abc123secret"
REDIS_CONNECTION_STRING="redis.cache.windows.net,password=secret123"
```

### After: ✅ Zero Secrets
```csharp
// Use DefaultAzureCredential for everything
var credential = new DefaultAzureCredential();

// Key Vault - no secret needed
var keyVaultClient = new SecretClient(new Uri(keyVaultUrl), credential);

// Azure SQL - AAD auth, no password
var connectionString = "Server=...;Authentication=Active Directory Default;";

// Redis - AAD auth
var redisConfig = await ConfigurationOptions.Parse(redisEndpoint)
    .ConfigureForAzureWithTokenCredentialAsync(credential);

// Microsoft Graph - MI auth
var graphClient = new GraphServiceClient(credential);
```

**Result**: ✅ OWASP Top 10 A07 (Authentication Failures) mitigated

---

## References

- [Managed Identity with Azure Functions](https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity)
- [Azure SQL with Entra ID Authentication](https://learn.microsoft.com/en-us/azure/azure-sql/database/authentication-aad-overview)
- [Azure Redis with Entra ID](https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/cache-azure-active-directory-for-authentication)
- [SWA API Limitations](https://learn.microsoft.com/en-us/azure/static-web-apps/apis-functions)

---

*Last Updated: December 24, 2024*

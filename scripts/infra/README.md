# RAJ Financial - Infrastructure Setup Guide

This guide explains how to deploy the complete RAJ Financial environment including Azure infrastructure and Entra External ID configuration.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         RAJ Financial Platform                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────┐         ┌─────────────────┐                        │
│  │  Blazor WASM    │  HTTPS  │  Azure          │                        │
│  │  Static Web App │◄───────►│  Functions API  │                        │
│  └────────┬────────┘         └────────┬────────┘                        │
│           │                           │                                 │
│           │ OIDC                      │ Managed Identity                │
│           ▼                           ▼                                 │
│  ┌─────────────────┐         ┌─────────────────┐                        │
│  │  Entra          │         │  Azure SQL      │                        │
│  │  External ID    │         │  (Free Tier)    │                        │
│  └─────────────────┘         └─────────────────┘                        │
│                                       │                                 │
│                              ┌────────┴────────┐                        │
│                              ▼                 ▼                        │
│                     ┌──────────────┐  ┌──────────────┐                  │
│                     │  Key Vault   │  │  Redis Cache │                  │
│                     │  (Secrets)   │  │  (Sessions)  │                  │
│                     └──────────────┘  └──────────────┘                  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Prerequisites

1. **Azure CLI** - [Install](https://aka.ms/install-azure-cli)
2. **PowerShell 7+** - [Install](https://aka.ms/install-powershell)
3. **Azure Subscription** - With permissions to create resources
4. **Entra External ID Tenants** - Dev and Prod tenants already created

## Quick Start

### Deploy Development Environment

```powershell
cd scripts/infra

# Full deployment (infrastructure + Entra)
.\deploy-environment.ps1 -Environment dev

# Infrastructure only
.\deploy-environment.ps1 -Environment dev -SkipEntra

# Entra configuration only
.\deploy-environment.ps1 -Environment dev -SkipInfra
```

### Deploy Production Environment

```powershell
.\deploy-environment.ps1 -Environment prod
```

## What Gets Created

### Azure Infrastructure (per environment)

| Resource | Dev Name | Prod Name | Purpose |
|----------|----------|-----------|---------|
| Resource Group | `rg-rajfinancial-dev` | `rg-rajfinancial-prod` | Container for all resources |
| SQL Server | `sql-rajfinancial-dev` | `sql-rajfinancial-prod` | Database server (Entra-only auth) |
| SQL Database | `sqldb-rajfinancial-dev` | `sqldb-rajfinancial-prod` | Free tier (dev) / Basic (prod) |
| Function App | `func-rajfinancial-dev` | `func-rajfinancial-prod` | API backend |
| Static Web App | `stapp-rajfinancial-dev` | `stapp-rajfinancial-prod` | Blazor WASM frontend |
| Storage Account | `strajfinancialdev` | `strajfinancialprod` | Documents and blobs |
| Key Vault | `kv-rajfinancial-dev` | `kv-rajfinancial-prod` | Secrets management |
| Redis Cache | `redis-rajfinancial-dev` | `redis-rajfinancial-prod` | Session state |
| App Insights | `appi-rajfinancial-dev` | `appi-rajfinancial-prod` | Monitoring |

### Entra External ID (per tenant)

| Item | Dev Tenant | Prod Tenant |
|------|------------|-------------|
| Tenant Domain | `rajfinancialdev.onmicrosoft.com` | `rajfinancialprod.onmicrosoft.com` |
| SPA App | `rajfinancial-spa-dev` | `rajfinancial-spa` |
| API App | `rajfinancial-api-dev` | `rajfinancial-api` |
| App Roles | Client, Administrator | Client, Administrator |
| OAuth2 Scopes | user_impersonation, Accounts.Read, Accounts.ReadWrite | Same |

## Security Model

### Managed Identity (No Secrets!)

All Azure resources use **Managed Identity** for authentication:

```
Function App ──► Managed Identity ──► SQL Database
                                  ──► Key Vault
                                  ──► Storage Account
                                  ──► Redis Cache
```

**Connection strings contain NO SECRETS:**
```
Server=tcp:sql-rajfinancial-dev.database.windows.net,1433;
Database=sqldb-rajfinancial-dev;
Authentication=Active Directory Default;
Encrypt=True;
```

### Entra External ID Roles

| Role | Description | Permissions |
|------|-------------|-------------|
| **Client** | Standard user | Own data, grant access to others |
| **Administrator** | Platform staff | System-wide access |

Fine-grained access (spouses, CPAs, attorneys) is handled through **DataAccessGrant** entities, not app roles.

## Individual Scripts

### `register-entra-apps.ps1`

Registers SPA and API applications in Entra External ID:

```powershell
.\register-entra-apps.ps1 -Environment dev
.\register-entra-apps.ps1 -Environment prod
```

Creates:
- SPA app with redirect URIs
- API app with OAuth2 scopes
- App roles (Client, Administrator)
- Service principals

### `configure-sql-access.ps1`

Grants the Function App's Managed Identity access to SQL Database:

```powershell
.\configure-sql-access.ps1 -Environment dev
```

Executes T-SQL to:
- Create database user from external provider
- Assign db_datareader and db_datawriter roles
- (Dev only) Assign db_ddladmin for EF Core migrations

### `add-entra-federated-credentials.ps1`

Adds GitHub OIDC credentials for passwordless CI/CD:

```powershell
..\add-entra-federated-credentials.ps1 `
    -EntraTenantId "496527a2-41f8-4297-a979-c916e7255a22" `
    -AppClientId "<spa-app-client-id>"
```

## App Role GUIDs

These GUIDs are hardcoded and must match across all configuration files:

### Development

| Role | GUID |
|------|------|
| Client | `bc34bd6c-38b8-46a6-9d4c-d338afeea81f` |
| Administrator | `2202014c-e4b9-4ab9-9e6a-4cc53e13598f` |

### Production

| Role | GUID |
|------|------|
| Client | `d4e5f6a7-b8c9-4d5e-a6b7-c8d9e0f1a2b3` |
| Administrator | `1a2b3c4d-5e6f-4a5b-8c9d-0e1f2a3b4c5d` |

## Post-Deployment Configuration

After running `deploy-environment.ps1`:

1. **Update Client Configuration**
   ```json
   // src/Client/wwwroot/appsettings.Development.json
   {
     "AzureAd": {
       "Authority": "https://rajfinancialdev.ciamlogin.com/",
       "ClientId": "<spa-app-id-from-output>"
     }
   }
   ```

2. **Update API Configuration**
   ```json
   // src/Api/local.settings.json
   {
     "Values": {
       "SqlConnectionString": "Server=tcp:sql-rajfinancial-dev.database.windows.net,1433;Database=sqldb-rajfinancial-dev;Authentication=Active Directory Default;Encrypt=True;"
     }
   }
   ```

3. **Grant Admin Consent**
   - Go to Entra portal → App registrations → SPA app → API permissions
   - Click "Grant admin consent"

4. **Add GitHub Secrets**
   - `ENTRA_DEV_TENANT_ID`
   - `ENTRA_CLIENT_ID`
   - `AZURE_SUBSCRIPTION_ID`

## Troubleshooting

### "Not logged into correct tenant"

```powershell
az login --tenant <tenant-id> --allow-no-subscriptions
```

### "SQL access denied"

Ensure you're in the SQL Administrators Entra group, then run:
```powershell
.\configure-sql-access.ps1 -Environment dev
```

### "App registration already exists"

The script handles this gracefully - it will update the existing registration.

## Cost Estimates

### Development Environment

| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| SQL Database | Free | $0 |
| Function App | Consumption | ~$0 (free tier) |
| Static Web App | Free | $0 |
| Redis Cache | Basic C0 | ~$16 |
| Key Vault | Standard | ~$0.03/operation |
| Storage | LRS | ~$0.02/GB |
| **Total** | | **~$20/month** |

### Production Environment

| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| SQL Database | Basic | ~$5 |
| Function App | EP1 | ~$150 |
| Static Web App | Free | $0 |
| Redis Cache | Standard C0 | ~$40 |
| Key Vault | Standard | ~$1 |
| Storage | LRS | ~$5 |
| **Total** | | **~$200/month** |

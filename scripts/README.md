# RAJ Financial - Scripts

This directory contains all automation scripts for infrastructure provisioning, CI/CD setup, and operational tasks.

## ?? Directory Structure

```
scripts/
??? README.md                           # This file
??? infra/                              # Infrastructure provisioning (one-time setup)
?   ??? register-entra-apps.ps1         # Register SPA & API apps in Entra External ID
??? setup-entra-oidc.ps1                # Create GitHub Actions OIDC service principal
??? add-entra-federated-credentials.ps1 # Add OIDC credentials to existing app
??? configure-entra-app-roles.ps1       # Configure app roles (Admin, Advisor, Client)
??? configure-user-flows.ps1            # Check/configure Entra user flows & MFA
??? check-user-flows.ps1                # Quick check of current user flows
??? README-ENTRA-OIDC.md                # Detailed OIDC setup documentation
```

## ?? Script Categories

### Infrastructure Provisioning (`infra/`)

One-time setup scripts for initial environment configuration.

| Script | Purpose | When to Use |
|--------|---------|-------------|
| `register-entra-apps.ps1` | Registers SPA and API applications in Entra External ID | Initial setup of dev/prod environments |

### CI/CD & OIDC Setup

Scripts for configuring GitHub Actions authentication.

| Script | Purpose | When to Use |
|--------|---------|-------------|
| `setup-entra-oidc.ps1` | Creates a **new** service principal for GitHub Actions OIDC | Setting up a new GitHub Actions ? Entra integration |
| `add-entra-federated-credentials.ps1` | Adds OIDC credentials to an **existing** app registration | Adding GitHub Actions auth to existing apps |

### Configuration & Operations

Scripts for ongoing configuration and diagnostics.

| Script | Purpose | When to Use |
|--------|---------|-------------|
| `configure-entra-app-roles.ps1` | Adds/updates app roles in Entra | After app registration, role changes |
| `configure-user-flows.ps1` | Checks and documents user flow/MFA configuration | MFA configuration review |
| `check-user-flows.ps1` | Quick diagnostic for user flows | Troubleshooting auth issues |

---

## ?? Quick Start

### Prerequisites

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) installed
- [PowerShell 7+](https://github.com/PowerShell/PowerShell) (cross-platform)
- [Microsoft.Graph PowerShell module](https://docs.microsoft.com/powershell/microsoftgraph/) (for some scripts)
- Appropriate permissions in Entra External ID tenant

### Initial Environment Setup

For a **new environment** (dev or prod), run these scripts in order:

```powershell
cd scripts

# 1. Register the SPA and API applications in Entra
.\infra\register-entra-apps.ps1 -Environment dev

# 2. Configure app roles
.\configure-entra-app-roles.ps1 -AppObjectId "<from-step-1>" -TenantId "<tenant-id>"

# 3. Setup GitHub Actions OIDC (for CI/CD)
.\setup-entra-oidc.ps1 -EntraTenantId "<tenant-id>"
```

### Adding GitHub Actions OIDC to Existing App

If you already have an app registration and just need OIDC:

```powershell
.\add-entra-federated-credentials.ps1 `
    -EntraTenantId "496527a2-41f8-4297-a979-c916e7255a22" `
    -AppClientId "2d6a08c7-b142-4d53-a307-9ac75bae75eb"
```

---

## ?? Detailed Script Documentation

### `infra/register-entra-apps.ps1`

Registers both the SPA (Blazor WASM) and API (Azure Functions) applications in Entra External ID.

**Parameters:**
- `-Environment` (Required): `dev` or `prod`

**What it does:**
1. Creates API app registration with OAuth2 scopes
2. Creates SPA app registration with redirect URIs
3. Configures app roles (Client, Administrator)
4. Creates service principals
5. Outputs configuration for `appsettings.json`

**Example:**
```powershell
.\infra\register-entra-apps.ps1 -Environment dev
```

**Output:**
- Creates `entra-config-dev.json` with all IDs and configuration

---

### `setup-entra-oidc.ps1`

Creates a **new** service principal specifically for GitHub Actions to manage Entra app registrations via OIDC.

**Parameters:**
- `-EntraTenantId` (Required): Your Entra External ID tenant ID
- `-AppName` (Optional): Display name for the app (default: "GitHub Actions - RAJ Financial Entra Manager")

**What it does:**
1. Creates a new app registration
2. Creates service principal
3. Grants `Application.ReadWrite.All` permission
4. Grants admin consent
5. Creates federated credentials for production, development, preview environments

**Example:**
```powershell
.\setup-entra-oidc.ps1 -EntraTenantId "496527a2-41f8-4297-a979-c916e7255a22"
```

**GitHub Secrets to add:**
- `ENTRA_CLIENT_ID`: Application (Client) ID from output
- `ENTRA_DEV_TENANT_ID`: Your tenant ID

---

### `add-entra-federated-credentials.ps1`

Adds GitHub OIDC federated credentials to an **existing** app registration.

**Parameters:**
- `-EntraTenantId` (Required): Your Entra External ID tenant ID
- `-AppClientId` (Required): Existing app's Client ID

**What it does:**
1. Finds existing app registration
2. Ensures service principal exists
3. Adds `Application.ReadWrite.All` permission (if missing)
4. Grants admin consent
5. Creates federated credentials (skips if already exist)

**Example:**
```powershell
.\add-entra-federated-credentials.ps1 `
    -EntraTenantId "496527a2-41f8-4297-a979-c916e7255a22" `
    -AppClientId "2d6a08c7-b142-4d53-a307-9ac75bae75eb"
```

---

### `configure-entra-app-roles.ps1`

Configures app roles in an existing Entra app registration.

**Parameters:**
- `-AppObjectId` (Required): The **Object ID** (not Client ID) of the app
- `-TenantId` (Required): Your Entra tenant ID

**What it does:**
1. Fetches current app roles
2. Adds missing roles (Administrator, Advisor, Client)
3. Preserves existing roles

**Example:**
```powershell
# First, login to the tenant
az login --tenant "496527a2-41f8-4297-a979-c916e7255a22" --allow-no-subscriptions

# Then run the script
.\configure-entra-app-roles.ps1 `
    -AppObjectId "abc12345-..." `
    -TenantId "496527a2-41f8-4297-a979-c916e7255a22"
```

---

### `configure-user-flows.ps1`

Checks and documents user flow configuration including MFA settings.

**Parameters:**
- `-Environment` (Optional): `Dev` or `Prod` (default: Dev)
- `-UpdateMFA` (Optional): Switch to trigger MFA update (manual steps provided)

**What it does:**
1. Connects to Microsoft Graph
2. Lists all user flows in the tenant
3. Shows current MFA configuration
4. Provides manual steps for MFA changes

**Example:**
```powershell
# Check Dev tenant
.\configure-user-flows.ps1

# Check Prod tenant
.\configure-user-flows.ps1 -Environment Prod
```

---

### `check-user-flows.ps1`

Quick diagnostic script to check user flows in the Dev tenant.

**Example:**
```powershell
.\check-user-flows.ps1
```

---

## ?? Environment Configuration

### Development Tenant

| Setting | Value |
|---------|-------|
| Tenant ID | `496527a2-41f8-4297-a979-c916e7255a22` |
| Domain | `rajfinancialdev.onmicrosoft.com` |
| CIAM Login | `rajfinancialdev.ciamlogin.com` |
| MFA | **Disabled** (for testing) |

### Production Tenant

| Setting | Value |
|---------|-------|
| Tenant ID | `cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6` |
| Domain | `rajfinancialprod.onmicrosoft.com` |
| CIAM Login | `rajfinancialprod.ciamlogin.com` |
| MFA | **Enabled** (Always on) |

---

## ?? GitHub Secrets Reference

These secrets are required for CI/CD workflows:

| Secret | Description | Source |
|--------|-------------|--------|
| `AZURE_CLIENT_ID` | Azure subscription service principal | Azure Portal |
| `AZURE_TENANT_ID` | Azure subscription tenant ID | Azure Portal |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | Azure Portal |
| `ENTRA_CLIENT_ID` | Entra OIDC app Client ID | `setup-entra-oidc.ps1` output |
| `ENTRA_DEV_TENANT_ID` | Entra External ID tenant ID | Known value |
| `ENTRA_SPA_APP_OBJECT_ID` | SPA app Object ID (for redirect URI management) | Entra Portal |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_*` | SWA deployment token | Azure Portal |

---

## ?? Additional Resources

- [README-ENTRA-OIDC.md](./README-ENTRA-OIDC.md) - Detailed OIDC setup guide
- [Azure OIDC with GitHub Actions](https://learn.microsoft.com/azure/developer/github/connect-from-azure)
- [Entra External ID Documentation](https://learn.microsoft.com/entra/external-id/)
- [Federated Identity Credentials](https://learn.microsoft.com/entra/workload-id/workload-identity-federation)

---

## ?? Troubleshooting

### "AADSTS700016: Application not found"

The Client ID is incorrect or the app doesn't exist in the tenant.

```powershell
# Verify the app exists
az login --tenant "<tenant-id>" --allow-no-subscriptions
az ad app show --id "<client-id>"
```

### "Insufficient privileges"

You need admin consent for `Application.ReadWrite.All`.

1. Go to [Entra Admin Center](https://entra.microsoft.com)
2. Find the app registration
3. Go to **API permissions** ? **Grant admin consent**

### "Interactive authentication required"

OIDC federated credentials aren't set up correctly.

```powershell
# Check federated credentials
az ad app federated-credential list --id "<client-id>"
```

---

**Last Updated:** December 2025

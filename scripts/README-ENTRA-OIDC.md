# Setting Up Entra External ID OIDC for GitHub Actions

This guide will help you configure OIDC authentication so GitHub Actions can manage your Entra External ID app registrations without requiring interactive login.

## Prerequisites

- **Azure CLI** installed ([Install guide](https://learn.microsoft.com/cli/azure/install-azure-cli))
- **PowerShell** (Windows built-in or [PowerShell Core](https://github.com/PowerShell/PowerShell))
- **Permissions** in your Entra External ID tenant:
  - Application Administrator, Cloud Application Administrator, or Global Administrator
  - Ability to grant admin consent for API permissions

## Step 1: Get Your Entra External ID Tenant ID

If you don't already have it, get your Entra External ID tenant ID:

1. Go to [Microsoft Entra Admin Center](https://entra.microsoft.com/)
2. Navigate to **Settings > Tenant properties**
3. Copy the **Tenant ID**

## Step 2: Get Your App Registration Client ID

Get the Client ID from your existing `rajfinancial-spa-dev` app registration:

1. Go to [Microsoft Entra Admin Center](https://entra.microsoft.com/)
2. Navigate to **Applications > App registrations**
3. Find **rajfinancial-spa-dev**
4. Copy the **Application (client) ID**

## Step 3: Run the Setup Script

Open PowerShell and run:

```powershell
cd D:\Code\rajfinancial\scripts

.\add-entra-federated-credentials.ps1 -EntraTenantId "YOUR-ENTRA-TENANT-ID" -AppClientId "YOUR-APP-CLIENT-ID"
```

**Example:**
```powershell
.\add-entra-federated-credentials.ps1 `
    -EntraTenantId "12345678-1234-1234-1234-123456789abc" `
    -AppClientId "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

### What the Script Does

1. ✅ Logs into your Entra External ID tenant
2. ✅ Finds your existing app registration (`rajfinancial-spa-dev`)
3. ✅ Creates a service principal (if needed)
4. ✅ Adds **Application.ReadWrite.All** permission (if not present)
5. ✅ Grants admin consent
6. ✅ Creates federated credentials for:
   - `production` environment
   - `development` environment
   - `preview` environments

### Handling Errors

**If admin consent fails:**
The script will provide instructions to grant consent manually:
1. Go to: https://entra.microsoft.com/
2. Navigate to: **Applications > App registrations**
3. Find: **rajfinancial-spa-dev**
4. Click: **API permissions > Grant admin consent**

## Step 4: Add GitHub Secrets

After the script completes, it will output values to add to GitHub.

### Add Secrets to GitHub

1. Go to: [https://github.com/jamesconsultingllc/rajfinancial/settings/secrets/actions](https://github.com/jamesconsultingllc/rajfinancial/settings/secrets/actions)

2. Click **New repository secret**

3. Add the following secrets:

| Secret Name | Value | Notes |
|------------|--------|-------|
| `ENTRA_CLIENT_ID` | *(from script output)* | Application (Client) ID |
| `ENTRA_DEV_TENANT_ID` | *(your Entra tenant ID)* | May already exist |

**Example output from script:**
```
📋 Add these secrets to your GitHub repository:

  Secret Name: ENTRA_CLIENT_ID
  Secret Value: a1b2c3d4-e5f6-7890-abcd-ef1234567890

  Secret Name: ENTRA_DEV_TENANT_ID
  Secret Value: 12345678-1234-1234-1234-123456789abc
  (This may already exist)
```

## Step 5: Verify Setup

The workflow has been updated to use OIDC authentication. The changes include:

### In `.github/workflows/azure-static-web-apps-gray-cliff-072f3b510.yml`:

- ✅ Added **Azure Login with OIDC (Entra External ID)** step before managing redirect URIs
- ✅ Removed `tenant-id` parameter from the custom action calls

### In `.github/actions/manage-entra-redirect-uris/action.yml`:

- ✅ Removed interactive `az login` step
- ✅ Improved error handling for new app registrations
- ✅ Now relies on OIDC authentication from parent workflow

## Step 6: Test the Workflow

Push a feature branch to trigger the workflow:

```bash
git checkout develop
git pull
git checkout -b feature/test-oidc-setup
git add .
git commit -m "test: verify Entra OIDC authentication setup"
git push -u origin feature/test-oidc-setup
```

### Expected Behavior

1. Workflow runs without prompting for device code login
2. Preview environment is created
3. Redirect URI is added to Entra app registration automatically
4. Build completes successfully

### Verify in Azure

Check that the redirect URI was added:

1. Go to [Microsoft Entra Admin Center](https://entra.microsoft.com/)
2. Navigate to **Applications > App registrations**
3. Find your SWA app registration (the one with `ENTRA_SPA_APP_OBJECT_ID`)
4. Click **Authentication**
5. Verify the preview URL was added to redirect URIs

## Troubleshooting

### Error: "AADSTS700016: Application not found in the directory"

**Cause:** The `ENTRA_CLIENT_ID` secret is incorrect or not set.

**Fix:**
1. Verify the secret value matches the output from the setup script
2. Re-run the setup script if needed

### Error: "Insufficient privileges to complete the operation"

**Cause:** The service principal doesn't have admin consent for `Application.ReadWrite.All`.

**Fix:**
1. Go to [Entra Admin Center](https://entra.microsoft.com/)
2. Navigate to **Applications > App registrations**
3. Find **rajfinancial-spa-dev**
4. Click **API permissions > Grant admin consent for [tenant]**

### Error: "Couldn't find 'web' in ''"

**Cause:** This error should be fixed by the updated action, but if it persists:

**Fix:** Verify that the `ENTRA_SPA_APP_OBJECT_ID` secret is the **Object ID** (not Client ID) of your app registration.

## Security Considerations

### What is OIDC?

OIDC (OpenID Connect) authentication eliminates the need for storing long-lived secrets. Instead:

- GitHub Actions gets a short-lived token from GitHub's OIDC provider
- Azure validates the token matches the federated credential
- Token expires after the workflow completes

### Federated Credentials

The setup script creates three federated credentials:

| Credential | Subject Identifier | Usage |
|-----------|-------------------|-------|
| `github-oidc-production` | `repo:jamesconsultingllc/rajfinancial:environment:production` | Production deployments |
| `github-oidc-development` | `repo:jamesconsultingllc/rajfinancial:environment:development` | Development deployments |
| `github-oidc-preview` | `repo:jamesconsultingllc/rajfinancial:environment:preview` | Preview environments |

### Permissions

Your app registration (`rajfinancial-spa-dev`) has **Application.ReadWrite.All** permission, which allows it to:

- ✅ Read app registrations (including itself)
- ✅ Update redirect URIs (on itself)
- ✅ Manage app configuration

**Note:** The app manages its own redirect URIs via GitHub Actions. This is a secure pattern because:
- Only your GitHub repository can authenticate (via federated credentials)
- The app cannot modify other apps in the tenant
- No long-lived secrets are stored in GitHub

## Additional Resources

- [Azure OIDC with GitHub Actions](https://learn.microsoft.com/azure/developer/github/connect-from-azure)
- [Federated Identity Credentials](https://learn.microsoft.com/entra/workload-id/workload-identity-federation)
- [GitHub OIDC with Azure](https://docs.github.com/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)

## Support

If you encounter issues:

1. Check the [Troubleshooting](#troubleshooting) section above
2. Review GitHub Actions logs for detailed error messages
3. Verify all secrets are configured correctly
4. Ensure federated credentials were created successfully

---

**Last Updated:** 2025-12-19
**Script Version:** 1.0.0

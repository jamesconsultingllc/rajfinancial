# Azure Permissions Required for Test User Cleanup

## Overview

The test cleanup functionality in `TestUserCleanupHelper.cs` requires specific Microsoft Graph API permissions to delete test users from Entra ID after tests complete.

## Required Permissions

To enable test user cleanup, your Azure AD App Registration needs the following Microsoft Graph API permissions:

### Recommended: Least Privilege Approach ✅

| Permission | Type | Reason |
|------------|------|--------|
| `User.Read.All` | Application | Search for test users by UPN pattern |
| `User.DeleteRestore.All` | Application | Delete test users after tests complete |

**This is the recommended approach** because it:
- ✅ Follows principle of least privilege
- ✅ Only grants search + delete permissions (not full read/write)
- ✅ Easier to justify to security teams
- ✅ Lower risk if credentials are compromised

### Alternative: Broader Access (Not Recommended)

| Permission | Type | Reason | Risk |
|------------|------|--------|------|
| `User.ReadWrite.All` | Application | Can read, write, and delete all users | 🔴 High - grants unnecessary permissions |
| `Directory.ReadWrite.All` | Application | Can read and write all directory data | 🔴 Very High - overly broad |

⚠️ **Only use these if you have other requirements beyond test cleanup**

### Why Application Permissions?

- Tests run in a non-interactive CI/CD environment
- No user is signed in during automated test execution
- Application permissions work without user context

---

## Setup Instructions

### Step 1: Navigate to App Registration

1. Go to https://portal.azure.com
2. Navigate to **Azure Active Directory** → **App registrations**
3. Find your app registration (e.g., "RAJ Financial Dev Tests")
4. Click on it to open the details

### Step 2: Add API Permissions

1. In the left menu, click **API permissions**
2. Click **+ Add a permission**
3. Select **Microsoft Graph**
4. Select **Application permissions** (NOT Delegated permissions)
5. Search for and select:
   - `User.ReadWrite.All`
6. Click **Add permissions**

### Step 3: Grant Admin Consent

⚠️ **CRITICAL**: Application permissions require admin consent

1. After adding the permission, you'll see it in the list with status "Not granted"
2. Click **Grant admin consent for [Your Tenant]**
3. Confirm the consent
4. The status should change to "Granted for [Your Tenant]" with a green checkmark

### Step 4: Configure Environment Variables

Set the following environment variables for test execution:

```bash
# Azure AD Tenant ID (found in Azure Portal → Azure AD → Overview)
AZURE_TENANT_ID=496527a2-41f8-4297-a979-c916e7255a22

# App Registration Client ID
AZURE_CLIENT_ID=your-app-client-id-here

# App Registration Client Secret (create in "Certificates & secrets")
AZURE_CLIENT_SECRET=your-client-secret-here
```

**OR** add to `appsettings.local.json`:

```json
{
  "AzureTenantId": "496527a2-41f8-4297-a979-c916e7255a22",
  "AzureClientId": "your-app-client-id-here",
  "AzureClientSecret": "your-client-secret-here"
}
```

### Step 5: Create Client Secret

1. In your App Registration, go to **Certificates & secrets**
2. Click **+ New client secret**
3. Add a description (e.g., "Test User Cleanup")
4. Select expiration (e.g., 6 months, 12 months, 24 months)
5. Click **Add**
6. **COPY THE SECRET VALUE IMMEDIATELY** - it won't be shown again
7. Use this value for `AZURE_CLIENT_SECRET`

---

## Security Best Practices

### Principle of Least Privilege

If you only need to delete users (not read all users), consider creating a custom role with minimal permissions. However, Microsoft Graph doesn't offer a "Delete only" permission - `User.ReadWrite.All` is the minimum.

### Separate App Registrations

Consider using separate app registrations for different environments:

- **Dev Tests**: `User.ReadWrite.All` (can delete test users)
- **Prod Tests**: No delete permissions (or no cleanup at all)

### Protect Client Secrets

- **NEVER commit secrets to Git**
- Use Azure Key Vault for production
- Use environment variables or `appsettings.local.json` (in `.gitignore`)
- Rotate secrets regularly (every 6-12 months)

### Limit Scope with Filters

The `TestUserCleanupHelper` only deletes users that:
1. Are tagged for cleanup in ScenarioContext
2. Have UPN matching test email pattern (e.g., `test-e2e-*@rajlegacy.org`)

This prevents accidental deletion of real users.

---

## Verification

### Test the Permissions

Run a test with cleanup enabled:

```bash
cd tests/AcceptanceTests
dotnet test --filter "FullyQualifiedName~NewUserCanCreateAnAccountThroughEntraExternalID"
```

**Expected output if permissions are correct:**
```
✓ Cleanup: Deleted user test-e2e-20241226163556-a64c86d6@rajlegacy.org
```

**Expected output if permissions are missing:**
```
⚠ Cleanup skipped: AZURE_TENANT_ID and AZURE_CLIENT_ID not configured
```

**Expected output if permissions are insufficient:**
```
❌ Cleanup failed: Insufficient privileges to complete the operation
```

---

## Troubleshooting

### Error: "Insufficient privileges"

**Cause**: Admin consent not granted or wrong permission type (delegated instead of application)

**Fix**:
1. Verify you added **Application permissions**, not Delegated
2. Ensure admin consent was granted (green checkmark)
3. Wait 5-10 minutes for permissions to propagate

### Error: "Invalid client secret"

**Cause**: Client secret expired or incorrect

**Fix**:
1. Create a new client secret in Azure Portal
2. Update `AZURE_CLIENT_SECRET` environment variable
3. Restart test execution

### Error: "AADSTS700016: Application not found"

**Cause**: Incorrect `AZURE_CLIENT_ID`

**Fix**:
1. Verify the Client ID matches your App Registration
2. Check you're using the correct tenant

---

## Alternative: Manual Cleanup

If you don't want to grant these permissions, you can manually delete test users:

1. Go to https://portal.azure.com
2. Navigate to **Azure Active Directory** → **Users**
3. Search for users with UPN: `test-e2e-*`
4. Select and delete test users manually

---

## References

- [Microsoft Graph API Permissions](https://learn.microsoft.com/en-us/graph/permissions-reference)
- [User Resource Type](https://learn.microsoft.com/en-us/graph/api/resources/user)
- [Delete User API](https://learn.microsoft.com/en-us/graph/api/user-delete)

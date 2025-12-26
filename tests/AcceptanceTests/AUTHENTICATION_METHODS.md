# Test User Cleanup - Authentication Methods

The `TestUserCleanupHelper` now supports multiple authentication methods with explicit client secret handling.

## Method 1: Client Secret (Recommended for Local Dev)

**Use case**: Local development with app permissions

**Setup**:
1. Get client secret from Azure Portal:
   - App registrations → Your app → Certificates & secrets
   - Create new client secret (or use existing)
   - Copy the **Value**

2. Set environment variables:

**PowerShell:**
```powershell
$env:AZURE_TENANT_ID = "496527a2-41f8-4297-a979-c916e7255a22"
$env:AZURE_CLIENT_ID = "2d6a08c7-b142-4d53-a307-9ac75bae75eb"
$env:AZURE_CLIENT_SECRET = "your-client-secret-here"
```

**Bash:**
```bash
export AZURE_TENANT_ID="496527a2-41f8-4297-a979-c916e7255a22"
export AZURE_CLIENT_ID="2d6a08c7-b142-4d53-a307-9ac75bae75eb"
export AZURE_CLIENT_SECRET="your-client-secret-here"
```

**Required permissions**:
- Application: `User.Read.All` + `User.DeleteRestore.All`

**Log output**:
```
ℹ️  Using client secret authentication (app permissions)
✓ Successfully obtained access token for Microsoft Graph
✓ Deleted test user: test-e2e-20241226172555-ece65add@rajlegacy.org
```

---

## Method 2: Federated Credentials (Recommended for CI/CD)

**Use case**: GitHub Actions with OIDC (no secrets!)

**Setup**:
1. Configure federated credential in Azure Portal:
   - App registrations → Your app → Certificates & secrets → Federated credentials
   - Add credential for GitHub Actions
   - Entity type: Environment (e.g., "test")

2. GitHub Actions workflow:

```yaml
permissions:
  id-token: write  # Required for OIDC
  contents: read

jobs:
  test:
    runs-on: ubuntu-latest
    environment: test  # Must match federated credential

    steps:
      - name: Run E2E Tests
        env:
          AZURE_TENANT_ID: ${{ secrets.AZURE_TENANT_ID }}
          AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}
          # No AZURE_CLIENT_SECRET needed!
        run: dotnet test
```

**Required permissions**:
- Application: `User.Read.All` + `User.DeleteRestore.All`

**Log output**:
```
ℹ️  Using DefaultAzureCredential (federated/managed identity/Azure CLI)
✓ Successfully obtained access token for Microsoft Graph
✓ Deleted test user: test-e2e-20241226172555-ece65add@rajlegacy.org
```

---

## Method 3: Azure CLI (Quick Local Testing)

**Use case**: Quick local testing with your user credentials

**Setup**:
```bash
# Login with your Azure account
az login

# Make sure you don't have AZURE_CLIENT_SECRET set
# (PowerShell)
Remove-Item Env:\AZURE_CLIENT_SECRET

# (Bash)
unset AZURE_CLIENT_SECRET

# Run tests
dotnet test
```

**Required permissions**:
- Delegated: `User.DeleteRestore.All` (with admin consent)
- Directory role: **User Administrator** or higher

**Log output**:
```
ℹ️  Using DefaultAzureCredential (federated/managed identity/Azure CLI)
✓ Successfully obtained access token for Microsoft Graph
✓ Deleted test user: test-e2e-20241226172555-ece65add@rajlegacy.org
```

---

## Method 4: Managed Identity (Azure-hosted)

**Use case**: Running tests in Azure (VM, Container, App Service)

**Setup**:
1. Enable Managed Identity on your Azure resource
2. Grant permissions to the Managed Identity:
   - Azure Portal → Azure AD → Enterprise Applications
   - Find your Managed Identity
   - Assign API permissions

**No environment variables needed** - automatic!

**Log output**:
```
ℹ️  Using DefaultAzureCredential (federated/managed identity/Azure CLI)
✓ Successfully obtained access token for Microsoft Graph
✓ Deleted test user: test-e2e-20241226172555-ece65add@rajlegacy.org
```

---

## Decision Matrix

| Scenario | Recommended Method | Why |
|----------|-------------------|-----|
| **Local Development** | Client Secret (Method 1) | Explicit, works with app permissions |
| **GitHub Actions** | Federated Credentials (Method 2) | No secrets, most secure |
| **Azure DevOps** | Federated Credentials (Method 2) | No secrets, most secure |
| **Quick Test** | Azure CLI (Method 3) | Fast setup, uses your account |
| **Azure-hosted Tests** | Managed Identity (Method 4) | Automatic, no config needed |

---

## Troubleshooting

### "ℹ️ Using DefaultAzureCredential" but deletions fail with Forbidden

**Cause**: Using Azure CLI (your user credentials) without proper delegated permissions or directory role

**Fix**:
- Option A: Switch to client secret (Method 1)
- Option B: Add delegated `User.DeleteRestore.All` permission + User Administrator role

### "Failed to authenticate with Azure"

**Cause**: No credentials available

**Fix**:
1. Check environment variables are set: `echo $env:AZURE_CLIENT_SECRET`
2. If using Azure CLI, run: `az login`
3. Check for typos in tenant/client IDs

### "ℹ️ Using client secret authentication" but still getting Forbidden

**Cause**: Application permissions not granted or admin consent missing

**Fix**:
1. Azure Portal → App registrations → API permissions
2. Verify permissions are granted with green checkmark
3. Click "Grant admin consent" if needed
4. Wait 5-10 minutes for propagation

---

## Security Best Practices

1. ✅ **Use federated credentials in CI/CD** - No secrets to leak
2. ✅ **Rotate client secrets regularly** - Max 12-24 months
3. ✅ **Use separate apps for dev/prod** - Limit blast radius
4. ✅ **Never commit secrets to Git** - Use environment variables
5. ✅ **Prefer app permissions over user permissions** - Principle of least privilege
6. ✅ **Use Managed Identity in Azure** - No credential management needed

---

## Next Steps

1. Choose your authentication method based on the decision matrix above
2. Configure the required permissions in Azure Portal
3. Set environment variables (if using client secret)
4. Run tests and verify cleanup works:
   ```bash
   dotnet test --filter "FullyQualifiedName~NewUserCanCreateAnAccountThroughEntraExternalID"
   ```
5. Check for successful deletion in test output

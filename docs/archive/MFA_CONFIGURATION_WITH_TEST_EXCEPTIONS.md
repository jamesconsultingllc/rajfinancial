# MFA Configuration with Test User Exceptions
## Entra External ID Conditional Access

---

## Overview

This guide shows you how to **require MFA for all users EXCEPT test users** in Microsoft Entra External ID.

### Strategy

1. Create a **security group** called "Test Users - No MFA"
2. Add your test users to this group
3. Create a **Conditional Access policy** requiring MFA for all users
4. **Exclude** the "Test Users - No MFA" group from the policy

---

## Part 1: Create Security Group for Test Users

### Option A: Azure Portal (Manual)

1. Go to [Entra Admin Center](https://entra.microsoft.com)
2. Select your tenant: `rajfinancialdev.onmicrosoft.com`
3. Navigate to **Groups** → **All groups**
4. Click **+ New group**
5. Configure:
   - **Group type**: Security
   - **Group name**: `Test Users - No MFA`
   - **Group description**: `Test users exempt from MFA requirement`
   - **Membership type**: Assigned
6. Click **Create**
7. Note the **Object ID** of the group

### Option B: PowerShell (Automated)

```powershell
# Connect to Microsoft Graph
Connect-MgGraph -TenantId "496527a2-41f8-4297-a979-c916e7255a22" -Scopes "Group.ReadWrite.All"

# Create the security group
$group = New-MgGroup -DisplayName "Test Users - No MFA" `
    -MailEnabled:$false `
    -SecurityEnabled:$true `
    -MailNickname "test-users-no-mfa" `
    -Description "Test users exempt from MFA requirement"

Write-Host "Group created with Object ID: $($group.Id)"
```

---

## Part 2: Add Test Users to the Group

### Create Test Users

```powershell
# Define test users for each role
$testUsers = @(
    @{
        DisplayName = "Test Client User"
        UserPrincipalName = "testclient@rajfinancialdev.onmicrosoft.com"
        PasswordProfile = @{
            Password = "Test@123456"  # Change this!
            ForceChangePasswordNextSignIn = $false
        }
    },
    @{
        DisplayName = "Test Advisor User"
        UserPrincipalName = "testadvisor@rajfinancialdev.onmicrosoft.com"
        PasswordProfile = @{
            Password = "Test@123456"  # Change this!
            ForceChangePasswordNextSignIn = $false
        }
    },
    @{
        DisplayName = "Test Admin User"
        UserPrincipalName = "testadmin@rajfinancialdev.onmicrosoft.com"
        PasswordProfile = @{
            Password = "Test@123456"  # Change this!
            ForceChangePasswordNextSignIn = $false
        }
    }
)

# Create each test user
$groupId = "<GROUP-OBJECT-ID-FROM-PART-1>"

foreach ($user in $testUsers) {
    # Create user
    $newUser = New-MgUser -DisplayName $user.DisplayName `
        -UserPrincipalName $user.UserPrincipalName `
        -PasswordProfile $user.PasswordProfile `
        -AccountEnabled:$true `
        -UsageLocation "US"

    Write-Host "Created user: $($newUser.UserPrincipalName) with ID: $($newUser.Id)"

    # Add to test group
    New-MgGroupMember -GroupId $groupId -DirectoryObjectId $newUser.Id
    Write-Host "Added $($newUser.UserPrincipalName) to Test Users - No MFA group"
}

Write-Host ""
Write-Host "✅ All test users created and added to the group!"
```

### Add Existing Users to Group

```powershell
# Get the group
$groupId = "<GROUP-OBJECT-ID>"

# Get user by email
$user = Get-MgUser -Filter "userPrincipalName eq 'testuser@rajfinancialdev.onmicrosoft.com'"

# Add user to group
New-MgGroupMember -GroupId $groupId -DirectoryObjectId $user.Id

Write-Host "Added $($user.UserPrincipalName) to Test Users - No MFA group"
```

---

## Part 3: Create Conditional Access Policy for MFA

### Prerequisites

**⚠️ Important**: Entra External ID requires **Azure AD Premium P1** for Conditional Access.

- Free tier: ❌ No Conditional Access
- Premium P1: ✅ Conditional Access available
- Premium P2: ✅ Conditional Access + advanced features

**Cost**: ~$6/user/month for Premium P1

### Alternative for Free Tier: MFA at User Flow Level

If you don't have Premium P1, you can enable MFA for the entire user flow (no exceptions):

1. Go to **External Identities** → **User flows**
2. Select your user flow (e.g., `B2C_1_signup_signin`)
3. Under **Settings** → **Multifactor authentication**
4. Select **Email** or **Text message**
5. Set **Enforcement** to **Always on**

**Limitation**: Cannot exclude test users with this approach.

### Create Conditional Access Policy (Premium P1 Required)

1. Go to [Entra Admin Center](https://entra.microsoft.com)
2. Navigate to **Protection** → **Conditional Access**
3. Click **+ New policy** → **Create new policy**
4. Configure:

**General:**
- **Name**: `Require MFA for All Users Except Test Accounts`
- **State**: On

**Assignments:**
- **Users**:
  - **Include**: All users
  - **Exclude**:
    - Groups: Select `Test Users - No MFA`
    - Directory roles: Global Administrator (prevent lockout)

- **Target resources**:
  - **Select what this policy applies to**: Cloud apps
  - **Include**: All cloud apps

- **Conditions**: (None needed for basic MFA)

**Access controls:**
- **Grant**:
  - ☑️ Require multifactor authentication
  - **For multiple controls**: Require all the selected controls

- **Session**: (None)

5. Review and create
6. Set **Enable policy**: On
7. Click **Create**

---

## Part 4: Testing

### Test 1: Production User (MFA Required)

1. Open incognito/private browsing window
2. Go to your app: `https://your-dev-swa.azurestaticapps.net`
3. Click "Sign Up as Client"
4. Create a new account with personal email (not in test group)
5. **Expected**: You should be prompted for MFA after signup

### Test 2: Test User (No MFA)

1. Open incognito/private browsing window
2. Go to your app
3. Click "Sign In"
4. Sign in with: `testclient@rajfinancialdev.onmicrosoft.com`
5. Password: `Test@123456` (or whatever you set)
6. **Expected**: You should sign in WITHOUT MFA prompt

### Test 3: Role Assignment

After signing in as test user:

1. Open browser dev tools → Application → Storage → Local Storage
2. Check for access token
3. Copy token and paste at https://jwt.ms
4. Verify `roles` claim contains expected role

---

## Part 5: Update App Role GUIDs

The `CompleteRoleAssignment.cs` function has placeholder GUIDs. Update them:

### Get Actual App Role GUIDs

```powershell
# Connect to Graph
Connect-MgGraph -Scopes "Application.Read.All"

# Get your app registration
$appId = "<YOUR-APP-CLIENT-ID>"
$app = Get-MgApplication -Filter "appId eq '$appId'"

# Display app roles with GUIDs
$app.AppRoles | Select-Object DisplayName, Value, Id | Format-Table

# Output example:
# DisplayName   Value          Id
# -----------   -----          --
# Administrator Administrator  12345678-1234-1234-1234-123456789012
# Advisor       Advisor        23456789-2345-2345-2345-234567890123
# Client        Client         34567890-3456-3456-3456-345678901234
```

### Update CompleteRoleAssignment.cs

Replace the placeholder GUIDs in `src/Api/Functions/Auth/CompleteRoleAssignment.cs`:

```csharp
private static readonly Dictionary<string, Guid> RoleMapping = new()
{
    { "Client", Guid.Parse("34567890-3456-3456-3456-345678901234") },      // ⬅️ Update this
    { "Advisor", Guid.Parse("23456789-2345-2345-2345-234567890123") },     // ⬅️ Update this
    { "Administrator", Guid.Parse("12345678-1234-1234-1234-123456789012") }  // ⬅️ Update this
};
```

---

## Part 6: Grant Graph API Permissions to Managed Identity

The Azure Function needs permission to assign app roles via Microsoft Graph.

### Local Development (Using Azure CLI)

For local testing, sign in with Azure CLI:

```bash
# Sign in to Azure with your admin account
az login --tenant 496527a2-41f8-4297-a979-c916e7255a22 --allow-no-subscriptions

# Verify you're signed in
az account show
```

The `DefaultAzureCredential` in the code will use your Azure CLI credentials locally.

### Production (Using Managed Identity)

When deployed to Azure Functions, grant permissions to the Managed Identity:

1. **Enable Managed Identity** on Function App (already done in execution plan)

2. **Grant Graph API Permissions**:

```powershell
# Connect to Graph as admin
Connect-MgGraph -Scopes "AppRoleAssignment.ReadWrite.All, Application.Read.All"

# Get Function App's Managed Identity
$functionAppName = "func-rajfinancial-dev"
$functionAppObjectId = (Get-AzFunctionApp -Name $functionAppName).IdentityPrincipalId

# Get Microsoft Graph Service Principal
$graphSp = Get-MgServicePrincipal -Filter "appId eq '00000003-0000-0000-c000-000000000000'"

# Get AppRoleAssignment.ReadWrite.All permission
$appRole = $graphSp.AppRoles | Where-Object { $_.Value -eq "AppRoleAssignment.ReadWrite.All" }

# Grant the permission to Managed Identity
New-MgServicePrincipalAppRoleAssignment `
    -ServicePrincipalId $functionAppObjectId `
    -PrincipalId $functionAppObjectId `
    -ResourceId $graphSp.Id `
    -AppRoleId $appRole.Id

Write-Host "✅ Granted AppRoleAssignment.ReadWrite.All to Function App MI"
```

---

## Part 7: Local Testing Steps

### 1. Install .NET 9 SDK

```bash
# Check current version
dotnet --version

# Install .NET 9 if needed
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0
```

### 2. Restore Packages

```bash
cd D:\Code\rajfinancial\src\Api
dotnet restore
```

### 3. Update Configuration

Edit `local.settings.json` and replace placeholders:

```json
{
  "Values": {
    "EntraExternalId:ClientId": "abc123...",  // From app registration
    "EntraExternalId:ServicePrincipalId": "def456..."  // Service principal object ID
  }
}
```

**Get Service Principal ID:**
```powershell
$appId = "<YOUR-APP-CLIENT-ID>"
$sp = Get-MgServicePrincipal -Filter "appId eq '$appId'"
Write-Host "Service Principal ID: $($sp.Id)"
```

### 4. Build the Project

```bash
dotnet build
```

### 5. Run Locally

```bash
# Start the Functions runtime
func start

# You should see:
# Azure Functions Core Tools
# Functions:
#   AssignRoleDuringSignup: [POST] http://localhost:7071/api/auth/assign-role-signup
#   CompleteRoleAssignment: [POST] http://localhost:7071/api/auth/complete-role
#   HealthCheck: [GET] http://localhost:7071/api/health
```

### 6. Test Health Check

```bash
curl http://localhost:7071/api/health

# Expected response:
# {"status":"healthy","service":"RajFinancial API"}
```

### 7. Test Role Assignment (Manual)

**Create a test user first**, then:

```powershell
# Test CompleteRoleAssignment endpoint
$userId = "<TEST-USER-OBJECT-ID>"
$body = @{
    userId = $userId
    role = "Client"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/auth/complete-role" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"

# Expected response:
# {
#   "success": true,
#   "role": "Client"
# }
```

### 8. Verify Role Was Assigned

```powershell
$userId = "<TEST-USER-OBJECT-ID>"

# Get user's app role assignments
Get-MgUserAppRoleAssignment -UserId $userId | Format-Table

# You should see the assigned role
```

---

## Troubleshooting

### Error: "Insufficient permissions to assign app roles"

**Fix**: Ensure your Azure CLI account has admin permissions, or grant Graph API permissions to Managed Identity (see Part 6).

### Error: "Service principal configuration missing"

**Fix**: Update `local.settings.json` with correct `ServicePrincipalId`.

### MFA still prompts for test users

**Fix**: Verify test user is in the "Test Users - No MFA" group:

```powershell
$groupId = "<GROUP-OBJECT-ID>"
Get-MgGroupMember -GroupId $groupId | Select-Object Id, DisplayName
```

### Conditional Access not available

**Issue**: Free tier doesn't support Conditional Access.

**Fix**: Upgrade to Premium P1, or use user flow-level MFA (no test exceptions possible).

---

## Summary

✅ **What We Built:**
1. Security group for test users exempt from MFA
2. Conditional Access policy requiring MFA for everyone except test group
3. Azure Functions to automatically assign roles during signup
4. Local testing setup

✅ **What Works:**
- Test users sign in WITHOUT MFA
- Production users sign in WITH MFA
- Users automatically get correct role based on signup button clicked

✅ **Next Steps:**
1. Configure user flow in Entra portal
2. Add API Connector to user flow
3. Deploy Functions to Azure
4. Test end-to-end signup flow

---

*Last Updated: December 24, 2024*

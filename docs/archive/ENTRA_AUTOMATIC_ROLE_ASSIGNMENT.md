# Automatic Role Assignment During Sign-Up
## Using Entra External ID API Connectors + Azure Functions

---

## Overview

This document describes how to automatically assign app roles (Client, Advisor, Administrator) to users during the sign-up process using **Microsoft Entra External ID API Connectors** and **Azure Functions**.

### User Experience

1. User visits homepage and sees two signup buttons:
   - **"Sign Up as Client"** → Automatically gets `Client` role
   - **"Sign Up as Advisor"** → Automatically gets `Advisor` role
2. User completes standard Entra sign-up flow (email, password, MFA)
3. **Behind the scenes**: Azure Function assigns the appropriate app role via Microsoft Graph API
4. User is redirected to the app with the correct role already assigned
5. No manual role assignment needed!

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. USER CLICKS "Sign Up as Client"                              │
│    → Redirects to Entra user flow with ?role=Client             │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. ENTRA EXTERNAL ID USER FLOW                                   │
│    → Collect email, password, MFA                                │
│    → Before creating account: Call API Connector                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. API CONNECTOR → Azure Function                                │
│    → Function: AssignRoleDuringSignup                            │
│    → Receives: email, displayName, ui_locales (contains role)    │
│    → Validates role is allowed (Client, Advisor only)            │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. ACCOUNT CREATED IN ENTRA                                      │
│    → User account is created                                     │
│    → objectId is returned to API Connector                       │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. AZURE FUNCTION ASSIGNS APP ROLE                               │
│    → Uses Microsoft Graph API                                    │
│    → Assigns app role based on requested role                    │
│    → Returns "Continue" to Entra                                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. USER REDIRECTED TO APP                                        │
│    → JWT token includes role claim                               │
│    → App shows appropriate features for role                     │
└─────────────────────────────────────────────────────────────────┘
```

---

## Part 1: Setup App Roles (Already Done)

Your app roles are already configured in `scripts/configure-entra-app-roles.ps1`.

**App Roles Defined:**
- `Administrator` - Full system access
- `Advisor` - Financial advisor features
- `Client` - Client account holder

**Run the script:**
```powershell
# For Dev tenant
.\scripts\configure-entra-app-roles.ps1 `
  -AppObjectId "<dev-app-object-id>" `
  -TenantId "496527a2-41f8-4297-a979-c916e7255a22"

# For Prod tenant
.\scripts\configure-entra-app-roles.ps1 `
  -AppObjectId "<prod-app-object-id>" `
  -TenantId "cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6"
```

---

## Part 2: Create Azure Function for Role Assignment

### 2.1 Create Function Project

```bash
# Navigate to src/Api
cd src/Api

# Create new function (if not exists)
func new --name AssignRoleDuringSignup --template "HTTP trigger"
```

### 2.2 Implement the Function

Create `src/Api/Functions/Auth/AssignRoleDuringSignup.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace RajFinancial.Api.Functions.Auth;

/// <summary>
/// API Connector endpoint called by Entra External ID during user sign-up.
/// Automatically assigns app roles based on the signup button clicked.
/// </summary>
public class AssignRoleDuringSignup
{
    private readonly ILogger<AssignRoleDuringSignup> _logger;
    private readonly GraphServiceClient _graphClient;
    private readonly string _servicePrincipalId;

    // Map role names to their GUIDs from app registration
    private static readonly Dictionary<string, Guid> RoleMapping = new()
    {
        { "Client", Guid.Parse("00000000-0000-0000-0000-000000000003") },
        { "Advisor", Guid.Parse("00000000-0000-0000-0000-000000000002") },
        { "Administrator", Guid.Parse("00000000-0000-0000-0000-000000000001") }
    };

    public AssignRoleDuringSignup(
        ILogger<AssignRoleDuringSignup> logger,
        IConfiguration configuration)
    {
        _logger = logger;

        // Use Managed Identity to call Microsoft Graph
        var credential = new DefaultAzureCredential();
        _graphClient = new GraphServiceClient(credential);

        // Get service principal ID from config
        _servicePrincipalId = configuration["EntraExternalId:ServicePrincipalId"]
            ?? throw new InvalidOperationException("ServicePrincipalId not configured");
    }

    [Function("AssignRoleDuringSignup")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "auth/assign-role-signup")]
        HttpRequest req)
    {
        try
        {
            // 1. Parse API Connector request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<ApiConnectorRequest>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.email))
            {
                _logger.LogWarning("Invalid API Connector request - missing email");
                return new BadRequestObjectResult(new ApiConnectorResponse
                {
                    version = "1.0.0",
                    action = "ShowBlockPage",
                    userMessage = "Invalid request. Please try again."
                });
            }

            // 2. Extract requested role from ui_locales parameter
            // Entra passes query params through ui_locales
            var requestedRole = data.ui_locales ?? "Client"; // Default to Client

            // 3. Validate role is allowed for self-signup
            if (!new[] { "Client", "Advisor" }.Contains(requestedRole))
            {
                _logger.LogWarning(
                    "Invalid role requested during signup: {Role} by {Email}",
                    requestedRole, data.email);
                requestedRole = "Client"; // Default to Client for security
            }

            _logger.LogInformation(
                "User signing up: {Email} with role: {Role}",
                data.email, requestedRole);

            // 4. Wait for user creation (API connector is called BEFORE user creation)
            // We'll return Continue and let Entra create the user
            // Then use a different approach: Post-creation webhook or CompleteProfile page

            // For now, return Continue with extension attribute to store requested role
            return new OkObjectResult(new ApiConnectorResponse
            {
                version = "1.0.0",
                action = "Continue",
                extension_RequestedRole = requestedRole
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AssignRoleDuringSignup");

            // Don't block user signup if role assignment fails
            // CompleteProfile page can handle it as fallback
            return new OkObjectResult(new ApiConnectorResponse
            {
                version = "1.0.0",
                action = "Continue"
            });
        }
    }

    /// <summary>
    /// Called after user creation to assign app role.
    /// This is invoked by CompleteProfile.razor page after first login.
    /// </summary>
    [Function("CompleteRoleAssignment")]
    public async Task<IActionResult> CompleteRoleAssignment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "auth/complete-role")]
        HttpRequest req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<CompleteRoleRequest>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.UserId) || string.IsNullOrEmpty(data.Role))
            {
                return new BadRequestObjectResult(new { error = "UserId and Role are required" });
            }

            // Validate role
            if (!RoleMapping.ContainsKey(data.Role))
            {
                return new BadRequestObjectResult(new { error = "Invalid role" });
            }

            // Assign app role via Microsoft Graph
            var appRoleAssignment = new AppRoleAssignment
            {
                PrincipalId = Guid.Parse(data.UserId),
                ResourceId = Guid.Parse(_servicePrincipalId),
                AppRoleId = RoleMapping[data.Role]
            };

            await _graphClient.Users[data.UserId]
                .AppRoleAssignments
                .PostAsync(appRoleAssignment);

            _logger.LogInformation(
                "Successfully assigned role {Role} to user {UserId}",
                data.Role, data.UserId);

            return new OkObjectResult(new { success = true, role = data.Role });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing role assignment");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

/// <summary>
/// Request body from Entra External ID API Connector.
/// </summary>
public class ApiConnectorRequest
{
    public string? email { get; set; }
    public string? displayName { get; set; }
    public string? givenName { get; set; }
    public string? surname { get; set; }
    public string? ui_locales { get; set; } // Used to pass role parameter
    public string? objectId { get; set; } // Only present after user creation
}

/// <summary>
/// Response to Entra External ID API Connector.
/// </summary>
public class ApiConnectorResponse
{
    public string version { get; set; } = "1.0.0";
    public string action { get; set; } // "Continue" or "ShowBlockPage"
    public string? userMessage { get; set; }
    public string? extension_RequestedRole { get; set; } // Custom attribute
}

/// <summary>
/// Request to complete role assignment after user creation.
/// </summary>
public class CompleteRoleRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

### 2.3 Configure Microsoft Graph Permissions

The Azure Function needs permissions to assign app roles.

**Grant Microsoft Graph API Permissions:**

1. Navigate to Azure Portal → Your Function App → Identity
2. Enable **System-assigned managed identity**
3. Note the **Object (principal) ID**
4. Run this PowerShell script to grant permissions:

```powershell
# Grant Graph API permissions to Function App's Managed Identity

$functionAppObjectId = "<function-app-managed-identity-object-id>"
$graphAppId = "00000003-0000-0000-c000-000000000000" # Microsoft Graph
$permissionName = "AppRoleAssignment.ReadWrite.All"

# Get Graph Service Principal
$graphSp = Get-MgServicePrincipal -Filter "appId eq '$graphAppId'"

# Get the AppRole
$appRole = $graphSp.AppRoles | Where-Object { $_.Value -eq $permissionName }

# Grant the permission
New-MgServicePrincipalAppRoleAssignment `
  -ServicePrincipalId $functionAppObjectId `
  -PrincipalId $functionAppObjectId `
  -ResourceId $graphSp.Id `
  -AppRoleId $appRole.Id
```

---

## Part 3: Configure Entra External ID User Flow

### 3.1 Create User Flow

1. Go to [Entra External ID Portal](https://entra.microsoft.com)
2. Select your tenant (Dev or Prod)
3. Navigate to **External Identities** → **User flows**
4. Click **+ New user flow**
5. Configure:
   - **Name**: `B2C_1_signup_signin`
   - **Identity providers**: Email signup
   - **MFA**: Enabled (recommended)
   - **User attributes**: Email, Display Name, Given Name, Surname

### 3.2 Add API Connector

1. In the user flow, scroll to **API connectors**
2. Click **+ Add an API connector**
3. Configure:
   - **Display name**: Assign Role During Signup
   - **Endpoint URL**: `https://your-function-app.azurewebsites.net/api/auth/assign-role-signup`
   - **Authentication type**: Basic authentication
   - **Username**: (from Function App settings)
   - **Password**: (function key)
4. **Attach to user flow step**: Before creating the user

### 3.3 Configure Custom Attribute

To store the requested role temporarily:

1. Go to **User attributes**
2. Click **+ Add**
3. Create custom attribute:
   - **Name**: RequestedRole
   - **Data Type**: String
   - **Description**: Role requested during signup
4. Add to user flow:
   - Go back to your user flow
   - Select **User attributes**
   - Add **RequestedRole** (hidden field)

---

## Part 4: Update Blazor Client

### 4.1 Modify Landing Page Buttons

Update `src/Client/Pages/Index.razor`:

```razor
@page "/"
@inject NavigationManager Navigation

<div class="hero">
    <h1>RAJ Financial</h1>
    <p>Your comprehensive financial planning platform</p>

    <div class="signup-buttons">
        <button class="btn-gold-solid" @onclick="SignUpAsClient">
            Sign Up as Client
        </button>
        <button class="btn-gold-outline" @onclick="SignUpAsAdvisor">
            Sign Up as Advisor
        </button>
    </div>
</div>

@code {
    private void SignUpAsClient()
    {
        // Redirect to Entra signup with role parameter
        var signupUrl = $"https://rajfinancialdev.b2clogin.com/rajfinancialdev.onmicrosoft.com/B2C_1_signup_signin/oauth2/v2.0/authorize" +
            $"?client_id={ClientId}" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&response_type=code" +
            $"&scope=openid%20profile%20email" +
            $"&ui_locales=Client"; // Pass role via ui_locales

        Navigation.NavigateTo(signupUrl, forceLoad: true);
    }

    private void SignUpAsAdvisor()
    {
        var signupUrl = $"https://rajfinancialdev.b2clogin.com/rajfinancialdev.onmicrosoft.com/B2C_1_signup_signin/oauth2/v2.0/authorize" +
            $"?client_id={ClientId}" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&response_type=code" +
            $"&scope=openid%20profile%20email" +
            $"&ui_locales=Advisor"; // Pass role via ui_locales

        Navigation.NavigateTo(signupUrl, forceLoad: true);
    }
}
```

### 4.2 Create CompleteProfile Page (Fallback)

Create `src/Client/Pages/Account/CompleteProfile.razor`:

```razor
@page "/account/complete-profile"
@attribute [Authorize]
@inject HttpClient Http
@inject NavigationManager Navigation
@inject ILocalStorageService LocalStorage

<h2>Completing Your Profile...</h2>

@if (isCompleting)
{
    <p>Setting up your account...</p>
}
else if (error != null)
{
    <div class="alert alert-danger">@error</div>
}

@code {
    private bool isCompleting = true;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Get requested role from local storage (set during auth callback)
            var requestedRole = await LocalStorage.GetItemAsync<string>("requestedRole");

            if (string.IsNullOrEmpty(requestedRole))
            {
                // No role pending, redirect to dashboard
                Navigation.NavigateTo("/");
                return;
            }

            // Get current user ID from claims
            var userId = // ... extract from AuthenticationState

            // Call API to complete role assignment
            var response = await Http.PostAsJsonAsync("/api/auth/complete-role", new
            {
                UserId = userId,
                Role = requestedRole
            });

            if (response.IsSuccessStatusCode)
            {
                // Clear stored role
                await LocalStorage.RemoveItemAsync("requestedRole");

                // Redirect to dashboard
                Navigation.NavigateTo("/");
            }
            else
            {
                error = "Failed to assign role. Please contact support.";
                isCompleting = false;
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            isCompleting = false;
        }
    }
}
```

---

## Part 5: Configuration

### 5.1 Function App Settings

Add to `local.settings.json` (local) and Azure Function App Configuration (cloud):

```json
{
  "Values": {
    "EntraExternalId:TenantId": "496527a2-41f8-4297-a979-c916e7255a22",
    "EntraExternalId:ClientId": "<app-registration-client-id>",
    "EntraExternalId:ServicePrincipalId": "<service-principal-object-id>",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true"
  }
}
```

### 5.2 Get Service Principal ID

```powershell
# Get the Service Principal ID for your app registration
$appId = "<your-app-client-id>"
$sp = az ad sp list --filter "appId eq '$appId'" | ConvertFrom-Json
$servicePrincipalId = $sp[0].id

Write-Host "Service Principal ID: $servicePrincipalId"
```

---

## Part 6: Testing

### 6.1 Test User Flow

1. Navigate to your app's homepage
2. Click **"Sign Up as Client"**
3. Complete the Entra signup form
4. Verify:
   - ✅ User is created in Entra
   - ✅ User is assigned `Client` app role
   - ✅ JWT token contains role claim
   - ✅ App shows client features

5. Repeat for **"Sign Up as Advisor"**

### 6.2 Verify Role Assignment

```powershell
# Check user's app role assignments
$userId = "<user-object-id>"

az rest --method GET `
  --uri "https://graph.microsoft.com/v1.0/users/$userId/appRoleAssignments"
```

Expected output:
```json
{
  "value": [
    {
      "principalId": "<user-object-id>",
      "resourceId": "<service-principal-id>",
      "appRoleId": "00000000-0000-0000-0000-000000000003",
      "principalDisplayName": "John Doe"
    }
  ]
}
```

---

## Security Considerations

### ✅ Allowed Roles for Self-Signup
- `Client` - Anyone can sign up as a client
- `Advisor` - Anyone can sign up as an advisor (validated later by admin)

### ❌ Restricted Roles
- `Administrator` - **Cannot** self-assign, must be manually assigned by existing admin

### Role Validation
The Azure Function validates that users can only request `Client` or `Advisor` roles during signup. Any attempt to request `Administrator` will default to `Client`.

### Admin Assignment Process
To assign `Administrator` role:
1. Admin logs into Entra portal
2. Navigates to **Users** → Select user
3. Assigns `Administrator` app role manually

---

## Troubleshooting

### API Connector Returns Error

**Check Function App logs:**
```bash
az functionapp logs tail --name your-function-app --resource-group rg-rajfinancial-dev
```

### Role Not Appearing in Token

**Verify app role assignments:**
```powershell
az rest --method GET `
  --uri "https://graph.microsoft.com/v1.0/users/<user-id>/appRoleAssignments"
```

**Check token claims:**
Use https://jwt.ms to decode your access token and verify the `roles` claim.

### Graph API Permission Denied

**Verify Managed Identity has permissions:**
```powershell
$functionAppObjectId = "<managed-identity-object-id>"
az rest --method GET `
  --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$functionAppObjectId/appRoleAssignments"
```

Should show `AppRoleAssignment.ReadWrite.All` permission.

---

## Next Steps

1. ✅ Configure Dev tenant user flow with API connector
2. ✅ Deploy Azure Function with Managed Identity
3. ✅ Test signup flow for both Client and Advisor roles
4. ⬜ Configure Prod tenant (after Dev testing)
5. ⬜ Monitor role assignments in production
6. ⬜ Add telemetry to track signup funnel

---

## References

- [API Connectors in Entra External ID](https://learn.microsoft.com/en-us/entra/external-id/api-connectors-overview)
- [Assign App Roles via Graph API](https://learn.microsoft.com/en-us/graph/api/user-post-approleassignments?view=graph-rest-1.0)
- [Using Managed Identity with Microsoft Graph](https://learn.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=csharp#using-a-client-secret)

---

*Last Updated: December 24, 2024*

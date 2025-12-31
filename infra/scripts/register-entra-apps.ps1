# ============================================================================
# RAJ Financial - Entra External ID App Registration Script
# ============================================================================
# This script registers the SPA (Blazor WASM) and API (Azure Functions) 
# applications in Microsoft Entra External ID.
#
# Prerequisites:
#   - Azure CLI installed (az --version)
#   - Microsoft Graph CLI extension (az extension add --name microsoft-graph)
#   - Logged into the correct tenant (az login --tenant <tenant-id>)
#
# Usage:
#   .\register-entra-apps.ps1 -Environment dev
#   .\register-entra-apps.ps1 -Environment prod
#
# Role Model:
#   - Client: Standard user who owns their data and can grant access to others
#   - Administrator: Platform staff with system-wide access
#
# Fine-grained access control (spouses, attorneys, CPAs viewing client data)
# is handled through DataAccessGrant entities, NOT through app roles.
# ============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment
)

# ============================================================================
# Configuration
# ============================================================================

# Role GUIDs - these MUST match the values in appsettings.json / local.settings.json
# Dev and Prod use different GUIDs for complete environment isolation
$roleGuids = @{
    dev = @{
        Client = "bc34bd6c-38b8-46a6-9d4c-d338afeea81f"
        Administrator = "2202014c-e4b9-4ab9-9e6a-4cc53e13598f"
    }
    prod = @{
        # Production GUIDs - generate new ones for production
        # Run: [guid]::NewGuid().ToString() in PowerShell to generate
        Client = "00000000-0000-0000-0000-000000000001"  # TODO: Generate for prod
        Administrator = "00000000-0000-0000-0000-000000000002"  # TODO: Generate for prod
    }
}

$config = @{
    dev = @{
        TenantId = "496527a2-41f8-4297-a979-c916e7255a22"
        TenantDomain = "rajfinancialdev.onmicrosoft.com"
        SpaName = "rajfinancial-spa-dev"
        ApiName = "rajfinancial-api-dev"
        SpaRedirectUris = @(
            "https://localhost:5001/authentication/login-callback",
            "https://localhost:7001/authentication/login-callback",
            "http://localhost:5000/authentication/login-callback"
        )
        SpaLogoutUri = "https://localhost:5001/authentication/logout-callback"
        ApiAppIdUri = "api://rajfinancial-api-dev"
        RoleGuids = $roleGuids.dev
    }
    prod = @{
        TenantId = "cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6"
        TenantDomain = "rajfinancialprod.onmicrosoft.com"
        SpaName = "rajfinancial-spa"
        ApiName = "rajfinancial-api"
        SpaRedirectUris = @(
            "https://rajfinancial.com/authentication/login-callback",
            "https://app.rajfinancial.net/authentication/login-callback"
        )
        SpaLogoutUri = "https://app.rajfinancial.net/authentication/logout-callback"
        ApiAppIdUri = "api://rajfinancial-api"
        RoleGuids = $roleGuids.prod
    }
}

$envConfig = $config[$Environment]

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " RAJ Financial - Entra App Registration" -ForegroundColor Cyan
Write-Host " Environment: $Environment" -ForegroundColor Cyan
Write-Host " Tenant: $($envConfig.TenantDomain)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Pre-flight Checks
# ============================================================================

Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check if logged into Azure CLI
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Error "Not logged into Azure CLI. Run: az login --tenant $($envConfig.TenantId)"
    exit 1
}

# Check if logged into correct tenant
if ($account.tenantId -ne $envConfig.TenantId) {
    Write-Warning "Currently logged into tenant: $($account.tenantId)"
    Write-Host "Switching to tenant: $($envConfig.TenantId)" -ForegroundColor Yellow
    az login --tenant $envConfig.TenantId --allow-no-subscriptions
}

Write-Host "Prerequisites OK" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Step 1: Register API Application
# ============================================================================

Write-Host "Step 1: Registering API Application ($($envConfig.ApiName))..." -ForegroundColor Yellow

# Define API permissions (scopes) that the API will expose
$apiOAuth2PermissionScopes = @(
    @{
        adminConsentDescription = "Allows the app to access RAJ Financial API on behalf of the signed-in user"
        adminConsentDisplayName = "Access RAJ Financial API"
        id = [guid]::NewGuid().ToString()
        isEnabled = $true
        type = "User"
        userConsentDescription = "Allow the application to access RAJ Financial API on your behalf"
        userConsentDisplayName = "Access RAJ Financial API"
        value = "user_impersonation"
    },
    @{
        adminConsentDescription = "Allows the app to read financial accounts"
        adminConsentDisplayName = "Read Accounts"
        id = [guid]::NewGuid().ToString()
        isEnabled = $true
        type = "User"
        userConsentDescription = "Allow the application to read your financial accounts"
        userConsentDisplayName = "Read your accounts"
        value = "Accounts.Read"
    },
    @{
        adminConsentDescription = "Allows the app to read and write financial accounts"
        adminConsentDisplayName = "Read and Write Accounts"
        id = [guid]::NewGuid().ToString()
        isEnabled = $true
        type = "User"
        userConsentDescription = "Allow the application to read and modify your financial accounts"
        userConsentDisplayName = "Manage your accounts"
        value = "Accounts.ReadWrite"
    }
) | ConvertTo-Json -Depth 10 -Compress

# Define App Roles
# NOTE: These GUIDs MUST match the values in appsettings.json and local.settings.json
# The simplified two-role model:
#   - Client: Standard user who owns their data
#   - Administrator: Platform staff with system-wide access
# Fine-grained access (spouses, attorneys, CPAs) is handled via DataAccessGrant entities
$apiAppRoles = @(
    @{
        allowedMemberTypes = @("User")
        description = "Standard user who owns their financial data and can grant access to others (spouses, CPAs, attorneys, etc.)"
        displayName = "Client"
        id = $envConfig.RoleGuids.Client
        isEnabled = $true
        value = "Client"
    },
    @{
        allowedMemberTypes = @("User")
        description = "Platform administrator with system-wide access for user management and support"
        displayName = "Administrator"
        id = $envConfig.RoleGuids.Administrator
        isEnabled = $true
        value = "Administrator"
    }
) | ConvertTo-Json -Depth 10 -Compress

# Create API app registration
$apiAppJson = @{
    displayName = $envConfig.ApiName
    signInAudience = "AzureADMyOrg"
    api = @{
        oauth2PermissionScopes = $apiOAuth2PermissionScopes | ConvertFrom-Json
        requestedAccessTokenVersion = 2
    }
    appRoles = $apiAppRoles | ConvertFrom-Json
} | ConvertTo-Json -Depth 10

$apiApp = az rest `
    --method POST `
    --uri "https://graph.microsoft.com/v1.0/applications" `
    --headers "Content-Type=application/json" `
    --body $apiAppJson 2>$null | ConvertFrom-Json

if (-not $apiApp) {
    # App might already exist, try to find it
    Write-Host "API app may already exist, searching..." -ForegroundColor Yellow
    $existingApps = az rest `
        --method GET `
        --uri "https://graph.microsoft.com/v1.0/applications?\`$filter=displayName eq '$($envConfig.ApiName)'" `
        2>$null | ConvertFrom-Json
    
    if ($existingApps.value.Count -gt 0) {
        $apiApp = $existingApps.value[0]
        Write-Host "Found existing API app: $($apiApp.appId)" -ForegroundColor Green
        
        # Update the app roles to ensure they match
        Write-Host "Updating app roles..." -ForegroundColor Yellow
        az rest `
            --method PATCH `
            --uri "https://graph.microsoft.com/v1.0/applications/$($apiApp.id)" `
            --headers "Content-Type=application/json" `
            --body "{`"appRoles`": $apiAppRoles}" 2>$null
        Write-Host "App roles updated" -ForegroundColor Green
    } else {
        Write-Error "Failed to create or find API application"
        exit 1
    }
} else {
    Write-Host "Created API app: $($apiApp.appId)" -ForegroundColor Green
}

# Set the Application ID URI
$apiIdentifierUri = $envConfig.ApiAppIdUri
az rest `
    --method PATCH `
    --uri "https://graph.microsoft.com/v1.0/applications/$($apiApp.id)" `
    --headers "Content-Type=application/json" `
    --body "{`"identifierUris`": [`"$apiIdentifierUri`"]}" 2>$null

Write-Host "Set API identifier URI: $apiIdentifierUri" -ForegroundColor Green

# Create Service Principal for API app
$apiSpExists = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals?\`$filter=appId eq '$($apiApp.appId)'" `
    2>$null | ConvertFrom-Json

if ($apiSpExists.value.Count -eq 0) {
    $apiSp = az rest `
        --method POST `
        --uri "https://graph.microsoft.com/v1.0/servicePrincipals" `
        --headers "Content-Type=application/json" `
        --body "{`"appId`": `"$($apiApp.appId)`"}" 2>$null | ConvertFrom-Json
    Write-Host "Created Service Principal for API app" -ForegroundColor Green
    Write-Host "  Service Principal ID: $($apiSp.id)" -ForegroundColor Gray
} else {
    Write-Host "Service Principal already exists: $($apiSpExists.value[0].id)" -ForegroundColor Green
}

Write-Host ""

# ============================================================================
# Step 2: Register SPA Application
# ============================================================================

Write-Host "Step 2: Registering SPA Application ($($envConfig.SpaName))..." -ForegroundColor Yellow

# Get the user_impersonation scope ID from the API app
$userImpersonationScope = ($apiApp.api.oauth2PermissionScopes | Where-Object { $_.value -eq "user_impersonation" }).id

# Create SPA app registration
$spaRedirectUrisJson = $envConfig.SpaRedirectUris | ConvertTo-Json -Compress

$spaAppJson = @{
    displayName = $envConfig.SpaName
    signInAudience = "AzureADMyOrg"
    spa = @{
        redirectUris = $envConfig.SpaRedirectUris
    }
    requiredResourceAccess = @(
        @{
            resourceAppId = $apiApp.appId
            resourceAccess = @(
                @{
                    id = $userImpersonationScope
                    type = "Scope"
                }
            )
        },
        @{
            # Microsoft Graph - openid, profile, email, offline_access
            resourceAppId = "00000003-0000-0000-c000-000000000000"
            resourceAccess = @(
                @{ id = "37f7f235-527c-4136-accd-4a02d197296e"; type = "Scope" },  # openid
                @{ id = "14dad69e-099b-42c9-810b-d002981feec1"; type = "Scope" },  # profile
                @{ id = "64a6cdd6-aab1-4aaf-94b8-3cc8405e90d0"; type = "Scope" },  # email
                @{ id = "7427e0e9-2fba-42fe-b0c0-848c9e6a8182"; type = "Scope" }   # offline_access
            )
        }
    )
} | ConvertTo-Json -Depth 10

$spaApp = az rest `
    --method POST `
    --uri "https://graph.microsoft.com/v1.0/applications" `
    --headers "Content-Type=application/json" `
    --body $spaAppJson 2>$null | ConvertFrom-Json

if (-not $spaApp) {
    # App might already exist, try to find it
    Write-Host "SPA app may already exist, searching..." -ForegroundColor Yellow
    $existingApps = az rest `
        --method GET `
        --uri "https://graph.microsoft.com/v1.0/applications?\`$filter=displayName eq '$($envConfig.SpaName)'" `
        2>$null | ConvertFrom-Json
    
    if ($existingApps.value.Count -gt 0) {
        $spaApp = $existingApps.value[0]
        Write-Host "Found existing SPA app: $($spaApp.appId)" -ForegroundColor Green
    } else {
        Write-Error "Failed to create or find SPA application"
        exit 1
    }
} else {
    Write-Host "Created SPA app: $($spaApp.appId)" -ForegroundColor Green
}

# Create Service Principal for SPA app
$spaSpExists = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals?\`$filter=appId eq '$($spaApp.appId)'" `
    2>$null | ConvertFrom-Json

if ($spaSpExists.value.Count -eq 0) {
    az rest `
        --method POST `
        --uri "https://graph.microsoft.com/v1.0/servicePrincipals" `
        --headers "Content-Type=application/json" `
        --body "{`"appId`": `"$($spaApp.appId)`"}" 2>$null
    Write-Host "Created Service Principal for SPA app" -ForegroundColor Green
}

Write-Host ""

# ============================================================================
# Step 3: Output Configuration
# ============================================================================

Write-Host "============================================" -ForegroundColor Green
Write-Host " Registration Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "API Application:" -ForegroundColor Cyan
Write-Host "  Display Name: $($envConfig.ApiName)"
Write-Host "  Application (Client) ID: $($apiApp.appId)"
Write-Host "  Object ID: $($apiApp.id)"
Write-Host "  Identifier URI: $apiIdentifierUri"
Write-Host ""
Write-Host "  App Roles:" -ForegroundColor Yellow
Write-Host "    Client:        $($envConfig.RoleGuids.Client)"
Write-Host "    Administrator: $($envConfig.RoleGuids.Administrator)"
Write-Host ""
Write-Host "SPA Application:" -ForegroundColor Cyan
Write-Host "  Display Name: $($envConfig.SpaName)"
Write-Host "  Application (Client) ID: $($spaApp.appId)"
Write-Host "  Object ID: $($spaApp.id)"
Write-Host ""

# ============================================================================
# Step 4: Generate Configuration Files
# ============================================================================

Write-Host "Generating configuration snippets..." -ForegroundColor Yellow
Write-Host ""

$clientConfig = @"
// src/Client/wwwroot/appsettings.$Environment.json
{
  "AzureAd": {
    "Authority": "https://$($envConfig.TenantDomain.Replace('.onmicrosoft.com', '')).ciamlogin.com/",
    "ClientId": "$($spaApp.appId)",
    "ValidateAuthority": true
  },
  "ApiScopes": {
    "Default": "$apiIdentifierUri/user_impersonation"
  },
  "ApiBaseUrl": "https://localhost:7071/api"
}
"@

$apiConfig = @"
// src/Api/appsettings.$Environment.json
{
  "AzureAdB2C": {
    "Instance": "https://$($envConfig.TenantDomain.Replace('.onmicrosoft.com', '')).ciamlogin.com/",
    "Domain": "$($envConfig.TenantDomain)",
    "TenantId": "$($envConfig.TenantId)",
    "ClientId": "$($apiApp.appId)",
    "Scopes": "openid profile email offline_access"
  },
  "AppRoles": {
    "Client": "$($envConfig.RoleGuids.Client)",
    "Administrator": "$($envConfig.RoleGuids.Administrator)"
  },
  "EntraExternalId": {
    "ServicePrincipalId": "<service-principal-id-from-above>"
  }
}
"@

Write-Host "Client Configuration:" -ForegroundColor Cyan
Write-Host $clientConfig
Write-Host ""
Write-Host "API Configuration:" -ForegroundColor Cyan
Write-Host $apiConfig
Write-Host ""

# Save to output file
$outputPath = Join-Path $PSScriptRoot "entra-config-$Environment.json"
$output = @{
    environment = $Environment
    tenantId = $envConfig.TenantId
    tenantDomain = $envConfig.TenantDomain
    api = @{
        displayName = $envConfig.ApiName
        appId = $apiApp.appId
        objectId = $apiApp.id
        identifierUri = $apiIdentifierUri
        roles = @{
            Client = $envConfig.RoleGuids.Client
            Administrator = $envConfig.RoleGuids.Administrator
        }
    }
    spa = @{
        displayName = $envConfig.SpaName
        appId = $spaApp.appId
        objectId = $spaApp.id
    }
} | ConvertTo-Json -Depth 5

$output | Out-File -FilePath $outputPath -Encoding utf8
Write-Host "Configuration saved to: $outputPath" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Step 5: Admin Consent Reminder
# ============================================================================

Write-Host "============================================" -ForegroundColor Yellow
Write-Host " IMPORTANT: Next Steps" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Update your configuration files with the generated values above"
Write-Host ""
Write-Host "2. Grant admin consent for API permissions:"
Write-Host "   Go to Azure Portal -> Entra External ID -> App registrations"
Write-Host "   -> $($envConfig.SpaName) -> API permissions -> Grant admin consent"
Write-Host ""
Write-Host "   Or run this command:" -ForegroundColor Cyan
Write-Host "   az ad app permission admin-consent --id $($spaApp.appId)"
Write-Host ""
Write-Host "3. Update the ServicePrincipalId in your API configuration:"
Write-Host "   Get it from the API app's Enterprise Application (Service Principal)"
Write-Host ""
Write-Host "4. The app roles are:" -ForegroundColor Cyan
Write-Host "   - Client: For standard users who own their financial data"
Write-Host "   - Administrator: For platform staff"
Write-Host ""
Write-Host "   Note: Fine-grained access (spouses, CPAs, attorneys viewing client data)"
Write-Host "   is handled through DataAccessGrant entities in the database,"
Write-Host "   NOT through additional app roles."
Write-Host ""


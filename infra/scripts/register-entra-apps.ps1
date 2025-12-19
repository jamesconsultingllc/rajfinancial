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
    }
    prod = @{
        TenantId = "cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6"
        TenantDomain = "rajfinancialprod.onmicrosoft.com"
        SpaName = "rajfinancial-spa"
        ApiName = "rajfinancial-api"
        SpaRedirectUris = @(
            "https://rajfinancial.com/authentication/login-callback",
            "https://www.rajfinancial.com/authentication/login-callback"
        )
        SpaLogoutUri = "https://rajfinancial.com/authentication/logout-callback"
        ApiAppIdUri = "api://rajfinancial-api"
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
$apiAppRoles = @(
    @{
        allowedMemberTypes = @("User")
        description = "Primary consumer account holder with full access to own data"
        displayName = "User"
        id = [guid]::NewGuid().ToString()
        isEnabled = $true
        value = "user"
    },
    @{
        allowedMemberTypes = @("User")
        description = "Financial advisor with read access to assigned clients"
        displayName = "Advisor"
        id = [guid]::NewGuid().ToString()
        isEnabled = $true
        value = "advisor"
    },
    @{
        allowedMemberTypes = @("User")
        description = "Estate planning attorney with access to beneficiary data"
        displayName = "Attorney"
        id = [guid]::NewGuid().ToString()
        isEnabled = $true
        value = "attorney"
    },
    @{
        allowedMemberTypes = @("User")
        description = "CPA/Tax professional with access to financial data"
        displayName = "Accountant"
        id = [guid]::NewGuid().ToString()
        isEnabled = $true
        value = "accountant"
    },
    @{
        allowedMemberTypes = @("User")
        description = "Platform administrator with full access"
        displayName = "Admin"
        id = [guid]::NewGuid().ToString()
        isEnabled = $true
        value = "admin"
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
    } else {
        Write-Error "Failed to create or find API application"
        exit 1
    }
} else {
    Write-Host "Created API app: $($apiApp.appId)" -ForegroundColor Green
}

# Set the Application ID URI
$apiIdentifierUri = "$($envConfig.ApiAppIdUri)"
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
    az rest `
        --method POST `
        --uri "https://graph.microsoft.com/v1.0/servicePrincipals" `
        --headers "Content-Type=application/json" `
        --body "{`"appId`": `"$($apiApp.appId)`"}" 2>$null
    Write-Host "Created Service Principal for API app" -ForegroundColor Green
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
// src/Client/appsettings.$Environment.json
{
  "AzureAdB2C": {
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
// src/Api/appsettings.$Environment.json (AzureAdB2C section)
{
  "AzureAdB2C": {
    "Instance": "https://$($envConfig.TenantDomain.Replace('.onmicrosoft.com', '')).ciamlogin.com/",
    "Domain": "$($envConfig.TenantDomain)",
    "TenantId": "$($envConfig.TenantId)",
    "ClientId": "$($apiApp.appId)",
    "Scopes": "openid profile email offline_access"
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
Write-Host " IMPORTANT: Admin Consent Required" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "You may need to grant admin consent for the API permissions."
Write-Host "Go to Azure Portal -> Entra External ID -> App registrations"
Write-Host "-> $($envConfig.SpaName) -> API permissions -> Grant admin consent"
Write-Host ""
Write-Host "Or run this command:" -ForegroundColor Cyan
Write-Host "az ad app permission admin-consent --id $($spaApp.appId)"
Write-Host ""


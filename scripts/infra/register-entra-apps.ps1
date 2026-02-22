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
    [string]$Environment,

    [Parameter(Mandatory = $false)]
    [string]$KeyVaultName,

    [Parameter(Mandatory = $false)]
    [string]$AzureSubscriptionId,

    [Parameter(Mandatory = $false)]
    [switch]$UpdateBicepParams,

    [Parameter(Mandatory = $false)]
    [switch]$SkipKeyVault
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
        # Production GUIDs - stable values for production environment
        Client = "d4e5f6a7-b8c9-4d5e-a6b7-c8d9e0f1a2b3"
        Administrator = "1a2b3c4d-5e6f-4a5b-8c9d-0e1f2a3b4c5d"
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
        UserFlowName = "SignUp_SignIn"
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
        UserFlowName = "SignUp_SignIn"
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

# Check if API app already exists
$existingApiApps = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/applications?`$filter=displayName eq '$($envConfig.ApiName)'" `
    2>$null | ConvertFrom-Json

$apiAppExists = $existingApiApps.value.Count -gt 0
$existingApiApp = if ($apiAppExists) { $existingApiApps.value[0] } else { $null }

# Define stable scope GUIDs (same across re-runs, different per environment)
# These are derived deterministically so they're stable
$scopeGuids = @{
    dev = @{
        user_impersonation = "e1d2c3b4-a5f6-4789-8901-234567890abc"
        Accounts_Read = "f2e3d4c5-b6a7-4890-9012-345678901bcd"
        Accounts_ReadWrite = "a3b4c5d6-c7d8-4901-0123-456789012cde"
    }
    prod = @{
        user_impersonation = "11111111-2222-3333-4444-555566667777"
        Accounts_Read = "22222222-3333-4444-5555-666677778888"
        Accounts_ReadWrite = "33333333-4444-5555-6666-777788889999"
    }
}

$envScopeGuids = $scopeGuids[$Environment]

# If app exists, preserve existing scope IDs to avoid breaking existing tokens
if ($apiAppExists -and $existingApiApp.api.oauth2PermissionScopes) {
    foreach ($scope in $existingApiApp.api.oauth2PermissionScopes) {
        $key = $scope.value -replace '\.', '_'
        if ($envScopeGuids.ContainsKey($key)) {
            $envScopeGuids[$key] = $scope.id
            Write-Host "  Preserving existing scope ID for $($scope.value): $($scope.id)" -ForegroundColor Gray
        }
    }
}

# Define API permissions (scopes) that the API will expose
$apiOAuth2PermissionScopes = @(
    @{
        adminConsentDescription = "Allows the app to access RAJ Financial API on behalf of the signed-in user"
        adminConsentDisplayName = "Access RAJ Financial API"
        id = $envScopeGuids.user_impersonation
        isEnabled = $true
        type = "User"
        userConsentDescription = "Allow the application to access RAJ Financial API on your behalf"
        userConsentDisplayName = "Access RAJ Financial API"
        value = "user_impersonation"
    },
    @{
        adminConsentDescription = "Allows the app to read financial accounts"
        adminConsentDisplayName = "Read Accounts"
        id = $envScopeGuids.Accounts_Read
        isEnabled = $true
        type = "User"
        userConsentDescription = "Allow the application to read your financial accounts"
        userConsentDisplayName = "Read your accounts"
        value = "Accounts.Read"
    },
    @{
        adminConsentDescription = "Allows the app to read and write financial accounts"
        adminConsentDisplayName = "Read and Write Accounts"
        id = $envScopeGuids.Accounts_ReadWrite
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

# Create or update API app registration
if ($apiAppExists) {
    # App exists - update it
    $apiApp = $existingApiApp
    Write-Host "Found existing API app: $($apiApp.appId)" -ForegroundColor Green
    
    # Update app roles and OAuth2 scopes
    Write-Host "Updating app roles and OAuth2 scopes..." -ForegroundColor Yellow
    
    $updateBody = @{
        appRoles = $apiAppRoles | ConvertFrom-Json
        api = @{
            oauth2PermissionScopes = $apiOAuth2PermissionScopes | ConvertFrom-Json
            requestedAccessTokenVersion = 2
        }
    } | ConvertTo-Json -Depth 10
    
    # Write to temp file to avoid PowerShell JSON escaping issues with az rest --body
    $updateTempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($updateTempFile, $updateBody)
    
    try {
        az rest `
            --method PATCH `
            --uri "https://graph.microsoft.com/v1.0/applications/$($apiApp.id)" `
            --headers "Content-Type=application/json" `
            --body "@$updateTempFile" 2>$null
    } finally {
        Remove-Item $updateTempFile -ErrorAction SilentlyContinue
    }
    
    Write-Host "App roles and OAuth2 scopes updated" -ForegroundColor Green
} else {
    # Create new app
    $apiAppJson = @{
        displayName = $envConfig.ApiName
        signInAudience = "AzureADMyOrg"
        api = @{
            oauth2PermissionScopes = $apiOAuth2PermissionScopes | ConvertFrom-Json
            requestedAccessTokenVersion = 2
        }
        appRoles = $apiAppRoles | ConvertFrom-Json
    } | ConvertTo-Json -Depth 10

    # Write to temp file to avoid PowerShell JSON escaping issues with az rest --body
    $createTempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($createTempFile, $apiAppJson)
    
    try {
        $apiApp = az rest `
            --method POST `
            --uri "https://graph.microsoft.com/v1.0/applications" `
            --headers "Content-Type=application/json" `
            --body "@$createTempFile" 2>$null | ConvertFrom-Json
    } finally {
        Remove-Item $createTempFile -ErrorAction SilentlyContinue
    }

    if (-not $apiApp) {
        Write-Error "Failed to create API application"
        exit 1
    }
    Write-Host "Created API app: $($apiApp.appId)" -ForegroundColor Green
}

# Set the Application ID URI
$apiIdentifierUri = $envConfig.ApiAppIdUri
$uriBody = @{ identifierUris = @($apiIdentifierUri) } | ConvertTo-Json -Compress
$uriBodyFile = [System.IO.Path]::GetTempFileName()
[System.IO.File]::WriteAllText($uriBodyFile, $uriBody)
az rest `
    --method PATCH `
    --uri "https://graph.microsoft.com/v1.0/applications/$($apiApp.id)" `
    --headers "Content-Type=application/json" `
    --body "@$uriBodyFile" 2>$null
Remove-Item $uriBodyFile -ErrorAction SilentlyContinue

Write-Host "Set API identifier URI: $apiIdentifierUri" -ForegroundColor Green

# Create Service Principal for API app
$apiSpExists = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals?`$filter=appId eq '$($apiApp.appId)'" `
    2>$null | ConvertFrom-Json

if ($apiSpExists.value.Count -eq 0) {
    $apiSpBody = @{ appId = $apiApp.appId } | ConvertTo-Json -Compress
    $apiSpBodyFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($apiSpBodyFile, $apiSpBody)
    $apiSp = az rest `
        --method POST `
        --uri "https://graph.microsoft.com/v1.0/servicePrincipals" `
        --headers "Content-Type=application/json" `
        --body "@$apiSpBodyFile" 2>$null | ConvertFrom-Json
    Remove-Item $apiSpBodyFile -ErrorAction SilentlyContinue
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

# Check if SPA app already exists
$existingSpaApps = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/applications?`$filter=displayName eq '$($envConfig.SpaName)'" `
    2>$null | ConvertFrom-Json

$spaAppExists = $existingSpaApps.value.Count -gt 0

# Define the SPA app body (used for both create and update)
$spaAppBody = @{
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

if ($spaAppExists) {
    # Update existing app
    $spaApp = $existingSpaApps.value[0]
    Write-Host "Found existing SPA app: $($spaApp.appId)" -ForegroundColor Green
    Write-Host "Updating SPA app configuration..." -ForegroundColor Yellow

    $spaUpdateFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($spaUpdateFile, $spaAppBody)

    try {
        az rest `
            --method PATCH `
            --uri "https://graph.microsoft.com/v1.0/applications/$($spaApp.id)" `
            --headers "Content-Type=application/json" `
            --body "@$spaUpdateFile" 2>$null
    } finally {
        Remove-Item $spaUpdateFile -ErrorAction SilentlyContinue
    }

    Write-Host "SPA app configuration updated" -ForegroundColor Green
} else {
    # Create new app
    $spaCreateFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($spaCreateFile, $spaAppBody)

    try {
        $spaApp = az rest `
            --method POST `
            --uri "https://graph.microsoft.com/v1.0/applications" `
            --headers "Content-Type=application/json" `
            --body "@$spaCreateFile" 2>$null | ConvertFrom-Json
    } finally {
        Remove-Item $spaCreateFile -ErrorAction SilentlyContinue
    }

    if (-not $spaApp) {
        Write-Error "Failed to create SPA application"
        exit 1
    }
    Write-Host "Created SPA app: $($spaApp.appId)" -ForegroundColor Green
}

# Create Service Principal for SPA app
$spaSpExists = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals?`$filter=appId eq '$($spaApp.appId)'" `
    2>$null | ConvertFrom-Json

if ($spaSpExists.value.Count -eq 0) {
    $spaSpBody = @{ appId = $spaApp.appId } | ConvertTo-Json -Compress
    $spaSpBodyFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($spaSpBodyFile, $spaSpBody)
    az rest `
        --method POST `
        --uri "https://graph.microsoft.com/v1.0/servicePrincipals" `
        --headers "Content-Type=application/json" `
        --body "@$spaSpBodyFile" 2>$null
    Remove-Item $spaSpBodyFile -ErrorAction SilentlyContinue
    Write-Host "Created Service Principal for SPA app" -ForegroundColor Green
}

Write-Host ""

# ============================================================================
# Step 2.5: Link SPA Application to User Flow
# ============================================================================
# The SPA app must be explicitly added to the user flow's includeApplications
# list. Without this, users get AADSTS7500529 when attempting sign-in.
# Redirect URIs on the app registration alone are NOT sufficient.
# ============================================================================

Write-Host "Step 2.5: Linking SPA app to user flow ($($envConfig.UserFlowName))..." -ForegroundColor Yellow

# Find the user flow by display name
$userFlows = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/beta/identity/authenticationEventsFlows" `
    2>$null | ConvertFrom-Json

$targetFlow = $userFlows.value | Where-Object { $_.displayName -eq $envConfig.UserFlowName }

if (-not $targetFlow) {
    Write-Host "WARNING: User flow '$($envConfig.UserFlowName)' not found. Create it in the Entra portal first, then re-run this script." -ForegroundColor Red
    Write-Host "Sign-in will NOT work until the SPA app is linked to a user flow." -ForegroundColor Red
} else {
    $flowId = $targetFlow.id
    Write-Host "Found user flow: $($targetFlow.displayName) (ID: $flowId)" -ForegroundColor Green

    # Check if SPA app is already linked
    $linkedApps = az rest `
        --method GET `
        --uri "https://graph.microsoft.com/beta/identity/authenticationEventsFlows/$flowId/conditions/applications/includeApplications" `
        2>$null | ConvertFrom-Json

    $alreadyLinked = $linkedApps.value | Where-Object { $_.appId -eq $spaApp.appId }

    if ($alreadyLinked) {
        Write-Host "SPA app is already linked to user flow '$($envConfig.UserFlowName)'" -ForegroundColor Green
    } else {
        Write-Host "Adding SPA app ($($spaApp.appId)) to user flow..." -ForegroundColor Yellow

        $linkBody = @{
            "@odata.type" = "#microsoft.graph.authenticationConditionApplication"
            appId = $spaApp.appId
        } | ConvertTo-Json -Compress
        $linkTempFile = [System.IO.Path]::GetTempFileName()
        [System.IO.File]::WriteAllText($linkTempFile, $linkBody)

        try {
            az rest `
                --method POST `
                --uri "https://graph.microsoft.com/beta/identity/authenticationEventsFlows/$flowId/conditions/applications/includeApplications" `
                --headers "Content-Type=application/json" `
                --body "@$linkTempFile" 2>$null | Out-Null

            Write-Host "Successfully linked SPA app to user flow '$($envConfig.UserFlowName)'" -ForegroundColor Green
        } catch {
            Write-Host "ERROR: Failed to link SPA app to user flow: $_" -ForegroundColor Red
            Write-Host "You may need to link the app manually in the Entra portal." -ForegroundColor Red
        } finally {
            Remove-Item $linkTempFile -ErrorAction SilentlyContinue
        }
    }
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
# Step 5: Get Service Principal ID
# ============================================================================

Write-Host "Step 5: Getting Service Principal ID..." -ForegroundColor Yellow

$apiServicePrincipal = az rest `
    --method GET `
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals?`$filter=appId eq '$($apiApp.appId)'" `
    2>$null | ConvertFrom-Json

$servicePrincipalId = $apiServicePrincipal.value[0].id
Write-Host "Service Principal ID: $servicePrincipalId" -ForegroundColor Green
Write-Host ""

# ============================================================================
# Step 6: Store Secrets in Key Vault (optional)
# ============================================================================

$keyVaultDefaults = @{
    dev = "kv-rajfinancial-dev"
    prod = "kv-rajfinancial-prod"
}

$subscriptionDefaults = @{
    dev = "your-dev-subscription-id"
    prod = "your-prod-subscription-id"
}

if (-not $SkipKeyVault) {
    $vaultName = if ($KeyVaultName) { $KeyVaultName } else { $keyVaultDefaults[$Environment] }

    Write-Host "Step 6: Storing secrets in Key Vault ($vaultName)..." -ForegroundColor Yellow

    # Switch to Azure subscription context (different from Entra tenant)
    if ($AzureSubscriptionId) {
        Write-Host "Switching to Azure subscription: $AzureSubscriptionId" -ForegroundColor Yellow
        az account set --subscription $AzureSubscriptionId
    } else {
        Write-Host "Note: Using current Azure subscription. Use -AzureSubscriptionId to specify." -ForegroundColor Yellow
    }

    # Store secrets in Key Vault
    $secrets = @{
        "EntraExternalId-TenantId" = $envConfig.TenantId
        "EntraExternalId-ClientId" = $apiApp.appId
        "EntraExternalId-ServicePrincipalId" = $servicePrincipalId
        "EntraExternalId-SpaClientId" = $spaApp.appId
        "AppRoles-Client" = $envConfig.RoleGuids.Client
        "AppRoles-Administrator" = $envConfig.RoleGuids.Administrator
    }

    foreach ($secret in $secrets.GetEnumerator()) {
        Write-Host "  Setting $($secret.Key)..." -ForegroundColor Gray
        az keyvault secret set `
            --vault-name $vaultName `
            --name $secret.Key `
            --value $secret.Value `
            --output none 2>$null

        if ($LASTEXITCODE -eq 0) {
            Write-Host "    Stored: $($secret.Key)" -ForegroundColor Green
        } else {
            Write-Warning "    Failed to store $($secret.Key) - check Key Vault permissions"
        }
    }

    Write-Host ""
} else {
    Write-Host "Step 6: Skipping Key Vault storage (use -SkipKeyVault:$false to enable)" -ForegroundColor Yellow
    Write-Host ""
}

# ============================================================================
# Step 7: Update Bicep Parameter Files (optional)
# ============================================================================

if ($UpdateBicepParams) {
    Write-Host "Step 7: Updating Bicep parameter files..." -ForegroundColor Yellow

    $bicepParamPath = Join-Path $PSScriptRoot "..\..\infra\parameters\$Environment.bicepparam"

    if (Test-Path $bicepParamPath) {
        $content = Get-Content $bicepParamPath -Raw

        # Update the parameters
        $content = $content -replace "param entraExternalIdClientId = '[^']*'", "param entraExternalIdClientId = '$($apiApp.appId)'"
        $content = $content -replace "param appRoleClient = '[^']*'", "param appRoleClient = '$($envConfig.RoleGuids.Client)'"
        $content = $content -replace "param appRoleAdministrator = '[^']*'", "param appRoleAdministrator = '$($envConfig.RoleGuids.Administrator)'"

        $content | Out-File -FilePath $bicepParamPath -Encoding utf8 -NoNewline
        Write-Host "Updated: $bicepParamPath" -ForegroundColor Green
    } else {
        Write-Warning "Bicep parameter file not found: $bicepParamPath"
    }

    Write-Host ""
} else {
    Write-Host "Step 7: Skipping Bicep parameter update (use -UpdateBicepParams to enable)" -ForegroundColor Yellow
    Write-Host ""
}

# ============================================================================
# Summary & Next Steps
# ============================================================================

Write-Host "============================================" -ForegroundColor Green
Write-Host " Registration Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Registered Applications:" -ForegroundColor Cyan
Write-Host "  API App ID:           $($apiApp.appId)"
Write-Host "  API Object ID:        $($apiApp.id)"
Write-Host "  Service Principal ID: $servicePrincipalId"
Write-Host "  SPA App ID:           $($spaApp.appId)"
Write-Host ""

if (-not $SkipKeyVault) {
    Write-Host "Key Vault Secrets Stored:" -ForegroundColor Cyan
    Write-Host "  EntraExternalId-TenantId"
    Write-Host "  EntraExternalId-ClientId"
    Write-Host "  EntraExternalId-ServicePrincipalId"
    Write-Host "  EntraExternalId-SpaClientId"
    Write-Host "  AppRoles-Client"
    Write-Host "  AppRoles-Administrator"
    Write-Host ""
}

Write-Host "============================================" -ForegroundColor Yellow
Write-Host " Next Steps" -ForegroundColor Yellow
Write-Host "============================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Grant admin consent for API permissions:" -ForegroundColor White
Write-Host "   az ad app permission admin-consent --id $($spaApp.appId)" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. Deploy infrastructure (if not already done):" -ForegroundColor White
Write-Host "   az deployment sub create --location southcentralus --template-file infra/main.bicep --parameters infra/parameters/$Environment.bicepparam" -ForegroundColor Cyan
Write-Host ""
Write-Host "3. Deploy the Function App:" -ForegroundColor White
Write-Host "   Push to the '$( if ($Environment -eq 'dev') { 'develop' } else { 'main' })' branch or run the GitHub Action manually" -ForegroundColor Cyan
Write-Host ""
Write-Host "App Roles:" -ForegroundColor Yellow
Write-Host "  - Client: Standard users who own their financial data"
Write-Host "  - Administrator: Platform staff with system-wide access"
Write-Host ""
Write-Host "Note: Fine-grained access (spouses, CPAs, attorneys) is handled"
Write-Host "through DataAccessGrant entities, NOT through app roles."
Write-Host ""


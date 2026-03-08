# ============================================================================
# RAJ Financial - Create ROPC App Registration
# ============================================================================
# This script creates an ROPC (Resource Owner Password Credentials) app
# registration for integration and E2E testing. ROPC allows automated tests
# to acquire tokens using username/password without interactive login.
#
# What it does:
#   1. Creates a public client app with isFallbackPublicClient=true
#   2. Adds API permission for the API app's user_impersonation scope
#   3. Grants admin consent (eliminates per-user consent prompts)
#
# Prerequisites:
#   - Azure CLI installed (az --version)
#   - Logged into the correct Entra External ID tenant:
#       az login --tenant <tenant-id> --allow-no-subscriptions
#   - register-entra-apps.ps1 has already been run (API app must exist)
#
# Usage:
#   .\create-ropc-app.ps1 -Environment dev
#   .\create-ropc-app.ps1 -Environment prod
#
# Idempotent: Safe to run multiple times. Checks for existing app before
# creating. Re-running will ensure permissions and consent are configured.
# ============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment
)

$ErrorActionPreference = "Stop"

# ============================================================================
# Configuration
# ============================================================================

$config = @{
    dev = @{
        TenantId     = "496527a2-41f8-4297-a979-c916e7255a22"
        TenantDomain = "rajfinancialdev.onmicrosoft.com"
        ApiAppName   = "rajfinancial-api-dev"
        RopcAppName  = "rajfinancial-ropc-dev"
    }
    prod = @{
        TenantId     = "cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6"
        TenantDomain = "rajfinancialprod.onmicrosoft.com"
        ApiAppName   = "rajfinancial-api"
        RopcAppName  = "rajfinancial-ropc"
    }
}

$envConfig = $config[$Environment]

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "RAJ Financial - Create ROPC App Registration" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Tenant: $($envConfig.TenantDomain)" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Verify tenant context
# ============================================================================

Write-Host "Verifying Azure CLI tenant context..." -ForegroundColor Yellow
$currentTenant = az account show --query tenantId -o tsv 2>$null
if ($currentTenant -ne $envConfig.TenantId) {
    Write-Error "Wrong tenant! Current: $currentTenant, Expected: $($envConfig.TenantId). Run: az login --tenant $($envConfig.TenantId) --allow-no-subscriptions"
    exit 1
}
Write-Host "  Tenant verified: $($envConfig.TenantDomain)" -ForegroundColor Green

# ============================================================================
# Get API app details
# ============================================================================

Write-Host ""
Write-Host "Looking up API app: $($envConfig.ApiAppName)..." -ForegroundColor Yellow

$apiApp = az ad app list --filter "displayName eq '$($envConfig.ApiAppName)'" --query "[0]" -o json | ConvertFrom-Json
if (-not $apiApp) {
    Write-Error "API app '$($envConfig.ApiAppName)' not found. Run register-entra-apps.ps1 first."
    exit 1
}

$apiAppId = $apiApp.appId
Write-Host "  API App ID: $apiAppId" -ForegroundColor Green

# Get user_impersonation scope ID
$scopeId = $apiApp.api.oauth2PermissionScopes | Where-Object { $_.value -eq "user_impersonation" } | Select-Object -ExpandProperty id
if (-not $scopeId) {
    Write-Error "user_impersonation scope not found on API app. Check register-entra-apps.ps1."
    exit 1
}
Write-Host "  Scope ID: $scopeId" -ForegroundColor Green

# ============================================================================
# Create or get ROPC app
# ============================================================================

Write-Host ""
Write-Host "Checking for existing ROPC app: $($envConfig.RopcAppName)..." -ForegroundColor Yellow

$ropcApp = az ad app list --filter "displayName eq '$($envConfig.RopcAppName)'" --query "[0]" -o json | ConvertFrom-Json

if ($ropcApp) {
    Write-Host "  ROPC app already exists: $($ropcApp.appId)" -ForegroundColor Green
    $ropcAppId = $ropcApp.appId
    $ropcObjectId = $ropcApp.id
} else {
    Write-Host "  Creating ROPC app..." -ForegroundColor Yellow
    
    $ropcApp = az ad app create `
        --display-name $envConfig.RopcAppName `
        --is-fallback-public-client true `
        --sign-in-audience "AzureADMyOrg" `
        -o json | ConvertFrom-Json
    
    $ropcAppId = $ropcApp.appId
    $ropcObjectId = $ropcApp.id
    Write-Host "  Created ROPC app: $ropcAppId" -ForegroundColor Green
    
    # Wait for propagation
    Write-Host "  Waiting for app propagation..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
}

# ============================================================================
# Add API permission
# ============================================================================

Write-Host ""
Write-Host "Configuring API permission..." -ForegroundColor Yellow

# Check if permission already exists
$existingPermissions = az ad app show --id $ropcAppId --query "requiredResourceAccess" -o json | ConvertFrom-Json
$hasPermission = $existingPermissions | Where-Object { $_.resourceAppId -eq $apiAppId }

if ($hasPermission) {
    Write-Host "  API permission already configured" -ForegroundColor Green
} else {
    Write-Host "  Adding user_impersonation permission..." -ForegroundColor Yellow
    az ad app permission add `
        --id $ropcAppId `
        --api $apiAppId `
        --api-permissions "$scopeId=Scope" | Out-Null
    Write-Host "  Permission added" -ForegroundColor Green
    
    # Wait for propagation
    Start-Sleep -Seconds 3
}

# ============================================================================
# Grant admin consent
# ============================================================================

Write-Host ""
Write-Host "Granting admin consent..." -ForegroundColor Yellow

try {
    az ad app permission admin-consent --id $ropcAppId 2>&1 | Out-Null
    Write-Host "  Admin consent granted" -ForegroundColor Green
} catch {
    Write-Host "  Admin consent may already be granted or requires manual action" -ForegroundColor Yellow
}

# ============================================================================
# Summary
# ============================================================================

Write-Host ""
Write-Host "============================================================================" -ForegroundColor Green
Write-Host "ROPC App Created Successfully!" -ForegroundColor Green
Write-Host "============================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "ROPC App Details:" -ForegroundColor White
Write-Host "  Display Name: $($envConfig.RopcAppName)" -ForegroundColor Gray
Write-Host "  App (Client) ID: $ropcAppId" -ForegroundColor Cyan
Write-Host "  Object ID: $ropcObjectId" -ForegroundColor Gray
Write-Host ""
Write-Host "GitHub Secret to add (environment: $Environment):" -ForegroundColor White
Write-Host "  ENTRA_ROPC_CLIENT_ID = $ropcAppId" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  1. Add the secret above to GitHub environment '$Environment'" -ForegroundColor Gray
Write-Host "  2. Run create-test-users.ps1 -Environment $Environment" -ForegroundColor Gray
Write-Host "  3. Add test user passwords to GitHub secrets" -ForegroundColor Gray
Write-Host ""

#!/usr/bin/env pwsh
# ============================================================================
# RAJ Financial - Complete Environment Deployment Script
# ============================================================================
# This script deploys all Azure infrastructure AND configures Entra External ID
# app registrations to fully reproduce an environment.
#
# Usage:
#   .\deploy-environment.ps1 -Environment dev
#   .\deploy-environment.ps1 -Environment prod
#   .\deploy-environment.ps1 -Environment dev -SkipInfra  # Entra only
#   .\deploy-environment.ps1 -Environment dev -SkipEntra  # Infra only
#
# Prerequisites:
#   - Azure CLI installed and logged in (az login)
#   - Permissions to create resources in the Azure subscription
#   - Permissions to create app registrations in Entra External ID tenant
# ============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment,

    [Parameter()]
    [switch]$SkipInfra,

    [Parameter()]
    [switch]$SkipEntra,

    [Parameter()]
    [switch]$SkipSqlAccess,

    [Parameter()]
    [string]$Location = 'southcentralus'
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot

# ============================================================================
# Configuration
# ============================================================================

$config = @{
    dev = @{
        SubscriptionId = "3f3156c6-94bc-4752-a9d7-0d43fe99c92a"  # Visual Studio Enterprise
        SubscriptionTenantId = "a2bc6fb5-fba9-40b4-9ecc-2acf61cae876"  # Azure subscription tenant
        ResourceGroup = "raj-financial-dev-rg"
        EntraTenantId = "496527a2-41f8-4297-a979-c916e7255a22"  # Entra External ID tenant
        EntraTenantDomain = "rajfinancialdev.onmicrosoft.com"
        RoleGuids = @{
            Client = "bc34bd6c-38b8-46a6-9d4c-d338afeea81f"
            Administrator = "2202014c-e4b9-4ab9-9e6a-4cc53e13598f"
        }
    }
    prod = @{
        SubscriptionId = "3f3156c6-94bc-4752-a9d7-0d43fe99c92a"  # Visual Studio Enterprise (or separate prod sub)
        SubscriptionTenantId = "a2bc6fb5-fba9-40b4-9ecc-2acf61cae876"  # Azure subscription tenant
        ResourceGroup = "raj-financial-prod-rg"
        EntraTenantId = "cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6"  # Entra External ID tenant
        EntraTenantDomain = "rajfinancialprod.onmicrosoft.com"
        RoleGuids = @{
            Client = "d4e5f6a7-b8c9-4d5e-a6b7-c8d9e0f1a2b3"
            Administrator = "1a2b3c4d-5e6f-4a5b-8c9d-0e1f2a3b4c5d"
        }
    }
}

$envConfig = $config[$Environment]

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  RAJ Financial - Environment Deployment                        ║" -ForegroundColor Cyan
Write-Host "║  Environment: $Environment                                              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# Pre-flight Checks
# ============================================================================

Write-Host "→ Pre-flight checks..." -ForegroundColor Yellow

# Check Azure CLI
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI not found. Install from https://aka.ms/install-azure-cli"
    exit 1
}

# Check if logged in
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Error "Not logged into Azure CLI. Run: az login"
    exit 1
}

Write-Host "  ✓ Azure CLI authenticated as: $($account.user.name)" -ForegroundColor Green

# ============================================================================
# Step 1: Deploy Azure Infrastructure
# ============================================================================

if (-not $SkipInfra) {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  Step 1: Deploying Azure Infrastructure" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

    # Switch to Azure subscription (different tenant from Entra External ID)
    Write-Host "  Switching to Azure subscription..." -ForegroundColor Yellow
    az account set --subscription $envConfig.SubscriptionId 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Logging into Azure subscription tenant..." -ForegroundColor Yellow
        az login --tenant $envConfig.SubscriptionTenantId --output none
        az account set --subscription $envConfig.SubscriptionId
    }
    Write-Host "  ✓ Using subscription: $($envConfig.SubscriptionId)" -ForegroundColor Green

    $infraDir = Join-Path $scriptDir ".." ".." "infra"
    $paramFile = Join-Path $infraDir "parameters" "$Environment.bicepparam"
    $templateFile = Join-Path $infraDir "main.bicep"

    if (-not (Test-Path $templateFile)) {
        Write-Error "Bicep template not found: $templateFile"
        exit 1
    }

    Write-Host "  Template: $templateFile" -ForegroundColor Gray
    Write-Host "  Parameters: $paramFile" -ForegroundColor Gray
    Write-Host ""

    # Deploy to existing resource group instead of subscription scope
    Write-Host "  Deploying infrastructure (this may take 10-15 minutes)..." -ForegroundColor Yellow

    $deploymentName = "rajfinancial-$Environment-$(Get-Date -Format 'yyyyMMddHHmmss')"

    # Check if resource group exists
    $rgExists = az group exists --name $envConfig.ResourceGroup 2>$null
    if ($rgExists -eq "false") {
        Write-Host "  Creating resource group: $($envConfig.ResourceGroup)" -ForegroundColor Yellow
        az group create --name $envConfig.ResourceGroup --location $Location --output none
    }

    $deployment = az deployment sub create `
        --name $deploymentName `
        --location $Location `
        --template-file $templateFile `
        --parameters $paramFile `
        --output json 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Infrastructure deployment failed: $deployment"
        exit 1
    }

    $outputs = $deployment | ConvertFrom-Json
    Write-Host "  ✓ Infrastructure deployed successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Resources created:" -ForegroundColor Cyan
    Write-Host "    Resource Group: $($outputs.properties.outputs.resourceGroupName.value)" -ForegroundColor Gray
    Write-Host "    Function App: $($outputs.properties.outputs.functionAppUrl.value)" -ForegroundColor Gray
    Write-Host "    SQL Server: $($outputs.properties.outputs.sqlServerFqdn.value)" -ForegroundColor Gray
    Write-Host "    Redis: $($outputs.properties.outputs.redisHostName.value)" -ForegroundColor Gray
    Write-Host "    (SWA exists in 'rajfinancial' RG - uses preview environments)" -ForegroundColor Gray

    # Save outputs for later steps
    $infraOutputs = $outputs.properties.outputs
} else {
    Write-Host ""
    Write-Host "  ⊘ Skipping infrastructure deployment (--SkipInfra)" -ForegroundColor DarkGray
}

# ============================================================================
# Step 2: Configure SQL Database Access for Managed Identity
# ============================================================================

if (-not $SkipInfra -and -not $SkipSqlAccess) {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  Step 2: Configuring SQL Database Access" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

    $sqlAccessScript = Join-Path $scriptDir "configure-sql-access.ps1"
    if (Test-Path $sqlAccessScript) {
        & $sqlAccessScript -Environment $Environment
    } else {
        Write-Host "  ⚠ SQL access script not found. Run manually:" -ForegroundColor Yellow
        Write-Host "    .\configure-sql-access.ps1 -Environment $Environment" -ForegroundColor Gray
    }
}

# ============================================================================
# Step 3: Register Entra External ID Applications
# ============================================================================

if (-not $SkipEntra) {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  Step 3: Registering Entra External ID Applications" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

    # Switch to Entra tenant
    Write-Host "  Switching to Entra External ID tenant..." -ForegroundColor Yellow
    az login --tenant $envConfig.EntraTenantId --allow-no-subscriptions --output none

    $entraScript = Join-Path $scriptDir "register-entra-apps.ps1"
    if (Test-Path $entraScript) {
        & $entraScript -Environment $Environment
    } else {
        Write-Error "Entra registration script not found: $entraScript"
        exit 1
    }

    # Check if we should add federated credentials
    $entraConfigFile = Join-Path $scriptDir "entra-config-$Environment.json"
    if (Test-Path $entraConfigFile) {
        $entraConfig = Get-Content $entraConfigFile | ConvertFrom-Json
        $spaAppId = $entraConfig.spa.appId

        Write-Host ""
        Write-Host "  Adding GitHub OIDC federated credentials..." -ForegroundColor Yellow

        $fedCredScript = Join-Path $scriptDir ".." "add-entra-federated-credentials.ps1"
        if (Test-Path $fedCredScript) {
            & $fedCredScript -EntraTenantId $envConfig.EntraTenantId -AppClientId $spaAppId
        }
    }
} else {
    Write-Host ""
    Write-Host "  ⊘ Skipping Entra configuration (--SkipEntra)" -ForegroundColor DarkGray
}

# ============================================================================
# Step 4: Summary
# ============================================================================

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║  ✓ Deployment Complete!                                        ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

if (-not $SkipInfra) {
    Write-Host "Azure Infrastructure:" -ForegroundColor Cyan
    Write-Host "  • Resource Group: rg-rajfinancial-$Environment" -ForegroundColor Gray
    Write-Host "  • All resources use Managed Identity (no secrets in code)" -ForegroundColor Gray
    Write-Host ""
}

if (-not $SkipEntra) {
    Write-Host "Entra External ID:" -ForegroundColor Cyan
    Write-Host "  • Tenant: $($envConfig.EntraTenantDomain)" -ForegroundColor Gray
    Write-Host "  • SPA and API apps registered with roles and scopes" -ForegroundColor Gray
    Write-Host "  • Configuration saved to: scripts/infra/entra-config-$Environment.json" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Update src/Client/wwwroot/appsettings.$Environment.json" -ForegroundColor Gray
Write-Host "  2. Update src/Api/local.settings.json (or appsettings.$Environment.json)" -ForegroundColor Gray
Write-Host "  3. Grant admin consent in Entra portal for API permissions" -ForegroundColor Gray
Write-Host "  4. Add GitHub secrets for OIDC authentication" -ForegroundColor Gray
Write-Host ""

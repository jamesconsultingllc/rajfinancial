#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Sets up OIDC federated credentials for GitHub Actions to manage Entra External ID app registrations.

.DESCRIPTION
    This script creates a service principal in your Entra External ID tenant with federated credentials
    configured for GitHub Actions OIDC authentication. The service principal will have permissions to
    manage app registrations (add/remove redirect URIs).

.PARAMETER EntraTenantId
    The tenant ID of your Entra External ID tenant.

.PARAMETER AppName
    The display name for the app registration. Default: "GitHub Actions - RAJ Financial Entra Manager"

.EXAMPLE
    .\setup-entra-oidc.ps1 -EntraTenantId "your-entra-tenant-id"

.NOTES
    Prerequisites:
    - Azure CLI installed and configured
    - Permissions to create app registrations in the Entra External ID tenant
    - Permissions to grant admin consent for API permissions
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$EntraTenantId,

    [Parameter(Mandatory = $false)]
    [string]$AppName = "GitHub Actions - RAJ Financial Entra Manager"
)

$ErrorActionPreference = "Stop"

# GitHub repository details
$GitHubOrg = "jamesconsultingllc"
$GitHubRepo = "rajfinancial"
$GitHubRepoFull = "$GitHubOrg/$GitHubRepo"

Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Entra External ID - GitHub OIDC Setup" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login to Entra tenant
Write-Host "→ Step 1: Logging into Entra External ID tenant..." -ForegroundColor Yellow
Write-Host "  Tenant ID: $EntraTenantId" -ForegroundColor Gray
az login --tenant $EntraTenantId --allow-no-subscriptions --output none
Write-Host "✓ Logged in successfully" -ForegroundColor Green
Write-Host ""

# Step 2: Create app registration
Write-Host "→ Step 2: Creating app registration..." -ForegroundColor Yellow
Write-Host "  App name: $AppName" -ForegroundColor Gray

$appJson = az ad app create `
    --display-name $AppName `
    --sign-in-audience "AzureADMyOrg" `
    --output json

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create app registration"
    exit 1
}

$app = $appJson | ConvertFrom-Json
$appId = $app.appId
$appObjectId = $app.id

Write-Host "✓ App registration created" -ForegroundColor Green
Write-Host "  Application (Client) ID: $appId" -ForegroundColor Gray
Write-Host "  Object ID: $appObjectId" -ForegroundColor Gray
Write-Host ""

# Step 3: Create service principal
Write-Host "→ Step 3: Creating service principal..." -ForegroundColor Yellow

$spJson = az ad sp create --id $appId --output json
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create service principal"
    exit 1
}

$sp = $spJson | ConvertFrom-Json
$spObjectId = $sp.id

Write-Host "✓ Service principal created" -ForegroundColor Green
Write-Host "  Service Principal Object ID: $spObjectId" -ForegroundColor Gray
Write-Host ""

# Step 4: Grant API permissions
Write-Host "→ Step 4: Granting API permissions..." -ForegroundColor Yellow
Write-Host "  Permission: Application.ReadWrite.All (Microsoft Graph)" -ForegroundColor Gray

# Microsoft Graph App ID
$graphAppId = "00000003-0000-0000-c000-000000000000"

# Application.ReadWrite.All - Role ID
$appReadWriteAllRoleId = "1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9"

# Add the permission
az ad app permission add `
    --id $appId `
    --api $graphAppId `
    --api-permissions "$appReadWriteAllRoleId=Role" `
    --output none

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to add API permission"
    exit 1
}

Write-Host "✓ API permission added" -ForegroundColor Green
Write-Host ""

# Step 5: Grant admin consent
Write-Host "→ Step 5: Granting admin consent..." -ForegroundColor Yellow
Write-Host "  ⚠ You must have Global Administrator or Privileged Role Administrator role" -ForegroundColor DarkYellow

az ad app permission admin-consent --id $appId --output none

if ($LASTEXITCODE -ne 0) {
    Write-Warning "Failed to grant admin consent automatically."
    Write-Host ""
    Write-Host "  Please grant admin consent manually:" -ForegroundColor Yellow
    Write-Host "  1. Go to: https://entra.microsoft.com/" -ForegroundColor Gray
    Write-Host "  2. Navigate to: Applications > App registrations" -ForegroundColor Gray
    Write-Host "  3. Find app: $AppName" -ForegroundColor Gray
    Write-Host "  4. Click: API permissions > Grant admin consent" -ForegroundColor Gray
    Write-Host ""
    $continue = Read-Host "Press Enter after granting consent manually (or Ctrl+C to exit)"
} else {
    Write-Host "✓ Admin consent granted" -ForegroundColor Green
}
Write-Host ""

# Step 6: Create federated credentials
Write-Host "→ Step 6: Creating federated credentials for GitHub OIDC..." -ForegroundColor Yellow

$environments = @(
    @{
        Name = "production"
        Subject = "repo:${GitHubRepoFull}:environment:production"
        Description = "GitHub Actions OIDC for production environment"
    },
    @{
        Name = "development"
        Subject = "repo:${GitHubRepoFull}:environment:development"
        Description = "GitHub Actions OIDC for development environment"
    },
    @{
        Name = "preview"
        Subject = "repo:${GitHubRepoFull}:environment:preview"
        Description = "GitHub Actions OIDC for preview environments"
    }
)

foreach ($env in $environments) {
    Write-Host "  Creating credential for: $($env.Name)" -ForegroundColor Gray

    $credentialParams = @{
        name = "github-oidc-$($env.Name)"
        issuer = "https://token.actions.githubusercontent.com"
        subject = $env.Subject
        description = $env.Description
        audiences = @("api://AzureADTokenExchange")
    } | ConvertTo-Json -Compress

    az ad app federated-credential create `
        --id $appId `
        --parameters $credentialParams `
        --output none

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to create federated credential for $($env.Name)"
    } else {
        Write-Host "  ✓ Federated credential created for: $($env.Name)" -ForegroundColor Green
    }
}

Write-Host ""

# Step 7: Summary
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  ✓ Setup Complete!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Add these secrets to your GitHub repository:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Secret Name: ENTRA_CLIENT_ID" -ForegroundColor White
Write-Host "  Secret Value: $appId" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Secret Name: ENTRA_DEV_TENANT_ID" -ForegroundColor White
Write-Host "  Secret Value: $EntraTenantId" -ForegroundColor Cyan
Write-Host "  (This may already exist)" -ForegroundColor Gray
Write-Host ""
Write-Host "🔗 Add secrets at:" -ForegroundColor Yellow
Write-Host "  https://github.com/$GitHubRepoFull/settings/secrets/actions" -ForegroundColor Blue
Write-Host ""
Write-Host "📝 Summary:" -ForegroundColor Yellow
Write-Host "  • App Registration: $AppName" -ForegroundColor Gray
Write-Host "  • Application ID: $appId" -ForegroundColor Gray
Write-Host "  • Object ID: $appObjectId" -ForegroundColor Gray
Write-Host "  • Federated Credentials: 3 (production, development, preview)" -ForegroundColor Gray
Write-Host "  • API Permissions: Application.ReadWrite.All" -ForegroundColor Gray
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Add the secrets to GitHub (see above)" -ForegroundColor Gray
Write-Host "  2. Update your workflow YAML file" -ForegroundColor Gray
Write-Host "  3. Remove the 'az login' step from the custom action" -ForegroundColor Gray
Write-Host ""

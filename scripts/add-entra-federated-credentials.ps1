#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Adds GitHub OIDC federated credentials to an existing Entra app registration.

.DESCRIPTION
    This script adds federated credentials to your existing Entra External ID app registration
    to enable GitHub Actions OIDC authentication. The app can then manage its own redirect URIs
    without requiring interactive login.

.PARAMETER EntraTenantId
    The tenant ID of your Entra External ID tenant.

.PARAMETER AppClientId
    The Application (Client) ID of the existing app registration (e.g., rajfinancial-spa-dev).

.EXAMPLE
    .\add-entra-federated-credentials.ps1 -EntraTenantId "your-tenant-id" -AppClientId "your-app-client-id"

.NOTES
    Prerequisites:
    - Azure CLI installed and configured
    - Permissions to manage the app registration in the Entra External ID tenant
    - The app must already have Application.ReadWrite.All permission (or it will be added)
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$EntraTenantId,

    [Parameter(Mandatory = $true)]
    [string]$AppClientId
)

$ErrorActionPreference = "Stop"

# GitHub repository details
$GitHubOrg = "jamesconsultingllc"
$GitHubRepo = "rajfinancial"
$GitHubRepoFull = "$GitHubOrg/$GitHubRepo"

Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  Add GitHub OIDC Federated Credentials" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login to Entra tenant
Write-Host "→ Step 1: Logging into Entra External ID tenant..." -ForegroundColor Yellow
Write-Host "  Tenant ID: $EntraTenantId" -ForegroundColor Gray
az login --tenant $EntraTenantId --allow-no-subscriptions --output none
Write-Host "✓ Logged in successfully" -ForegroundColor Green
Write-Host ""

# Step 2: Get app details
Write-Host "→ Step 2: Retrieving app registration details..." -ForegroundColor Yellow
Write-Host "  App Client ID: $AppClientId" -ForegroundColor Gray

$appJson = az ad app show --id $AppClientId --output json
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to find app registration with Client ID: $AppClientId"
    exit 1
}

$app = $appJson | ConvertFrom-Json
$appObjectId = $app.id
$appDisplayName = $app.displayName

Write-Host "✓ App registration found" -ForegroundColor Green
Write-Host "  Display Name: $appDisplayName" -ForegroundColor Gray
Write-Host "  Object ID: $appObjectId" -ForegroundColor Gray
Write-Host ""

# Step 3: Check if service principal exists
Write-Host "→ Step 3: Checking for service principal..." -ForegroundColor Yellow

$spJson = az ad sp show --id $AppClientId --output json 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "  Service principal not found, creating..." -ForegroundColor Gray
    $spJson = az ad sp create --id $AppClientId --output json
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create service principal"
        exit 1
    }
    Write-Host "✓ Service principal created" -ForegroundColor Green
} else {
    Write-Host "✓ Service principal already exists" -ForegroundColor Green
}

$sp = $spJson | ConvertFrom-Json
$spObjectId = $sp.id
Write-Host "  Service Principal Object ID: $spObjectId" -ForegroundColor Gray
Write-Host ""

# Step 4: Check and add API permissions if needed
Write-Host "→ Step 4: Checking API permissions..." -ForegroundColor Yellow

# Microsoft Graph App ID
$graphAppId = "00000003-0000-0000-c000-000000000000"

# Application.ReadWrite.All - Role ID
$appReadWriteAllRoleId = "1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9"

# Check if permission already exists
$currentPermissions = $app.requiredResourceAccess | Where-Object { $_.resourceAppId -eq $graphAppId }
$hasPermission = $false

if ($currentPermissions) {
    $hasPermission = $currentPermissions.resourceAccess | Where-Object { $_.id -eq $appReadWriteAllRoleId }
}

if (-not $hasPermission) {
    Write-Host "  Adding Application.ReadWrite.All permission..." -ForegroundColor Gray

    az ad app permission add `
        --id $AppClientId `
        --api $graphAppId `
        --api-permissions "$appReadWriteAllRoleId=Role" `
        --output none

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to add API permission"
        exit 1
    }

    Write-Host "✓ API permission added" -ForegroundColor Green
    Write-Host ""

    # Grant admin consent
    Write-Host "→ Step 5: Granting admin consent..." -ForegroundColor Yellow
    Write-Host "  ⚠ You must have Global Administrator or Privileged Role Administrator role" -ForegroundColor DarkYellow

    az ad app permission admin-consent --id $AppClientId --output none

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to grant admin consent automatically."
        Write-Host ""
        Write-Host "  Please grant admin consent manually:" -ForegroundColor Yellow
        Write-Host "  1. Go to: https://entra.microsoft.com/" -ForegroundColor Gray
        Write-Host "  2. Navigate to: Applications > App registrations" -ForegroundColor Gray
        Write-Host "  3. Find app: $appDisplayName" -ForegroundColor Gray
        Write-Host "  4. Click: API permissions > Grant admin consent" -ForegroundColor Gray
        Write-Host ""
        $continue = Read-Host "Press Enter after granting consent manually (or Ctrl+C to exit)"
    } else {
        Write-Host "✓ Admin consent granted" -ForegroundColor Green
    }
} else {
    Write-Host "✓ Application.ReadWrite.All permission already exists" -ForegroundColor Green
}
Write-Host ""

# Step 6: Get existing federated credentials
Write-Host "→ Step 6: Checking existing federated credentials..." -ForegroundColor Yellow

$existingCredsJson = az ad app federated-credential list --id $AppClientId --output json
$existingCreds = $existingCredsJson | ConvertFrom-Json

Write-Host "  Found $($existingCreds.Count) existing federated credential(s)" -ForegroundColor Gray
Write-Host ""

# Step 7: Create federated credentials
Write-Host "→ Step 7: Creating GitHub OIDC federated credentials..." -ForegroundColor Yellow

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

$addedCount = 0
$skippedCount = 0

foreach ($env in $environments) {
    $credName = "github-oidc-$($env.Name)"

    # Check if credential already exists
    $exists = $existingCreds | Where-Object { $_.name -eq $credName }

    if ($exists) {
        Write-Host "  ⊘ Skipping '$credName' (already exists)" -ForegroundColor DarkGray
        $skippedCount++
        continue
    }

    Write-Host "  Creating credential for: $($env.Name)" -ForegroundColor Gray

    $credentialParams = @{
        name = $credName
        issuer = "https://token.actions.githubusercontent.com"
        subject = $env.Subject
        description = $env.Description
        audiences = @("api://AzureADTokenExchange")
    } | ConvertTo-Json -Compress

    az ad app federated-credential create `
        --id $AppClientId `
        --parameters $credentialParams `
        --output none

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to create federated credential for $($env.Name)"
    } else {
        Write-Host "  ✓ Federated credential created for: $($env.Name)" -ForegroundColor Green
        $addedCount++
    }
}

Write-Host ""
Write-Host "Summary: Added $addedCount, Skipped $skippedCount" -ForegroundColor Gray
Write-Host ""

# Step 8: Summary
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  ✓ Setup Complete!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 GitHub Secrets Required:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Secret Name: ENTRA_CLIENT_ID" -ForegroundColor White
Write-Host "  Secret Value: $AppClientId" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Secret Name: ENTRA_DEV_TENANT_ID" -ForegroundColor White
Write-Host "  Secret Value: $EntraTenantId" -ForegroundColor Cyan
Write-Host ""
Write-Host "🔗 Add secrets at:" -ForegroundColor Yellow
Write-Host "  https://github.com/$GitHubRepoFull/settings/secrets/actions" -ForegroundColor Blue
Write-Host ""
Write-Host "📝 Configuration Summary:" -ForegroundColor Yellow
Write-Host "  • App Registration: $appDisplayName" -ForegroundColor Gray
Write-Host "  • Application ID: $AppClientId" -ForegroundColor Gray
Write-Host "  • Object ID: $appObjectId" -ForegroundColor Gray
Write-Host "  • Federated Credentials: $($existingCreds.Count + $addedCount) total ($addedCount new)" -ForegroundColor Gray
Write-Host "  • API Permissions: Application.ReadWrite.All" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Add the secrets to GitHub (see above)" -ForegroundColor Gray
Write-Host "  2. The workflow YAML is already updated" -ForegroundColor Gray
Write-Host "  3. Commit and push your changes to test" -ForegroundColor Gray
Write-Host ""

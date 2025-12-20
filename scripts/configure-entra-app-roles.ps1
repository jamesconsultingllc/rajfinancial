#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Configures app roles in Entra External ID app registration.

.DESCRIPTION
    Adds Administrator, Advisor, and Client app roles to the specified
    Entra External ID app registration using Microsoft Graph API.

.PARAMETER AppObjectId
    The Object ID of the Entra app registration.

.PARAMETER TenantId
    The Entra External ID tenant ID.

.EXAMPLE
    .\configure-entra-app-roles.ps1 -AppObjectId "your-app-object-id" -TenantId "your-tenant-id"

.NOTES
    Requires Azure CLI to be installed and authenticated.
    Run 'az login --tenant <tenant-id> --allow-no-subscriptions' first.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$AppObjectId,

    [Parameter(Mandatory = $true)]
    [string]$TenantId
)

$ErrorActionPreference = "Stop"

Write-Host "🔐 Configuring App Roles for Raj Financial" -ForegroundColor Cyan
Write-Host "App Object ID: $AppObjectId" -ForegroundColor Gray
Write-Host "Tenant ID: $TenantId" -ForegroundColor Gray
Write-Host ""

# Check if Azure CLI is installed
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI is not installed. Please install it from https://aka.ms/install-azure-cli"
    exit 1
}

# Check if logged in
Write-Host "Checking Azure CLI authentication..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in. Please run:" -ForegroundColor Red
    Write-Host "  az login --tenant $TenantId --allow-no-subscriptions" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Authenticated as: $($account.user.name)" -ForegroundColor Green
Write-Host ""

# Define app roles
$appRoles = @(
    @{
        id                 = [guid]::NewGuid().ToString()
        displayName        = "Administrator"
        description        = "Full system access for IT staff and system administrators"
        value              = "Administrator"
        isEnabled          = $true
        allowedMemberTypes = @("User")
    },
    @{
        id                 = [guid]::NewGuid().ToString()
        displayName        = "Advisor"
        description        = "Financial advisors who manage client accounts and portfolios"
        value              = "Advisor"
        isEnabled          = $true
        allowedMemberTypes = @("User")
    },
    @{
        id                 = [guid]::NewGuid().ToString()
        displayName        = "Client"
        description        = "Client portal access for registered customers"
        value              = "Client"
        isEnabled          = $true
        allowedMemberTypes = @("User")
    }
)

# Get current app roles to avoid duplicates
Write-Host "📋 Fetching current app roles..." -ForegroundColor Yellow
$currentApp = az rest --method GET `
    --uri "https://graph.microsoft.com/v1.0/applications/$AppObjectId" `
    --headers "Content-Type=application/json" | ConvertFrom-Json

$existingRoles = @()
if ($currentApp.appRoles) {
    $existingRoles = $currentApp.appRoles | Where-Object { $_.isEnabled -eq $true }
}

Write-Host "Found $($existingRoles.Count) existing app role(s)" -ForegroundColor Gray

# Merge roles (keep existing, add new)
$mergedRoles = @()

# Add existing roles
foreach ($role in $existingRoles) {
    Write-Host "  ↳ Keeping: $($role.displayName)" -ForegroundColor Gray
    $mergedRoles += $role
}

# Add new roles if not already present
foreach ($newRole in $appRoles) {
    $exists = $existingRoles | Where-Object { $_.value -eq $newRole.value }
    if (-not $exists) {
        Write-Host "  ↳ Adding: $($newRole.displayName)" -ForegroundColor Green
        $mergedRoles += $newRole
    } else {
        Write-Host "  ↳ Skipping: $($newRole.displayName) (already exists)" -ForegroundColor Yellow
    }
}

# Update app registration with merged roles
Write-Host ""
Write-Host "🔄 Updating app registration..." -ForegroundColor Yellow

$body = @{
    appRoles = $mergedRoles
} | ConvertTo-Json -Depth 10 -Compress

try {
    az rest --method PATCH `
        --uri "https://graph.microsoft.com/v1.0/applications/$AppObjectId" `
        --headers "Content-Type=application/json" `
        --body $body | Out-Null

    Write-Host "✅ App roles configured successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "App Roles:" -ForegroundColor Cyan
    foreach ($role in $mergedRoles | Where-Object { $_.isEnabled -eq $true }) {
        Write-Host "  • $($role.displayName) ($($role.value))" -ForegroundColor White
        Write-Host "    $($role.description)" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Assign roles to users in Entra admin center" -ForegroundColor White
    Write-Host "  2. Users will receive role claims in their access tokens" -ForegroundColor White
    Write-Host "  3. Use policies in Blazor app to check roles" -ForegroundColor White

} catch {
    Write-Error "Failed to update app roles: $_"
    exit 1
}

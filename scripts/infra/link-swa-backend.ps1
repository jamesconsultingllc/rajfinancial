#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Links the Azure Functions backend to the Static Web App.

.DESCRIPTION
    This script configures the SWA to proxy /api/* requests to the standalone
    Azure Functions app. This enables the React frontend to call the API
    using relative URLs (/api/...) without CORS issues.

    Prerequisites:
    - Azure CLI installed and logged in
    - SWA must be Standard tier (not Free) for linked backends
    - Functions app must be deployed

.PARAMETER Environment
    The target environment (dev or prod).

.EXAMPLE
    .\link-swa-backend.ps1 -Environment dev
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment
)

$ErrorActionPreference = 'Stop'

# Configuration
$subscriptionId = '3f3156c6-94bc-4752-a9d7-0d43fe99c92a'
$swaResourceGroup = 'rajfinancial'
$swaName = 'rajfinancial'

# Environment-specific config
$config = @{
    dev = @{
        functionAppName = 'func-rajfinancial-dev'
        functionAppRg = 'raj-financial-dev-rg'
        region = 'centralus'
    }
    prod = @{
        functionAppName = 'func-rajfinancial-prod'
        functionAppRg = 'raj-financial-prod-rg'
        region = 'centralus'
    }
}

$envConfig = $config[$Environment]

Write-Host "=== Linking SWA Backend for $Environment ===" -ForegroundColor Cyan
Write-Host "SWA: $swaName (resource group: $swaResourceGroup)"
Write-Host "Functions: $($envConfig.functionAppName) (resource group: $($envConfig.functionAppRg))"
Write-Host ""

# Check if already linked
Write-Host "Checking existing backends..." -ForegroundColor Yellow
$existingBackends = $null
$existingBackendsRaw = az staticwebapp backends show `
    --name $swaName `
    --resource-group $swaResourceGroup `
    --subscription $subscriptionId 2>&1

try {
    $existingBackendsText = $existingBackendsRaw -join "`n"
    if ($existingBackendsText -and $existingBackendsText.Trim().StartsWith('{')) {
        $existingBackends = $existingBackendsText | ConvertFrom-Json -ErrorAction Stop
    }
}catch {
    Write-Host "No existing backends found or unable to parse existing backend configuration." -ForegroundColor Gray
    $existingBackends = $null
}

if ($existingBackends) {
    Write-Host "Existing backend found:" -ForegroundColor Yellow
    Write-Host "  - $($existingBackends.name): $($existingBackends.backendResourceId)" -ForegroundColor Gray

    if ($existingBackends.backendResourceId -and $existingBackends.backendResourceId -like "*$($envConfig.functionAppName)*") {
        Write-Host "Backend already linked for $Environment environment." -ForegroundColor Green
        exit 0
    }
}

# Get Functions app resource ID
Write-Host "Getting Functions app resource ID..." -ForegroundColor Yellow
$functionAppId = az functionapp show `
    --name $envConfig.functionAppName `
    --resource-group $envConfig.functionAppRg `
    --subscription $subscriptionId `
    --query id -o tsv

if (-not $functionAppId) {
    Write-Error "Functions app '$($envConfig.functionAppName)' not found in resource group '$($envConfig.functionAppRg)'"
    exit 1
}

Write-Host "Functions Resource ID: $functionAppId" -ForegroundColor Gray

# Link the backend
Write-Host "Linking backend to SWA..." -ForegroundColor Yellow
$result = az staticwebapp backends link `
    --name $swaName `
    --resource-group $swaResourceGroup `
    --backend-resource-id $functionAppId `
    --backend-region $envConfig.region `
    --subscription $subscriptionId 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to link backend: $result"
    exit 1
}

Write-Host ""
Write-Host "=== Backend linked successfully! ===" -ForegroundColor Green
Write-Host "SWA will now proxy /api/* requests to: $($envConfig.functionAppName)" -ForegroundColor Cyan

# Verify
Write-Host ""
Write-Host "Verifying health endpoint..." -ForegroundColor Yellow
$swaHostname = az staticwebapp show `
    --name $swaName `
    --resource-group $swaResourceGroup `
    --subscription $subscriptionId `
    --query defaultHostname -o tsv

$healthUrl = "https://$swaHostname/api/health"
Write-Host "Testing: $healthUrl" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 30
    Write-Host "Health check passed: $($response | ConvertTo-Json -Compress)" -ForegroundColor Green
}
catch {
    Write-Host "Health check failed (this may be normal if Functions app is cold starting)" -ForegroundColor Yellow
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Gray
}

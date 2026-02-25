# ============================================================================
# RAJ Financial - Configure GitHub Actions Service Principal
# ============================================================================
# This script configures the Azure role assignments needed for GitHub Actions
# workflows to deploy and manage Azure resources.
#
# Prerequisites:
#   - Azure CLI installed (az --version)
#   - Logged into Azure subscription: az login
#   - Owner or User Access Administrator role on the subscription
#
# Usage:
#   .\configure-github-sp.ps1
#
# What it does:
#   1. Ensures the github-actions SP has Contributor on the resource group
#   2. Adds Website Contributor at subscription level for SWA operations
#
# Why subscription-level Website Contributor?
#   The az staticwebapp environment delete command requires
#   Microsoft.Web/locations/operationResults/read permission, which is scoped
#   at the subscription/location level, not the resource group level.
# ============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ServicePrincipalName = "github-actions",
    
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroupName = "rajfinancial"
)

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " Configure GitHub Actions Service Principal" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Get current subscription
$subscription = az account show --query "{id:id, name:name}" -o json | ConvertFrom-Json
Write-Host "Subscription: $($subscription.name)" -ForegroundColor Yellow
Write-Host "Subscription ID: $($subscription.id)" -ForegroundColor Gray
Write-Host ""

# Find the service principal
Write-Host "Looking up service principal: $ServicePrincipalName..." -ForegroundColor Yellow
$sp = az ad sp list --filter "displayName eq '$ServicePrincipalName'" --query "[0]" -o json | ConvertFrom-Json
if (-not $sp) {
    Write-Error "Service principal '$ServicePrincipalName' not found. Create it first with: az ad sp create-for-rbac --name $ServicePrincipalName"
    exit 1
}
Write-Host "  App ID: $($sp.appId)" -ForegroundColor Green
Write-Host "  Object ID: $($sp.id)" -ForegroundColor Gray
Write-Host ""

# Check/create Contributor role on resource group
Write-Host "Checking Contributor role on resource group..." -ForegroundColor Yellow
$rgScope = "/subscriptions/$($subscription.id)/resourceGroups/$ResourceGroupName"
$rgAssignment = az role assignment list --assignee $sp.id --scope $rgScope --role "Contributor" --query "[0]" -o json 2>$null | ConvertFrom-Json

if ($rgAssignment) {
    Write-Host "  Contributor role already assigned on $ResourceGroupName" -ForegroundColor Green
} else {
    Write-Host "  Assigning Contributor role on $ResourceGroupName..." -ForegroundColor Yellow
    az role assignment create --assignee $sp.id --role "Contributor" --scope $rgScope | Out-Null
    Write-Host "  Contributor role assigned" -ForegroundColor Green
}

# Check/create Website Contributor at subscription level
Write-Host ""
Write-Host "Checking Website Contributor role at subscription level..." -ForegroundColor Yellow
$subScope = "/subscriptions/$($subscription.id)"
$subAssignment = az role assignment list --assignee $sp.id --scope $subScope --role "Website Contributor" --query "[0]" -o json 2>$null | ConvertFrom-Json

if ($subAssignment) {
    Write-Host "  Website Contributor role already assigned at subscription level" -ForegroundColor Green
} else {
    Write-Host "  Assigning Website Contributor role at subscription level..." -ForegroundColor Yellow
    Write-Host "  (Required for SWA environment delete operations)" -ForegroundColor Gray
    az role assignment create --assignee $sp.id --role "Website Contributor" --scope $subScope | Out-Null
    Write-Host "  Website Contributor role assigned" -ForegroundColor Green
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host " Configuration Complete" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Role assignments for '$ServicePrincipalName':" -ForegroundColor White
Write-Host "  - Contributor on resource group: $ResourceGroupName" -ForegroundColor Gray
Write-Host "  - Website Contributor on subscription (for SWA operations)" -ForegroundColor Gray
Write-Host ""

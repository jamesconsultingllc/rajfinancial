// ============================================================================
// RAJ Financial - Development Environment Parameters
// ============================================================================
using '../main.bicep'

param environment = 'dev'
param location = 'southcentralus'
param baseName = 'rajfinancial'
param resourceGroupNameOverride = 'raj-financial-dev-rg'

// Microsoft Entra External ID - Development Tenant
// Tenant: rajfinancialdev.onmicrosoft.com
param entraExternalIdTenantId = '496527a2-41f8-4297-a979-c916e7255a22'

// SQL Admin - Rudy James Jr (from Azure subscription tenant)
param sqlAdminObjectId = '5641fa2a-0ebd-4b2b-9a2a-76e1bcbfff28'
param sqlAdminName = 'Rudy James Jr'
param sqlAdminPrincipalType = 'User'

// SQL Server location override (South Central US has capacity issues)
param sqlLocation = 'eastus2'

param tags = {
  Project: 'RAJ Financial'
  Environment: 'dev'
  ManagedBy: 'Bicep'
  CostCenter: 'Development'
}


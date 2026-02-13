// ============================================================================
// RAJ Financial - Production Environment Parameters
// ============================================================================
using '../main.bicep'

param environment = 'prod'
param location = 'southcentralus'
param baseName = 'rajfinancial'
param resourceGroupNameOverride = 'raj-financial-prod-rg'

// Microsoft Entra External ID - Production Tenant
// Tenant: rajfinancialprod.onmicrosoft.com
param entraExternalIdTenantId = 'cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6'

// SQL Admin - Rudy James Jr (from Azure subscription tenant)
// TODO: Consider using a dedicated SQL Admins group for production
param sqlAdminObjectId = '5641fa2a-0ebd-4b2b-9a2a-76e1bcbfff28'
param sqlAdminName = 'Rudy James Jr'
param sqlAdminPrincipalType = 'User'

param tags = {
  Project: 'RAJ Financial'
  Environment: 'prod'
  ManagedBy: 'Bicep'
  CostCenter: 'Production'
}


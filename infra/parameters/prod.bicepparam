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

// API App Registration (rajfinancial-api)
param entraExternalIdClientId = '8914457b-06ae-447b-bbc1-f2fba572bc99'

// App Roles (defined in API app registration)
param appRoleClient = '45b703ca-e11c-43b5-bb49-3d8843775793'
param appRoleAdministrator = '073dcf8c-01ab-497e-9cf0-7c4422830793'
param appRoleAdvisor = 'fb5b3be7-e8b3-40df-8160-9ac5099b947b'

// Entra Service Principal ID (API app's enterprise application Object ID)
param entraServicePrincipalId = '241a8797-5217-44c9-aadc-0870fa6e5b34'

// SQL Admin - Rudy James Jr (from Azure subscription tenant)
// TODO: Consider using a dedicated SQL Admins group for production
param sqlAdminObjectId = '5641fa2a-0ebd-4b2b-9a2a-76e1bcbfff28'
param sqlAdminName = 'Rudy James Jr'
param sqlAdminPrincipalType = 'User'

// SQL Server location override (Free tier not available in South Central US)
param sqlLocation = 'centralus'

// Function App location override (no Dynamic VM quota in South Central US)
param functionAppLocation = 'centralus'

param tags = {
  Project: 'RAJ Financial'
  Environment: 'prod'
  ManagedBy: 'Bicep'
  CostCenter: 'Production'
}


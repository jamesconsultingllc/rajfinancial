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

// API App Registration (rajfinancial-api-dev)
param entraExternalIdClientId = '211438af-9f00-47be-a367-796dd7770113'

// App Roles (defined in API app registration)
param appRoleClient = 'bc34bd6c-38b8-46a6-9d4c-d338afeea81f'
param appRoleAdministrator = '2202014c-e4b9-4ab9-9e6a-4cc53e13598f'
param appRoleAdvisor = '4e553302-8f00-43ff-a903-359adb9de807'

// Entra Service Principal ID (API app's enterprise application Object ID)
param entraServicePrincipalId = '36d45037-7129-43d5-928b-9daab4862ff1'

// SQL Admin - Rudy James Jr (from Azure subscription tenant)
param sqlAdminObjectId = '5641fa2a-0ebd-4b2b-9a2a-76e1bcbfff28'
param sqlAdminName = 'Rudy James Jr'
param sqlAdminPrincipalType = 'User'

// SQL Server location override (Free tier not available in South Central US)
param sqlLocation = 'centralus'

// Function App location override (no Dynamic VM quota in South Central US)
param functionAppLocation = 'centralus'

param tags = {
  Project: 'RAJ Financial'
  Environment: 'dev'
  ManagedBy: 'Bicep'
  CostCenter: 'Development'
}


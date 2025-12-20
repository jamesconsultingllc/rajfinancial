// ============================================================================
// RAJ Financial - Production Environment Parameters
// ============================================================================
using 'main.bicep'

param environment = 'prod'
param location = 'southcentralus'
param baseName = 'rajfinancial'

// Microsoft Entra External ID - Production Tenant
// Tenant: rajfinancialprod.onmicrosoft.com
param entraExternalIdTenantId = 'cc4d96fb-ebb5-4aef-8ac3-1d4f947dd2b6'

param tags = {
  Project: 'RAJ Financial'
  Environment: 'prod'
  ManagedBy: 'Bicep'
  CostCenter: 'Production'
}


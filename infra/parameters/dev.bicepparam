// ============================================================================
// RAJ Financial - Development Environment Parameters
// ============================================================================
using 'main.bicep'

param environment = 'dev'
param location = 'southcentralus'
param baseName = 'rajfinancial'

// Microsoft Entra External ID - Development Tenant
// Tenant: rajfinancialdev.onmicrosoft.com
param entraExternalIdTenantId = '496527a2-41f8-4297-a979-c916e7255a22'

param tags = {
  Project: 'RAJ Financial'
  Environment: 'dev'
  ManagedBy: 'Bicep'
  CostCenter: 'Development'
}


// ============================================================================
// RAJ Financial - Identity & RBAC Module
// ============================================================================
// Configures role assignments for Managed Identity access to resources.
// This enables passwordless, secure access between Azure services.
// ============================================================================

@description('The principal ID of the Function App Managed Identity')
param functionAppPrincipalId string

@description('The name of the Storage Account')
param storageAccountName string

// ============================================================================
// Role Definition IDs (Built-in Azure RBAC Roles)
// ============================================================================

// Storage Blob Data Contributor - read/write blobs
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

// Storage Queue Data Contributor - read/write queues (for Azure Functions triggers)
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'

// ============================================================================
// Existing Resources
// ============================================================================

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

// Note: SQL Server access is configured via T-SQL after deployment
// See: scripts/infra/configure-sql-access.ps1

// ============================================================================
// Storage Account Role Assignments
// ============================================================================

resource storageBlobContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, functionAppPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    principalId: functionAppPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalType: 'ServicePrincipal'
  }
}

resource storageQueueContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, functionAppPrincipalId, storageQueueDataContributorRoleId)
  scope: storageAccount
  properties: {
    principalId: functionAppPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataContributorRoleId)
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// Note: SQL Database Access
// ============================================================================
// SQL Database access via Managed Identity requires T-SQL commands after
// infrastructure deployment. Run this in the database:
//
//   CREATE USER [func-rajfinancial-dev] FROM EXTERNAL PROVIDER;
//   ALTER ROLE db_datareader ADD MEMBER [func-rajfinancial-dev];
//   ALTER ROLE db_datawriter ADD MEMBER [func-rajfinancial-dev];
//
// This is handled by the post-deployment script: scripts/infra/configure-sql-access.ps1
// ============================================================================

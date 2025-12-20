// ============================================================================
// RAJ Financial - Main Infrastructure Orchestrator
// ============================================================================
// This Bicep file orchestrates the deployment of all Azure resources for
// the RAJ Financial platform. Use environment-specific parameter files.
//
// Usage:
//   az deployment sub create --location eastus --template-file main.bicep --parameters parameters/dev.bicepparam
//   az deployment sub create --location eastus --template-file main.bicep --parameters parameters/prod.bicepparam
// ============================================================================

targetScope = 'subscription'

// ============================================================================
// Parameters
// ============================================================================

@description('The environment name (dev, prod)')
@allowed(['dev', 'prod'])
param environment string

@description('The Azure region for all resources')
param location string = 'southcentralus'

@description('The base name for all resources')
param baseName string = 'rajfinancial'

@description('Microsoft Entra External ID Tenant ID for this environment')
param entraExternalIdTenantId string

@description('Tags to apply to all resources')
param tags object = {
  Project: 'RAJ Financial'
  Environment: environment
  ManagedBy: 'Bicep'
}

// ============================================================================
// Variables
// ============================================================================

var resourceGroupName = 'rg-${baseName}-${environment}'
var keyVaultName = 'kv-${baseName}-${environment}'
var sqlServerName = 'sql-${baseName}-${environment}'
var sqlDatabaseName = 'sqldb-${baseName}-${environment}'
var functionAppName = 'func-${baseName}-${environment}'
var staticWebAppName = 'stapp-${baseName}-${environment}'
var storageAccountName = 'st${baseName}${environment}'
var redisName = 'redis-${baseName}-${environment}'
var appInsightsName = 'appi-${baseName}-${environment}'
var logAnalyticsName = 'log-${baseName}-${environment}'

// ============================================================================
// Resource Group
// ============================================================================

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// ============================================================================
// Monitoring (Deploy first - other resources depend on it)
// ============================================================================

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-deployment'
  scope: rg
  params: {
    location: location
    appInsightsName: appInsightsName
    logAnalyticsName: logAnalyticsName
    tags: tags
  }
}

// ============================================================================
// Key Vault
// ============================================================================

module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault-deployment'
  scope: rg
  params: {
    location: location
    keyVaultName: keyVaultName
    tags: tags
  }
}

// ============================================================================
// Storage Account (for documents/blobs)
// ============================================================================

module storage 'modules/storage.bicep' = {
  name: 'storage-deployment'
  scope: rg
  params: {
    location: location
    storageAccountName: storageAccountName
    tags: tags
  }
}

// ============================================================================
// Azure SQL Database
// ============================================================================

module sql 'modules/sql.bicep' = {
  name: 'sql-deployment'
  scope: rg
  params: {
    location: location
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
    environment: environment
    tags: tags
  }
}

// ============================================================================
// Azure Redis Cache
// ============================================================================

module redis 'modules/redis.bicep' = {
  name: 'redis-deployment'
  scope: rg
  params: {
    location: location
    redisName: redisName
    environment: environment
    tags: tags
  }
}

// ============================================================================
// Azure Functions
// ============================================================================

module functions 'modules/functions.bicep' = {
  name: 'functions-deployment'
  scope: rg
  params: {
    location: location
    functionAppName: functionAppName
    storageAccountName: storage.outputs.storageAccountName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    appInsightsInstrumentationKey: monitoring.outputs.appInsightsInstrumentationKey
    keyVaultUri: keyVault.outputs.keyVaultUri
    environment: environment
    tags: tags
  }
}

// ============================================================================
// Static Web App (Blazor WASM)
// ============================================================================

module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp-deployment'
  scope: rg
  params: {
    location: location
    staticWebAppName: staticWebAppName
    tags: tags
  }
}

// ============================================================================
// Identity & RBAC Assignments
// ============================================================================

module identity 'modules/identity.bicep' = {
  name: 'identity-deployment'
  scope: rg
  params: {
    functionAppPrincipalId: functions.outputs.functionAppPrincipalId
    keyVaultName: keyVault.outputs.keyVaultName
    storageAccountName: storage.outputs.storageAccountName
    sqlServerName: sql.outputs.sqlServerName
  }
}

// ============================================================================
// Outputs
// ============================================================================

output resourceGroupName string = rg.name
output keyVaultUri string = keyVault.outputs.keyVaultUri
output functionAppUrl string = functions.outputs.functionAppUrl
output staticWebAppUrl string = staticWebApp.outputs.staticWebAppUrl
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
output redisHostName string = redis.outputs.redisHostName
output storageAccountName string = storage.outputs.storageAccountName
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString


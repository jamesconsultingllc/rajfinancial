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

@description('Override for resource group name (if not provided, uses rg-{baseName}-{environment})')
param resourceGroupNameOverride string = ''

@description('Microsoft Entra External ID Tenant ID for this environment')
param entraExternalIdTenantId string

@description('Entra External ID API Client ID (from API app registration)')
param entraExternalIdClientId string

@description('Client App Role GUID')
param appRoleClient string

@description('Administrator App Role GUID')
param appRoleAdministrator string

@description('Advisor App Role GUID')
param appRoleAdvisor string

@description('Entra External ID Service Principal ID (GUID, not sensitive)')
param entraServicePrincipalId string = ''

@description('Entra admin object ID for SQL Server (user or group)')
param sqlAdminObjectId string

@description('Entra admin display name for SQL Server')
param sqlAdminName string

@description('Entra admin principal type for SQL Server')
@allowed(['User', 'Group', 'Application'])
param sqlAdminPrincipalType string = 'User'

@description('Override location for SQL Server (some regions have capacity issues)')
param sqlLocation string = ''

@description('Override location for Function App (some regions have quota issues)')
param functionAppLocation string = ''

@description('Tags to apply to all resources')
param tags object = {
  Project: 'RAJ Financial'
  Environment: environment
  ManagedBy: 'Bicep'
}

// ============================================================================
// Variables
// ============================================================================

var resourceGroupName = resourceGroupNameOverride != '' ? resourceGroupNameOverride : 'rg-${baseName}-${environment}'
// SQL naming: prod gets clean name (rajfinancial), dev gets suffix (rajfinancial-dev)
var sqlServerName = environment == 'prod' ? baseName : '${baseName}-${environment}'
var sqlDatabaseName = baseName
var functionAppName = 'func-${baseName}-${environment}'
var storageAccountName = 'st${baseName}${environment}'
// var redisName = 'redis-${baseName}-${environment}'  // Redis disabled
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
    location: sqlLocation != '' ? sqlLocation : location
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
    environment: environment
    entraAdminObjectId: sqlAdminObjectId
    entraAdminName: sqlAdminName
    entraAdminPrincipalType: sqlAdminPrincipalType
    tags: tags
  }
}

// ============================================================================
// Azure Redis Cache (DISABLED - not needed yet, saves ~$15/month)
// ============================================================================
// Uncomment when caching is needed for performance optimization
// module redis 'modules/redis.bicep' = {
//   name: 'redis-deployment'
//   scope: rg
//   params: {
//     location: location
//     redisName: redisName
//     environment: environment
//     tags: tags
//   }
// }

// ============================================================================
// Azure Functions
// ============================================================================

module functions 'modules/functions.bicep' = {
  name: 'functions-deployment'
  scope: rg
  params: {
    location: functionAppLocation != '' ? functionAppLocation : location
    functionAppName: functionAppName
    storageAccountName: storage.outputs.storageAccountName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    appInsightsInstrumentationKey: monitoring.outputs.appInsightsInstrumentationKey
    entraServicePrincipalId: entraServicePrincipalId
    sqlServerFqdn: sql.outputs.sqlServerFqdn
    sqlDatabaseName: sqlDatabaseName
    entraExternalIdTenantId: entraExternalIdTenantId
    entraExternalIdClientId: entraExternalIdClientId
    appRoleClient: appRoleClient
    appRoleAdministrator: appRoleAdministrator
    appRoleAdvisor: appRoleAdvisor
    environment: environment
    tags: tags
  }
}

// ============================================================================
// Static Web App with Linked Backend
// ============================================================================
// NOTE: SWA already exists in 'rajfinancial' resource group (Central US)
// The SWA was created before this Bicep and uses preview environments.
// Existing SWA: gray-cliff-072f3b510.azurestaticapps.net
// Custom Domain: app.rajfinancial.net
//
// To migrate to IaC-managed SWA:
// 1. Update DNS CNAME for app.rajfinancial.net to point to new SWA hostname
// 2. Uncomment the module below
// 3. Deploy Bicep
// 4. Get new deployment token: az staticwebapp secrets list --name <swa> --query "properties.apiKey" -o tsv
// 5. Update GitHub secret AZURE_STATIC_WEB_APPS_API_TOKEN with new token
// 6. Update workflow to use new SWA name in deploy action
// ============================================================================

// module staticWebApp 'modules/staticwebapp.bicep' = {
//   name: 'staticwebapp-deployment'
//   scope: rg
//   params: {
//     location: 'centralus'  // Must match existing SWA region
//     staticWebAppName: 'stapp-${baseName}-${environment}'
//     skuName: 'Standard'  // Standard required for linked backends
//     linkedBackendResourceId: functions.outputs.functionAppId
//     linkedBackendRegion: 'centralus'
//     customDomain: environment == 'prod' ? 'app.rajfinancial.net' : ''
//     repositoryUrl: 'https://github.com/jamesconsultingllc/rajfinancial'
//     repositoryBranch: environment == 'prod' ? 'main' : 'develop'
//     tags: tags
//   }
// }

// ============================================================================
// Identity & RBAC Assignments
// ============================================================================

module identity 'modules/identity.bicep' = {
  name: 'identity-deployment'
  scope: rg
  params: {
    functionAppPrincipalId: functions.outputs.functionAppPrincipalId
    storageAccountName: storage.outputs.storageAccountName
  }
}

// ============================================================================
// Outputs
// ============================================================================

output resourceGroupName string = rg.name
output functionAppUrl string = functions.outputs.functionAppUrl
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
// output redisHostName string = redis.outputs.redisHostName  // Redis disabled
output storageAccountName string = storage.outputs.storageAccountName
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString


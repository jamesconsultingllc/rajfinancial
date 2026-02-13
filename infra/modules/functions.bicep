// ============================================================================
// RAJ Financial - Azure Functions Module
// ============================================================================
// Creates an Azure Functions app for the API backend.
// Uses Consumption plan for dev, Premium for prod.
//
// Security:
//   - System-assigned Managed Identity
//   - Key Vault references for secrets
//   - HTTPS only
// ============================================================================

@description('The Azure region for resources')
param location string

@description('The name for Function App')
param functionAppName string

@description('The storage account name for Function App')
param storageAccountName string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Application Insights instrumentation key')
param appInsightsInstrumentationKey string

@description('Key Vault name for secret references')
param keyVaultName string

@description('SQL Server FQDN')
param sqlServerFqdn string

@description('SQL Database name')
param sqlDatabaseName string

@description('Entra External ID Tenant ID')
param entraExternalIdTenantId string

@description('Entra External ID API Client ID')
param entraExternalIdClientId string

@description('Client App Role GUID')
param appRoleClient string

@description('Administrator App Role GUID')
param appRoleAdministrator string

@description('The environment (dev or prod)')
@allowed(['dev', 'prod'])
param environment string

@description('Tags to apply to resources')
param tags object

// ============================================================================
// App Service Plan
// ============================================================================

var planName = 'asp-${functionAppName}'

// Use Consumption (Y1) for dev - pay per execution, no VM quota needed
// Use Elastic Premium EP1 for prod - scale-out, always warm, VNet support
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: environment == 'dev' ? {
    name: 'Y1'
    tier: 'Dynamic'
  } : {
    name: 'EP1'
    tier: 'ElasticPremium'
  }
  properties: {
    reserved: false // Windows
  }
}

// ============================================================================
// Function App
// ============================================================================

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      use32BitWorkerProcess: false
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      cors: {
        allowedOrigins: [
          'https://localhost:5001'
          'https://localhost:7001'
          'https://*.azurestaticapps.net'
          'https://app.rajfinancial.net'
          'https://rajfinancial.com'
        ]
        supportCredentials: true
      }
      appSettings: [
        // ====================================================================
        // Azure Functions Runtime
        // ====================================================================
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        // ====================================================================
        // Monitoring
        // ====================================================================
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        // ====================================================================
        // Database - Connection string built from components (Managed Identity)
        // ====================================================================
        {
          name: 'SqlConnectionString'
          value: 'Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default'
        }
        // ====================================================================
        // Entra External ID (non-sensitive - public values from tokens)
        // ====================================================================
        {
          name: 'EntraExternalId__TenantId'
          value: entraExternalIdTenantId
        }
        {
          name: 'EntraExternalId__ClientId'
          value: entraExternalIdClientId
        }
        // ====================================================================
        // Entra External ID (sensitive - Key Vault reference)
        // ====================================================================
        {
          name: 'EntraExternalId__ServicePrincipalId'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=EntraExternalId-ServicePrincipalId)'
        }
        // ====================================================================
        // App Roles
        // ====================================================================
        {
          name: 'AppRoles__Client'
          value: appRoleClient
        }
        {
          name: 'AppRoles__Administrator'
          value: appRoleAdministrator
        }
      ]
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

output functionAppId string = functionApp.id
output functionAppName string = functionApp.name
output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
output functionAppPrincipalId string = functionApp.identity.principalId
output functionAppTenantId string = functionApp.identity.tenantId

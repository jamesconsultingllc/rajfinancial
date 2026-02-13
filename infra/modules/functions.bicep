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

@description('Key Vault URI for secret references')
param keyVaultUri string

@description('The environment (dev or prod)')
@allowed(['dev', 'prod'])
param environment string

@description('Tags to apply to resources')
param tags object

// ============================================================================
// App Service Plan
// ============================================================================

var planName = 'asp-${functionAppName}'

// Use Basic B1 for dev (Consumption/Dynamic not available in all subscriptions)
// Use Elastic Premium EP1 for prod (scale-out, always warm)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: environment == 'dev' ? {
    name: 'B1'
    tier: 'Basic'
    size: 'B1'
    capacity: 1
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
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'KeyVaultUri'
          value: keyVaultUri
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
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

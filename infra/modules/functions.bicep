// ============================================================================
// RAJ Financial - Azure Functions Module
// ============================================================================
// Creates an Azure Functions app for the API backend.
// Uses Consumption (Y1) plan — upgrade to EP1 if VNet/always-warm needed.
//
// Security:
//   - System-assigned Managed Identity
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

@description('Entra External ID Service Principal ID')
param entraServicePrincipalId string

@description('SQL Server FQDN')
param sqlServerFqdn string

@description('SQL Database name')
param sqlDatabaseName string

@description('Entra External ID Tenant ID')
param entraExternalIdTenantId string

@description('Entra External ID API Client ID')
param entraExternalIdClientId string

@description('Entra External ID authority instance URL (e.g., https://rajfinancialdev.ciamlogin.com/). Used by JwtBearerValidator to build the OIDC discovery URL.')
param entraExternalIdInstance string

@description('Accepted values for the JWT aud claim. Include both the App ID URI (api://...) and the client-id GUID to tolerate token issuance variants.')
param entraExternalIdValidAudiences array

@description('Client App Role GUID')
param appRoleClient string

@description('Administrator App Role GUID')
param appRoleAdministrator string

@description('Advisor App Role GUID')
param appRoleAdvisor string

@description('Tags to apply to resources')
param tags object

// ============================================================================
// App Service Plan
// ============================================================================

var planName = 'asp-${functionAppName}'

// Expand the ValidAudiences array into individual app settings. The .NET configuration
// binder flattens `EntraExternalId__ValidAudiences__N` entries into the IList<string>
// ValidAudiences property on EntraExternalIdOptions.
var validAudiencesAppSettings = [for (audience, index) in entraExternalIdValidAudiences: {
  name: 'EntraExternalId__ValidAudiences__${index}'
  value: audience
}]

// Use Consumption (Y1) plan for all environments - pay per execution only.
// To use EP1 (Elastic Premium) for VNet integration or always-warm, update this SKU explicitly.
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
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
      netFrameworkVersion: 'v10.0'
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
        ]
        supportCredentials: true
      }
      appSettings: concat([
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
        {
          name: 'EntraExternalId__Instance'
          value: entraExternalIdInstance
        }
        // ====================================================================
        // Entra External ID (Service Principal ID - not sensitive)
        // ====================================================================
        {
          name: 'EntraExternalId__ServicePrincipalId'
          value: entraServicePrincipalId
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
        {
          name: 'AppRoles__Advisor'
          value: appRoleAdvisor
        }
      ], validAudiencesAppSettings)
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

// ============================================================================
// RAJ Financial - Static Web App Module
// ============================================================================
// Creates an Azure Static Web App for hosting the React frontend.
// Optionally links a backend Azure Functions app for /api/* proxying.
// ============================================================================

@description('The Azure region for resources')
param location string

@description('The name for Static Web App')
param staticWebAppName string

@description('Tags to apply to resources')
param tags object

@description('SKU name for the Static Web App')
@allowed(['Free', 'Standard'])
param skuName string = 'Standard'

@description('Optional: Resource ID of the Azure Functions app to link as backend')
param linkedBackendResourceId string = ''

@description('Optional: Region of the linked backend (must match Functions app region)')
param linkedBackendRegion string = ''

@description('Optional: Custom domain to configure (e.g., app.rajfinancial.net)')
param customDomain string = ''

@description('GitHub repository URL for source control integration')
param repositoryUrl string = ''

@description('GitHub branch for deployments')
param repositoryBranch string = 'main'

// ============================================================================
// Static Web App
// ============================================================================

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    repositoryUrl: repositoryUrl != '' ? repositoryUrl : null
    branch: repositoryUrl != '' ? repositoryBranch : null
    provider: repositoryUrl != '' ? 'GitHub' : null
    buildProperties: {
      skipGithubActionWorkflowGeneration: true
    }
  }
}

// ============================================================================
// Linked Backend (Azure Functions API)
// ============================================================================
// Links the standalone Azure Functions app as a backend.
// SWA will proxy /api/* requests to the Functions app automatically.
// This requires Standard tier SWA.
// ============================================================================

resource linkedBackend 'Microsoft.Web/staticSites/linkedBackends@2023-12-01' = if (linkedBackendResourceId != '') {
  parent: staticWebApp
  name: 'backend'
  properties: {
    backendResourceId: linkedBackendResourceId
    region: linkedBackendRegion != '' ? linkedBackendRegion : location
  }
}

// ============================================================================
// Custom Domain
// ============================================================================
// Adds a custom domain to the SWA. Requires DNS CNAME record pointing to
// the SWA default hostname BEFORE deployment.
// ============================================================================

resource customDomainResource 'Microsoft.Web/staticSites/customDomains@2023-12-01' = if (customDomain != '') {
  parent: staticWebApp
  name: customDomain
  dependsOn: [
    linkedBackend
  ]
  properties: {}
}

// ============================================================================
// Outputs
// ============================================================================

output staticWebAppId string = staticWebApp.id
output staticWebAppName string = staticWebApp.name
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname

// Note: Deployment token must be retrieved via CLI after deployment:
// az staticwebapp secrets list --name <swa-name> --resource-group <rg> --query "properties.apiKey" -o tsv

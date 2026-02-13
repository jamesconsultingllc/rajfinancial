// ============================================================================
// RAJ Financial - Key Vault Module
// ============================================================================
// Creates Key Vault and optionally stores Entra External ID secrets.
// Secrets are only created if the parameter values are provided.
// ============================================================================

@description('The Azure region for resources')
param location string

@description('The name for Key Vault')
param keyVaultName string

@description('Tags to apply to resources')
param tags object

// ============================================================================
// Optional Entra External ID Secrets (passed from PowerShell registration)
// ============================================================================

@description('Entra External ID Service Principal ID (optional - only stored if provided)')
@secure()
param entraServicePrincipalId string = ''

// ============================================================================
// Key Vault
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// ============================================================================
// Secrets (only created if values provided)
// ============================================================================

resource servicePrincipalIdSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(entraServicePrincipalId)) {
  parent: keyVault
  name: 'EntraExternalId-ServicePrincipalId'
  properties: {
    value: entraServicePrincipalId
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri


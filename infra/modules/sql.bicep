// ============================================================================
// RAJ Financial - Azure SQL Database Module
// ============================================================================
// Creates an Azure SQL Server with a database using the FREE tier for dev
// and a production-appropriate tier for prod.
//
// Security:
//   - Entra-only authentication (no SQL passwords)
//   - Managed Identity access (no connection strings with secrets)
//   - TLS 1.2 minimum
//   - Auditing enabled
// ============================================================================

@description('The Azure region for resources')
param location string

@description('The name for SQL Server')
param sqlServerName string

@description('The name for SQL Database')
param sqlDatabaseName string

@description('The environment (dev or prod)')
@allowed(['dev', 'prod'])
param environment string

@description('Tags to apply to resources')
param tags object

@description('Entra admin object ID for SQL Server administration (user or group)')
param entraAdminObjectId string

@description('Entra admin display name (user or group name)')
param entraAdminName string

@description('Entra admin principal type')
@allowed(['User', 'Group', 'Application'])
param entraAdminPrincipalType string = 'User'

// ============================================================================
// SQL Server
// ============================================================================

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    // Entra-only authentication (most secure - no SQL passwords)
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: entraAdminName
      principalType: entraAdminPrincipalType
      sid: entraAdminObjectId
      tenantId: subscription().tenantId
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    restrictOutboundNetworkAccess: 'Disabled'
  }
}

// ============================================================================
// SQL Database
// ============================================================================
// FREE tier (dev): 32GB storage, 100 DTUs burst, perfect for development
// Basic tier (prod): Entry-level production with SLA
// ============================================================================

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: environment == 'dev' ? {
    name: 'Free'
    tier: 'Free'
  } : {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: environment == 'dev' ? 34359738368 : 2147483648 // 32GB free, 2GB basic
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: environment == 'dev' ? 'Local' : 'Geo'
    isLedgerOn: false
  }
}

// ============================================================================
// Firewall Rules
// ============================================================================

// Allow Azure services (required for Azure Functions with Managed Identity)
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ============================================================================
// Auditing (Security Best Practice)
// ============================================================================

resource sqlServerAudit 'Microsoft.Sql/servers/auditingSettings@2023-08-01-preview' = {
  parent: sqlServer
  name: 'default'
  properties: {
    state: 'Enabled'
    isAzureMonitorTargetEnabled: true
    retentionDays: environment == 'dev' ? 7 : 90
  }
}

// ============================================================================
// Advanced Threat Protection (Prod only - has cost)
// ============================================================================

resource threatProtection 'Microsoft.Sql/servers/securityAlertPolicies@2023-08-01-preview' = if (environment == 'prod') {
  parent: sqlServer
  name: 'Default'
  properties: {
    state: 'Enabled'
    emailAccountAdmins: true
  }
}

// ============================================================================
// Outputs
// ============================================================================

output sqlServerId string = sqlServer.id
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseId string = sqlDatabase.id
output sqlDatabaseName string = sqlDatabase.name
output sqlServerPrincipalId string = sqlServer.identity.principalId

// Connection string for Managed Identity (no secrets!)
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabase.name};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;'

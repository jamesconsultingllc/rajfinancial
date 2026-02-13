// ============================================================================
// RAJ Financial - Azure Redis Cache Module
// ============================================================================
// Creates an Azure Redis Cache for session state and caching.
// Uses Basic tier for dev (lowest cost) and Standard for prod (SLA).
//
// Access:
//   - Managed Identity authentication via Entra ID
//   - No access keys required
// ============================================================================

@description('The Azure region for resources')
param location string

@description('The name for Redis Cache')
param redisName string

@description('The environment (dev or prod)')
@allowed(['dev', 'prod'])
param environment string

@description('Tags to apply to resources')
param tags object

// ============================================================================
// Redis Cache
// ============================================================================

resource redis 'Microsoft.Cache/redis@2024-03-01' = {
  name: redisName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    sku: {
      name: environment == 'dev' ? 'Basic' : 'Standard'
      family: 'C'
      capacity: 0 // C0 = 250MB, lowest tier
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisVersion: '6'
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'aad-enabled': 'true'
      'maxmemory-policy': 'volatile-lru'
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

output redisId string = redis.id
output redisName string = redis.name
output redisHostName string = redis.properties.hostName
output redisSslPort int = redis.properties.sslPort
output redisPrincipalId string = redis.identity.principalId

// Connection string for Managed Identity (no access keys!)
output connectionString string = '${redis.properties.hostName}:${redis.properties.sslPort},ssl=True,abortConnect=False'

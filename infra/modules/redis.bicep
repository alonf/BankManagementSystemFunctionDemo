param redisCacheName string
param location string
resource redisCache 'Microsoft.Cache/Redis@2022-06-01' = {
  name: redisCacheName
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
  }
}

output redisConnectionString string = redisCache.properties.hostName
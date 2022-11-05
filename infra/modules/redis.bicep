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
var primaryKey = listkeys(resourceId('Microsoft.Cache/Redis', redisCacheName), '2022-06-01').primaryKey
output redisConnectionString string = '${redisCache.properties.hostName}:${redisCache.properties.sslPort},password=${primaryKey},ssl=True,abortConnect=False,syncTimeout=2000,allowAdmin=true'


    
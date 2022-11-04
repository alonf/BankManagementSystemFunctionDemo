param functionAppName string
param location string
param tags object
param appInsightsInstrumentationKey string
param storageAccountConnectionString string
param hostingPlanId string
param additionalAppSettings array = []


resource functionApp 'Microsoft.Web/sites@2022-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  tags: tags
  
  properties: {
    httpsOnly: true
    serverFarmId: hostingPlanId
    clientAffinityEnabled: true
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v6.0'
      use32BitWorkerProcess: false
      
      appSettings: union([
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'AzureWebJobsStorage'
          value: storageAccountConnectionString
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageAccountConnectionString
        }
      ], additionalAppSettings)
    }
  }
}
  
//output the function base url
output functionBaseUrl string = 'https://${functionApp.properties.defaultHostName}/api'

//output the function key
output functionAppKey string = listKeys('${functionApp.id}/host/default', functionApp.apiVersion).functionKeys.default
                                
param functionsAppName string
param location string
param tags object
param appInsightsInstrumentationKey string
param storageAccountConnectionString string
param hostingPlanId string
param additionalAppSettings array = []


resource functionsApp 'Microsoft.Web/sites@2022-03-01' = {
  name: functionsAppName
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
  
//output the function app url
output functionsAppUrl string = functionsApp.properties.defaultHostName


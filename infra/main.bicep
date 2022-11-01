param branchName string
param cosmosDbUrl string
param cosmosDBDatabaseName string
param location string = resourceGroup().location

@secure()
param cosmosDBConnectionString string

@secure()
param cosmosDBKey string

param tags object = {}

var branch = toLower(last(split(branchName, '/')))

var signalRName = '${branch}-bms-signalr'
var servicebusNamespaceName = '${branch}-bms-servicebus'

var workspaceName = '${branch}-bms-log-analytics'
var appInsightsName = '${branch}-bms-app-insights'
var storageAccountName = '${branch}-bms-storageaccount'
var azureFunctionsHostingPlanName = '${branch}-bms-hostingplan'
var BMSAccountManagerServiceFunctionsAppName = 'accountmanager' 
var BMSNotificationManagerServiceFunctionsAppName = 'notificationmanager'
var BMSUserInfoAccessorServiceFunctionsAppName = 'userinfoaccessor' 
var BMSCheckingAccountAccessorServiceFunctionsAppName = 'checkingaccountaccessor'
var BMSLiabilityValidatorEngineServiceFunctionsAppName = 'liabilityvalidatorengine' 


//create the containers app required services
module functionsAppInfra 'modules/functions-app-infra.bicep' = {
  name: 'functionsAppInfraDeployment'
  params: {
    location: location
    appInsightsName: appInsightsName
    workspaceName: workspaceName
    storageAccountName: storageAccountName
    hostingPlanName: azureFunctionsHostingPlanName
    tags: tags
  }
}
var hostingPlanId = functionsAppInfra.outputs.hostingPlanId
var appInsightsInstrumentationKey = functionsAppInfra.outputs.appInsightsInstrumentationKey
var storageAccountConnectionString = functionsAppInfra.outputs.storageAccountConnectionString



module signalr 'modules/signalr.bicep' = {
  name: 'signalrDeployment'
  params: {
    signalRName: signalRName
    location: location
  }
}
var signalRConnectionString = signalr.outputs.signalRConnectionString


module servicebus 'modules/servicebus.bicep' = {
  name: 'servicebusQueuesAndPubSubDeployment'
  params: {
    location: location
    servicebusNamespaceName: servicebusNamespaceName
  }
}
var serviceBusConnectionString = servicebus.outputs.serviceBusConnectionString



module BMSCheckingAccountAccessorFunctionsApp 'modules/functions-app.bicep' = {
  name: BMSCheckingAccountAccessorServiceFunctionsAppName
  params: {
    functionsAppName: BMSCheckingAccountAccessorServiceFunctionsAppName
    appInsightsInstrumentationKey: appInsightsInstrumentationKey
    tags: tags
    hostingPlanId: hostingPlanId
    storageAccountConnectionString: storageAccountConnectionString
    location: location
    additionalAppSettings: [
     {
       name: 'QueueConnectionString'
       value:
     }
    ]
  }
   dependsOn:  [
    functionsAppInfra
    ]
}


module BMSUserInfoAccessorFunctionsApp 'modules/functions-app.bicep' = {
  name: BMSUserInfoAccessorServiceFunctionsAppName
  params: {
    functionsAppName: BMSUserInfoAccessorServiceFunctionsAppName
    appInsightsInstrumentationKey: appInsightsInstrumentationKey
    tags: tags
    hostingPlanId: hostingPlanId
    storageAccountConnectionString: storageAccountConnectionString
    location: location
  }
   dependsOn:  [
    functionsAppInfra
    ]
}

module BMSLiabilityValidatorEngineFunctionsApp 'modules/functions-app.bicep' = {
  name: BMSLiabilityValidatorEngineServiceFunctionsAppName
  params: {
    functionsAppName: BMSLiabilityValidatorEngineServiceFunctionsAppName
    appInsightsInstrumentationKey: appInsightsInstrumentationKey
    tags: tags
    hostingPlanId: hostingPlanId
    storageAccountConnectionString: storageAccountConnectionString
    location: location
  }
   dependsOn:  [
    functionsAppInfra
    ]
}


module BMSNotificationManagerFunctionsApp 'modules/functions-app.bicep' = {
  name: BMSNotificationManagerServiceFunctionsAppName
  params: {
    functionsAppName: BMSNotificationManagerServiceFunctionsAppName
    appInsightsInstrumentationKey: appInsightsInstrumentationKey
    tags: tags
    hostingPlanId: hostingPlanId
    storageAccountConnectionString: storageAccountConnectionString
    location: location
  }
   dependsOn:  [
    functionsAppInfra
    ]
}


module BMSAccountManagerFunctionsApp 'modules/functions-app.bicep' = {
  name: BMSAccountManagerServiceFunctionsAppName
  params: {
    functionsAppName: BMSAccountManagerServiceFunctionsAppName
    appInsightsInstrumentationKey: appInsightsInstrumentationKey
    tags: tags
    hostingPlanId: hostingPlanId
    storageAccountConnectionString: storageAccountConnectionString
    location: location
  }
   dependsOn:  [
    functionsAppInfra
    ]
}


output webServiceUrl string = BMSAccountManagerFunctionsApp.outputs.functionsAppUrl
      
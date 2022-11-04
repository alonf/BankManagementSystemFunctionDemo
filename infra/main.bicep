param branchName string
param location string = resourceGroup().location

@secure()
param cosmosDBConnectionString string

param tags object = {}
var branch = toLower(last(split(branchName, '/')))

var signalRName = '${branch}-bms-signalr'
var redisName = '${branch}-bms-redis'

var workspaceName = '${branch}-bms-log-analytics'
var appInsightsName = '${branch}-bms-app-insights'
var storageAccountName = '${branch}bmsstorageaccount'
var azureFunctionsHostingPlanName = '${branch}-bms-hostingplan'
var BMSAccountManagerServiceFunctionAppName = '${branch}-bms-accountmanager' 
var BMSNotificationManagerServiceFunctionAppName = '${branch}-bms-notificationmanager'
var BMSUserInfoAccessorServiceFunctionAppName = '${branch}-bms-userinfoaccessor' 
var BMSCheckingAccountAccessorServiceFunctionAppName = '${branch}-bms-checkingaccountaccessor'
var BMSLiabilityValidatorEngineServiceFunctionAppName = '${branch}-bms-liabilityvalidatorengine' 


//create the containers app required services
module functionAppInfra 'modules/functions-app-infra.bicep' = {
  name: 'functionAppInfraDeployment'
  params: {
    location: location
    appInsightsName: appInsightsName
    workspaceName: workspaceName
    storageAccountName: storageAccountName
    hostingPlanName: azureFunctionsHostingPlanName
    tags: tags
  }
}
var hostingPlanId = functionAppInfra.outputs.hostingPlanId
var appInsightsInstrumentationKey = functionAppInfra.outputs.appInsightsInstrumentationKey
var storageAccountConnectionString = functionAppInfra.outputs.storageAccountConnectionString


module signalr 'modules/signalr.bicep' = {
  name: 'signalrDeployment'
  params: {
    signalRName: signalRName
    location: location
  }
}
var signalRConnectionString = signalr.outputs.signalRConnectionString

module redis 'modules/redis.bicep' = {
  name: 'redisDeployment'
  params: {
    redisCacheName: redisName
    location: location
  }
}
var redisConnectionString = redis.outputs.redisConnectionString

//module storagequeue 'modules/storagequeue.bicep' = {
//  name: 'storageQueuesDeployment'
//  params: {
//   storageAccountName: storageAccountName
//  }
//}

module BMSCheckingAccountAccessorFunctionApp 'modules/functions-app.bicep' = {
  name: BMSCheckingAccountAccessorServiceFunctionAppName
  params: {
    functionAppName: BMSCheckingAccountAccessorServiceFunctionAppName
    appInsightsInstrumentationKey: appInsightsInstrumentationKey
    tags: tags
    hostingPlanId: hostingPlanId
    storageAccountConnectionString: storageAccountConnectionString
    location: location
    additionalAppSettings: [
     {
       name: 'QueueConnectionString'
       value: storageAccountConnectionString
     }
     {
       name: 'cosmosDBConnectionString'
       value: cosmosDBConnectionString
     }
    ]
  }
   dependsOn:  [
    functionAppInfra
    ]
}

var getBalanceUrl = '${BMSCheckingAccountAccessorFunctionApp.outputs.functionBaseUrl}/getBalanceUrl?key=${BMSCheckingAccountAccessorFunctionApp.outputs.functionAppKey}'
var getAccountTransactionHistoryUrl = '${BMSCheckingAccountAccessorFunctionApp.outputs.functionBaseUrl}/GetAccountTransactionHistory?key=${BMSCheckingAccountAccessorFunctionApp.outputs.functionAppKey}'
var getAccountInfoUrl = '${BMSCheckingAccountAccessorFunctionApp.outputs.functionBaseUrl}/GetAccountInfo?key=${BMSCheckingAccountAccessorFunctionApp.outputs.functionAppKey}'

module BMSUserInfoAccessorFunctionApp 'modules/functions-app.bicep' = {
  name: BMSUserInfoAccessorServiceFunctionAppName
  params: {
    functionAppName: BMSUserInfoAccessorServiceFunctionAppName
    appInsightsInstrumentationKey: appInsightsInstrumentationKey
    tags: tags
    hostingPlanId: hostingPlanId
    storageAccountConnectionString: storageAccountConnectionString
    location: location
    additionalAppSettings: [
     {
       name: 'QueueConnectionString'
       value: storageAccountConnectionString
     }
     {
       name: 'cosmosDBConnectionString'
       value: cosmosDBConnectionString
     }
    ]
  }
   dependsOn:  [
    functionAppInfra
    ]
}

var getAccountIdByEmailUrl = '${BMSUserInfoAccessorFunctionApp}.outputs.functionBaseUrl}/GetAccountIdByEmail?key=${BMSUserInfoAccessorFunctionApp.outputs.functionAppKey}'



module BMSLiabilityValidatorEngineFunctionApp 'modules/functions-app.bicep' = {
  name: BMSLiabilityValidatorEngineServiceFunctionAppName
  params: {
    functionAppName: BMSLiabilityValidatorEngineServiceFunctionAppName
    appInsightsInstrumentationKey: appInsightsInstrumentationKey
    tags: tags
    hostingPlanId: hostingPlanId
    storageAccountConnectionString: storageAccountConnectionString
    location: location
    additionalAppSettings: [
     {
       name: 'getBalanceUrl'
       value: getBalanceUrl
     }
     {
       name: 'getAccountInfoUrl'
       value: getAccountInfoUrl
     }
    ]
  }
   dependsOn:  [
    functionAppInfra
    BMSCheckingAccountAccessorFunctionApp
    BMSUserInfoAccessorFunctionApp
    ]
}

var checkLiabilityUrl = '${BMSLiabilityValidatorEngineFunctionApp.outputs.functionBaseUrl}/CheckLiabilityUrl?key=${BMSLiabilityValidatorEngineFunctionApp.outputs.functionAppKey}'


module BMSNotificationManagerFunctionApp 'modules/functions-app.bicep' = {
  name: BMSNotificationManagerServiceFunctionAppName
  params: {
    functionAppName: BMSNotificationManagerServiceFunctionAppName
    appInsightsInstrumentationKey: appInsightsInstrumentationKey
    tags: tags
    hostingPlanId: hostingPlanId
    storageAccountConnectionString: storageAccountConnectionString
    location: location
    additionalAppSettings: [
     {
       name: 'QueueConnectionString'
       value: storageAccountConnectionString
     }
     {
       name: 'AzureSignalRConnectionString'
       value: signalRConnectionString
     }
    ]
  }
   dependsOn:  [
    functionAppInfra
    signalr
    ]
}


module BMSAccountManagerFunctionApp 'modules/functions-app.bicep' = {
  name: BMSAccountManagerServiceFunctionAppName
  params: {
    functionAppName: BMSAccountManagerServiceFunctionAppName
    appInsightsInstrumentationKey: appInsightsInstrumentationKey
    tags: tags
    hostingPlanId: hostingPlanId
    storageAccountConnectionString: storageAccountConnectionString
    location: location
    additionalAppSettings: [
     {
       name: 'QueueConnectionString'
       value: storageAccountConnectionString
     }
     {
       name: 'getBalanceUrl'
       value: getBalanceUrl
     }
     {
       name: 'getAccountTransactionHistoryUrl'
       value: getAccountTransactionHistoryUrl
     }
     {
       name: 'getAccountIdByEmailUrl'
       value: getAccountIdByEmailUrl
     }
     {
       name: 'checkLiabilityUrl'
       value: checkLiabilityUrl
     }
     {
         name: 'RedisConnectionString'
         value: redisConnectionString
     }
   ]
  }
   dependsOn:  [
    functionAppInfra
    redis
    BMSLiabilityValidatorEngineFunctionApp
    ]
}


output accountManagerUrl string = '${BMSAccountManagerFunctionApp.outputs.functionBaseUrl}?key=${BMSNotificationManagerFunctionApp.outputs.functionAppKey}'
output negotiateUrl string = '${BMSNotificationManagerFunctionApp.outputs.functionBaseUrl}/Negotiate?key=${BMSNotificationManagerFunctionApp.outputs.functionAppKey}'

      
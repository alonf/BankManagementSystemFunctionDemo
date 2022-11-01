//param storageAccountName string

//param queueNames array = [
//  'accounttransactionqueue'
//  'client-response-queue'
//  'customer-registration-queue'
//]


//resource queues 'Microsoft.Storage/storageAccounts/queueServices/queues@2022-05-01' = [for queueName in queueNames: {
//  name: '${storageAccountName}/${queueName}'
//  properties: {
//  }
//}]

////output the account transaction queue connection string
//output accountTransactionQueueConnectionString string = queues[0].properties.metadata.connectionString

////output the client response queue connection string
//output clientResponseQueueConnectionString string = queues[1].properties.metadata.connectionString

////output the customer registration queue connection string
//output customerRegistrationQueueConnectionString string = queues[2].properties.metadata.connectionString
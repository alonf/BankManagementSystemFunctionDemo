using System;
using System.Collections;
using BMS.Accessors.CheckingAccount.DB;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace BMS.Accessors.CheckingAccount
{
    public class CheckingAccountAccessor
    {
        const string DatabaseName = "BMSDB";
        const string CollectionName = "AccountTransaction";
        private readonly IAccountDBFactory _accountDBFactory;
        private readonly ITransactionDBFactory _transactionDBFactory;

        public CheckingAccountAccessor(IAccountDBFactory accountDBFactory, ITransactionDBFactory transactionDBFactory)
        {
            _accountDBFactory = accountDBFactory;
            _transactionDBFactory = transactionDBFactory;
        }

        [FunctionName("UpdateAccount")]
        public void UpdateAccount([QueueTrigger("account-transaction-queue", Connection = "QueueConnectionString")]string myQueueItem,
            [CosmosDB(
                    databaseName: DatabaseName,
                    collectionName: CollectionName,
                    ConnectionStringSetting = "cosmosDBConnectionString")]
                    DocumentClient documentClient, ILogger log)
        {
            log.LogInformation($"UpdateAccount Queue trigger function processed: {myQueueItem}");
        }
    }
}

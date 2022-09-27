using BMS.Accessors.CheckingAccount.Contracts.Requests;
using BMS.Accessors.CheckingAccount.DB;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;

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
        public void UpdateAccount([QueueTrigger("account-transaction-queue", Connection = "QueueConnectionString")] AccountTransactionRequest requestItem,
            [CosmosDB(
                    databaseName: DatabaseName,
                    collectionName: CollectionName,
                    ConnectionStringSetting = "cosmosDBConnectionString")]
                    DocumentClient documentClient, ILogger log)
        {
            try
            {
                log.LogInformation($"UpdateAccount Queue trigger function processed request id: {requestItem.RequestId}");

                var transactionDB = _transactionDBFactory.Create(documentClient);
                transactionDB.UpdateBalance(requestItem.RequestId, requestItem.AccountId, requestItem.Amount);
                //todo: pubsub on success
            }
            catch(Exception ex)
            {
                //todo: pubsub on failure if not transiate
            }

        }
    }
}

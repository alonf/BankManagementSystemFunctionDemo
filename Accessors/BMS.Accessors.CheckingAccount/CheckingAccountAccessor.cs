using BMS.Accessors.CheckingAccount.Contracts.Requests;
using BMS.Accessors.CheckingAccount.Contracts.Responses;
using BMS.Accessors.CheckingAccount.DB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace BMS.Accessors.CheckingAccount
{
    public class CheckingAccountAccessor
    {
        const string DatabaseName = "BMSDB";
        const string CollectionName = "AccountTransaction";
        private readonly ICosmosDBWrapperFactory _cosmosDBWrapperFactory;

        public CheckingAccountAccessor(ICosmosDBWrapperFactory cosmosDBWrapperFactory)
        {
            _cosmosDBWrapperFactory = cosmosDBWrapperFactory;
        }

        [FunctionName("UpdateAccount")]
        public async Task UpdateAccountAsync([QueueTrigger("account-transaction-queue", Connection = "QueueConnectionString")] AccountTransactionRequest requestItem,
            [CosmosDB(
                    databaseName: DatabaseName,
                    collectionName: CollectionName,
                    ConnectionStringSetting = "cosmosDBConnectionString")]
                    DocumentClient documentClient, ILogger log)
        {
            try
            {
                log.LogInformation($"UpdateAccount Queue trigger function processed request id: {requestItem.RequestId}");

                var cosmosDBWrapper = _cosmosDBWrapperFactory.Create(documentClient, DatabaseName, log);
                await cosmosDBWrapper.UpdateBalanceAsync(requestItem.RequestId, requestItem.AccountId, requestItem.Amount);
                //todo: push to upstream queue on success
            }
            catch(Exception ex)
            {
                //todo: push to upstream queue on failure if not a transient failure
                log.LogError($"UpdateAccountAsync: error: {ex}");
                throw; //retry
            }
        }

        [FunctionName("GetBalance")]
        public async Task<IActionResult> GetBalanceAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [CosmosDB(
                    databaseName: DatabaseName,
                    collectionName: CollectionName,
                    ConnectionStringSetting = "cosmosDBConnectionString")]
                    DocumentClient documentClient,
            ILogger log)
        {
            log.LogInformation("GetBalance HTTP trigger function processed a request.");

            string accountId = req.Query["accountId"];

           if (string.IsNullOrEmpty(accountId))
           {
                log.LogError("GetBalance: missing account id parameter");
                return new BadRequestErrorMessageResult("missing accountId parameter");
            }

            var cosmosDBWrapper = _cosmosDBWrapperFactory.Create(documentClient, DatabaseName, log);
            var balance = await cosmosDBWrapper.GetBalanceAsync(accountId);
            var balanceInfo = new BalanceInfo()
            {
                AccountId = accountId,
                Balance = balance
            };
            return new OkObjectResult(balanceInfo);
        }


        [FunctionName("GetAccountInfo")]
        public async Task<IActionResult> GetAccountInfoAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: DatabaseName,
                collectionName: CollectionName,
                ConnectionStringSetting = "cosmosDBConnectionString")]
            DocumentClient documentClient,
            ILogger log)
        {
            log.LogInformation("GetAccountInfo HTTP trigger function processed a request.");

            string accountId = req.Query["accountId"];

            if (string.IsNullOrEmpty(accountId))
            {
                log.LogError("GetAccountInfo: missing account id parameter");
                return new BadRequestErrorMessageResult("missing accountId parameter");
            }

            var cosmosDBWrapper = _cosmosDBWrapperFactory.Create(documentClient, DatabaseName, log);
            var accountInfo = await cosmosDBWrapper.GetAccountInfoAsync(accountId);
            
            return new OkObjectResult(accountInfo);
        }

        [FunctionName("GetAccountTransactionHistory")]
        public async Task<IActionResult> GetAccountTransactionHistoryAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [CosmosDB(
                    databaseName: DatabaseName,
                    collectionName: CollectionName,
                    ConnectionStringSetting = "cosmosDBConnectionString")]
                    DocumentClient documentClient,
            ILogger log)
        {
            log.LogInformation("GetAccountTransactionHistoryAsync HTTP trigger function processed a request.");

            string accountId = req.Query["accountId"];

            if (string.IsNullOrEmpty(accountId))
            {
                log.LogError("GetBalance: missing account id parameter");
                return new BadRequestErrorMessageResult("missing accountId parameter");
            }
            
            string numberOfTransactionsText = req.Query["numberOfTransactions"];

            if (string.IsNullOrEmpty(numberOfTransactionsText))
            {
                log.LogError("GetBalance: missing numberOfTransactions parameter");
                return new BadRequestErrorMessageResult("missing numberOfTransactions parameter");
            }
            
            var numberOfTransactions = int.Parse(numberOfTransactionsText);

            var cosmosDBWrapper = _cosmosDBWrapperFactory.Create(documentClient, DatabaseName, log);
            var transactions = await cosmosDBWrapper.GetAccountTransactionHistoryAsync(accountId, numberOfTransactions);
            
            return new OkObjectResult(transactions);
        }
    }
}

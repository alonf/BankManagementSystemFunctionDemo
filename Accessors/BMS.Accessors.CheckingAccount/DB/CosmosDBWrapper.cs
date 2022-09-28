using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BMS.Accessors.CheckingAccount.DB
{

    public class CosmosDBWrapper : ICosmosDBWrapper
    {
        private readonly DocumentClient _documentClient;
        private readonly string _databaseName;
        private readonly ILogger _logger;
        private const string AccountInfoName = "AccountInfo";
        private const string AccountTransactionName = "AccountTransaction";
        
        public CosmosDBWrapper(DocumentClient documentClient, string databaseName, ILogger logger)
        {
            _documentClient = documentClient;
            _databaseName = databaseName;
            _logger = logger;
        }

        public async Task UpdateBalanceAsync(string requestId, string accountId, decimal amount)
        {
            try
            {
                var transactionRecord = new AccountTransactionRecord()
                {
                    Id = requestId,
                    AccountId = accountId,
                    TransactionAmount = amount,
                    TransactionTime = DateTimeOffset.UtcNow
                };

                await InitDBIfNotExistsAsync();

                Uri accountTransactionCollectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, "AccountTransaction");

                //create or update the record (if this is a retry operation)
                var response = await _documentClient.UpsertDocumentAsync(accountTransactionCollectionUri, transactionRecord);
                _logger.LogInformation($"UpdateBalanceAsync: insert transaction to collection, result status: {response.StatusCode} ");

                //find the account info document
                Uri accountInfoCollectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, AccountInfoName);
                var accountInfoQuery = _documentClient.CreateDocumentQuery<AccountInfo>(accountInfoCollectionUri, new FeedOptions { EnableCrossPartitionQuery = true}).Where(r => r.Id == accountId).AsDocumentQuery();
                var accountInfo = (await accountInfoQuery.ExecuteNextAsync<AccountInfo>()).FirstOrDefault();
                
                //first record for the account
                if (accountInfo == null)
                {
                    var firstRecord = new AccountInfo()
                    {
                        Id = accountId,
                        AccountBalance = 0,
                        OverdraftLimit = 1000 //default
                    };

                    try
                    {
                        //in a case of race condition, only a single document is created (id)
                        await _documentClient.CreateDocumentAsync(accountInfoCollectionUri, firstRecord);

                        _logger.LogInformation($"UpdateBalanceAsync: create first account, result status: {response.StatusCode} ");
                    }
                    catch(DocumentClientException ex)
                    {
                        if (ex.StatusCode != System.Net.HttpStatusCode.Conflict)
                            throw;
                    }

                    accountInfoQuery = _documentClient.CreateDocumentQuery<AccountInfo>(accountInfoCollectionUri, new FeedOptions { EnableCrossPartitionQuery = true }).Where(r => r.Id == accountId).AsDocumentQuery();
                    accountInfo = (await accountInfoQuery.ExecuteNextAsync<AccountInfo>()).FirstOrDefault();

                    if (accountInfo == null)
                    {
                        _logger.LogError("UpdateBalanceAsync: Error creating or querying account document");
                        throw new Exception("Error creating account document");
                    }
                }

                //check if already processed
                if (accountInfo.AccountTransactions.Contains(transactionRecord.Id))
                {
                    _logger.LogError($"UpdateBalanceAsync: {transactionRecord.Id} already proccessed");
                    return;
                }
                accountInfo.AccountTransactions.Add(transactionRecord.Id);
                accountInfo.AccountBalance += transactionRecord.TransactionAmount;

                var ac = new AccessCondition { Condition = accountInfo.ETag, Type = AccessConditionType.IfMatch };

                //throws exception on concurrency conflicts => retry
                await _documentClient.ReplaceDocumentAsync(accountInfo.Self, accountInfo,
                    new RequestOptions { AccessCondition = ac });
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateBalanceAsync: exception: {ex}");
                throw;
            }
        }

        private async Task InitDBIfNotExistsAsync()
        {
            var resourceResponse = await _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = _databaseName });

            Uri databaseUri = UriFactory.CreateDatabaseUri(_databaseName);
            var documentCollection = new DocumentCollection
            {
                Id = AccountTransactionName,
                PartitionKey = new PartitionKeyDefinition
                {
                    Paths = new Collection<string> { "/requestId" }
                }
            };

            //create collection if not exist
            await _documentClient.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection);

            documentCollection = new DocumentCollection
            {
                Id = AccountInfoName,
                PartitionKey = new PartitionKeyDefinition
                {
                    Paths = new Collection<string> { "/overdraftLimit" }
                }
            };

            //create collection if not exist
            await _documentClient.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection);
        }

        public async Task<IList<AccountTransactionRecord>> GetAccountTransactionHistoryAsync(string accountId, int numberOfTransactions)
        {
            try
            {
                //get the account record
                var accountInfo = await GetAccountInfoAsync(accountId);

                if (accountInfo == null)
                {
                    _logger.LogWarning($"GetAccountTransactionHistoryAsync: account id: {accountId} not found");
                    return null;
                }

                var transactionIds = accountInfo.AccountTransactions.Skip(Math.Max(accountInfo.AccountTransactions.Count - numberOfTransactions, 0));

                Uri accountTransactionsCollectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, AccountTransactionName);

                var transactionQuery = _documentClient
                    .CreateDocumentQuery<AccountTransactionRecord>(accountTransactionsCollectionUri,
                        new FeedOptions { EnableCrossPartitionQuery = true }).Where(r => transactionIds.Contains(r.Id)).AsDocumentQuery<AccountTransactionRecord>();

                var accountTransactions = new List<AccountTransactionRecord>();
                double charge = 0.0;
                do
                {
                    var result = await transactionQuery.ExecuteNextAsync<AccountTransactionRecord>();
                    accountTransactions.AddRange(result.ToArray());
                    charge += result.RequestCharge;
                } while (transactionQuery.HasMoreResults);

                _logger.LogInformation($"GetAccountTransactionHistoryAsync: Querying for transactions returned {accountTransactions.Count} transactions and cost {charge} RU");

                return accountTransactions;
            }
            catch(Exception ex)
            {
                _logger.LogError($"GetAccountTransactionHistoryAsync: exception: {ex}");
                throw;
            }
        }

        public async Task<decimal> GetBalanceAsync(string accountId)
        {
            //get the account record
            AccountInfo accountInfo = await GetAccountInfoAsync(accountId);

            if (accountInfo == null)
            {
                _logger.LogWarning($"GetBalanceAsync: account id: {accountId} is not found");
                throw new KeyNotFoundException();
            }

            return accountInfo.AccountBalance;
        }

        private async Task<AccountInfo> GetAccountInfoAsync(string accountId)
        {
            Uri accountInfoCollectionUri = UriFactory.CreateDocumentCollectionUri(_databaseName, AccountInfoName);
            var accountInfoQuery = _documentClient.CreateDocumentQuery<AccountInfo>(accountInfoCollectionUri, new FeedOptions { EnableCrossPartitionQuery = true }).Where(r => r.Id == accountId).AsDocumentQuery();
            var accountInfo = (await accountInfoQuery.ExecuteNextAsync<AccountInfo>()).FirstOrDefault();
            return accountInfo;
        }

        public async Task SetAccountBalanceLowLimitAsync(string accountId, decimal limit)
        {
            try
            {
                //get the account record
                AccountInfo accountInfo = await GetAccountInfoAsync(accountId);
                
                if (accountInfo == null)
                {
                    _logger.LogWarning($"GetBalanceAsync: account id: {accountId} is not found");
                    throw new KeyNotFoundException();
                }

                accountInfo.OverdraftLimit = limit;

                var ac = new AccessCondition { Condition = accountInfo.ETag, Type = AccessConditionType.IfMatch };

                //throws exception on concurrency conflicts => retry
                await _documentClient.ReplaceDocumentAsync(accountInfo.Self, accountInfo,
                    new RequestOptions { AccessCondition = ac });
            }
            catch (Exception ex)
            {
                _logger.LogError($"SetAccountBalanceLowLimit: exception: {ex}");
                throw;
            }
        }

        public async Task<decimal> GetAccountBalanceLowLimitAsync(string accountId)
        {
            //get the account record
            AccountInfo accountInfo = await GetAccountInfoAsync(accountId);
            
            if (accountInfo == null)
            {
                _logger.LogWarning($"GetAccountBalanceLowLimit: account id: {accountId} is not found");
                throw new KeyNotFoundException();
            }

            return accountInfo.OverdraftLimit;
        }

        
    }
}

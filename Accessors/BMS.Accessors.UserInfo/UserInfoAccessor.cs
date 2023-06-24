using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace BMS.Accessors.UserInfo;

public class UserInfoAccessor
{
    const string DatabaseName = "BMSDB";
    const string CollectionName = "UserInfo";

    [FunctionName("RegisterCustomer")]
    public async Task RegisterCustomer([QueueTrigger("customer-registration-queue", Connection = "QueueConnectionString")] string myQueueItem,
        [CosmosDB(
            databaseName: DatabaseName,
            collectionName: CollectionName,
            ConnectionStringSetting = "cosmosDBConnectionString")]
        DocumentClient documentClient,
        [Queue("client-response-queue", Connection = "QueueConnectionString")] IAsyncCollector<JObject> responseQueue,
        ILogger log)
    {
        string userAccountId = "unknown";
        string requestId = "unknown";
        string callerId = "unknown";
        try
        {
            log.LogInformation($"RegisterCustomer: Queue trigger function processed: {myQueueItem}");

            var customerRegistrationInfo = JObject.Parse(myQueueItem);
            ValidateInput(customerRegistrationInfo);

            customerRegistrationInfo["id"] = customerRegistrationInfo["accountId"];
            customerRegistrationInfo.Remove("accountId");

            requestId = customerRegistrationInfo["requestId"]?.Value<string>();
            callerId = customerRegistrationInfo["callerId"]?.Value<string>();


            //create db if not exist

            await InitDbIfNotExistsAsync(documentClient);
                
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, "UserInfo");
            userAccountId = customerRegistrationInfo["id"]?.Value<string>();
              
            var sql = "SELECT * FROM c WHERE c.id = @id";
            var sqlQuery = new SqlQuerySpec(sql,
                new SqlParameterCollection(new[] {
                    new SqlParameter("@id", userAccountId)
                }));


            var query = documentClient.CreateDocumentQuery(collectionUri, sqlQuery, new FeedOptions() { EnableCrossPartitionQuery = true }).ToArray();

            if (query.Any())
            {
                await EnqueueResponseMessageAsync(responseQueue, "RegisterCustomer",
                    true, "Customer already exist",
                    requestId,
                    callerId,
                    userAccountId);

                log.LogInformation($"RegisterCustomer: User account id: {userAccountId} already exist");
                return;
            }

            //it will throw if Item exist in case of a race condition
            var documentCreationResponse = await documentClient.CreateDocumentAsync(collectionUri, customerRegistrationInfo);

            if (documentCreationResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(1000);
                log.LogError("RegisterCustomer: Too many requests");
                throw new Exception("Too many requests, try again");
            }

            if (IsSuccessStatusCode(documentCreationResponse.StatusCode))
            {
                await EnqueueResponseMessageAsync(responseQueue, "RegisterCustomer",
                    true, "Customer registered successfully",
                    requestId,
                    callerId,
                    userAccountId);
                log.LogInformation("RegisterCustomer: New account created");
                return;
            }
            //else
            await EnqueueResponseMessageAsync(responseQueue, "RegisterCustomer",
                false, "Customer registered failed, retrying",
                requestId,
                callerId,
                userAccountId);
            log.LogError($"RegisterCustomer: account creation failed with status code: {documentCreationResponse.StatusCode}");
        }
        catch (JSchemaValidationException schemaValidationException)
        {
            log.LogError($"Json validation error on queued message: {schemaValidationException}");
            await EnqueueResponseMessageAsync(responseQueue, "RegisterCustomer",
                false, "Customer registered failed, message format incorrect",
                requestId,
                callerId,
                userAccountId);
        }
        catch (DocumentClientException ex)
        {
            log.LogError($"RegisterCustomer: DocumentClientException when accessing cosmosDB: {ex}");
            if (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(1000);
                throw; //retry
            }
            await EnqueueResponseMessageAsync(responseQueue, "RegisterCustomer",
                false, "Customer registered failed, Database access error. Retrying",
                requestId,
                callerId,
                userAccountId);
        }
        catch (Exception ex)
        {
            log.LogError($"RegisterCustomer: A problem occur, exception: {ex}");
            await EnqueueResponseMessageAsync(responseQueue, "RegisterCustomer",
                false, "Customer registered failed, unknown server error. Retrying",
                requestId,
                callerId,
                userAccountId);
            throw; //retry
        }
    }

    private static async Task InitDbIfNotExistsAsync(DocumentClient documentClient)
    {
        await documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseName });

        Uri databaseUri = UriFactory.CreateDatabaseUri(DatabaseName);
        var documentCollection = new DocumentCollection
        {
            Id = "UserInfo",
            PartitionKey = new PartitionKeyDefinition
            {
                Paths = new Collection<string> { "/email" }
            }
        };

        //create collection if not exist
        await documentClient.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection);
    }

    private bool IsSuccessStatusCode(HttpStatusCode statusCode)
    {
        return ((int)statusCode >= 200) && ((int)statusCode <= 299);
    }

    private async Task EnqueueResponseMessageAsync(IAsyncCollector<JObject> responseQueue, string actionName, bool isSuccessful,
        string resultMessage, string requestId, string callerId, string accountId = "")
    {
        var responseMessage = new JObject
        {
            ["actionName"] = actionName,
            ["isSuccessful"] = isSuccessful,
            ["resultMessage"] = resultMessage,
            ["requestId"] = requestId,
            ["callerId"] = callerId,
            ["accountId"] = accountId
        };

        await responseQueue.AddAsync(responseMessage);
    }

    private void ValidateInput(JObject customerRegistrationInfo)
    {
        string schemaJson = @"{
                  '$schema' : 'https://json-schema.org/draft/2020-12/schema',
                  'description': 'a user information creation request',
                  'title': 'UserInfo',
                  'type': 'object',
                  'properties': {
                    'requestId': {'type': 'string'},
                    'accountId': {'type': 'string'},
                    'fullName': {'type': 'string'},
                    'email': {
                        'type': 'string',
                        'pattern': '^\\S+@\\S+\\.\\S+$',
                        'format': 'email',
                        'minLength': 6,
                        'maxLength': 127
                    }
                  },
                    'required' : ['requestId', 'accountId', 'fullName', 'email']
                }";

        JSchema schema = JSchema.Parse(schemaJson);

        customerRegistrationInfo.Validate(schema); //throws exception if not valid
    }

    [FunctionName("GetAccountIdByEmail")]
    public async Task<IActionResult> GetAccountIdByEmail(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        [CosmosDB(
            databaseName: DatabaseName,
            collectionName: CollectionName,
            ConnectionStringSetting = "cosmosDBConnectionString")]
        DocumentClient documentClient,
        ILogger logger)
    {
        try
        {
            logger.LogInformation("GetAccountIdByEmail HTTP trigger function processed a request.");

            string email = req.Query["email"];

            if (string.IsNullOrEmpty(email))
            {
                logger.LogError("GetAccountIdByEmailAsync: email is empty");
                return new BadRequestErrorMessageResult("email parameter is missing");
            }

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, "UserInfo");

            var sql = "SELECT c.id FROM c WHERE c.email = @email";
            var sqlQuery = new SqlQuerySpec(sql,
                new SqlParameterCollection(new[] {
                    new SqlParameter("@email", email)
                }));


            var query = documentClient.CreateDocumentQuery(collectionUri, sqlQuery, new FeedOptions() { EnableCrossPartitionQuery = true }).AsDocumentQuery();
                
            var accountIds = new List<string>();
            double charge = 0.0;
            do
            {
                var result = await query.ExecuteNextAsync();
                var ids = result.Select(r => "'" + JObject.Parse(r.ToString())["id"].ToString() + "'").Cast<string>();
                accountIds.AddRange(ids);
                charge += result.RequestCharge;
            } while (query.HasMoreResults);

            logger.LogInformation($"GetAccountIdByEmail: Querying for account ids returned {accountIds.Count} accounts and cost {charge} RUs");

            return new OkObjectResult(JObject.Parse($"{{'accountIds':[{String.Join(',', accountIds)}]}}"));
        }
        catch(Exception ex)
        {
            logger.LogError($"GetAccountIdByEmailAsync: A problem occur, exception: {ex}");
            return new InternalServerErrorResult();
        }
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Web.Http;
using Azure.Storage.Queues;
using AutoMapper;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata.Ecma335;
using StackExchange.Redis;

namespace BMS.Managers.Account
{
    public class AccountManager
    {
        private readonly IMapper _mapper;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<AccountManager> _logger;

        public AccountManager(IMapper mapper, HttpClient httpClient, 
            IConfiguration configuration, IConnectionMultiplexer connectionMultiplexer, ILogger<AccountManager> logger)
        {
            _mapper = mapper;
            _httpClient = httpClient;
            _configuration = configuration;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
        }
        
        [FunctionName("RegisterCustomer")]
        public async Task<IActionResult> RegisterCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Queue("customer-registration-queue", Connection = "QueueConnectionString")] QueueClient customerRegistrationQueue)
        {
            try
            {
                _logger.LogInformation("HTTP trigger RegisterCustomer function processed a request.");

                //extract request from the body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<Contracts.Requests.CustomerRegistrationInfo>(requestBody);
                
                //validate the input object
                Validator.ValidateObject(data, new ValidationContext(data, null, null));
                
                //first check for idempotency
                if (await RequestAlreadyProcessedAsync(data.RequestId))
                {
                    _logger.LogInformation($"RegisterCustomer request id {data.RequestId} already processed");
                    return new ConflictObjectResult($"Request id {data.RequestId} already processed");
                }
                
                //create a customer registration request for the User accessor
                var customerRegistrationInfoSubmit = _mapper.Map<Contracts.Submits.CustomerRegistrationInfo>(data);
                var messagePayload = JsonConvert.SerializeObject(customerRegistrationInfoSubmit);
                
                //push the customer registration request
                await customerRegistrationQueue.CreateIfNotExistsAsync();
                await customerRegistrationQueue.SendMessageAsync(messagePayload);
                _logger.LogInformation($"RegisterCustomer request added: {messagePayload}");
                
                return new OkObjectResult("Register customer request received");
            }
            catch (ValidationException validationException)
            {
                _logger.LogError($"RegisterCustomer: Input error, validation result: {validationException.Message}");
                return new BadRequestErrorMessageResult(validationException.Message);
            }
            catch (Exception e)
            {
                _logger.LogError($"RegisterCustomer: Error occurred when processing a message: {e}");
                return new InternalServerErrorResult();
            }
        }

        private async Task<bool> RequestAlreadyProcessedAsync(string requestId)
        {
            var db = _connectionMultiplexer.GetDatabase();
            var tran = db.CreateTransaction();
            tran.AddCondition(Condition.KeyNotExists(requestId));
            _ = tran.StringSetAsync(requestId, "true", TimeSpan.FromMinutes(5), When.NotExists);
            bool committed = await tran.ExecuteAsync();
           
            return !committed;
        }

        [FunctionName("GetAccountId")]
        public async Task<IActionResult> GetAccountId(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("HTTP trigger GetAccountId function processed a request.");

                var email = req.Query["email"];
                
                if (string.IsNullOrWhiteSpace(email))
                {
                    return new BadRequestErrorMessageResult("Expecting the account owner email address");
                }

                var userAccessorUrl = _configuration["userAccessorUrl"];
                if (string.IsNullOrWhiteSpace(userAccessorUrl))
                {
                    _logger.LogError($"Configuration error. Missing UserAccountUrl value");
                    return new InternalServerErrorResult();
                }
                
                //call User Info Accessor to get the user info
                var uri = QueryHelpers.AddQueryString(userAccessorUrl, "email", email);
                string accountIdInfoJson = await _httpClient.GetStringAsync(uri);
                var data = JsonConvert.DeserializeObject<Contracts.Responses.AccountIdInfo>(accountIdInfoJson);

                _logger.LogInformation($"GetAccountId returned: {accountIdInfoJson}");

                return new OkObjectResult(accountIdInfoJson);
            }
            catch (Exception e)
            {
                _logger.LogError($"GetAccountId: Error occurred when processing a message: {e}");
                return new InternalServerErrorResult();
            }
        }


        [FunctionName("GetAccountBalance")]
        public async Task<IActionResult> GetAccountBalance(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("HTTP trigger GetAccountBalance function processed a request.");

                var accountId = req.Query["accountId"];

                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return new BadRequestErrorMessageResult("Expecting the accountId");
                }

                var getBalanceUrl = _configuration["getBalanceUrl"];
                if (string.IsNullOrWhiteSpace(getBalanceUrl))
                {
                    _logger.LogError($"Configuration error. Missing getBalanceUrl value");
                    return new InternalServerErrorResult();
                }

                //call checking account Accessor to get the account balance
                var uri = QueryHelpers.AddQueryString(getBalanceUrl, "accountId", accountId);
                var balanceInfoJson = await _httpClient.GetStringAsync(uri);
                var data = JsonConvert.DeserializeObject<Contracts.Responses.BalanceInfo>(balanceInfoJson);

                _logger.LogInformation($"GetAccountBalance returned: {balanceInfoJson}");

                return new OkObjectResult(data);
            }
            catch (Exception e)
            {
                _logger.LogError($"GetAccountBalance: Error occurred when processing a message: {e}");
                return new InternalServerErrorResult();
            }
        }


        [FunctionName("GetAccountTransactionHistory")]
        public async Task<IActionResult> GetAccountTransactionHistoryAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("HTTP trigger GetAccountTransactionHistory function processed a request.");

                var accountId = req.Query["accountId"];

                if (string.IsNullOrWhiteSpace(accountId))
                {
                    return new BadRequestErrorMessageResult("Expecting the accountId");
                }

                var getAccountTransactionHistoryUrl = _configuration["getAccountTransactionHistoryUrl"];
                if (string.IsNullOrWhiteSpace(getAccountTransactionHistoryUrl))
                {
                    _logger.LogError($"Configuration error. Missing getAccountTransactionHistoryUrl value");
                    return new InternalServerErrorResult();
                }

                string numberOfTransactions = req.Query["numberOfTransactions"];

                if (string.IsNullOrEmpty(numberOfTransactions))
                {
                    numberOfTransactions = "10"; //default
                }


                //call checking account Accessor to get the account balance
                var uri = QueryHelpers.AddQueryString(getAccountTransactionHistoryUrl, new Dictionary<string, string>
                {
                    { "accountId", accountId },
                    { "numberOfTransactions", numberOfTransactions }
                });
                    
                var accountTransactionHistoryJson = await _httpClient.GetStringAsync(uri);
                var data = JsonConvert.DeserializeObject<Contracts.Responses.AccountTransactionResponse[]>(accountTransactionHistoryJson);

                _logger.LogInformation($"GetAccountTransactionHistory returned: {accountTransactionHistoryJson}");

                return new OkObjectResult(data);
            }
            catch (Exception e)
            {
                _logger.LogError($"GetAccountTransactionHistory: Error occurred when processing a message: {e}");
                return new InternalServerErrorResult();
            }
        }
        

        [FunctionName("Deposit")]
        public async Task<IActionResult> Deposit(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Queue("account-transaction-queue", Connection = "QueueConnectionString")] QueueClient accountTransactionQueue)
        {
            try
            {
                _logger.LogInformation("HTTP trigger Deposit function processed a request.");

                //extract request from the body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<Contracts.Requests.AccountTransactionInfo>(requestBody);

                //validate the input object
                Validator.ValidateObject(data, new ValidationContext(data, null, null));

                //first check for idempotency
                if (await RequestAlreadyProcessedAsync(data.RequestId))
                {
                    _logger.LogInformation($"Deposit request id {data.RequestId} already processed");
                    return new ConflictObjectResult($"Request id {data.RequestId} already processed");
                }
                
                //create a customer registration request for the User accessor
                var accountTransactionSubmit = _mapper.Map<Contracts.Submits.AccountTransactionSubmit>(data);
                var messagePaylod = JsonConvert.SerializeObject(accountTransactionSubmit);

                //push the customer registration request
                await accountTransactionQueue.CreateIfNotExistsAsync();
                await accountTransactionQueue.SendMessageAsync(messagePaylod);
                _logger.LogInformation($"Deposit request added: {messagePaylod}");

                return new OkObjectResult("Deposit request received");
            }
            catch (ValidationException validationException)
            {
                _logger.LogError($"Deposit: Input error, validation resilt: {validationException.Message}");
                return new BadRequestErrorMessageResult(validationException.Message);
            }
            catch (Exception e)
            {
                _logger.LogError($"Deposit: Error occurred when processing a message: {e}");
                return new InternalServerErrorResult();
            }
        }

        [FunctionName("Withdraw")]
        public async Task<IActionResult> Withdraw(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Queue("account-transaction-queue", Connection = "QueueConnectionString")] QueueClient accountTransactionQueue)
        {
            try
            {
                _logger.LogInformation("HTTP trigger Withdraw function processed a request.");

                //extract request from the body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<Contracts.Requests.AccountTransactionInfo>(requestBody);

                //validate the input object
                Validator.ValidateObject(data, new ValidationContext(data, null, null));

                //first check for idempotency
                if (await RequestAlreadyProcessedAsync(data.RequestId))
                {
                    _logger.LogInformation($"Withdraw request id {data.RequestId} already processed");
                    return new ConflictObjectResult($"Request id {data.RequestId} already processed");
                }

                if (! await CheckLiabilityAsync(data.AccountId, data.Amount))
                {
                    _logger.LogInformation($"Withdraw request failed, the withdraw operation is forbidden");
                    return new BadRequestErrorMessageResult("The user is not allowed to withdraw");
                }
                
                data.Amount = -data.Amount;
                //create a customer registration request for the User accessor
                var accountTransactionSubmit = _mapper.Map<Contracts.Submits.AccountTransactionSubmit>(data);
                var messagePaylod = JsonConvert.SerializeObject(accountTransactionSubmit);

                //push the customer registration request
                await accountTransactionQueue.CreateIfNotExistsAsync();
                await accountTransactionQueue.SendMessageAsync(messagePaylod);
                _logger.LogInformation($"Withdraw request added: {messagePaylod}");

                return new OkObjectResult("Withdraw request received");
            }
            catch (ValidationException validationException)
            {
                _logger.LogError($"Withdraw: Input error, validation resilt: {validationException.Message}");
                return new BadRequestErrorMessageResult(validationException.Message);
            }
            catch (Exception e)
            {
                _logger.LogError($"Withdraw: Error occurred when processing a message: {e}");
                return new InternalServerErrorResult();
            }
        }

        private async Task<bool> CheckLiabilityAsync(string accountId, decimal amount)
        {
            //Check liabilty
            var liabilityValidatorUrl = _configuration["liabilityValidatorUrl"];
            if (string.IsNullOrWhiteSpace(liabilityValidatorUrl))
            {
                _logger.LogError($"Configuration error. Missing liabilityValidatorUrl value");
                throw new Exception("Configuration error. Missing liabilityValidatorUrl value");
            }

            var queryParameters = new Dictionary<string, string>
            {
                {"accountId", accountId },
                {"amount", amount.ToString() }
            };

            var uri = QueryHelpers.AddQueryString(liabilityValidatorUrl, queryParameters);
            string liabilityCheckResultJsonText = await _httpClient.GetStringAsync(uri);
            var liabilityCheckResultJson = JObject.Parse(liabilityCheckResultJsonText);

            if (!liabilityCheckResultJson.ContainsKey("result"))
            {
                _logger.LogError($"liabilityValidator result error");
                throw new Exception("liabilityValidator result error");
            }

            return liabilityCheckResultJson["result"].Value<bool>();
        }
    }
}

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BMS.Engines.LiabilityValidator;

public class LiabilityValidatorEngine
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LiabilityValidatorEngine> _logger;

    public LiabilityValidatorEngine(HttpClient httpClient,
        IConfiguration configuration, 
        ILogger<LiabilityValidatorEngine> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }
    [FunctionName("CheckLiability")]
    public async Task<IActionResult> CheckLiabilityAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("HTTP trigger function CheckLiabilityAsync processed a request.");

        var accountId = req.Query["accountId"];

        if (string.IsNullOrWhiteSpace(accountId))
        {
            return new BadRequestErrorMessageResult("Expecting the accountId parameter");
        }

        var amountText = req.Query["amount"];

        if (string.IsNullOrWhiteSpace(amountText))
        {
            return new BadRequestErrorMessageResult("Expecting the amount parameter");
        }

        decimal amount = decimal.Parse(amountText);

        //get the current balance
        var getBalanceUrl = _configuration["getBalanceUrl"];
        if (string.IsNullOrWhiteSpace(getBalanceUrl))
        {
            _logger.LogError("Configuration error. Missing getBalanceUrl value");
            return new InternalServerErrorResult();
        }

        var getBalanceResult = await QueryAccountInformationAsync(
            getBalanceUrl, accountId, "balance");

        if (getBalanceResult.value == null)
            return getBalanceResult.result;

            
        //get the Overdraft Limit
        var getAccountInfoUrl = _configuration["getAccountInfoUrl"];
        if (string.IsNullOrWhiteSpace(getAccountInfoUrl))
        {
            _logger.LogError("Configuration error. Missing getAccountInfoUrl value");
            return new InternalServerErrorResult();
        }

        var getOverdraftLimitResult = await QueryAccountInformationAsync(
            getAccountInfoUrl, accountId, "overdraftLimit");

        if (getOverdraftLimitResult.value == null)
            return getOverdraftLimitResult.result;
            
        decimal balance = getBalanceResult.value.Value;
        decimal overdraftLimit = -getOverdraftLimitResult.value.Value;
        string ticket = getBalanceResult.ticket;

        var withdrawAllowed = balance - amount >= overdraftLimit;
        _logger.LogInformation($"Withdrawing {amount} from account id: {accountId} with balance of {balance} is" +
                               (withdrawAllowed ? string.Empty : "not ") +
                               $"allowed. The overdraft limit is: {overdraftLimit}");
            
        return new OkObjectResult(JObject.Parse($"{{'withdrawAllowed':'{withdrawAllowed}', 'ticket':'{ticket}'}}"));
    }

    private async Task<(IActionResult result, decimal? value, string ticket)> QueryAccountInformationAsync(string serviceUrl, string accountId, string jsonProperty)
    {
        string accountInfoJson;
        //call checking account Accessor to get the account balance
        var uri = QueryHelpers.AddQueryString(serviceUrl, "accountId", accountId);
        try
        {
            accountInfoJson = await _httpClient.GetStringAsync(uri);
        }
        catch (Exception e) //todo: change to the not fount http result in such case
        {
            _logger.LogError($"Error account id:{accountId} not found. Exception: {e}");
            return (new NotFoundObjectResult(JObject.Parse("{'withdrawAllowed':'false'}")), null, null);
        }
            
        decimal? value;
        string ticket;
        try
        {
            var json = JObject.Parse(accountInfoJson);
            value = json?.GetValue(jsonProperty)?.Value<decimal?>();
            if (value == null)
            {
                _logger.LogError($"Error when trying to get the {jsonProperty} from account id:{accountId}");
                return (new InternalServerErrorResult(), null, null);
            }
            _logger.LogInformation($"{serviceUrl} returned: {accountInfoJson}");

            ticket = json.GetValue("ticket")?.Value<string>();
            if (string.IsNullOrEmpty(ticket))
            {
                _logger.LogError($"no _etag for this operation:{serviceUrl}");
            }
            _logger.LogInformation($"{serviceUrl} returned: {accountInfoJson}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Error when trying to call {serviceUrl} with account id:{accountId}, exception:{e}");
            return (new InternalServerErrorResult(), null, null);
        }

        return (new OkObjectResult(string.Empty), value, ticket);
    }
}
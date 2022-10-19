using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace BMS.Managers.Notification
{
    public class NotificationManager
    { 
        [FunctionName("negotiate")]
        public static IActionResult Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous)]
            HttpRequest req,
            [SignalRConnectionInfo(HubName = "accountmanagercallback",  UserId = "{headers.x-application-user-id}")]
            SignalRConnectionInfo connectionInfo, ILogger log)

        {
            try
            {
                log.LogInformation("the Negotiate begins");
                if (!req.HttpContext.Response.Headers.ContainsKey("Access-Control-Allow-Credentials"))
                {
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                }
                if (req.Headers.ContainsKey("Origin") && !req.HttpContext.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
                {
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", req.Headers["Origin"][0]);
                }
                if (req.Headers.ContainsKey("Access-Control-Request-Headers"))
                {
                    req.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", req.Headers["access-control-request-headers"][0]);
                }
                log.LogInformation("negotiate API succeeded.");
                return new OkObjectResult(connectionInfo);
            }
            catch (Exception ex)
            {
                log.LogInformation($"Negotiate error: {ex.Message}");
                return new BadRequestResult();
            }
        }

     
        [FunctionName("AccountCallbackHandler")]
        [return: SignalR(HubName = "accountmanagercallback")]
        public static SignalRMessage AccountCallbackHandlerAsync(
            [QueueTrigger("client-response-queue", Connection = "QueueConnectionString")]
            Contracts.Requests.AccountCallbackRequest accountCallbackRequest,
            ILogger logger)
        {

            logger.LogInformation($"Received response: {accountCallbackRequest}");
            return new SignalRMessage
                {
                   // the message will only be sent to this user ID
                   UserId = accountCallbackRequest.CallerId,
                   Target = "accountcallback",
                    Arguments = new object[] { accountCallbackRequest }
                };

        }
    }
}
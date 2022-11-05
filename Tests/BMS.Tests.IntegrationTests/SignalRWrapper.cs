using BMS.Tests.IntegrationTests.Contracts;
using BMS.Tests.IntegrationTests.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace BMS.Tests.IntegrationTests;

public class SignalRWrapper : ISignalRWrapper
{
    private readonly HubConnection _signalRHubConnection;
    private readonly List<AccountCallbackRequest> _signalRMessagesReceived = new();
    private readonly SemaphoreSlim _signalRMessageReceived = new(0);

    public SignalRWrapper(ITestOutputHelper testOutputHelper, IFunctionKeyProvider keyProvider)
    {
        var signalRUrl = Environment.GetEnvironmentVariable("BMS_SIGNALR_URL");
        if (string.IsNullOrEmpty(signalRUrl))
            signalRUrl = "http://localhost:7043/api/";

        var signalrNegotiateFunctionKey = keyProvider.GetKey("NEGOTIATE") ?? String.Empty;

        _signalRHubConnection = new HubConnectionBuilder()
            .WithUrl(signalRUrl, c => {
                c.Headers.Add("x-application-user-id", "Teller1");
                c.Headers.Add("x-function-key", signalrNegotiateFunctionKey);
            })
            .WithAutomaticReconnect().ConfigureLogging(lb =>
            {
                lb.AddProvider(new XUnitLoggerProvider(testOutputHelper));
                lb.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();
    }

    async Task ISignalRWrapper.StartSignalR()
    {
        try
        {
            _signalRMessagesReceived.Clear();

            if (_signalRHubConnection.State == HubConnectionState.Connected)
                return;

            await _signalRHubConnection.StartAsync();

            _signalRHubConnection.On<AccountCallbackRequest>("accountcallback", message =>
            {
                _signalRMessagesReceived.Add(message);
                TestOutputHelper.WriteLine($"SignalR received message: {message}");
                _signalRMessageReceived.Release();
            });
        }
        catch (Exception e)
        {
            TestOutputHelper.WriteLine($"Error with SignalR: {e.Message}");
            throw;
        }
    }

    async Task<bool> ISignalRWrapper.WaitForSignalREventAsync(int timeoutInSeconds)
    {
        var isSucceeded = await _signalRMessageReceived.WaitAsync(timeoutInSeconds * 1000);
        await Task.Delay(1000);
        return isSucceeded;
    }

    async Task<bool> ISignalRWrapper.WaitForSignalREventWithConditionAsync(int timeoutInSeconds, Func<IList<AccountCallbackRequest>, bool> condition)
    {
        var startTime = DateTimeOffset.UtcNow;
        bool result = false;
        do
        {
            var timeToWait = (int)(timeoutInSeconds - (DateTimeOffset.UtcNow - startTime).TotalSeconds);
            if (timeToWait <= 0)
                break;

            await _signalRMessageReceived.WaitAsync(timeToWait * 1000);
        } while (!(result = condition(_signalRMessagesReceived)));

        return result;
    }

    IList<AccountCallbackRequest> ISignalRWrapper.Messages => _signalRMessagesReceived;

    public ITestOutputHelper TestOutputHelper { get; }
}
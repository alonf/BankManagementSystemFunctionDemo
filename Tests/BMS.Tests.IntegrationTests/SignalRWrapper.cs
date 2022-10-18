using BMS.Tests.IntegrationTests.Contracts;
using BMS.Tests.IntegrationTests.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace BMS.Tests.IntegrationTests;

public class SignalRWrapper : ISignalRWrapper
{
    private readonly HubConnection _signalRHubConnection;
    private readonly List<AccountCallbackRequest> _signalRMessagesReceived = new ();
    private readonly SemaphoreSlim _signalRMessageReceived = new(0);

    public SignalRWrapper(ITestOutputHelper testOutputHelper)
    {
        var signalRUrl = Environment.GetEnvironmentVariable("BMS_SIGNALR_URL");
        if (string.IsNullOrEmpty(signalRUrl))
            signalRUrl = "http://localhost:7071/api/";

        _signalRHubConnection = new HubConnectionBuilder()
            .WithUrl(signalRUrl, c=>c.Headers.Add("x-ms-client-principal-id","Teller1"))
            .WithAutomaticReconnect().ConfigureLogging(lb =>
            {
                lb.AddProvider(new XUnitLoggerProvider(testOutputHelper));
                lb.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();
        
    }

    async Task ISignalRWrapper.StartSignalR()
    {
        _signalRMessagesReceived.Clear();

        if (_signalRHubConnection.State == HubConnectionState.Connected)
            return;

        await _signalRHubConnection.StartAsync();

        _signalRHubConnection.On<string, object>("accountCallback", (user, message) =>
        {
            
            //_signalRMessagesReceived.Add(result);
            _signalRMessageReceived.Release();
        });

        //_signalRHubConnection.On(nameof(Object), "accountCallback", result =>
        //{
        //    //_signalRMessagesReceived.Add(result);
        //    _signalRMessageReceived.Release();
        //});
    }

    async Task<bool> ISignalRWrapper.WaitForSignalREventAsync(int timeoutInSeconds)
    {
        var isSucceeded = await _signalRMessageReceived.WaitAsync(timeoutInSeconds * 1000);
        await Task.Delay(1000);
        return isSucceeded;
    }

    IList<AccountCallbackRequest> ISignalRWrapper.Messages => _signalRMessagesReceived;
}
using Xunit.Abstractions;

namespace BMS.Tests.IntegrationTests;

public class SignalRWrapperFactory : ISignalRWrapperFactory
{
    ISignalRWrapper ISignalRWrapperFactory.Create(ITestOutputHelper testOutputHelper)
    {
        return new SignalRWrapper(testOutputHelper);
    }
}
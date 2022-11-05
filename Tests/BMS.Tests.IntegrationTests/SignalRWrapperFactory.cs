using Xunit.Abstractions;

namespace BMS.Tests.IntegrationTests;

public class SignalRWrapperFactory : ISignalRWrapperFactory
{
    private readonly IFunctionKeyProvider _functionKeyProvider;
    
    public SignalRWrapperFactory(IFunctionKeyProvider keyProvider)
    {
        _functionKeyProvider = keyProvider;
    }
    ISignalRWrapper ISignalRWrapperFactory.Create(ITestOutputHelper testOutputHelper)
    {
        return new SignalRWrapper(testOutputHelper, _functionKeyProvider);
    }
}
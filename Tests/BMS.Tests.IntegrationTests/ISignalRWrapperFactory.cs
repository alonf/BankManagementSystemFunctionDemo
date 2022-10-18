using Xunit.Abstractions;

namespace BMS.Tests.IntegrationTests;

public interface ISignalRWrapperFactory
{
    ISignalRWrapper Create(ITestOutputHelper testOutputHelper);
}
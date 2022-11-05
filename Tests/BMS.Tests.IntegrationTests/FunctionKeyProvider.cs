namespace BMS.Tests.IntegrationTests;

public class FunctionKeyProvider : IFunctionKeyProvider
{
    public string GetKey(string functionName)
    {
        var key = Environment.GetEnvironmentVariable($"{functionName.ToUpper()}_FUNCTION_KEY");
        if (string.IsNullOrEmpty(key))
            return string.Empty;
        return key;
    }
}
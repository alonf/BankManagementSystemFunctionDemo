using BMS.Utilities.HTTPClientHelper;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(BMS.Engines.LiabilityValidator.Startup))]

namespace BMS.Engines.LiabilityValidator
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddRobustHttpClient<LiabilityValidatorEngine>();

        }
    }
}

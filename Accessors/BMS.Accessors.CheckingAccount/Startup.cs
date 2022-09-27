using AutoMapper;
using BMS.Accessors.CheckingAccount.DB;
using BMS.Utilities.HTTPClientHelper;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

[assembly: FunctionsStartup(typeof(BMS.Accessors.CheckingAccount.Startup))]



namespace BMS.Accessors.CheckingAccount
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddRobustHttpClient<CheckingAccountAccessor>();
            builder.Services.AddSingleton<IAccountDBFactory, AccountDBFactory>();
            builder.Services.AddSingleton<ITransactionDB, TransactionDB>();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
                        
            var mapperConfig = new MapperConfiguration(mc => { mc.AddProfile(new AutoMappingProfile()); });
            IMapper mapper = mapperConfig.CreateMapper();
            mapperConfig.AssertConfigurationIsValid();
            builder.Services.AddSingleton(mapper);
        }
    }
}

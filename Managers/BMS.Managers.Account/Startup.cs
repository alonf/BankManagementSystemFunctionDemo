using AutoMapper;
using BMS.Utilities.HTTPClientHelper;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;
using System;

[assembly: FunctionsStartup(typeof(BMS.Managers.Account.Startup))]

namespace BMS.Managers.Account
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddRobustHttpClient<AccountManager>();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

            var configuration = builder.GetContext().Configuration;
            
            var redisConnectionString = configuration["RedisConnectionString"];
            if (string.IsNullOrEmpty(redisConnectionString))
            {
                throw new Exception("Missing configuration");
            }

            var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            builder.Services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);
            
            var mapperConfig = new MapperConfiguration(mc => { mc.AddProfile(new AutoMappingProfile()); });
            IMapper mapper = mapperConfig.CreateMapper();
            mapperConfig.AssertConfigurationIsValid();
            builder.Services.AddSingleton(mapper);

        }
    }
}

﻿using BMS.Accessors.CheckingAccount.DB;
using BMS.Utilities.HTTPClientHelper;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

[assembly: FunctionsStartup(typeof(BMS.Accessors.CheckingAccount.Startup))]

namespace BMS.Accessors.CheckingAccount;

internal class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddRobustHttpClient<CheckingAccountAccessor>();
        builder.Services.AddSingleton<ICosmosDBWrapperFactory, CosmosDBWrapperFactory>();

        JsonConvert.DefaultSettings = () => new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
    }
}
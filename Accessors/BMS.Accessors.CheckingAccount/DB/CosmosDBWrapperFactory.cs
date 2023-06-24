using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;

namespace BMS.Accessors.CheckingAccount.DB;

public class CosmosDBWrapperFactory : ICosmosDBWrapperFactory
{
    public ICosmosDBWrapper Create(DocumentClient documentClient, string databaseName, ILogger logger)
    {
        return new CosmosDBWrapper(documentClient, databaseName, logger);
    }
}
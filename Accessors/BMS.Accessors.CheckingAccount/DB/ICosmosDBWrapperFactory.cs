using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;

namespace BMS.Accessors.CheckingAccount.DB;

public interface ICosmosDBWrapperFactory
{
    ICosmosDBWrapper Create(DocumentClient documentClient, string databaseName, ILogger logger);
}
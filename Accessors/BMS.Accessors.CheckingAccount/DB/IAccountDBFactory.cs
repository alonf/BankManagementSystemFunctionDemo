using Microsoft.Azure.Documents.Client;

namespace BMS.Accessors.CheckingAccount.DB
{
    public interface IAccountDBFactory
    {
        AccountDB Create(DocumentClient documentClient);
    }
}
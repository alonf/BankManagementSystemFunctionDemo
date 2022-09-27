using Microsoft.Azure.Documents.Client;

namespace BMS.Accessors.CheckingAccount.DB
{
    public interface ITransactionDBFactory
    {
        TransactionDB Create(DocumentClient documentClient);
    }
}
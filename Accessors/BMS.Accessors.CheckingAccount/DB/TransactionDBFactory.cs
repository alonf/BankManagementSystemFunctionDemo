using Microsoft.Azure.Documents.Client;

namespace BMS.Accessors.CheckingAccount.DB
{
    public class TransactionDBFactory : ITransactionDBFactory
    {
        public TransactionDB Create(DocumentClient documentClient)
        {
            return new TransactionDB(documentClient);
        }
    }
}

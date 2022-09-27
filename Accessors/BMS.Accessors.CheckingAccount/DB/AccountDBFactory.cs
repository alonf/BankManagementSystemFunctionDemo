using Microsoft.Azure.Documents.Client;

namespace BMS.Accessors.CheckingAccount.DB
{
    public class AccountDBFactory : IAccountDBFactory
    {
        public AccountDB Create(DocumentClient documentClient)
        {
            return new AccountDB(documentClient);
        }
    }
}

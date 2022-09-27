using Microsoft.Azure.Documents.Client;

namespace BMS.Accessors.CheckingAccount.DB
{
    public class AccountDB : IAccountDB
    {
        private DocumentClient _documentClient;

        public AccountDB(DocumentClient documentClient)
        {
            _documentClient = documentClient;
        }

        public void SetAccountBalanceLowLimit(string accountId, decimal limit)
        {

        }

        public void GetAccountBalanceLowLimit(string accountId)
        {

        }
    }
}

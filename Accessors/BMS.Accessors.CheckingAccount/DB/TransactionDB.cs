using Microsoft.Azure.Documents.Client;
using System.Collections.Generic;

namespace BMS.Accessors.CheckingAccount.DB
{
    public class TransactionDB : ITransactionDB
    {
        private DocumentClient _documentClient;

        public TransactionDB(DocumentClient documentClient)
        {
            _documentClient = documentClient;
        }

        public void UpdateBalance(string requestId, string accountId, decimal amount)
        {

        }

        public decimal GetBalance(string accountId)
        {
            return 0;
        }

        public IList<AccountTransactionInfo> GetAccountTransactionHistory(string accountId, int numberOfTransactions)
        {
            return null;
        }
    }
}

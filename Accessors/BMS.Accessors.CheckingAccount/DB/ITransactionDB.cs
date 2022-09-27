using System.Collections.Generic;

namespace BMS.Accessors.CheckingAccount.DB
{
    public interface ITransactionDB
    {
        IList<AccountTransactionInfo> GetAccountTransactionHistory(string accountId, int numberOfTransactions);
        decimal GetBalance(string accountId);
        void UpdateBalance(string requestId, string accountId, decimal amount);
    }
}
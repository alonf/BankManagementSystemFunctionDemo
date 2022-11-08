using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMS.Accessors.CheckingAccount.DB
{
    public interface ICosmosDBWrapper
    {
        Task<IList<AccountTransactionRecord>> GetAccountTransactionHistoryAsync(string accountId, int numberOfTransactions);
        Task<(decimal balance, string ticket)> GetBalanceAsync(string accountId);
        Task<(bool success, string errorMessage)> UpdateBalanceAsync(string requestId, string accountId, decimal amount, string ticket);
        Task<decimal> GetAccountBalanceLowLimitAsync(string accountId);
        Task SetAccountBalanceLowLimitAsync(string accountId, decimal limit);
        Task<AccountInfo> GetAccountInfoAsync(string accountId);
    }
}
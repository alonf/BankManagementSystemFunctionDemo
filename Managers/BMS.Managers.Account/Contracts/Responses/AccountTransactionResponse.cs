using System;

namespace BMS.Managers.Account.Contracts.Responses
{
    internal class AccountTransactionResponse
    {
        public string AccountId { get; set; }

        public decimal TransactionAmount { get; set; }

        public DateTimeOffset TransactionTime { get; set; }
    }
}
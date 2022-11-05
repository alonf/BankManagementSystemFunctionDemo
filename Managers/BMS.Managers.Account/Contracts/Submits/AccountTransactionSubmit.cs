namespace BMS.Managers.Account.Contracts.Submits
{
    public class AccountTransactionSubmit
    {
        public string RequestId { get; set; }

        public string CallerId { get; set; }

        public string SchemaVersion { get; set; } 

        public string AccountId { get; set; }

        public decimal Amount { get; set; }
    }
}
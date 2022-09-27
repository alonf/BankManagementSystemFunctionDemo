namespace BMS.Accessors.CheckingAccount.DB
{
    public interface IAccountDB
    {
        void GetAccountBalanceLowLimit(string accountId);
        void SetAccountBalanceLowLimit(string accountId, decimal limit);
    }
}
namespace BMS.Accessors.CheckingAccount.Contracts.Responses;

public class BalanceInfo
{
    public string AccountId { get; set; }
    public decimal Balance { get; set; }
    public string Ticket { get; set; }
}
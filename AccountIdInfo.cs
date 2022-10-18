using System;

namespace BMS.Tests.IntegrationTests.Contracts;

internal class AccountIdInfo
{
    public string[] AccountIds { get; set; }
}

internal class AccountTransactionResponse
{
    public string AccountId { get; set; }

    public decimal TransactionAmount { get; set; }

    public DateTimeOffset TransactionTime { get; set; }
}

internal class BalanceInfo
{
    public string AccountId { get; set; }
    public decimal Balance { get; set; }
}

internal class CustomerInfo
{
    public string AccountId { get; set; }

    public string SchemaVersion { get; set; }

    public string FullName { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string FullAddress { get; set; }
}

internal class AccountCallbackRequest
{
    public string ActionName { get; set; }
    public string ResultMessage { get; set; }
    public bool IsSuccessful { get; set; }
    public string RequestId { get; set; }
    public string AccountId { get; set; }
    public string CallerId { get; set; }

    public override string ToString()
    {
        return $"{nameof(ActionName)}: {ActionName}, {nameof(ResultMessage)}: {ResultMessage}, {nameof(IsSuccessful)}: {IsSuccessful}, {nameof(RequestId)}: {RequestId}, {nameof(AccountId)}: {AccountId}, {nameof(CallerId)}: {CallerId}";
    }
}


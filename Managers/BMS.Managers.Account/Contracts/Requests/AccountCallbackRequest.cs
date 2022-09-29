﻿namespace BMS.Managers.Account.Contracts.Requests;

public class AccountCallbackRequest
{
    public string ActionName { get; set; }
    public string ResultMessage { get; set; }
    public bool IsSuccessful { get; set; }
    public string RequestId { get; set; }
    public string AccountId { get; set; }

    public override string ToString()
    {
        return $"{nameof(ActionName)}: {ActionName}, {nameof(ResultMessage)}: {ResultMessage}, {nameof(IsSuccessful)}: {IsSuccessful}, {nameof(RequestId)}: {RequestId}, {nameof(AccountId)}: {AccountId}";
    }
}
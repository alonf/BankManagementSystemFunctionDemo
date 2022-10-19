﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMS.Managers.Notification.Contracts.Requests
{
    public class AccountCallbackRequest
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
}
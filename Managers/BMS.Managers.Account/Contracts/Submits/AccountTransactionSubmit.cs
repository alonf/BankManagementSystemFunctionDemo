using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BMS.Managers.Account.Contracts.Submits
{
    internal class AccountTransactionSubmit
    {
        public string RequestId { get; set; }

        public string SchemaVersion { get; set; } 

        public string AccountId { get; set; }

        public decimal Amount { get; set; }
    }
}
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace BMS.Managers.Account.Contracts.Requests
{
    internal class AccountTransactionInfo
    {
        [Required(ErrorMessage = "The RequestId is missing")]
        public string RequestId { get; set; }

        [ValidateNever]
        public string SchemaVersion { get; set; } = "1.0";

        [Required(ErrorMessage = "The AccountId is missing")]
        public string AccountId { get; set; }

        [Required(ErrorMessage = "The Amount is missing")]
        [Range(0.01, 1000000, ErrorMessage = "The Amount must be between 0.01 and 1000000")]
        public decimal Amount { get; set; }
    }
}

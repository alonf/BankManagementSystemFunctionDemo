using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMS.Accessors.CheckingAccount.Contracts.Requests
{
    public class AccountTransactionRequest
    {
        public string RequestId { get; set; }

        public string SchemaVersion { get; set; }

        public string AccountId { get; set; }

        public decimal Amount { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMS.Accessors.CheckingAccount.Contracts.Responses
{
    public class BalanceInfo
    {
        public string AccountId { get; set; }
        public decimal Balance { get; set; }
    }
}

﻿
namespace BMS.Managers.Account.Contracts.Submits
{
    internal class CustomerRegistrationInfo
    {
        public string RequestId { get; set; }

        public string AccountId { get; set; }

        public string SchemaVersion { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string FullAddress { get; set; }

    }
}

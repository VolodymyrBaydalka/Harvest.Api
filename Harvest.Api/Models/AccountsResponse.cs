using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class AccountsResponse
    {
        public User User { get; set; }
        public Account[] Accounts { get; set; } 
    }
}

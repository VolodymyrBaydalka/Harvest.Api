using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class UserRate : BaseModel
    {
        public decimal Amount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class UserRatesResponse
    {
        public UserRate[] CostRates { get; set; }
    }
}

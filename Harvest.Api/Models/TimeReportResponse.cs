using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class TimeReportResponse : PagedList
    {
        public TimeReport[] Results { get; set; }
    }
}

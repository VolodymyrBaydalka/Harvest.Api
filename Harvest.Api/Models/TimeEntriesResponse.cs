using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class TimeEntriesResponse : PagedList
    {
        public TimeEntry[] TimeEntries { get; set; }
    }
}

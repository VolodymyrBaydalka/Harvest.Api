using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class TasksResponse : PagedList
    {
        public Task[] Tasks { get; set; }
    }
}

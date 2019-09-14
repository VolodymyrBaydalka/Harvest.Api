using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class TaskAssignmentsResponse : PagedList
    {
        public TaskAssignment[] TaskAssignments { get; set; }
    }
}

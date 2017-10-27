using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class ProjectAssignmentsResponse : PagedList
    {
        public ProjectAssignment[] ProjectAssignments { get; set; }
    }
}

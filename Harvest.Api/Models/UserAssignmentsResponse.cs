using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class UserAssignmentsResponse : PagedList
    {
        public UserAssignment[] UserAssignments { get; set; }
    }
}

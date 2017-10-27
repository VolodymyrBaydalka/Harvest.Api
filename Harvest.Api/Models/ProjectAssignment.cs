using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class ProjectAssignment : BaseModel
    {
        public bool IsProjectManager { get; set; }
        public bool IsActive { get; set; }
        public decimal? Budget { get; set; }
        public decimal? HourlyRate { get; set; }

        public Project Project { get; set; }
        public IdNameModel Client { get; set; }

        public TaskAssignment[] TaskAssignments { get; set; }
    }
}

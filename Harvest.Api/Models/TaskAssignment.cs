using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class TaskAssignment : BaseModel
    {
        public bool Billable { get; set; }
        public bool IsActive { get; set; }
        public decimal? HourlyRate { get; set; }
        public decimal? Budget { get; set; }

        public IdNameModel Task { get; set; }

        public ProjectReference Project { get; set; }

        public override string ToString()
        {
            return Task.Name;
        }
    }
}

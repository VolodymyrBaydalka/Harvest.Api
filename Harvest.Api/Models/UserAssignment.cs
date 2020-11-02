using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Harvest.Api
{
    [DebuggerDisplay("{User.Name} - {Project.Name}")]
    public class UserAssignment : BaseModel
    {
        public ProjectReference Project { get; set; }
        public IdNameModel User { get; set; }
        public bool IsActive { get; set; }
        public bool IsProjectManager { get; set; }
        public bool UseDefaultRates { get; set; }
        public decimal? HourlyRate { get; set; }
        public decimal? Budget { get; set; }        
    }
}

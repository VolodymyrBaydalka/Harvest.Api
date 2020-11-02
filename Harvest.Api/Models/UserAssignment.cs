using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class UserAssignment : BaseModel
    {
        public ProjectReference Project { get; set; }
        public IdNameModel User { get; set; }
        public bool IsActive { get; set; }
        public bool IsProjectManager { get; set; }
        public bool UseDefaultRates { get; set; }
        public decimal? HourlyRate { get; set; }
        public decimal? Budget { get; set; }
        
        public override string ToString()
        {
            return User.Name;
        }
    }
}

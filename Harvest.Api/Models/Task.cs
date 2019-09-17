using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class Task : BaseModel
    {
        public string Name { get; set; }
        public bool BillableByDefault { get; set; }
        public decimal? DefaultHourlyRate { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class TimeReport
    {
        public long ClientId { get; set; }
        public string ClientName { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public long TaskId { get; set; }
        public string TaskName { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public bool IsContractor { get; set; }
        public decimal TotalHours { get; set; }
        public decimal BillableHours { get; set; }
        public string Currency { get; set; }
        public decimal BillableAmount { get; set; }
    }
}

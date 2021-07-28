using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class TimeEntry : BaseModel
    {
        public IdNameModel User { get; set; }
        public IdNameModel Client { get; set; }
        public IdNameModel Project { get; set; }
        public IdNameModel Task { get; set; }
        public IdNumberModel Invoice { get; set; }
        public decimal Hours { get; set; }
        public string Notes { get; set; }

        public DateTime SpentDate { get; set; }
        public bool IsLocked { get; set; }
        public string LockedReason { get; set; }
        public bool IsClosed { get; set; }
        public bool IsBilled { get; set; }
        public DateTime? TimerStartedAt { get; set; }

        public TimeSpan? StartedTime { get; set; }
        public TimeSpan? EndedTime { get; set; }

        public bool IsRunning { get; set; }
        public bool Billable { get; set; }
        public bool Budgeted { get; set; }
        public decimal? BillableRate { get; set; }
        public decimal? CostRate { get; set; }
        public ExternalReference ExternalReference { get; set; }
    }

    public class ExternalReference
    {
        public string Id { get; set; }
        public string GroupId { get; set; }
        public string Permalink { get; set; }
        public string Service { get; set; }
        public string ServiceIconUrl { get; set; }
    }
}

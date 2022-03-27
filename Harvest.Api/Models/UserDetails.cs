using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class UserDetails : User
    {
        public string Telephone { get; set; }
        public string TimeZone { get; set; }
        public bool HasAccessToAllFutureProjects { get; set; }
        public bool IsContractor { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsProjectManager { get; set; }
        public bool CanSeeRates { get; set; }
        public bool CanCreateProjects { get; set; }
        public bool CanCreateInvoices { get; set; }
        public bool IsActive { get; set; }
        public int? WeeklyCapacity { get; set; }
        public decimal? DefaultHourlyRate { get; set; }
        public decimal? CostRate { get; set; }
        public string[] Roles { get; set; }
        public string AvatarUrl { get; set; }
    }
}

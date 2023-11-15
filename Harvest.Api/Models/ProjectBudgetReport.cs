using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvest.Api
{
    public class ProjectBudgetReport
    {
        public long ClientId { get; set; }
        public string ClientName { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public bool BudgetIsMonthly { get; set; }
        public string BudgetBy { get; set; }
        public bool IsActive { get; set; }
        public decimal? Budget { get; set; }
        public decimal? BudgetSpent { get; set; }
        public decimal? BudgetRemaining { get; set; }
    }

    public class ProjectBudgetReportResponse : PagedList
    {
        public ProjectBudgetReport[] Results { get; set; }
    }
}


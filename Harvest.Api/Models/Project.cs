using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class Project : BaseModel
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public bool IsActive { get; set; }
        public string BillBy { get; set; }
        public decimal? Budget { get; set; }
        public string BudgetBy { get; set; }
        public bool NotifyWhenOverBudget { get; set; }
        public decimal OverBudgetNotificationPercentage { get; set; }
        public DateTime? OverBudgetNotificationDate { get; set; }
        public bool ShowBudgetToAll { get; set; }
        public DateTime? StartsOn { get; set; }
        public DateTime? EndsOn { get; set; }
        public bool IsBillable { get; set; }
        public string Notes { get; set; }
        public Client Client { get; set; }

        public decimal? CostBudget { get; set; }
        public bool CostBudgetIncludeExpenses { get; set; }
        public decimal? HourlyRate { get; set; }
    }


    public class ProjectsResponse : PagedList
    {
        public Project[] Projects { get; set; }
    }
}

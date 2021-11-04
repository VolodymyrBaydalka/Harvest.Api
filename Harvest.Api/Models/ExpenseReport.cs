using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvest.Api
{
    public class ExpenseReport
    {
        public long ClientId { get; set; }
        public string ClientName { get; set; }
        public long ProjectId { get; set; }
        public string ProjectName { get; set; }
        public long ExpenseCategoryId { get; set; }
        public string ExpenseCategoryName { get; set; }
        public long UserId { get; set; }
        public string UserName { get; set; }
        public bool IsContractor { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal BillableAmount { get; set; }
        public string Currency { get; set; }
    }
}

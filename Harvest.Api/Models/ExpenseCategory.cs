using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvest.Api
{
    public class ExpenseCategory : IdNameModel
    {
        public string UnitName { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class ExpenseCategoryDetail : ExpenseCategory
    {
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvest.Api
{
    public class ExpenseCategoryResponse : PagedList
    {
        public ExpenseCategoryDetail[] ExpenseCategories { get; set; }
    }
}

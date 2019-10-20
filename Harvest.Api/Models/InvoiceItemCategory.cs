using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class InvoiceItemCategory : BaseModel
    {
        public const string DefaultServiceName = "Service";
        public const string DefaultProductName = "Product";

        public string Name { get; set; }
        public bool UseAsService { get; set; }
        public bool UseAsExpense { get; set; }
    }

    public class InvoiceItemCategoriesReponse : PagedList
    {
        public InvoiceItemCategory[] InvoiceItemCategories { get; set; }
    }
}

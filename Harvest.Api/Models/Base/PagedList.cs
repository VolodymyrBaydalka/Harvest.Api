using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public abstract class PagedList
    {
        public int PerPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalEntries { get; set; }
        public int? NextPage { get; set; }
        public int? PreviousPage { get; set; }
        public int Page { get; set; }

        public PagedListLinks Links { get; set; }
    }
}

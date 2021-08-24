using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class PagedListLinks
    {
        public string First { get; set; }
        public string Next { get; set; }
        public string Previous { get; set; }
        public string Last { get; set; }
    }
}

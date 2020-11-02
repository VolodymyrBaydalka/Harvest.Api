using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Harvest.Api
{
    [DebuggerDisplay("{Name}")]
    public class IdNameModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}

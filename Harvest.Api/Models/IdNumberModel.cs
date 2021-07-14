using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Harvest.Api
{
    [DebuggerDisplay("{Number}")]
    public class IdNumberModel
    {
        public long Id { get; set; }
        public string Number { get; set; }
    }
}
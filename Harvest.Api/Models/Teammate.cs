using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Harvest.Api
{
    [DebuggerDisplay("{FirstName} {LastName}")]
    public class Teammate
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class TeammatesResponse : PagedList
    {
        public Teammate[] Teammates { get; set; }
    }
}

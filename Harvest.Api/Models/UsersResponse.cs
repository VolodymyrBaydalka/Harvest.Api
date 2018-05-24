using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class UsersResponse : PagedList
    {
        public UserDetails[] Users { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class User : BaseModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}

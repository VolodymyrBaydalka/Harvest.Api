using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class Client : BaseModel
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string Currency { get; set; }
        public string Address { get; set; }
        public string StatementKey { get; set; }
    }

    public class ClientsResponse : PagedList
    {
        public Client[] Clients { get; set; }
    }
}

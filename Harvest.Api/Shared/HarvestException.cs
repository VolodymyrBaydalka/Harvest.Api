using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Harvest.Api
{
    public class HarvestException : Exception
    {
        public HarvestException()
        {
        }

        public HarvestException(string message) : base(message)
        {
        }

        public HarvestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

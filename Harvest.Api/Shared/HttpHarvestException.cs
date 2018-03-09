using System;
using System.Net;

namespace Harvest.Api
{
    public class HttpHarvestException : HarvestException
    {

        public HttpStatusCode StatusCode { get; set; }

        public HttpHarvestException()
        {
        }

        public HttpHarvestException(string message) : base(message)
        {
        }

        public HttpHarvestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

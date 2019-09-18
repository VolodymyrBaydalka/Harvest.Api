using Harvest.Api;
using System;
using System.Collections.Generic;
using System.Text;

namespace XUnitTests
{
    public class HarvestClientFactory
    {
        public static HarvestClient Create()
        {
            var client = HarvestClient.FromAccessToken("TestClient", "<access token>");
            client.DefaultAccountId = null; 
            return client;
        }
    }
}

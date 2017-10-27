using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    internal class TokenModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public long ExpiresIn { get; set; }
    }
}

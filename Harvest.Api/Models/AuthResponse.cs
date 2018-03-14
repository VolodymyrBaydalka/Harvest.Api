using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    public class AuthResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public long ExpiresIn { get; set; }
        public string Scope { get; set; }
    }
}

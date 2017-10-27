using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Harvest.Api
{
    public class HarvestAuthentication
    {
        private static readonly RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
        private static Regex scopeRegex = new Regex("harvest:(?<scopeid>[^ ]*)");

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string UserAgent { get; set; }
        public Uri RedirectUri { get; set; }
        private string State { get; set; }

        public async Task<HarvestClient> HandleCallback(Uri callbackUri)
        {
            var query = HttpUtility.ParseQueryString(callbackUri.Query);
            var code = query["code"];
            var accessToken = query["access_token"];
            var tokenType = query["token_type"];
            var scope = query["scope"];
            var state = query["state"];

            if (state != this.State)
                throw new InvalidOperationException("Login states doesn't match");

            if (string.IsNullOrEmpty(accessToken))
            {
                using (var httpClient = new HttpClient())
                {
                    if (this.UserAgent != null)
                        httpClient.DefaultRequestHeaders.Add("User-Agent", this.UserAgent);

                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var token = await new RequestBuilder()
                        .Begin(HttpMethod.Post, "https://id.getharvest.com/api/v1/oauth2/token")
                        .Form("code", code)
                        .Form("client_id", this.ClientId)
                        .Form("client_secret", this.ClientSecret)
                        .Form("grant_type", "authorization_code")
                        .SendAsync<TokenModel>(httpClient);

                    accessToken = token.AccessToken;
                    tokenType = token.TokenType;
                }
            }

            var harvestScopes = ParseHarvestScopes(scope);

            return new HarvestClient(accessToken)
            {
                DefaultAccountId = DefaultAccountId(harvestScopes),
                UserAgent = this.UserAgent
            };
        }

        public Uri BuildUrl(string state = null, string scope = null)
        {
            this.State = state ?? GenerateState();

            var query = new Dictionary<string, string>
            {
                { "client_id", this.ClientId },
                { "redirect_uri", this.RedirectUri.ToString() },
                { "state", this.State },
                { "scope", scope },
                { "response_type", "code" }
            };

            return RequestBuilder.BuildUri("https://id.getharvest.com/oauth2/authorize", query);
        }

        public bool IsRedirectUri(Uri uri)
        {
            return uri.GetLeftPart(UriPartial.Path) == this.RedirectUri.GetLeftPart(UriPartial.Path);
        }

        private static long? DefaultAccountId(string[] scopes)
        {

            if (scopes != null && scopes.Length == 1 && long.TryParse(scopes[0], out long result))
                return result;

            return null;
        }

        private static string[] ParseHarvestScopes(string scope)
        {
            var result = new List<string>();
            var mathes = scopeRegex.Matches(scope);

            foreach (Match match in mathes)
            {
                if (match.Success)
                    result.Add(match.Groups["scopeid"].Value);
            }

            return result.ToArray();
        }

        private static string GenerateState()
        {
            byte[] data = new byte[32];
            random.GetBytes(data);
            return Convert.ToBase64String(data);
        }
    }
}

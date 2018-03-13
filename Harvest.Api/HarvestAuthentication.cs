using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

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

        public async Task<string> GetAccessTokenAsync(Uri callbackUri)
        {
            var queryParams = ParseQueryString(callbackUri.GetComponents(UriComponents.Query, UriFormat.UriEscaped));

            if (!queryParams.TryGetValue("state", out var state) || state != this.State)
                throw new InvalidOperationException("Login states doesn't match");

            queryParams.TryGetValue("code", out var code);
            queryParams.TryGetValue("token_type", out var tokenType);

            if (!queryParams.TryGetValue("access_token", out var accessToken))
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

            return accessToken;
        }

        public string[] GetScopes(Uri callbackUri)
        {
            var queryParams = ParseQueryString(callbackUri.GetComponents(UriComponents.Query, UriFormat.UriEscaped));
            queryParams.TryGetValue("scope", out var scope);
            return ParseHarvestScopes(scope);
        }

        public async Task<HarvestClient> CreateClientAsync(Uri callbackUri)
        {
            var accessToken = await GetAccessTokenAsync(callbackUri);
            var scopes = GetScopes(callbackUri);

            return new HarvestClient(accessToken)
            {
                DefaultAccountId = DefaultAccountId(scopes),
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

        private static Dictionary<string, string> ParseQueryString(String query)
        {
            return query.Split('&').Select(x => x.Split('='))
                .ToDictionary(x => Uri.UnescapeDataString(x[0]), y => y.Length > 1 ? Uri.UnescapeDataString(y[1]) : null);
        }

        private static string GenerateState()
        {
            byte[] data = new byte[32];
            random.GetBytes(data);
            return Convert.ToBase64String(data);
        }
    }
}

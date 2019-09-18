using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Harvest.Api
{
    class RequestBuilder
    {
        private const string AccountIdHeader = "Harvest-Account-Id";
        private const string DateFormat = "yyyy-MM-dd";
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";
        private const string TimeFormat = @"hh\:mm\:ss";
        private readonly static JsonSerializer _serializer;
        internal const string JsonMimeType = "application/json";

        public static HttpMethod PatchMethod = new HttpMethod("PATCH");

        private readonly Dictionary<string, string> _query = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _form = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>();
        private JObject _json;
        private HttpMethod _httpMethod;
        private Uri _uri;

        static RequestBuilder()
        {
            _serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                Converters = new[] {
                    new TimeSpanConverter()
                }
            });
        }

        public RequestBuilder Begin(HttpMethod httpMethod, Uri uri)
        {
            _uri = uri;
            _httpMethod = httpMethod;

            _query.Clear();
            _headers.Clear();
            _form.Clear();
            _json = null;

            return this;
        }

        public RequestBuilder Begin(HttpMethod httpMethod, string uri)
        {
            return Begin(httpMethod, new Uri(uri));
        }

        public RequestBuilder Begin(string uri)
        {
            return Begin(HttpMethod.Get, new Uri(uri));
        }

        public RequestBuilder Begin(Uri uri)
        {
            return Begin(HttpMethod.Get, uri);
        }

        public RequestBuilder Query(string name, string value)
        {
            if (value != null)
                this._query.Add(name, value);

            return this;
        }

        public RequestBuilder Query(string name, long? value)
        {
            return Query(name, value?.ToString());
        }

        public RequestBuilder Query(string name, int? value)
        {
            return Query(name, value?.ToString());
        }

        public RequestBuilder Query(string name, bool? value)
        {
            return Query(name, value?.ToString().ToLowerInvariant());
        }

        public RequestBuilder Query(string name, DateTime? value, bool truncateTime = false)
        {
            return Query(name, value?.ToString(truncateTime ? DateFormat : DateTimeFormat));
        }

        public RequestBuilder QueryPageSince(DateTime? updatedSince = null, int? page = null, int? perPage = null)
        {
            return Query("updated_since", updatedSince).Query("page", page).Query("per_page", perPage);
        }

        private RequestBuilder BodyInternal(string name, object value)
        {
            if (value == null)
                return this;

            if (_json == null)
                _form.Add(name, Convert.ToString(value));
            else
                _json.Add(name, JToken.FromObject(value));

            return this;
        }

        public RequestBuilder UseJson()
        {
            _json = new JObject();
            return this;
        }

        public RequestBuilder Body(string name, string value)
        {
            return BodyInternal(name, value);
        }

        public RequestBuilder Body(string name, TimeSpan? value)
        {
            return BodyInternal(name, value?.ToString(TimeFormat));
        }

        public RequestBuilder Body(string name, long? value)
        {
            return BodyInternal(name, value);
        }

        public RequestBuilder Body(string name, bool? value)
        {
            return BodyInternal(name, value);
        }

        public RequestBuilder Body(string name, decimal? value)
        {
            return BodyInternal(name, value);
        }

        public RequestBuilder Body(string name, DateTime? value, bool truncateTime = false)
        {
            return BodyInternal(name, value?.ToString(truncateTime ? DateFormat : DateTimeFormat));
        }

        public RequestBuilder Body(string name, ExternalReference value)
        {
            if (value == null)
                return this;

            if (_json == null)
            {
                _form.Add($"{name}.id", value.Id);
                _form.Add($"{name}.group_id", value.GroupId);
                _form.Add($"{name}.permalink", value.Permalink);
            }
            else
            {
                _json.Add(name, new JObject
                {
                    ["id"] = value.Id,
                    ["group_id"] = value.GroupId,
                    ["permalink"] = value.Permalink
                });
            }

            return this;
        }

        public RequestBuilder Body(string name, long[] value)
        {
            if (value == null)
                return this;

            if (_json == null)
                throw new NotImplementedException();
            else
                _json.Add(name, new JArray(value));

            return this;
        }

        public RequestBuilder Body(string name, string[] value)
        {
            if (value == null)
                return this;

            if (_json == null)
                throw new NotImplementedException();
            else
                _json.Add(name, new JArray(value));

            return this;
        }

        public RequestBuilder Header(string name, string value)
        {
            if (value != null)
                this._headers.Add(name, value);

            return this;
        }

        public RequestBuilder AccountId(long? accountId)
        {
            if (accountId != null)
                this._headers.Add(AccountIdHeader, accountId.ToString());

            return this;
        }

        public RequestBuilder UserAgent(string userAgent)
        {
            return Header("User-Agent", userAgent);
        }

        public async Task<T> SendAsync<T>(HttpClient httpClient, CancellationToken token = default)
        {
            var stream = await SendAsyncInternal(httpClient, token, true);

            using (var reader = new JsonTextReader(new StreamReader(stream)))
                return _serializer.Deserialize<T>(reader);
        }

        public async System.Threading.Tasks.Task SendAsync(HttpClient httpClient, CancellationToken token = default)
        {
            await SendAsyncInternal(httpClient, token, false);
        }

        private async Task<Stream> SendAsyncInternal(HttpClient httpClient, CancellationToken token, bool readRespose)
        {
            var request = new HttpRequestMessage(_httpMethod, BuildUri(_uri, _query));

            if (_httpMethod == HttpMethod.Post || _httpMethod == HttpMethod.Put || _httpMethod == PatchMethod)
            {
                if (_json != null)
                {
                    using (var stringWriter = new StringWriter())
                    {
                        _serializer.Serialize(stringWriter, _json);
                        request.Content = new StringContent(stringWriter.ToString(), null, JsonMimeType);
                    }
                }
                else
                {
                    request.Content = new FormUrlEncodedContent(_form);
                }
            }

            foreach (var header in _headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            var resp = await httpClient.SendAsync(request, token);

            try
            {
                resp.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException)
            {
                throw new HttpHarvestException(resp.ReasonPhrase) { StatusCode = resp.StatusCode };
            }

            if (readRespose)
                return await resp.Content.ReadAsStreamAsync();

            return null;
        }

        public static Uri BuildUri(string uri, IEnumerable<KeyValuePair<string, string>> query)
        {
            return new UriBuilder(uri) { Query = ToQuery(query) }.Uri;
        }

        public static Uri BuildUri(Uri uri, IEnumerable<KeyValuePair<string, string>> query)
        {
            return new UriBuilder(uri) { Query = ToQuery(query) }.Uri;
        }

        public static string ToQuery(IEnumerable<KeyValuePair<string, string>> query)
        {
            var builder = new StringBuilder();
            var first = true;

            foreach (var item in query)
            {
                if (first)
                    first = false;
                else
                    builder.Append("&");

                if (item.Value != null)
                    builder.Append(Uri.EscapeDataString(item.Key)).Append("=").Append(Uri.EscapeDataString(item.Value));
            }

            return builder.ToString();
        }
    }
}

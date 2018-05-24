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
        private readonly static JsonSerializer _serializer;
        internal const string JsonMimeType = "application/json";

        public static HttpMethod PatchMethod = new HttpMethod("PATCH");

        private Dictionary<string, string> _query = new Dictionary<string, string>();
        private Dictionary<string, string> _form = new Dictionary<string, string>();
        private Dictionary<string, string> _headers = new Dictionary<string, string>();
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
                Converters = new [] {
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

        public RequestBuilder Query(string name, long? value)
        {
            if (value != null)
                this._query.Add(name, value.ToString());

            return this;
        }

        public RequestBuilder Query(string name, int? value)
        {
            if (value != null)
                this._query.Add(name, value.ToString());

            return this;
        }

        public RequestBuilder Query(string name, bool? value)
        {
            if (value != null)
                this._query.Add(name, value.ToString().ToLowerInvariant());

            return this;
        }

        public RequestBuilder Query(string name, DateTime? value)
        {
            if (value != null)
                this._query.Add(name, value.Value.ToString(DateFormat));

            return this;
        }

        public RequestBuilder Query(string name, string value)
        {
            if (value != null)
                this._query.Add(name, value);

            return this;
        }

        public RequestBuilder Form(string name, string value)
        {
            if (value != null)
                this._form.Add(name, value);

            return this;
        }

        public RequestBuilder Form(string name, TimeSpan? value)
        {
            if (value != null)
                this._form.Add(name, value.ToString()); //TODO

            return this;
        }

        public RequestBuilder Form(string name, long? value)
        {
            if (value != null)
                this._form.Add(name, value.ToString());

            return this;
        }

        public RequestBuilder Form(string name, bool? value)
        {
            if (value != null)
                this._form.Add(name, value.ToString().ToLowerInvariant());

            return this;
        }

        public RequestBuilder Form(string name, decimal? value)
        {
            if (value != null)
                this._form.Add(name, value.ToString());

            return this;
        }

        public RequestBuilder Form(string name, DateTime? value)
        {
            if (value != null)
                this._form.Add(name, value.Value.ToString(DateFormat));

            return this;
        }

        public RequestBuilder Form(string name, ExternalReference value)
        {
            if (value != null)
            {
                this.Form($"{name}.id", value.Id);
                this.Form($"{name}.group_id", value.GroupId);
                this.Form($"{name}.permalink", value.Permalink);
            }

            return this;
        }

        public RequestBuilder UseJson()
        {
            _json = new JObject();
            return this;
        }

        public RequestBuilder Json(string name, ExternalReference value)
        {
            if (value != null)
            {
                var jref = new JObject
                {
                    ["id"] = value.Id,
                    ["group_id"] = value.GroupId,
                    ["permalink"] = value.Permalink
                };

                _json[name] = jref;
            };

            return this;
        }

        public RequestBuilder Json(string name, string value)
        {
            if (value != null)
                _json[name] = value;
            
            return this;
        }

        public RequestBuilder Json(string name, long? value)
        {
            if (value != null)
                _json[name] = value;

            return this;
        }

        public RequestBuilder Json(string name, decimal? value)
        {
            if (value != null)
                _json[name] = value;

            return this;
        }

        public RequestBuilder Json(string name, DateTime? value)
        {
            if (value != null)
                _json[name] = value.Value.ToString(DateFormat);

            return this;
        }


        public RequestBuilder Json(string name, TimeSpan? value)
        {
            if (value != null)
                _json[name] = value.Value.ToString(DateFormat);

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

        public async Task<T> SendAsync<T>(HttpClient httpClient, CancellationToken token = default(CancellationToken))
        {
            var stream = await SendAsyncInternal(httpClient, token, true);

            using (var reader = new JsonTextReader(new StreamReader(stream)))
                return _serializer.Deserialize<T>(reader);
        }

        public async System.Threading.Tasks.Task SendAsync(HttpClient httpClient, CancellationToken token = default(CancellationToken))
        {
            await SendAsyncInternal(httpClient, token, false);
        }

        private async Task<Stream> SendAsyncInternal(HttpClient httpClient, CancellationToken token, bool readRespose)
        {
            var request = new HttpRequestMessage(_httpMethod, BuildUri(_uri, _query));

            if (_httpMethod == HttpMethod.Post || _httpMethod == HttpMethod.Put || _httpMethod == PatchMethod)
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

            foreach (var header in _headers)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
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

        public static Uri BuildUri(string uri, Dictionary<string, string> query)
        {
            return new UriBuilder(uri) { Query = ToQuery(query) }.Uri;
        }

        public static Uri BuildUri(Uri uri, Dictionary<string, string> query)
        {
            return new UriBuilder(uri) { Query = ToQuery(query) }.Uri;
        }

        public static string ToQuery(Dictionary<string, string> query)
        {
            var builder = new StringBuilder();
            var first = true;

            foreach (var item in query)
            {
                if (first)
                    first = false;
                else
                    builder.Append("&");

                if(item.Value != null)
                    builder.Append(Uri.EscapeDataString(item.Key)).Append("=").Append(Uri.EscapeDataString(item.Value));
            }

            return builder.ToString();
        }
    }
}

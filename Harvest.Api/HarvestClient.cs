using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Harvest.Api
{
    public class HarvestClient : IDisposable
    {
        #region Constants
        private string tokenType = "Bearer";
        #endregion

        #region Members
        private HttpClient _httpClient;
        private RequestBuilder _requestBuilder = new RequestBuilder();
        private string _authState;
        #endregion

        #region Properties
        public long? DefaultAccountId { get; set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public DateTime ExpireAt { get; private set; }

        public string UserAgent { get; private set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public Uri RedirectUri { get; set; }
        #endregion

        #region Events
        public event EventHandler TokenRefreshed;
        #endregion

        #region Constructor
        public HarvestClient(string userAgent, HttpClientHandler httpClientHandler = null)
        {
            if (string.IsNullOrEmpty(userAgent))
                throw new ArgumentNullException(nameof(userAgent));

            this.UserAgent = userAgent;

            _httpClient = new HttpClient(httpClientHandler ?? new HttpClientHandler());
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(RequestBuilder.JsonMimeType));
            _httpClient.DefaultRequestHeaders.Add("User-Agent", this.UserAgent);
        }

        public HarvestClient(string userAgent, string accessToken, string refreshToken = null, long expiresIn = 0, HttpClientHandler httpClientHandler = null)
            : this(userAgent, httpClientHandler)
        {
            OnAuthorize(new AuthResponse { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresIn = 0 });
        }

        public HarvestClient(string userAgent, string clientId, string clientSecret, Uri redirectUri = null, HttpClientHandler httpClientHandler = null)
            : this(userAgent, httpClientHandler)
        {
            if(string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            if (string.IsNullOrEmpty(clientSecret))
                throw new ArgumentNullException(nameof(clientSecret));

            this.ClientId = clientId;
            this.ClientSecret = clientSecret;
            this.RedirectUri = redirectUri;
        }
        #endregion

        #region Auth methods
        public Uri BuildAuthorizationUrl(string state = null, string scope = null, bool codeType = true)
        {
            if (string.IsNullOrEmpty(ClientId))
                throw new InvalidOperationException("ClientId is empty or null");

            if (RedirectUri == null)
                throw new InvalidOperationException("RedirectUri is null");

            _authState = state ?? Utilities.GenerateState();

            var query = new Dictionary<string, string>
            {
                { "client_id", this.ClientId },
                { "redirect_uri", this.RedirectUri.ToString() },
                { "state", _authState },
                { "scope", scope },
                { "response_type", codeType ? "code" : "token" }
            };

            return RequestBuilder.BuildUri("https://id.getharvest.com/oauth2/authorize", query);
        }

        public async Task<AuthResponse> AuthorizeAsync(Uri callbackUri, bool defaultAccountId = true)
        {
            var query = Utilities.ParseQueryString(callbackUri.GetComponents(UriComponents.Query, UriFormat.UriEscaped));

            if (!query.TryGetValue("state", out var state) || state != _authState)
                throw new InvalidOperationException("OAuth states doesn't match");

            AuthResponse result = null;

            if (query.TryGetValue("token_type", out var tokenType) &&
                query.TryGetValue("access_token", out var accessToken))
            {
                query.TryGetValue("expires_in", out var expiresIn);

                result = new AuthResponse { AccessToken = accessToken, TokenType = tokenType, ExpiresIn = long.Parse(expiresIn) };
            }
            else if (query.TryGetValue("code", out var code))
            {
                result = await new RequestBuilder()
                    .Begin(HttpMethod.Post, "https://id.getharvest.com/api/v1/oauth2/token")
                    .Form("code", code)
                    .Form("client_id", this.ClientId)
                    .Form("client_secret", this.ClientSecret)
                    .Form("grant_type", "authorization_code")
                    .SendAsync<AuthResponse>(_httpClient);
            }

            if (result == null)
                throw new ArgumentException(nameof(callbackUri));

            query.TryGetValue("scope", out var scope);
            result.Scope = scope;

            if (defaultAccountId)
                this.DefaultAccountId = Utilities.FirstHarvestAccountId(scope);

            OnAuthorize(result);

            return result;
        }

        public async Task<AuthResponse> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(this.RefreshToken))
                throw new InvalidOperationException("Refresh token is empty");

            var result = await new RequestBuilder()
                .Begin(HttpMethod.Post, "https://id.getharvest.com/api/v1/oauth2/token")
                .Form("client_id", this.ClientId)
                .Form("client_secret", this.ClientSecret)
                .Form("grant_type", "refresh_token")
                .Form("refresh_token", this.RefreshToken)
                .SendAsync<AuthResponse>(_httpClient);

            OnAuthorize(result);

            return result;
        }

        private void OnAuthorize(AuthResponse auth)
        {
            this.AccessToken = auth.AccessToken;
            this.RefreshToken = auth.RefreshToken;
            this.ExpireAt = DateTime.Now.AddSeconds(auth.ExpiresIn);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType, auth.AccessToken);

            this.TokenRefreshed?.Invoke(this, EventArgs.Empty);
        }

        public bool IsRedirectUri(Uri uri)
        {
            if (RedirectUri == null)
                throw new InvalidOperationException("RedirectUri is null");

            return uri.GetLeftPart(UriPartial.Path) == this.RedirectUri.GetLeftPart(UriPartial.Path);
        }
        #endregion

        #region API methods
        public async Task<AccountsResponse> GetAccountsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await RefreshTokenIsNeeded();
            return await _requestBuilder
                .Begin("https://id.getharvest.com/api/v1/accounts")
                .SendAsync<AccountsResponse>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntriesResponse> GetTimeEntriesAsync(long? userId = null, long? clientId = null, long? projectId = null, bool? isBilled = null,
            DateTime? updatedSince = null, DateTime? fromDate = null, DateTime? toDate = null, int? page = null, int? perPage = null, long? accountId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder("https://api.harvestapp.com/v2/time_entries", accountId)
                .Query("user_id", userId)
                .Query("client_id", clientId)
                .Query("project_id", projectId)
                .Query("is_billed", isBilled)
                .Query("updated_since", updatedSince)
                .Query("from", fromDate)
                .Query("to", toDate)
                .Query("page", page)
                .Query("per_page", perPage)
                .SendAsync<TimeEntriesResponse>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> GetTimeEntryAsync(long entryId, long? accountId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"https://api.harvestapp.com/v2/time_entries/{entryId}", accountId)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> RestartTimeEntryAsync(long entryId, long? accountId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"https://api.harvestapp.com/v2/time_entries/{entryId}/restart", accountId, RequestBuilder.PatchMethod)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> StopTimeEntryAsync(long entryId, long? accountId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"https://api.harvestapp.com/v2/time_entries/{entryId}/stop", accountId, RequestBuilder.PatchMethod)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async ThreadingTask DeleteTimeEntryAsync(long entryId, long? accountId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RefreshTokenIsNeeded();
            await SimpleRequestBuilder($"https://api.harvestapp.com/v2/time_entries/{entryId}", accountId, HttpMethod.Delete)
                .SendAsync(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> CreateTimeEntryAsync(long projectId, long taskId, DateTime spentDate,
            TimeSpan? startedTime = null, TimeSpan? endedTime = null, decimal? hours = null, string notes = null, ExternalReference externalReference = null,
            long? accountId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"https://api.harvestapp.com/v2/time_entries", accountId, HttpMethod.Post)
                .Form("project_id", projectId)
                .Form("task_id", taskId)
                .Form("spent_date", spentDate)
                .Form("started_time", startedTime)
                .Form("ended_time", endedTime)
                .Form("hours", hours)
                .Form("notes", notes)
                .Form("external_reference", externalReference)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> UpdateTimeEntryAsync(long entryId,
            long? projectId = null, long? taskId = null, DateTime? spentDate = null, TimeSpan? startedTime = null, TimeSpan? endedTime = null,
            decimal? hours = null, string notes = null, ExternalReference externalReference = null,
            long? accountId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"https://api.harvestapp.com/v2/time_entries/{entryId}", accountId, RequestBuilder.PatchMethod)
                .Form("project_id", projectId)
                .Form("task_id", taskId)
                .Form("spent_date", spentDate)
                .Form("started_time", startedTime)
                .Form("ended_time", endedTime)
                .Form("hours", hours)
                .Form("notes", notes)
                .Form("external_reference", externalReference)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async Task<ProjectAssignmentsResponse> GetProjectAssignmentsAsync(long? userId = null, DateTime? updatedSince = null, int? page = null, int? perPage = null, long? accountId = null)
        {
            await RefreshTokenIsNeeded();
            var userIdOrMe = userId.HasValue ? userId.ToString() : "me";

            return await SimpleRequestBuilder($"https://api.harvestapp.com/v2/users/{userIdOrMe}/project_assignments", accountId)
                .Query("updated_since", updatedSince)
                .Query("page", page)
                .Query("per_page", perPage)
                .SendAsync<ProjectAssignmentsResponse>(_httpClient, CancellationToken.None);
        }

        public async Task<ProjectsResponse> GetProjectsAsync(long? clientId = null, DateTime? updatedSince = null, int? page = null, int? perPage = null, long? accountId = null)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder("https://api.harvestapp.com/v2/projects", accountId)
                .Query("client_id", clientId)
                .Query("updated_since", updatedSince)
                .Query("page", page)
                .Query("per_page", perPage)
                .SendAsync<ProjectsResponse>(_httpClient, CancellationToken.None);
        }

        public async Task<TasksResponse> GetTasksAsync(DateTime? updatedSince = null, int? page = null, int? perPage = null, long? accountId = null)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder("https://api.harvestapp.com/v2/tasks", accountId)
                .Query("updated_since", updatedSince)
                .Query("page", page)
                .Query("per_page", perPage)
                .SendAsync<TasksResponse>(_httpClient, CancellationToken.None);
        }
        #endregion

        #region Implementation
        private async System.Threading.Tasks.Task RefreshTokenIsNeeded()
        {
            if (this.ExpireAt <= DateTime.Now && string.IsNullOrEmpty(this.RefreshToken))
                await RefreshTokenAsync();
        }

        private RequestBuilder SimpleRequestBuilder(string url, long? accountId, HttpMethod httpMethod = null)
        {
            if (accountId == null && this.DefaultAccountId == null)
                throw new HarvestException("accountId or DefaultAccountId should be specified");

            return _requestBuilder.Begin(httpMethod ?? HttpMethod.Get, url)
                .AccountId(accountId ?? this.DefaultAccountId);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
        #endregion
    }
}

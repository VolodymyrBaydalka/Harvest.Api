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
        private const string tokenType = "Bearer";
        private const string harvestIdUrl = "https://id.getharvest.com";
        private const string harvestApiUrl = "https://api.harvestapp.com/v2";
        #endregion

        #region Members
        private readonly HttpClient _httpClient;
        private readonly RequestBuilder _requestBuilder = new RequestBuilder();
        #endregion

        #region Properties
        public long? DefaultAccountId { get; set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public DateTime ExpireAt { get; private set; }
        public string AuthState { get; private set; }

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
        #endregion

        #region Methods
        public static HarvestClient FromAccessToken(string userAgent, string accessToken, string refreshToken = null, long expiresIn = 0, HttpClientHandler httpClientHandler = null)
        {
            var client = new HarvestClient(userAgent, httpClientHandler);

            client.Authorize(accessToken, refreshToken, expiresIn);

            return client;
        }
        #endregion

        #region Auth methods
        public Uri BuildAuthorizationUrl(string state = null, string scope = null, bool codeType = true)
        {
            if (string.IsNullOrEmpty(ClientId))
                throw new InvalidOperationException("ClientId is empty or null");

            if (RedirectUri == null)
                throw new InvalidOperationException("RedirectUri is null");

            AuthState = state ?? Utilities.GenerateState();

            var query = new Dictionary<string, string>
            {
                { "client_id", this.ClientId },
                { "redirect_uri", this.RedirectUri.ToString() },
                { "state", AuthState },
                { "scope", scope },
                { "response_type", codeType ? "code" : "token" }
            };

            return RequestBuilder.BuildUri($"{harvestIdUrl}/oauth2/authorize", query);
        }

        public async Task<AuthResponse> AuthorizeAsync(Uri callbackUri, string state = null, bool defaultAccountId = true)
        {
            var query = Utilities.ParseQueryString(callbackUri.GetComponents(UriComponents.Query, UriFormat.UriEscaped));

            if (!query.TryGetValue("state", out var urlState) || urlState != (state ?? AuthState))
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
                    .Begin(HttpMethod.Post, $"{harvestIdUrl}/api/v1/oauth2/token")
                    .Form("code", code)
                    .Form("client_id", this.ClientId)
                    .Form("client_secret", this.ClientSecret)
                    .Form("grant_type", "authorization_code")
                    .SendAsync<AuthResponse>(_httpClient);
            }

            if (result == null)
                throw new ArgumentException(nameof(callbackUri));

            Authorize(result.AccessToken, result.RefreshToken, result.ExpiresIn);

            query.TryGetValue("scope", out var scope);
            result.Scope = scope;

            if (defaultAccountId)
                this.DefaultAccountId = Utilities.FirstHarvestAccountId(scope);

            return result;
        }

        public async Task<AuthResponse> RefreshTokenAsync()
        {
            if (string.IsNullOrEmpty(this.RefreshToken))
                throw new InvalidOperationException("Refresh token is empty");

            var result = await new RequestBuilder()
                .Begin(HttpMethod.Post, $"{harvestIdUrl}/api/v1/oauth2/token")
                .Form("client_id", this.ClientId)
                .Form("client_secret", this.ClientSecret)
                .Form("grant_type", "refresh_token")
                .Form("refresh_token", this.RefreshToken)
                .SendAsync<AuthResponse>(_httpClient);

            Authorize(result.AccessToken, result.RefreshToken, result.ExpiresIn);

            return result;
        }

        public void Authorize(string accessToken, string refreshToken = null, long expiresIn = 0)
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentException(nameof(accessToken));

            this.AccessToken = accessToken;
            this.RefreshToken = refreshToken;
            this.ExpireAt = DateTime.Now.AddSeconds(expiresIn);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(tokenType, accessToken);

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
        public async Task<AccountsResponse> GetAccountsAsync(CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await _requestBuilder
                .Begin($"{harvestIdUrl}/api/v1/accounts")
                .SendAsync<AccountsResponse>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntriesResponse> GetTimeEntriesAsync(long? userId = null, long? clientId = null, long? projectId = null, bool? isBilled = null,
            DateTime? updatedSince = null, DateTime? fromDate = null, DateTime? toDate = null, int? page = null, int? perPage = null, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"{harvestApiUrl}/time_entries", accountId)
                .Query("user_id", userId)
                .Query("client_id", clientId)
                .Query("project_id", projectId)
                .Query("is_billed", isBilled)
                .Query("from", fromDate, true)
                .Query("to", toDate, true)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<TimeEntriesResponse>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> GetTimeEntryAsync(long entryId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/time_entries/{entryId}", accountId)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> RestartTimeEntryAsync(long entryId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/time_entries/{entryId}/restart", accountId, RequestBuilder.PatchMethod)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> StopTimeEntryAsync(long entryId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/time_entries/{entryId}/stop", accountId, RequestBuilder.PatchMethod)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async ThreadingTask DeleteTimeEntryAsync(long entryId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            await SimpleRequestBuilder($"{harvestApiUrl}/time_entries/{entryId}", accountId, HttpMethod.Delete)
                .SendAsync(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> CreateTimeEntryAsync(long projectId, long taskId, DateTime spentDate,
            TimeSpan? startedTime = null, TimeSpan? endedTime = null, decimal? hours = null, string notes = null, ExternalReference externalReference = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"{harvestApiUrl}/time_entries", accountId, HttpMethod.Post)
                .Form("project_id", projectId)
                .Form("task_id", taskId)
                .Form("spent_date", spentDate, true)
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
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"{harvestApiUrl}/time_entries/{entryId}", accountId, RequestBuilder.PatchMethod)
                .Form("project_id", projectId)
                .Form("task_id", taskId)
                .Form("spent_date", spentDate, true)
                .Form("started_time", startedTime)
                .Form("ended_time", endedTime)
                .Form("hours", hours)
                .Form("notes", notes)
                .Form("external_reference", externalReference)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async Task<ProjectAssignmentsResponse> GetProjectAssignmentsAsync(long? userId = null, DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            var userIdOrMe = userId.HasValue ? userId.ToString() : "me";

            return await SimpleRequestBuilder($"{harvestApiUrl}/users/{userIdOrMe}/project_assignments", accountId)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<ProjectAssignmentsResponse>(_httpClient, cancellationToken);
        }

        public async Task<TaskAssignmentsResponse> GetTaskAssignmentsAsync(long? projectId = null, bool? isActive = null, DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            var projectPath = projectId.HasValue ? $"/projects/{projectId}" : string.Empty;

            return await SimpleRequestBuilder($"{harvestApiUrl}{projectPath}/task_assignments", accountId)
                .Query("is_active", isActive)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<TaskAssignmentsResponse>(_httpClient, cancellationToken);
        }

        public async Task<TaskAssignment> GetTaskAssignmentAsync(long projectId, long taskAssigmentId, DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"{harvestApiUrl}/projects/{projectId}/task_assignments/{taskAssigmentId}", accountId)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<TaskAssignment>(_httpClient, cancellationToken);
        }


        public async Task<TaskAssignment> CreateTaskAssignmentAsync(long projectId, long taskId, bool? isActive = null, bool? billable = null, decimal? hourlyRate = null, decimal? budget = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"{harvestApiUrl}/projects/{projectId}/task_assignments", accountId, HttpMethod.Post)
                .Form("task_id", taskId)
                .Form("is_active", isActive)
                .Form("billable", billable)
                .Form("hourly_rate", hourlyRate)
                .Form("budget", budget)
                .SendAsync<TaskAssignment>(_httpClient, cancellationToken);
        }

        public async Task<TaskAssignment> UpdateTaskAssignmentAsync(long projectId, long taskAssigmentId, bool? isActive = null, bool? billable = null, decimal? hourlyRate = null, decimal? budget = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"{harvestApiUrl}/projects/{projectId}/task_assignments/{taskAssigmentId}", accountId, RequestBuilder.PatchMethod)
                .Form("is_active", isActive)
                .Form("billable", billable)
                .Form("hourly_rate", hourlyRate)
                .Form("budget", budget)
                .SendAsync<TaskAssignment>(_httpClient, cancellationToken);
        }

        public async ThreadingTask DeleteTaskAssignmentAsync(long projectId, long taskAssigmentId,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            await SimpleRequestBuilder($"{harvestApiUrl}/projects/{projectId}/task_assignments/{taskAssigmentId}", accountId, HttpMethod.Delete)
                .SendAsync<TaskAssignment>(_httpClient, cancellationToken);
        }

        public async Task<ProjectsResponse> GetProjectsAsync(long? clientId = null, DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/projects", accountId)
                .Query("client_id", clientId)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<ProjectsResponse>(_httpClient, cancellationToken);
        }

        public async Task<TasksResponse> GetTasksAsync(DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/tasks", accountId)
                .Query("updated_since", updatedSince)
                .Query("page", page)
                .Query("per_page", perPage)
                .SendAsync<TasksResponse>(_httpClient, cancellationToken);
        }

        public async Task<Company> GetCompanyAsync(long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/company", accountId)
                .SendAsync<Company>(_httpClient, cancellationToken);
        }

        public async Task<UserDetails> GetMe(long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/users/me", accountId)
                .SendAsync<UserDetails>(_httpClient, cancellationToken);
        }

        public async Task<UserDetails> GetUser(long userId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/users/{userId}", accountId)
                .SendAsync<UserDetails>(_httpClient, cancellationToken);
        }

        public async Task<UsersResponse> GetUsers(bool? isActive = null, DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/users", accountId)
                .Query("is_active", isActive)
                .Query("updated_since", updatedSince)
                .Query("page", page)
                .Query("per_page", perPage)
                .SendAsync<UsersResponse>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> CreateUser(string firstName, string lastName, string email,
            string telephone = null, string timezone = null, bool? hasAccessToAllFutureProjects = null,
            bool? isContractor = null, bool? isAdmin = null, bool? isProjectManager = null,
            bool? canSeeRates = null, bool? canCreateProjects = null, bool? canCreateInvoices = null,
            bool? isActive = null, int? weeklyCapacity = null, decimal? defaultHourlyRate = null,
            decimal? costRate = null, string[] roles = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"{harvestApiUrl}/users", accountId, HttpMethod.Post)
                .Form("first_name", firstName)
                .Form("last_name", lastName)
                .Form("email", email)
                .Form("telephone", telephone)
                .Form("timezone", timezone)
                .Form("has_access_to_all_future_projects", hasAccessToAllFutureProjects)
                .Form("is_contractor", isContractor)
                .Form("is_admin", isAdmin)
                .Form("is_project_manager", isProjectManager)
                .Form("can_see_rates", canSeeRates)
                .Form("can_create_projects", canCreateProjects)
                .Form("can_create_invoices", canCreateInvoices)
                .Form("is_active", isActive)
                .Form("weekly_capacity", weeklyCapacity)
                .Form("default_hourly_rate", defaultHourlyRate)
                .Form("cost_rate", costRate)
                //TODO .Form("roles", roles)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> UpdateUser(int userId, string firstName = null, string lastName = null, string email = null,
            string telephone = null, string timezone = null, bool? hasAccessToAllFutureProjects = null,
            bool? isContractor = null, bool? isAdmin = null, bool? isProjectManager = null,
            bool? canSeeRates = null, bool? canCreateProjects = null, bool? canCreateInvoices = null,
            bool? isActive = null, int? weeklyCapacity = null, decimal? defaultHourlyRate = null,
            decimal? costRate = null, string[] roles = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"{harvestApiUrl}/users/{userId}", accountId, RequestBuilder.PatchMethod)
                .Form("first_name", firstName)
                .Form("last_name", lastName)
                .Form("email", email)
                .Form("telephone", telephone)
                .Form("timezone", timezone)
                .Form("has_access_to_all_future_projects", hasAccessToAllFutureProjects)
                .Form("is_contractor", isContractor)
                .Form("is_admin", isAdmin)
                .Form("is_project_manager", isProjectManager)
                .Form("can_see_rates", canSeeRates)
                .Form("can_create_projects", canCreateProjects)
                .Form("can_create_invoices", canCreateInvoices)
                .Form("is_active", isActive)
                .Form("weekly_capacity", weeklyCapacity)
                .Form("default_hourly_rate", defaultHourlyRate)
                .Form("cost_rate", costRate)
                //TODO .Form("roles", roles)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async ThreadingTask DeleteUser(long userId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            await SimpleRequestBuilder($"{harvestApiUrl}/users/{userId}", accountId, HttpMethod.Delete)
                .SendAsync(_httpClient, cancellationToken);
        }

        public async Task<ExpensesResponse> GetExpenses(long? userId = null, long? clientId = null, long? projectId = null, bool? isBilled = null,
            DateTime? updatedSince = null, int? page = null, int? perPage = null, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/expenses", accountId)
                .Query("user_id", userId)
                .Query("client_id", clientId)
                .Query("project_id", projectId)
                .Query("is_billed", isBilled)
                .Query("updated_since", updatedSince)
                .Query("page", page)
                .Query("per_page", perPage)
                .SendAsync<ExpensesResponse>(_httpClient, cancellationToken);
        }

        public async Task<Expense> GetExpense(long expenseId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/expenses/{expenseId}", accountId)
                .SendAsync<Expense>(_httpClient, cancellationToken);
        }
        #endregion

        #region Implementation
        private async ThreadingTask RefreshTokenIsNeeded()
        {
            if (this.ExpireAt <= DateTime.Now && !string.IsNullOrEmpty(this.RefreshToken))
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

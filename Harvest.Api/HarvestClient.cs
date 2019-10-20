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
                    .Body("code", code)
                    .Body("client_id", this.ClientId)
                    .Body("client_secret", this.ClientSecret)
                    .Body("grant_type", "authorization_code")
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
                .Body("client_id", this.ClientId)
                .Body("client_secret", this.ClientSecret)
                .Body("grant_type", "refresh_token")
                .Body("refresh_token", this.RefreshToken)
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
                .Body("project_id", projectId)
                .Body("task_id", taskId)
                .Body("spent_date", spentDate, true)
                .Body("started_time", startedTime)
                .Body("ended_time", endedTime)
                .Body("hours", hours)
                .Body("notes", notes)
                .Body("external_reference", externalReference)
                .SendAsync<TimeEntry>(_httpClient, cancellationToken);
        }

        public async Task<TimeEntry> UpdateTimeEntryAsync(long entryId,
            long? projectId = null, long? taskId = null, DateTime? spentDate = null, TimeSpan? startedTime = null, TimeSpan? endedTime = null,
            decimal? hours = null, string notes = null, ExternalReference externalReference = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"{harvestApiUrl}/time_entries/{entryId}", accountId, RequestBuilder.PatchMethod)
                .Body("project_id", projectId)
                .Body("task_id", taskId)
                .Body("spent_date", spentDate, true)
                .Body("started_time", startedTime)
                .Body("ended_time", endedTime)
                .Body("hours", hours)
                .Body("notes", notes)
                .Body("external_reference", externalReference)
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
                .Body("task_id", taskId)
                .Body("is_active", isActive)
                .Body("billable", billable)
                .Body("hourly_rate", hourlyRate)
                .Body("budget", budget)
                .SendAsync<TaskAssignment>(_httpClient, cancellationToken);
        }

        public async Task<TaskAssignment> UpdateTaskAssignmentAsync(long projectId, long taskAssigmentId, bool? isActive = null, bool? billable = null, decimal? hourlyRate = null, decimal? budget = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();

            return await SimpleRequestBuilder($"{harvestApiUrl}/projects/{projectId}/task_assignments/{taskAssigmentId}", accountId, RequestBuilder.PatchMethod)
                .Body("is_active", isActive)
                .Body("billable", billable)
                .Body("hourly_rate", hourlyRate)
                .Body("budget", budget)
                .SendAsync<TaskAssignment>(_httpClient, cancellationToken);
        }

        public async ThreadingTask DeleteTaskAssignmentAsync(long projectId, long taskAssigmentId,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            await SimpleRequestBuilder($"{harvestApiUrl}/projects/{projectId}/task_assignments/{taskAssigmentId}", accountId, HttpMethod.Delete)
                .SendAsync<TaskAssignment>(_httpClient, cancellationToken);
        }

        public async Task<ClientsResponse> GetClientsAsync(bool? isActive = null, DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/clients", accountId)
                .Query("is_active", isActive)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<ClientsResponse>(_httpClient, cancellationToken);
        }

        public async Task<Client> GetClientAsync(long? clientId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/clients/{clientId}", accountId)
                .SendAsync<Client>(_httpClient, cancellationToken);
        }

        public async Task<Client> CreateClientAsync(string name, bool? isActive = null, string address = null, string currency = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/clients", accountId, HttpMethod.Post)
                .Body("name", name)
                .Body("is_active", isActive)
                .Body("address", address)
                .Body("currency", currency)
                .SendAsync<Client>(_httpClient, cancellationToken);
        }

        public async Task<Client> UpdateClientAsync(long clientId, string name = null, bool? isActive = null, string address = null, string currency = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/clients/{clientId}", accountId, RequestBuilder.PatchMethod)
                .Body("name", name)
                .Body("is_active", isActive)
                .Body("address", address)
                .Body("currency", currency)
                .SendAsync<Client>(_httpClient, cancellationToken);
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

        public async Task<Project> GetProjectAsync(long projectId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/projects/{projectId}", accountId)
                .SendAsync<Project>(_httpClient, cancellationToken);
        }

        public async Task<Project> CreateProjectAsync(long clientId, string name, bool isBillable, string billBy = "none",
            string code = null, bool? isFixedFee = null, decimal? hourlyRate = null, decimal? budget = null, string budgetBy = "none",
            bool? budgetIsMonthly = null, bool? notifyWhenOverBudget = null, bool? overBudgetNotificationPercentage = null,
            bool? showBudgetToAll = null, decimal? costBudget = null, bool? costBudgetIncludeExpenses = null,
            decimal? fee = null, string notes = null, DateTime? startsOn = null, DateTime? endsOn = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/projects", accountId, HttpMethod.Post)
                .Body("client_id", clientId)
                .Body("name", name)
                .Body("is_billable", isBillable)
                .Body("bill_by", billBy)
                .Body("code", code)
                .Body("is_fixed_fee", isFixedFee)
                .Body("hourly_rate", hourlyRate)
                .Body("budget", budget)
                .Body("budget_by", budgetBy)
                .Body("budget_is_monthly", budgetIsMonthly)
                .Body("notify_when_over_budget", notifyWhenOverBudget)
                .Body("over_budget_notification_percentage", overBudgetNotificationPercentage)
                .Body("show_budget_to_all", showBudgetToAll)
                .Body("cost_budget", costBudget)
                .Body("cost_budget_include_expenses", costBudgetIncludeExpenses)
                .Body("fee", fee)
                .Body("notes", notes)
                .Body("starts_on", startsOn)
                .Body("ends_on", endsOn)
                .SendAsync<Project>(_httpClient, cancellationToken);
        }

        public async Task<Project> UpdateProjectAsync(long projectId, long? clientId = null, string name = null, bool? isBillable = null,
            string billBy = "none", string code = null, bool? isFixedFee = null, decimal? hourlyRate = null, decimal? budget = null,
            string budgetBy = "none", bool? budgetIsMonthly = null, bool? notifyWhenOverBudget = null, bool? overBudgetNotificationPercentage = null,
            bool? showBudgetToAll = null, decimal? costBudget = null, bool? costBudgetIncludeExpenses = null,
            decimal? fee = null, string notes = null, DateTime? startsOn = null, DateTime? endsOn = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/projects/{projectId}", accountId, RequestBuilder.PatchMethod)
                .Body("client_id", clientId)
                .Body("name", name)
                .Body("is_billable", isBillable)
                .Body("bill_by", billBy)
                .Body("code", code)
                .Body("is_fixed_fee", isFixedFee)
                .Body("hourly_rate", hourlyRate)
                .Body("budget", budget)
                .Body("budget_by", budgetBy)
                .Body("budget_is_monthly", budgetIsMonthly)
                .Body("notify_when_over_budget", notifyWhenOverBudget)
                .Body("over_budget_notification_percentage", overBudgetNotificationPercentage)
                .Body("show_budget_to_all", showBudgetToAll)
                .Body("cost_budget", costBudget)
                .Body("cost_budget_include_expenses", costBudgetIncludeExpenses)
                .Body("fee", fee)
                .Body("notes", notes)
                .Body("starts_on", startsOn)
                .Body("ends_on", endsOn)
                .SendAsync<Project>(_httpClient, cancellationToken);
        }

        public async ThreadingTask DeleteProjectAsync(long projectId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            await SimpleRequestBuilder($"{harvestApiUrl}/projects/{projectId}", accountId, HttpMethod.Delete)
                .SendAsync(_httpClient, cancellationToken);
        }

        public async Task<TasksResponse> GetTasksAsync(DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/tasks", accountId)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<TasksResponse>(_httpClient, cancellationToken);
        }

        public async Task<Task> GetTasksAsync(long taskId,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/tasks/{taskId}", accountId)
                .SendAsync<Task>(_httpClient, cancellationToken);
        }

        public async Task<Task> CreateTaskAsync(string name, bool? billableByDefault, decimal? defaultHourlyRate,
            bool? isDefault, bool? isActive, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/tasks", accountId, HttpMethod.Post)
                .Body("name", name)
                .Body("billable_by_default", billableByDefault)
                .Body("default_hourly_rate", defaultHourlyRate)
                .Body("is_default", isDefault)
                .Body("isActive", isActive)
                .SendAsync<Task>(_httpClient, cancellationToken);
        }

        public async Task<Task> UpdateTaskAsync(long taskId, string name = null, bool? billableByDefault = null, decimal? defaultHourlyRate = null,
            bool? isDefault = null, bool? isActive = null, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/tasks/{taskId}", accountId, RequestBuilder.PatchMethod)
                .Body("name", name)
                .Body("billable_by_default", billableByDefault)
                .Body("default_hourly_rate", defaultHourlyRate)
                .Body("is_default", isDefault)
                .Body("isActive", isActive)
                .SendAsync<Task>(_httpClient, cancellationToken);
        }

        public async ThreadingTask DeleteTaskAsync(long taskId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            await SimpleRequestBuilder($"{harvestApiUrl}/tasks/{taskId}", accountId, HttpMethod.Delete)
                .SendAsync(_httpClient, cancellationToken);
        }

        public async Task<Company> GetCompanyAsync(long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/company", accountId)
                .SendAsync<Company>(_httpClient, cancellationToken);
        }

        public async Task<UserRatesResponse> GetUserCostRatesAsync(long userId, DateTime? updatedSince = null, int? page = null, int? perPage = null, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/users/{userId}/cost_rates", accountId)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<UserRatesResponse>(_httpClient, cancellationToken);
        }

        public async Task<UserRate> GetUserCostRateAsync(long userId, long costRateId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/users/{userId}/cost_rates/{costRateId}", accountId)
                .SendAsync<UserRate>(_httpClient, cancellationToken);
        }

        public async Task<UserRate> CreateUserCostRateAsync(long userId, decimal amount, DateTime? startDate,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/users/{userId}/cost_rates", accountId, HttpMethod.Post)
                .Body("amount", amount)
                .Body("start_date", startDate)
                .SendAsync<UserRate>(_httpClient, cancellationToken);
        }

        public async Task<UserRatesResponse> GetUserBillableRatesAsync(long userId, DateTime? updatedSince = null, int? page = null, int? perPage = null, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/users/{userId}/billable_rates", accountId)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<UserRatesResponse>(_httpClient, cancellationToken);
        }

        public async Task<UserRate> GetUserBillableRateAsync(long userId, long costRateId, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/users/{userId}/billable_rates/{costRateId}", accountId)
                .SendAsync<UserRate>(_httpClient, cancellationToken);
        }

        public async Task<UserRate> CreateUserBillableRateAsync(long userId, decimal amount, DateTime? startDate,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/users/{userId}/billable_rates", accountId, HttpMethod.Post)
                .Body("amount", amount)
                .Body("start_date", startDate)
                .SendAsync<UserRate>(_httpClient, cancellationToken);
        }

        public async Task<RolesResponse> GetRolesAsync(DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/roles", accountId)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<RolesResponse>(_httpClient, cancellationToken);
        }

        public async Task<Role> GetRoleAsync(long roleId,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/roles/{roleId}", accountId)
                .SendAsync<Role>(_httpClient, cancellationToken);
        }

        public async Task<Role> CreateRoleAsync(string name, long[] userIds = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/roles", accountId, HttpMethod.Post)
                .UseJson()
                .Body("name", name)
                .Body("user_ids", userIds)
                .SendAsync<Role>(_httpClient, cancellationToken);
        }

        public async Task<Role> UpdateRoleAsync(long roleId, string name = null, long[] userIds = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/roles/{roleId}", accountId, RequestBuilder.PatchMethod)
                .UseJson()
                .Body("name", name)
                .Body("user_ids", userIds)
                .SendAsync<Role>(_httpClient, cancellationToken);
        }

        public async ThreadingTask DeleteRoleAsync(long roleId,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            await SimpleRequestBuilder($"{harvestApiUrl}/roles/{roleId}", accountId, HttpMethod.Delete)
                .SendAsync(_httpClient, cancellationToken);
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
                .QueryPageSince(updatedSince, page, perPage)
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
                .UseJson()
                .Body("first_name", firstName)
                .Body("last_name", lastName)
                .Body("email", email)
                .Body("telephone", telephone)
                .Body("timezone", timezone)
                .Body("has_access_to_all_future_projects", hasAccessToAllFutureProjects)
                .Body("is_contractor", isContractor)
                .Body("is_admin", isAdmin)
                .Body("is_project_manager", isProjectManager)
                .Body("can_see_rates", canSeeRates)
                .Body("can_create_projects", canCreateProjects)
                .Body("can_create_invoices", canCreateInvoices)
                .Body("is_active", isActive)
                .Body("weekly_capacity", weeklyCapacity)
                .Body("default_hourly_rate", defaultHourlyRate)
                .Body("cost_rate", costRate)
                .Body("roles", roles)
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
                .UseJson()
                .Body("first_name", firstName)
                .Body("last_name", lastName)
                .Body("email", email)
                .Body("telephone", telephone)
                .Body("timezone", timezone)
                .Body("has_access_to_all_future_projects", hasAccessToAllFutureProjects)
                .Body("is_contractor", isContractor)
                .Body("is_admin", isAdmin)
                .Body("is_project_manager", isProjectManager)
                .Body("can_see_rates", canSeeRates)
                .Body("can_create_projects", canCreateProjects)
                .Body("can_create_invoices", canCreateInvoices)
                .Body("is_active", isActive)
                .Body("weekly_capacity", weeklyCapacity)
                .Body("default_hourly_rate", defaultHourlyRate)
                .Body("cost_rate", costRate)
                .Body("roles", roles)
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

        public async Task<InvoicesResponse> GetInvoicesAsync(long? clientId = null, long? projectId = null, InvoiceState? state = null, DateTime? from = null, DateTime? to = null, 
            DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/invoices", accountId)
                .QueryPageSince(updatedSince, page, perPage)
                .Query("client_id", clientId)
                .Query("project_id", projectId)
                .Query("state", state?.ToString())
                .Query("from", from)
                .Query("to", to)
                .SendAsync<InvoicesResponse>(_httpClient, cancellationToken);
        }

        public async Task<Invoice> GetInvoiceAsync(string invoiceId, long? clientId = null, long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/invoices/{invoiceId}", accountId)
                .Query("client_id", clientId)
                .SendAsync<Invoice>(_httpClient, cancellationToken);
        }

        public async Task<InvoiceItemCategoriesReponse> GetInvoiceItemCategories(DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/invoice_item_categories", accountId)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<InvoiceItemCategoriesReponse>(_httpClient, cancellationToken);
        }

        public async Task<InvoiceItemCategory> GetInvoiceItemCategory(long invoiceItemCategoryId,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/invoice_item_categories/{invoiceItemCategoryId}", accountId)
                .SendAsync<InvoiceItemCategory>(_httpClient, cancellationToken);
        }

        public async Task<InvoiceItemCategory> CreateInvoiceItemCategory(string name,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/invoice_item_categories", accountId, HttpMethod.Post)
                .Body("name", name)
                .SendAsync<InvoiceItemCategory>(_httpClient, cancellationToken);
        }

        public async Task<InvoiceItemCategory> UpdateInvoiceItemCategory(long invoiceItemCategoryId, string name,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/invoice_item_categories/{invoiceItemCategoryId}", accountId, RequestBuilder.PatchMethod)
                .Body("name", name)
                .SendAsync<InvoiceItemCategory>(_httpClient, cancellationToken);
        }

        public async ThreadingTask DeleteInvoiceItemCategory(long invoiceItemCategoryId,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            await SimpleRequestBuilder($"{harvestApiUrl}/invoice_item_categories/{invoiceItemCategoryId}", accountId, HttpMethod.Delete)
                .SendAsync(_httpClient, cancellationToken);
        }


        public async Task<InvoicePaymentsReponse> GetInvoicePayments(long invoiceId,
            DateTime? updatedSince = null, int? page = null, int? perPage = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/invoices/{invoiceId}/payments", accountId)
                .QueryPageSince(updatedSince, page, perPage)
                .SendAsync<InvoicePaymentsReponse>(_httpClient, cancellationToken);
        }

        public async Task<InvoiceItemCategory> CreateInvoicePayment(long invoiceId,
            decimal amount, DateTime? paidAt = null, DateTime? paidDate = null, string notes = null,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            return await SimpleRequestBuilder($"{harvestApiUrl}/invoices/{invoiceId}/payments", accountId, HttpMethod.Post)
                .Body("amount", amount)
                .Body("paid_at", paidAt)
                .Body("paid_date", paidDate)
                .Body("notes", notes)
                .SendAsync<InvoiceItemCategory>(_httpClient, cancellationToken);
        }

        public async ThreadingTask DeleteInvoicePayment(long invoiceId, long paymentId,
            long? accountId = null, CancellationToken cancellationToken = default)
        {
            await RefreshTokenIsNeeded();
            await SimpleRequestBuilder($"{harvestApiUrl}/invoices/{invoiceId}/payments/{paymentId}", accountId, HttpMethod.Delete)
                .SendAsync(_httpClient, cancellationToken);
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

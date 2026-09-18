using System.Net.Http.Headers;
using System.Net.Http.Json;
using Core.Application.DTOs;
using Core.Domain.Entities.Identity;

namespace BasculaBotTests
{
    /// <summary>One of the six persistent BOT* test users — pure role defaults, no permission or
    /// terminal-mode overrides (design.md "Test-user templates" table).</summary>
    public sealed record BotUserTemplate(string UserCode, string Username, Role Role);

    public static class BotUserTemplates
    {
        public static readonly IReadOnlyList<BotUserTemplate> All = new[]
        {
            new BotUserTemplate("BOTOPERATOR", "BOTOPERATOR", Role.Operator),
            new BotUserTemplate("BOTDISPATCH", "BOTDISPATCH", Role.DispatchingOperator),
            new BotUserTemplate("BOTSUPERVISOR", "BOTSUPERVISOR", Role.Supervisor),
            new BotUserTemplate("BOTADMIN", "BOTADMIN", Role.Admin),
            new BotUserTemplate("BOTSUDO", "BOTSUDO", Role.Sudo),
            new BotUserTemplate("BOTCUSTSVC", "BOTCUSTSVC", Role.CustomerService),
        };

        public static BotUserTemplate ForRole(Role role) =>
            All.FirstOrDefault(t => t.Role == role)
                ?? throw new ArgumentOutOfRangeException(nameof(role), role, "No BOT* template defined for this role.");
    }

    /// <summary>
    /// Reads the bot suite's bootstrap/role credentials from VM environment variables and
    /// idempotently provisions the six BOT* template users via the real Admin/Sudo API
    /// (design.md "Provisioning algorithm"). Never seeds via migration/DB — API-driven only, and
    /// existing users are corrected in place rather than deleted/recreated.
    /// </summary>
    public sealed class BotUserProvisioner
    {
        public const string AdminIdentifierVar = "BasculaBotAdminIdentifier";
        public const string AdminPasswordVar = "BasculaBotAdminPassword";
        public const string RolePasswordVar = "BasculaBotRolePassword";

        private readonly HttpClient _http;

        /// <summary>The single shared password used to log in as any of the six BOT* users
        /// (design.md: one variable is simpler than six, acceptable since these are non-production,
        /// role-default, suite-only accounts).</summary>
        public string RolePassword { get; }

        private BotUserProvisioner(HttpClient http, string rolePassword)
        {
            _http = http;
            RolePassword = rolePassword;
        }

        private static string RequireEnv(string name) =>
            Environment.GetEnvironmentVariable(name)
                ?? throw new InvalidOperationException(
                    $"{name} is not set. Run through scripts/vm/run-bot-suite.ps1 with the bot " +
                    "bootstrap/role credentials configured (see scripts/vm/README.md).");

        /// <summary>
        /// Logs in as the bootstrap Admin/Sudo account. Reads and validates every required
        /// environment variable — and fails fast, before any HTTP call — if one is unset (spec:
        /// "Missing bootstrap credentials fail fast").
        /// </summary>
        public static async Task<BotUserProvisioner> ConnectAsync(CancellationToken ct = default)
        {
            string adminIdentifier = RequireEnv(AdminIdentifierVar);
            string adminPassword = RequireEnv(AdminPasswordVar);
            string rolePassword = RequireEnv(RolePasswordVar);

            string apiUrl = Environment.GetEnvironmentVariable("BASCULA_API_URL")
                ?? throw new InvalidOperationException(
                    "BASCULA_API_URL is not set. Run through scripts/vm/run-bot-suite.ps1, which " +
                    "starts a live API by default — the roleplays need one.");

            var http = new HttpClient { BaseAddress = new Uri(apiUrl) };

            HttpResponseMessage loginResponse = await http.PostAsJsonAsync(
                "api/Auth/Login", new LoginRequest(adminIdentifier, adminPassword), ct);
            loginResponse.EnsureSuccessStatusCode();

            LoginResponse? login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
            if (login is null)
                throw new InvalidOperationException("Bootstrap login returned an empty response.");

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

            return new BotUserProvisioner(http, rolePassword);
        }

        /// <summary>
        /// Ensures all six BOT* template users exist and match their template: create missing
        /// (POST), correct drifted (PUT), skip matching (no call) — design.md's provisioning
        /// algorithm, steps 2-5.
        /// </summary>
        public async Task ProvisionAsync(CancellationToken ct = default)
        {
            List<UserDto> existing = await _http.GetFromJsonAsync<List<UserDto>>("api/Users", ct)
                ?? new List<UserDto>();

            foreach (BotUserTemplate template in BotUserTemplates.All)
            {
                UserDto? match = existing.FirstOrDefault(u => u.UserCode == template.UserCode);

                if (match is null)
                {
                    var create = new CreateUserRequest(
                        Username: template.Username,
                        UserCode: template.UserCode,
                        Password: RolePassword,
                        Role: template.Role,
                        Name: "Bot",
                        LastName: template.Role.ToString());

                    HttpResponseMessage response = await _http.PostAsJsonAsync("api/Users", create, ct);
                    response.EnsureSuccessStatusCode();
                }
                else if (IsDrifted(match, template))
                {
                    var update = new UpdateUserRequest(
                        Username: null,
                        UserCode: null,
                        NewPassword: null,
                        Role: template.Role,
                        CanSelfAuthorizeGateOverride: null,
                        ResetCanSelfAuthorizeGateOverride: true,
                        CanCaptureWeightManuallyOverride: null,
                        ResetCanCaptureWeightManuallyOverride: true,
                        TerminalModeOverride: null,
                        ResetTerminalModeOverride: true);

                    HttpResponseMessage response = await _http.PutAsJsonAsync($"api/Users/{match.Id}", update, ct);
                    response.EnsureSuccessStatusCode();
                }
                // else: already matches its template — no mutating call (spec: "A matching user
                // triggers no mutation").
            }
        }

        private static bool IsDrifted(UserDto existing, BotUserTemplate template) =>
            existing.Role != template.Role
            || existing.CanSelfAuthorizeGateOverride is not null
            || existing.CanCaptureWeightManuallyOverride is not null
            || existing.TerminalModeOverride is not null;
    }

    /// <summary>xUnit collection fixture: provisions the six BOT* users once per test-run, before
    /// any roleplay, and shares the resolved role password with every test in the collection.</summary>
    public sealed class BotUserProvisioningFixture : IAsyncLifetime
    {
        public string RolePassword { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            BotUserProvisioner provisioner = await BotUserProvisioner.ConnectAsync();
            await provisioner.ProvisionAsync();
            RolePassword = provisioner.RolePassword;
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    [CollectionDefinition("BotSuite")]
    public sealed class BotSuiteCollection : ICollectionFixture<BotUserProvisioningFixture>
    {
    }
}

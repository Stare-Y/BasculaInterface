using System.Net.Http.Headers;
using BasculaTerminalTest.TestDoubles;
using Core.Application.DTOs;
using Core.Application.Security;
using Core.Application.Services;
using Core.Domain.Entities.Identity;
using Core.Domain.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace BasculaTerminalTest.Integration
{
    /// <summary>
    /// Boots the real <c>BasculaTerminalApi</c> host in-memory against a throwaway PostgreSQL
    /// container (real EF Core migrations run on startup, exactly as in production), with only two
    /// swaps: the serial-port <see cref="IBasculaService"/> becomes a <see cref="FakeBasculaService"/>,
    /// and a seeded <see cref="Role.Sudo"/> test user backs every client this factory hands out
    /// (issue #134 — every mutating endpoint now requires authentication by default).
    ///
    /// The ContpaqiSQL side keeps its production registration but is never exercised by these
    /// tests — no code path here issues a Contpaqi query — so a dummy connection string is enough.
    ///
    /// Requires a Docker-compatible engine (Docker on CI, Podman locally with its socket enabled).
    /// </summary>
    public sealed class BasculaApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

        public FakeBasculaService Bascula { get; } = new();

        /// <summary>A valid JWT for the seeded Sudo user (see <see cref="TestData"/>) — Sudo
        /// bypasses every authorization/permission check, so this is the right default identity
        /// for tests that aren't specifically exercising auth. <see cref="CreateClient()"/>
        /// attaches it automatically; tests exercising the self-authorize gate or auth itself use
        /// <see cref="TestData.SudoUserCode"/>/<see cref="TestData.SudoPassword"/> directly.</summary>
        public string SudoToken { get; private set; } = string.Empty;

        async Task IAsyncLifetime.InitializeAsync()
        {
            await _postgres.StartAsync();

            // AddPersistency reads these straight from the environment and throws if either is
            // missing — set before the host builds on first CreateClient()/Services access.
            Environment.SetEnvironmentVariable("PostgresWeightConnection", _postgres.GetConnectionString());
            Environment.SetEnvironmentVariable("ContpaqSQLConnection",
                "Server=localhost,1433;Database=none;User Id=sa;Password=Unused_1234;TrustServerCertificate=true");

            // Triggers the host build (and its startup migrations) if it hasn't happened yet, then
            // seeds a Sudo user directly (production seeds none at all — design.md Decision 9 of
            // add-user-authentication-and-audit-log — so tests seed their own).
            using IServiceScope scope = Services.CreateScope();
            IUserRepo userRepo = scope.ServiceProvider.GetRequiredService<IUserRepo>();
            await userRepo.CreateAsync(new User
            {
                Username = TestData.SudoUsername,
                UserCode = TestData.SudoUserCode,
                PasswordHash = UserPasswordHasher.Hash(TestData.SudoPassword),
                Role = Role.Sudo,
            });

            IAuthService authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            LoginResponse? login = await authService.LoginAsync(TestData.SudoUserCode, TestData.SudoPassword);
            SudoToken = login?.Token ?? throw new InvalidOperationException("Failed to mint a test token for the seeded Sudo user.");
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _postgres.DisposeAsync();
            await base.DisposeAsync();
        }

        /// <summary>Every client this factory hands out is pre-authenticated as the seeded Sudo
        /// user, since almost every existing test predates per-user auth and isn't exercising it.
        /// Tests that need an unauthenticated request build their own <see cref="HttpClient"/> via
        /// <see cref="WebApplicationFactory{TEntryPoint}.CreateClient()"/>'s base behavior is not
        /// reachable from here on purpose — see <see cref="CreateUnauthenticatedClient"/>.</summary>
        public new HttpClient CreateClient()
        {
            HttpClient client = base.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SudoToken);
            return client;
        }

        /// <summary>An anonymous client, for tests that specifically exercise the fallback
        /// authorization policy or the exempt GET/websocket surface.</summary>
        public HttpClient CreateUnauthenticatedClient() => base.CreateClient();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBasculaService>();
                services.AddSingleton<IBasculaService>(Bascula);

                // Swapping every ContpaqiSQLContext-backed repo keeps that context (which hits a
                // real SQL Server from its constructor) from ever being built.
                services.RemoveAll<IProductRepo>();
                services.AddScoped<IProductRepo, FakeProductRepo>();
                services.RemoveAll<IClienteProveedorRepo>();
                services.AddScoped<IClienteProveedorRepo, FakeClienteProveedorRepo>();
                services.RemoveAll<IDocumentRepo>();
                services.AddScoped<IDocumentRepo, FakeDocumentRepo>();

                // No AuthSettings override needed: Program.cs's JWT bearer validation and
                // AuthService's token issuance both read the same appsettings.json AuthSettings
                // section for this one test-host instance, so they agree on the signing key
                // without the test factory pinning anything.
            });
        }
    }

    [CollectionDefinition(Name)]
    public sealed class IntegrationCollection : ICollectionFixture<BasculaApiFactory>
    {
        public const string Name = "bascula-api";
    }
}

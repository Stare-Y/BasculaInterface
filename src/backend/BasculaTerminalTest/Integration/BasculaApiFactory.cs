using BasculaTerminalTest.TestDoubles;
using Core.Application.Services;
using Core.Application.Settings;
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
    /// and the manager-override password is pinned to a known value.
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

        async Task IAsyncLifetime.InitializeAsync()
        {
            await _postgres.StartAsync();

            // AddPersistency reads these straight from the environment and throws if either is
            // missing — set before the host builds on first CreateClient()/Services access.
            Environment.SetEnvironmentVariable("PostgresWeightConnection", _postgres.GetConnectionString());
            Environment.SetEnvironmentVariable("ContpaqSQLConnection",
                "Server=localhost,1433;Database=none;User Id=sa;Password=Unused_1234;TrustServerCertificate=true");
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _postgres.DisposeAsync();
            await base.DisposeAsync();
        }

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

                services.PostConfigure<WeightSettings>(o =>
                {
                    o.ChangeProductPasswordHash = TestData.PasswordHash; // sha256("password")
                });
            });
        }
    }

    [CollectionDefinition(Name)]
    public sealed class IntegrationCollection : ICollectionFixture<BasculaApiFactory>
    {
        public const string Name = "bascula-api";
    }
}

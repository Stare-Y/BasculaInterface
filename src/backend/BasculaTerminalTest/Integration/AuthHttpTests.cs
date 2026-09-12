using System.Net;
using System.Net.Http.Json;
using BasculaTerminalApi.Controllers;
using Core.Application.DTOs;
using Core.Application.Security;
using Core.Domain.Entities.Identity;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace BasculaTerminalTest.Integration
{
    /// <summary>
    /// <c>POST /api/Auth/Login</c> through the real HTTP surface (issue #134): identifier
    /// resolution (UserCode then Username), password verification, and the disabled-user case.
    /// </summary>
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class AuthHttpTests
    {
        private readonly BasculaApiFactory _factory;
        private readonly HttpClient _anonymousClient;

        public AuthHttpTests(BasculaApiFactory factory)
        {
            _factory = factory;
            _anonymousClient = factory.CreateUnauthenticatedClient();
        }

        private async Task<User> SeedUserAsync(string username, string userCode, string password, Role role = Role.Operator)
        {
            using IServiceScope scope = _factory.Services.CreateScope();
            IUserRepo userRepo = scope.ServiceProvider.GetRequiredService<IUserRepo>();
            return await userRepo.CreateAsync(new User
            {
                Username = username,
                UserCode = userCode,
                PasswordHash = UserPasswordHasher.Hash(password),
                Role = role,
            });
        }

        [Fact]
        public async Task Login_by_UserCode_succeeds_and_returns_a_token()
        {
            await SeedUserAsync("login-by-code", "LBC1", "s3cret");

            var resp = await _anonymousClient.PostAsJsonAsync("/api/Auth/Login", new LoginRequest("LBC1", "s3cret"));

            LoginResponse body = await resp.ReadAs<LoginResponse>();
            Assert.False(string.IsNullOrEmpty(body.Token));
            Assert.Equal("login-by-code", body.User.Username);
        }

        [Fact]
        public async Task Login_falls_back_to_Username_when_UserCode_does_not_match()
        {
            await SeedUserAsync("login-by-username", "LBU1", "s3cret");

            var resp = await _anonymousClient.PostAsJsonAsync("/api/Auth/Login", new LoginRequest("login-by-username", "s3cret"));

            LoginResponse body = await resp.ReadAs<LoginResponse>();
            Assert.Equal("LBU1", body.User.UserCode);
        }

        [Fact]
        public async Task Login_with_wrong_password_returns_401()
        {
            await SeedUserAsync("login-wrong-pw", "LWP1", "s3cret");

            var resp = await _anonymousClient.PostAsJsonAsync("/api/Auth/Login", new LoginRequest("LWP1", "not-the-password"));

            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task Login_with_unresolvable_identifier_returns_401()
        {
            var resp = await _anonymousClient.PostAsJsonAsync("/api/Auth/Login", new LoginRequest("nobody-at-all", "whatever"));

            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task A_disabled_user_cannot_log_in()
        {
            User user = await SeedUserAsync("disabled-user", "DIS1", "s3cret");

            using (IServiceScope scope = _factory.Services.CreateScope())
            {
                WeightDBContext db = scope.ServiceProvider.GetRequiredService<WeightDBContext>();
                Core.Domain.Entities.Identity.User tracked = await db.Users.FindAsync(user.Id) ?? throw new InvalidOperationException("Seeded user not found.");
                tracked.IsDeleted = true;
                await db.SaveChangesAsync();
            }

            var resp = await _anonymousClient.PostAsJsonAsync("/api/Auth/Login", new LoginRequest("DIS1", "s3cret"));

            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }
    }
}

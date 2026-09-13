using System.Net.Http.Json;
using Core.Application.DTOs;
using Core.Domain.Entities.Identity;

namespace BasculaTerminalTest.Integration
{
    /// <summary>
    /// Per-role default inactivity timeout applied through the real HTTP surface
    /// (fix-session-inactivity-timeout design.md Decision 3): <c>POST /api/Users</c> (Sudo/Admin
    /// only, per <see cref="BasculaTerminalApi.Controllers.UsersController"/>) seeds the role
    /// default when the request doesn't supply one, and an explicit value overrides it.
    /// </summary>
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class UsersControllerInactivityTimeoutHttpTests
    {
        private readonly BasculaApiFactory _factory;
        private readonly HttpClient _authenticatedClient;

        public UsersControllerInactivityTimeoutHttpTests(BasculaApiFactory factory)
        {
            _factory = factory;
            _authenticatedClient = factory.CreateClient(); // pre-authenticated as the seeded Sudo test user
        }

        [Theory]
        [InlineData(Role.Sudo, 2)]
        [InlineData(Role.Admin, 5)]
        [InlineData(Role.Supervisor, 5)]
        [InlineData(Role.Operator, 10)]
        [InlineData(Role.DispatchingOperator, 20)]
        public async Task Creating_a_user_without_an_explicit_value_persists_the_role_default(Role role, int expectedMinutes)
        {
            CreateUserRequest request = new($"user-{role}", $"CODE-{role}", "s3cretpw1", role);

            var resp = await _authenticatedClient.PostAsJsonAsync("/api/Users", request);

            UserDto body = await resp.ReadAs<UserDto>();
            Assert.Equal(expectedMinutes, body.InactivityTimeoutMinutes);
        }

        [Fact]
        public async Task Creating_a_user_with_an_explicit_value_overrides_the_role_default()
        {
            CreateUserRequest request = new("user-explicit-timeout", "CODEEXPT", "s3cretpw1", Role.Operator, InactivityTimeoutMinutes: 42);

            var resp = await _authenticatedClient.PostAsJsonAsync("/api/Users", request);

            UserDto body = await resp.ReadAs<UserDto>();
            Assert.Equal(42, body.InactivityTimeoutMinutes);
        }
    }
}

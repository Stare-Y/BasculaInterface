using System.Net;
using System.Net.Http.Json;
using Core.Application.DTOs;

namespace BasculaTerminalTest.Integration
{
    /// <summary>
    /// The fallback-authenticated policy (issue #134 / design.md Decision 3 of
    /// add-user-authentication-and-audit-log): every GET endpoint stays open; every mutating
    /// request requires a valid token by default, with no per-endpoint opt-in.
    /// </summary>
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class AuthorizationPolicyHttpTests
    {
        private readonly HttpClient _anonymousClient;

        public AuthorizationPolicyHttpTests(BasculaApiFactory factory)
        {
            _anonymousClient = factory.CreateUnauthenticatedClient();
        }

        [Fact]
        public async Task Anonymous_GET_Weight_Pending_succeeds()
        {
            var resp = await _anonymousClient.GetAsync("/api/Weight/Pending");

            await resp.EnsureOk();
        }

        [Fact]
        public async Task Anonymous_GET_Pedido_All_succeeds()
        {
            var resp = await _anonymousClient.GetAsync("/api/Pedido/All");

            await resp.EnsureOk();
        }

        [Fact]
        public async Task Anonymous_mutating_request_is_rejected_with_401()
        {
            var resp = await _anonymousClient.PostAsJsonAsync("/api/Weight", new WeightEntryDto());

            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }

        [Fact]
        public async Task Anonymous_request_to_UsersController_is_rejected()
        {
            var resp = await _anonymousClient.GetAsync("/api/Users");

            Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        }
    }
}

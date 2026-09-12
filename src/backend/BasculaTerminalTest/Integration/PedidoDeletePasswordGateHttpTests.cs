using System.Net;
using System.Net.Http.Json;
using BasculaTerminalApi.Controllers;

namespace BasculaTerminalTest.Integration
{
    /// <summary>
    /// The self-authorize gate extended to Pedido/PedidoLine deletion (issue #134, superseding
    /// issue #133 / extend-delete-password-gate), exercised through the real HTTP surface: an
    /// unresolvable/wrong credential must come back as <c>400</c> and leave the record untouched;
    /// the seeded Sudo user's credential must go through. PATCH .../{id}/Delete replaces the old
    /// unguarded DELETE ?id= routes, which must no longer resolve.
    /// </summary>
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class PedidoDeletePasswordGateHttpTests
    {
        private readonly BasculaApiFactory _factory;
        private readonly HttpClient _client;

        public PedidoDeletePasswordGateHttpTests(BasculaApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Deleting_a_pedido_with_an_unresolvable_credential_is_rejected_with_400()
        {
            (int pedidoId, _) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 10m);

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Pedido/{pedidoId}/Delete",
                new DeletePedidoRequest(GateIdentifier: "nobody", GatePassword: "wrong"));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            string body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("Credenciales inválidas o sin autorización.", body);
        }

        [Fact]
        public async Task Deleting_a_pedido_with_the_Sudo_credential_succeeds()
        {
            (int pedidoId, _) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 10m);

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Pedido/{pedidoId}/Delete",
                new DeletePedidoRequest(GateIdentifier: TestDoubles.TestData.SudoUserCode, GatePassword: TestDoubles.TestData.SudoPassword));

            await resp.EnsureOk();
        }

        [Fact]
        public async Task Deleting_a_missing_pedido_returns_404()
        {
            var resp = await _client.PatchAsJsonAsync(
                "/api/Pedido/999999/Delete",
                new DeletePedidoRequest(GateIdentifier: TestDoubles.TestData.SudoUserCode, GatePassword: TestDoubles.TestData.SudoPassword));

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Old_unguarded_pedido_delete_route_no_longer_resolves()
        {
            (int pedidoId, _) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 10m);

            var resp = await _client.DeleteAsync($"/api/Pedido?id={pedidoId}");

            Assert.True(
                resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
                $"Expected the old route to no longer resolve, got {(int)resp.StatusCode}");
        }

        [Fact]
        public async Task Deleting_a_pedido_line_with_an_unresolvable_credential_is_rejected_with_400()
        {
            (_, int lineId) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 10m);

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Pedido/Line/{lineId}/Delete",
                new DeletePedidoLineRequest(GateIdentifier: "nobody", GatePassword: "wrong"));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            string body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("Credenciales inválidas o sin autorización.", body);
        }

        [Fact]
        public async Task Deleting_a_pedido_line_with_the_Sudo_credential_succeeds()
        {
            (_, int lineId) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 10m);

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Pedido/Line/{lineId}/Delete",
                new DeletePedidoLineRequest(GateIdentifier: TestDoubles.TestData.SudoUserCode, GatePassword: TestDoubles.TestData.SudoPassword));

            await resp.EnsureOk();
        }

        [Fact]
        public async Task Deleting_a_missing_pedido_line_returns_404()
        {
            var resp = await _client.PatchAsJsonAsync(
                "/api/Pedido/Line/999999/Delete",
                new DeletePedidoLineRequest(GateIdentifier: TestDoubles.TestData.SudoUserCode, GatePassword: TestDoubles.TestData.SudoPassword));

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Old_unguarded_pedido_line_delete_route_no_longer_resolves()
        {
            (_, int lineId) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 10m);

            var resp = await _client.DeleteAsync($"/api/Pedido/Line?id={lineId}");

            Assert.True(
                resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
                $"Expected the old route to no longer resolve, got {(int)resp.StatusCode}");
        }
    }
}

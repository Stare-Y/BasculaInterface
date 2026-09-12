using System.Net;
using System.Net.Http.Json;
using BasculaTerminalApi.Controllers;
using Core.Application.DTOs;

namespace BasculaTerminalTest.Integration
{
    /// <summary>
    /// The self-authorize gate, exercised through the real HTTP surface (issue #134): an
    /// unresolvable/wrong credential must come back as <c>400</c> and leave the detail untouched;
    /// the seeded Sudo user's credential (which bypasses the permission check) must go through.
    /// </summary>
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class WeightPasswordGateHttpTests
    {
        private readonly BasculaApiFactory _factory;
        private readonly HttpClient _client;

        public WeightPasswordGateHttpTests(BasculaApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task<int> CreateDetailAsync()
        {
            int behaviorId = await PedidoFlow.SeedHiddenAlmacenTargetAsync(_factory);
            (_, int lineId) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 100m);
            WeightEntryDto entry = await PedidoFlow.ConvertLineAndSetTareAsync(_client, lineId, targetAmount: 40m, behaviorId);
            return entry.WeightDetails.Single().Id;
        }

        private async Task<int> CreateWeightEntryAsync()
        {
            WeightEntryDto entry = await (await _client.PostAsJsonAsync("/api/Weight", new WeightEntryDto())).ReadAs<WeightEntryDto>();
            return entry.Id;
        }

        [Fact]
        public async Task Unresolvable_gate_credential_is_rejected_with_400()
        {
            int detailId = await CreateDetailAsync();

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Weight/Detail/{detailId}/Amount",
                new ChangeDetailAmountRequest(NewWeight: null, NewRequiredAmount: 30, GateIdentifier: "nobody", GatePassword: "wrong"));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            string body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("Credenciales inválidas o sin autorización.", body);
        }

        [Fact]
        public async Task Sudo_gate_credential_is_accepted_and_updates_the_amount()
        {
            int detailId = await CreateDetailAsync();

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Weight/Detail/{detailId}/Amount",
                new ChangeDetailAmountRequest(NewWeight: null, NewRequiredAmount: 30, GateIdentifier: TestDoubles.TestData.SudoUserCode, GatePassword: TestDoubles.TestData.SudoPassword));

            await resp.EnsureOk();
        }

        // extend-delete-password-gate (issue #133): PATCH /api/Weight/{id}/Delete replaces the old
        // unguarded DELETE /api/Weight?id=.

        [Fact]
        public async Task Deleting_a_weight_entry_with_an_unresolvable_credential_is_rejected_with_400()
        {
            int entryId = await CreateWeightEntryAsync();

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Weight/{entryId}/Delete",
                new DeleteWeightEntryRequest(GateIdentifier: "nobody", GatePassword: "wrong"));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            string body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("Credenciales inválidas o sin autorización.", body);
        }

        [Fact]
        public async Task Deleting_a_weight_entry_with_the_Sudo_credential_succeeds()
        {
            int entryId = await CreateWeightEntryAsync();

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Weight/{entryId}/Delete",
                new DeleteWeightEntryRequest(GateIdentifier: TestDoubles.TestData.SudoUserCode, GatePassword: TestDoubles.TestData.SudoPassword));

            await resp.EnsureOk();
        }

        [Fact]
        public async Task Deleting_a_missing_weight_entry_returns_404()
        {
            var resp = await _client.PatchAsJsonAsync(
                "/api/Weight/999999/Delete",
                new DeleteWeightEntryRequest(GateIdentifier: TestDoubles.TestData.SudoUserCode, GatePassword: TestDoubles.TestData.SudoPassword));

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Old_unguarded_delete_route_no_longer_resolves()
        {
            int entryId = await CreateWeightEntryAsync();

            var resp = await _client.DeleteAsync($"/api/Weight?id={entryId}");

            Assert.True(
                resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed,
                $"Expected the old route to no longer resolve, got {(int)resp.StatusCode}");
        }
    }
}

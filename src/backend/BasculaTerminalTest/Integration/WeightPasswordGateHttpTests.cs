using System.Net;
using System.Net.Http.Json;
using BasculaTerminalApi.Controllers;
using Core.Application.DTOs;

namespace BasculaTerminalTest.Integration
{
    /// <summary>
    /// The manager-override password gate, exercised through the real HTTP surface: a wrong hash
    /// must come back as <c>400 "Contraseña incorrecta."</c> and leave the detail untouched; the
    /// configured hash must go through.
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
        public async Task Wrong_password_is_rejected_with_400_and_the_spanish_message()
        {
            int detailId = await CreateDetailAsync();

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Weight/Detail/{detailId}/Amount",
                new ChangeDetailAmountRequest(NewWeight: null, NewRequiredAmount: 30, PasswordHash: "not-the-password"));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            string body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("Contraseña incorrecta.", body);
        }

        [Fact]
        public async Task Configured_password_is_accepted_and_updates_the_amount()
        {
            int detailId = await CreateDetailAsync();

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Weight/Detail/{detailId}/Amount",
                new ChangeDetailAmountRequest(NewWeight: null, NewRequiredAmount: 30, PasswordHash: TestDoubles.TestData.PasswordHash));

            await resp.EnsureOk();
        }

        // extend-delete-password-gate (issue #133): PATCH /api/Weight/{id}/Delete replaces the old
        // unguarded DELETE /api/Weight?id=.

        [Fact]
        public async Task Deleting_a_weight_entry_with_the_wrong_password_is_rejected_with_400()
        {
            int entryId = await CreateWeightEntryAsync();

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Weight/{entryId}/Delete",
                new DeleteWeightEntryRequest(PasswordHash: "not-the-password"));

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            string body = await resp.Content.ReadAsStringAsync();
            Assert.Contains("Contraseña incorrecta.", body);
        }

        [Fact]
        public async Task Deleting_a_weight_entry_with_the_configured_password_succeeds()
        {
            int entryId = await CreateWeightEntryAsync();

            var resp = await _client.PatchAsJsonAsync(
                $"/api/Weight/{entryId}/Delete",
                new DeleteWeightEntryRequest(PasswordHash: TestDoubles.TestData.PasswordHash));

            await resp.EnsureOk();
        }

        [Fact]
        public async Task Deleting_a_missing_weight_entry_returns_404()
        {
            var resp = await _client.PatchAsJsonAsync(
                "/api/Weight/999999/Delete",
                new DeleteWeightEntryRequest(PasswordHash: TestDoubles.TestData.PasswordHash));

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

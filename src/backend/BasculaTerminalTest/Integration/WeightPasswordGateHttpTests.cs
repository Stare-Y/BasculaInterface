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
    }
}

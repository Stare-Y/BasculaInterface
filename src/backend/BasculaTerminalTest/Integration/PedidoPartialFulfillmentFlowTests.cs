using System.Net.Http.Json;
using BasculaTerminalApi.Controllers;
using Core.Application.DTOs;

namespace BasculaTerminalTest.Integration
{
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class PedidoPartialFulfillmentFlowTests
    {
        private readonly BasculaApiFactory _factory;
        private readonly HttpClient _client;

        public PedidoPartialFulfillmentFlowTests(BasculaApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Converting_a_line_creates_a_discharge_entry_against_the_provider_and_target()
        {
            int behaviorId = await PedidoFlow.SeedHiddenAlmacenTargetAsync(_factory);
            (_, int lineId) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 100m);

            var resp = await _client.PostAsJsonAsync(
                $"/api/Pedido/Line/{lineId}/ConvertToWeight",
                new ConvertLineToWeightRequest(null, 40m, behaviorId.ToString()));
            WeightEntryDto entry = await resp.ReadAs<WeightEntryDto>();

            Assert.True(entry.IsDischarge);
            Assert.Equal(behaviorId, entry.ExternalTargetBehaviorFK);
            WeightDetailDto detail = Assert.Single(entry.WeightDetails);
            Assert.Equal(40.0, detail.RequiredAmount);
        }

        [Fact]
        public async Task Recording_a_partial_weight_reduces_the_line_pending_amount_without_concluding_it()
        {
            int behaviorId = await PedidoFlow.SeedHiddenAlmacenTargetAsync(_factory);
            (int pedidoId, int lineId) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 100m);
            WeightEntryDto entry = await PedidoFlow.ConvertLineAndSetTareAsync(_client, lineId, targetAmount: 40m, behaviorId);
            int detailId = entry.WeightDetails.Single().Id;

            var weighResp = await _client.PutAsJsonAsync(
                $"/api/Weight/Detail/{detailId}/Weight",
                new RecordWeightRequest(40, "integration-test"));
            await weighResp.EnsureOk();

            PedidoDto reloaded = await (await _client.GetAsync($"/api/Pedido/{pedidoId}")).ReadAs<PedidoDto>();
            PedidoLineDto line = reloaded.Lines.Single();

            Assert.Equal(40m, line.ReceivedAmount);
            Assert.Equal(60m, line.PendingAmount);
            Assert.False(line.Concluded);
        }

        [Fact]
        public async Task Converting_more_than_the_pending_amount_is_rejected_with_400()
        {
            int behaviorId = await PedidoFlow.SeedHiddenAlmacenTargetAsync(_factory);
            (_, int lineId) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 50m);

            var resp = await _client.PostAsJsonAsync(
                $"/api/Pedido/Line/{lineId}/ConvertToWeight",
                new ConvertLineToWeightRequest(null, 80m, behaviorId.ToString()));

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, resp.StatusCode);
        }
    }
}

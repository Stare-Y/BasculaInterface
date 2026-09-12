using System.Net;
using System.Net.Http.Json;
using BasculaTerminalApi.Controllers;
using Core.Application.DTOs;

namespace BasculaTerminalTest.Integration
{
    /// <summary>
    /// <c>GET /api/Weight/{id}/Radiography</c> (issue #134 / design.md Decision 12 of
    /// add-user-authentication-and-audit-log): the entry plus every detail — including
    /// logically-deleted ones — and the audit trail recorded against it.
    /// </summary>
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public class AuditRadiographyHttpTests
    {
        private readonly BasculaApiFactory _factory;
        private readonly HttpClient _client;

        public AuditRadiographyHttpTests(BasculaApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Radiography_includes_a_soft_deleted_detail_and_its_audit_entry()
        {
            int behaviorId = await PedidoFlow.SeedHiddenAlmacenTargetAsync(_factory);
            (_, int lineId) = await PedidoFlow.CreatePedidoWithLineAsync(_client, requiredAmount: 40m);
            WeightEntryDto entry = await PedidoFlow.ConvertLineAndSetTareAsync(_client, lineId, targetAmount: 40m, behaviorId);
            int detailId = entry.WeightDetails.Single().Id;

            await (await _client.PatchAsJsonAsync(
                $"/api/Weight/Detail/{detailId}/Delete",
                new DeleteDetailRequest(GateIdentifier: TestDoubles.TestData.SudoUserCode, GatePassword: TestDoubles.TestData.SudoPassword))).EnsureOk();

            WeightEntryRadiographyDto radiography = await (await _client.GetAsync($"/api/Weight/{entry.Id}/Radiography")).ReadAs<WeightEntryRadiographyDto>();

            Assert.Contains(radiography.Details, d => d.Id == detailId);
            Assert.Contains(radiography.AuditLog, a => a.Action == "WeightDetail.DeleteSafely" && a.EntityId == detailId);
        }

        [Fact]
        public async Task Radiography_for_a_nonexistent_entry_returns_404()
        {
            var resp = await _client.GetAsync("/api/Weight/999999/Radiography");

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Radiography_includes_an_audit_entry_after_a_delete()
        {
            var createResp = await _client.PostAsJsonAsync("/api/Weight", new WeightEntryDto());
            WeightEntryDto entry = await createResp.ReadAs<WeightEntryDto>();

            await (await _client.PatchAsJsonAsync(
                $"/api/Weight/{entry.Id}/Delete",
                new DeleteWeightEntryRequest(GateIdentifier: TestDoubles.TestData.SudoUserCode, GatePassword: TestDoubles.TestData.SudoPassword))).EnsureOk();

            WeightEntryRadiographyDto radiography = await (await _client.GetAsync($"/api/Weight/{entry.Id}/Radiography")).ReadAs<WeightEntryRadiographyDto>();

            Assert.Equal(entry.Id, radiography.WeightEntry.Id);
            Assert.Contains(radiography.AuditLog, a => a.Action == "WeightEntry.DeleteSafely" && a.EntityId == entry.Id);
        }
    }
}

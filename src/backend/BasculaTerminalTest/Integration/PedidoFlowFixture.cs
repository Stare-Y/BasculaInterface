using System.Net.Http.Json;
using BasculaTerminalApi.Controllers;
using Core.Application.DTOs;
using Core.Domain.Entities.Behaviors;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace BasculaTerminalTest.Integration
{
    /// <summary>
    /// Shared setup steps for the pedido → convert → weigh flow, so each test states only the
    /// part it actually asserts on.
    /// </summary>
    internal static class PedidoFlow
    {
        public static async Task<int> SeedHiddenAlmacenTargetAsync(BasculaApiFactory factory)
        {
            using IServiceScope scope = factory.Services.CreateScope();
            WeightDBContext db = scope.ServiceProvider.GetRequiredService<WeightDBContext>();

            var behavior = new ExternalTargetBehavior
            {
                Hidden = true,
                TargetSerie = "A",
                TargetConcept = "C",
                TargetAlmacen = "01",
                AlmacenName = "Bodega Test",
            };
            db.ExternalTargetBehaviors.Add(behavior);
            await db.SaveChangesAsync();
            return behavior.Id;
        }

        public static async Task<(int PedidoId, int LineId)> CreatePedidoWithLineAsync(
            HttpClient client, decimal requiredAmount)
        {
            var resp = await client.PostAsJsonAsync("/api/Pedido", new PedidoDto
            {
                ProviderId = 1,
                ExpectedArrival = DateTime.UtcNow.AddDays(3),
                Lines = { new PedidoLineDto { ProductId = 1, RequiredAmount = requiredAmount } },
            });

            PedidoDto pedido = await resp.ReadAs<PedidoDto>();
            return (pedido.Id, pedido.Lines.Single().Id);
        }

        /// <summary>Converts part of a line to a fresh weight entry and gives that (discharge)
        /// entry a large tare so a reading can be recorded against it.</summary>
        public static async Task<WeightEntryDto> ConvertLineAndSetTareAsync(
            HttpClient client, int lineId, decimal targetAmount, int behaviorId)
        {
            var convertResp = await client.PostAsJsonAsync(
                $"/api/Pedido/Line/{lineId}/ConvertToWeight",
                new ConvertLineToWeightRequest(null, targetAmount, behaviorId.ToString()));
            WeightEntryDto entry = await convertResp.ReadAs<WeightEntryDto>();

            entry.TareWeight = 100_000;
            var tareResp = await client.PutAsJsonAsync("/api/Weight", entry);
            await tareResp.EnsureOk();

            return entry;
        }
    }
}

using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Behaviors;
using Core.Domain.Entities.ProviderOrders;
using Core.Domain.Entities.Weight;
using Core.Domain.Interfaces;

namespace Infrastructure.Service
{
    public class PedidoService : IPedidoService
    {
        private readonly IPedidoRepo _pedidoRepo;
        private readonly IPedidoLineRepo _pedidoLineRepo;
        private readonly IWeightRepo _weightRepo;
        private readonly IExternalTargetBehaviorRepo _externalTargetBehaviorRepo;

        public PedidoService(
            IPedidoRepo pedidoRepo,
            IPedidoLineRepo pedidoLineRepo,
            IWeightRepo weightRepo,
            IExternalTargetBehaviorRepo externalTargetBehaviorRepo)
        {
            _pedidoRepo = pedidoRepo;
            _pedidoLineRepo = pedidoLineRepo;
            _weightRepo = weightRepo;
            _externalTargetBehaviorRepo = externalTargetBehaviorRepo;
        }

        public async Task<PedidoDto> CreateAsync(PedidoDto dto)
        {
            Pedido entity = dto.ToEntity();
            Pedido created = await _pedidoRepo.CreateAsync(entity);
            // Reload so computed navigation/Line data is consistent with GetByIdAsync's shape.
            Pedido reloaded = await _pedidoRepo.GetByIdAsync(created.Id);
            return new PedidoDto(reloaded);
        }

        public async Task<PedidoDto> GetByIdAsync(int id)
        {
            return new PedidoDto(await _pedidoRepo.GetByIdAsync(id));
        }

        public async Task<IEnumerable<PedidoDto>> GetAllAsync(int top = 30, uint page = 1)
        {
            IEnumerable<Pedido> pedidos = await _pedidoRepo.GetAllAsync(top, page);
            return pedidos.Select(p => new PedidoDto(p));
        }

        public async Task<IEnumerable<PedidoDto>> GetByProviderIdAsync(int providerId, int top = 30, uint page = 1)
        {
            IEnumerable<Pedido> pedidos = await _pedidoRepo.GetByProviderIdAsync(providerId, top, page);
            return pedidos.Select(p => new PedidoDto(p));
        }

        public async Task UpdateAsync(PedidoDto dto)
        {
            await _pedidoRepo.UpdateAsync(dto.ToEntity());
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _pedidoRepo.DeleteAsync(id);
        }

        public async Task<PedidoLineDto> CreateLineAsync(PedidoLineDto dto)
        {
            PedidoLine created = await _pedidoLineRepo.CreateAsync(dto.ToEntity());
            PedidoLine reloaded = await _pedidoLineRepo.GetByIdAsync(created.Id);
            return new PedidoLineDto(reloaded);
        }

        public async Task UpdateLineAsync(PedidoLineDto dto)
        {
            await _pedidoLineRepo.UpdateAsync(dto.ToEntity());
        }

        public async Task<bool> DeleteLineAsync(int id)
        {
            return await _pedidoLineRepo.DeleteAsync(id);
        }

        public async Task CloseLineAsync(int lineId)
        {
            await _pedidoLineRepo.CloseAsync(lineId);
        }

        public async Task<WeightEntryDto> ConvertLineToWeightAsync(int lineId, int? weightEntryId, decimal? targetAmount, string? externalTarget)
        {
            PedidoLine line = await _pedidoLineRepo.GetByIdAsync(lineId);

            decimal received = line.WeightDetails
                .Where(d => d.IsLoaded && !d.IsDeleted)
                .Sum(d => (decimal)d.Weight);
            decimal pending = Math.Max(line.RequiredAmount - received, 0);

            if (line.ManuallyClosed || pending <= 0)
                throw new InvalidOperationException($"La línea de pedido #{line.Id} ya está concluida; no se puede convertir a peso.");

            decimal amount = targetAmount ?? pending;
            if (amount <= 0)
                throw new InvalidOperationException("El monto a convertir debe ser mayor que cero.");
            if (amount > pending)
                throw new InvalidOperationException($"El monto solicitado ({amount}) excede el pendiente de la línea ({pending}).");

            WeightDetail newDetail = new WeightDetail
            {
                FK_WeightedProductId = line.ProductId,
                RequiredAmount = (double)amount,
                ProductPrice = line.Price.HasValue ? (double)line.Price.Value : null,
                FK_PedidoLineId = line.Id
            };

            WeightEntry resultEntry;

            if (weightEntryId.HasValue)
            {
                // Appending to an open weight entry (e.g. the "Solo Pedidos" terminal adding
                // products to a truck already on the scale) — that entry's almacén target is
                // already fixed, so the picked almacén for this call is ignored here.
                WeightEntry existingEntry = await _weightRepo.GetByIdAsync(weightEntryId.Value);
                if (existingEntry.ConcludeDate != null)
                    throw new InvalidOperationException("No se pueden agregar productos a un proceso ya finalizado.");

                newDetail.FK_WeightEntryId = existingEntry.Id;
                await _weightRepo.CreateDetailAsync(newDetail);
                resultEntry = await _weightRepo.GetByIdAsync(existingEntry.Id);
            }
            else
            {
                Pedido pedido = await _pedidoRepo.GetByIdAsync(line.PedidoId);

                // The operator must pick an almacén target — one of the hidden
                // ExternalTargetBehavior rows — before a new weight entry is created
                // (design.md Decision 5). Its TargetAlmacen/Serie/Concept drive the ERP document.
                if (string.IsNullOrWhiteSpace(externalTarget) || !int.TryParse(externalTarget, out int behaviorId))
                    throw new InvalidOperationException("Debe seleccionar un almacén destino para la conversión.");

                ExternalTargetBehavior behavior = await _externalTargetBehaviorRepo.GetByIdAsync(behaviorId);
                if (!behavior.Hidden)
                    throw new InvalidOperationException("El destino seleccionado no es un almacén válido para pedidos.");

                WeightEntry newEntry = new WeightEntry
                {
                    PartnerId = pedido.ProviderId,
                    TareWeight = 0,
                    BruteWeight = 0,
                    // A pedido is always a provider delivery: the truck arrives loaded and
                    // discharges, so the running weight must count down (design.md Decision 7).
                    IsDischarge = true,
                    ExternalTargetBehaviorFK = behavior.Id,
                    WeightDetails = [newDetail]
                };

                resultEntry = await _weightRepo.CreateAsync(newEntry);
            }

            return new WeightEntryDto(resultEntry);
        }
    }
}

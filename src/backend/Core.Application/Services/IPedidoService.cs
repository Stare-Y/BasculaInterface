using Core.Application.DTOs;

namespace Core.Application.Services
{
    public interface IPedidoService
    {
        Task<PedidoDto> CreateAsync(PedidoDto dto);
        Task<PedidoDto> GetByIdAsync(int id);
        Task<IEnumerable<PedidoDto>> GetAllAsync(int top = 30, uint page = 1);
        Task<IEnumerable<PedidoDto>> GetByProviderIdAsync(int providerId, int top = 30, uint page = 1);
        Task UpdateAsync(PedidoDto dto);
        Task<bool> DeleteSafelyAsync(int id, GateCredential gateCredential);

        Task<PedidoLineDto> CreateLineAsync(PedidoLineDto dto);
        Task UpdateLineAsync(PedidoLineDto dto);
        Task<bool> DeleteLineSafelyAsync(int id, GateCredential gateCredential);

        /// <summary>Force-closes a line (design.md Decision 4) — accepts a short/cancelled shipment.</summary>
        Task CloseLineAsync(int lineId);

        /// <summary>
        /// Converts (all or part of) a pedido line's pending amount into a new WeightDetail,
        /// attached to an existing open WeightEntry when <paramref name="weightEntryId"/> is
        /// supplied, or a newly created one otherwise. Replaces the old
        /// ProviderPurchaseService.CreateWeightEntryAsync's throw-on-second-attempt behavior —
        /// this may be called repeatedly for the same line as long as pending &gt; 0.
        /// </summary>
        Task<WeightEntryDto> ConvertLineToWeightAsync(int lineId, int? weightEntryId, decimal? targetAmount, string? externalTarget);
    }
}

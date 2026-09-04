using Core.Domain.Entities.ProviderOrders;

namespace Core.Domain.Interfaces
{
    public interface IPedidoLineRepo
    {
        Task<PedidoLine> CreateAsync(PedidoLine line);
        Task<PedidoLine> GetByIdAsync(int id);
        Task UpdateAsync(PedidoLine line);
        Task<bool> DeleteAsync(int id);

        /// <summary>Sets ManuallyClosed=true (design.md Decision 4 — force-close for a short/cancelled shipment).</summary>
        Task CloseAsync(int id);
    }
}

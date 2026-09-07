using Core.Domain.Entities.ProviderOrders;

namespace Core.Domain.Interfaces
{
    public interface IPedidoRepo
    {
        Task<Pedido> CreateAsync(Pedido pedido);
        Task<Pedido> GetByIdAsync(int id);
        Task<IEnumerable<Pedido>> GetAllAsync(int top = 30, uint page = 1);
        Task<IEnumerable<Pedido>> GetByProviderIdAsync(int providerId, int top = 30, uint page = 1);
        Task UpdateAsync(Pedido pedido);
        Task<bool> DeleteAsync(int id);
    }
}

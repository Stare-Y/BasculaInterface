using Core.Domain.Entities.ProviderOrders;

namespace Core.Domain.Interfaces
{
    public interface IProviderPurchaseRepo
    {
        Task<ProviderPurchase> CreateAsync(ProviderPurchase providerPurchase);
        Task<ProviderPurchase> GetByIdAsync(int id);
        Task<ProviderPurchase?> GetByWeightEntryIdAsync(int weightEntryId);
        Task<IEnumerable<ProviderPurchase>> GetAllAsync(int top = 30, uint page = 1);
        Task<IEnumerable<ProviderPurchase>> GetByProviderIdAsync(int providerId, int top = 30, uint page = 1);
        Task UpdateAsync(ProviderPurchase providerPurchase);
        Task<bool> DeleteAsync(int id);
    }
}

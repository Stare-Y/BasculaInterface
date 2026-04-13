using Core.Application.DTOs;

namespace Core.Application.Services
{
    public interface IProviderPurchaseService
    {
        Task<ProviderPurchaseDto> CreateAsync(ProviderPurchaseDto dto);
        Task<ProviderPurchaseDto> GetByIdAsync(int id);
        Task<IEnumerable<ProviderPurchaseDto>> GetAllAsync(int top = 30, uint page = 1);
        Task<IEnumerable<ProviderPurchaseDto>> GetByProviderIdAsync(int providerId, int top = 30, uint page = 1);
        Task UpdateAsync(ProviderPurchaseDto dto);
        Task<bool> DeleteAsync(int id);
        Task<WeightEntryDto> CreateWeightEntryAsync(int purchaseId);
        Task ConcludeByWeightEntryAsync(int weightEntryId);
    }
}

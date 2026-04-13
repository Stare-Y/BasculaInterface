using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Interfaces;

namespace Infrastructure.Service
{
    public class ProviderPurchaseService : IProviderPurchaseService
    {
        private readonly IProviderPurchaseRepo _providerPurchaseRepo;

        public ProviderPurchaseService(IProviderPurchaseRepo providerPurchaseRepo)
        {
            _providerPurchaseRepo = providerPurchaseRepo;
        }

        public async Task<ProviderPurchaseDto> CreateAsync(ProviderPurchaseDto dto)
        {
            var entity = dto.ToEntity();
            var created = await _providerPurchaseRepo.CreateAsync(entity);
            return new ProviderPurchaseDto(created);
        }

        public async Task<ProviderPurchaseDto> GetByIdAsync(int id)
        {
            return new ProviderPurchaseDto(await _providerPurchaseRepo.GetByIdAsync(id));
        }

        public async Task<IEnumerable<ProviderPurchaseDto>> GetAllAsync(int top = 30, uint page = 1)
        {
            return (await _providerPurchaseRepo.GetAllAsync(top, page))
                .Select(pp => new ProviderPurchaseDto(pp));
        }

        public async Task<IEnumerable<ProviderPurchaseDto>> GetByProviderIdAsync(int providerId, int top = 30, uint page = 1)
        {
            return (await _providerPurchaseRepo.GetByProviderIdAsync(providerId, top, page))
                .Select(pp => new ProviderPurchaseDto(pp));
        }

        public async Task UpdateAsync(ProviderPurchaseDto dto)
        {
            var entity = dto.ToEntity();
            await _providerPurchaseRepo.UpdateAsync(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _providerPurchaseRepo.DeleteAsync(id);
        }
    }
}

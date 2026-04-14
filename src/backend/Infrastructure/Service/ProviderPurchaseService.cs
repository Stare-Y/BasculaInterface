using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Weight;
using Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Service
{
    public class ProviderPurchaseService : IProviderPurchaseService
    {
        private readonly IProviderPurchaseRepo _providerPurchaseRepo;
        private readonly IWeightRepo _weightRepo;
        private readonly IExternalTargetBehaviorRepo _externalTargetBehaviorRepo;
        private readonly ILogger<ProviderPurchaseService> _logger;

        public ProviderPurchaseService(IProviderPurchaseRepo providerPurchaseRepo, IWeightRepo weightRepo, IExternalTargetBehaviorRepo externalTargetBehaviorRepo, ILogger<ProviderPurchaseService> logger)
        {
            _providerPurchaseRepo = providerPurchaseRepo;
            _weightRepo = weightRepo;
            _externalTargetBehaviorRepo = externalTargetBehaviorRepo;
            _logger = logger;
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

        public async Task<WeightEntryDto> CreateWeightEntryAsync(int purchaseId, string externalTarget)
        {
            var purchase = await _providerPurchaseRepo.GetByIdAsync(purchaseId);

            if (purchase.WeightEntryId.HasValue)
                throw new InvalidOperationException($"El pedido #{purchase.Id} ya tiene una entrada de peso asignada (ID: {purchase.WeightEntryId}).");

            int? externalTargetBehaviorId = null;
            if (int.TryParse(externalTarget, out int behaviorId))
            {
                try
                {
                    var behavior = await _externalTargetBehaviorRepo.GetByIdAsync(behaviorId);
                    if (behavior.Hidden)
                        externalTargetBehaviorId = behavior.Id;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve ExternalTargetBehavior with ID {BehaviorId}", behaviorId);
                }
            }

            var weightEntry = new WeightEntry
            {
                PartnerId = purchase.ProviderId,
                TareWeight = 0,
                BruteWeight = 0,
                ExternalTargetBehaviorFK = externalTargetBehaviorId,
                WeightDetails =
                [
                    new WeightDetail
                    {
                        FK_WeightedProductId = purchase.ProductId,
                        RequiredAmount = (double)purchase.RequiredAmount
                    }
                ]
            };

            WeightEntry created = await _weightRepo.CreateAsync(weightEntry);

            purchase.WeightEntryId = created.Id;
            await _providerPurchaseRepo.UpdateAsync(purchase);

            return new WeightEntryDto(created);
        }

        public async Task<ProviderPurchaseDto?> GetByWeightEntryIdAsync(int weightEntryId)
        {
            var purchase = await _providerPurchaseRepo.GetByWeightEntryIdAsync(weightEntryId);
            return purchase is null ? null : new ProviderPurchaseDto(purchase);
        }

        public async Task ConcludeByWeightEntryAsync(int weightEntryId)
        {
            var purchase = await _providerPurchaseRepo.GetByWeightEntryIdAsync(weightEntryId);

            if (purchase is null || purchase.Concluded)
                return;

            purchase.Concluded = true;
            await _providerPurchaseRepo.UpdateAsync(purchase);
        }
    }
}

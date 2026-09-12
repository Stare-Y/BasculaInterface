using Core.Application.DTOs;
using Core.Application.DTOs.ContpaqiComercial;

namespace Core.Application.Services
{
    public interface IWeightService
    {
        Task<WeightEntryDto> CreateAsync(WeightEntryDto weightEntry);
        Task<WeightEntryDto> GetByIdAsync(int id);
        Task<IEnumerable<WeightEntryDto>> GetAllAsync(int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntryDto>> GetAllComplete(int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntryDto>> GetAllByPartnerAsync(int partnerId, int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntryDto>> GetByDateRange(DateOnly startDate, DateOnly endDate, int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntryDto>> GetPendingWeights(int top = 30, uint page = 1);
        Task UpdateAsync(WeightEntryDto weightEntry);
        Task<WeightDetailDto> CreateDetailAsync(WeightDetailDto detail);
        Task SetSecondaryTareAsync(int detailId, double tare);
        Task RecordWeightAsync(int detailId, double weight, string weightedBy);
        Task<WeightEntryDto> MarkDetailLoadedAsync(int detailId);
        Task ConcludeAsync(int weightEntryId);
        Task DeleteSafelyAsync(int id, GateCredential gateCredential);
        Task<bool> DeleteDetailAsync(int id);
        Task<GenericResponse<ContpaqiComercialResult>> SendToContpaqiComercial(int id);
        Task<CreditValidationResponse> ValidatePartnerCreditAsync(int partnerId, double requestedAmount);
        Task ChangeTargetDocumentBehavior(int weightId, int targetDocumentBehaviorId);
        Task ChangeDetailProductAsync(int detailId, int newProductId, GateCredential gateCredential);
        Task ChangePartnerAsync(int weightId, int newPartnerId, GateCredential gateCredential);
        Task ChangeDetailAmountAsync(int detailId, double? newWeight, double? newRequiredAmount, GateCredential gateCredential);
        Task DeleteDetailSafelyAsync(int detailId, GateCredential gateCredential);
    }
}

using Core.Domain.Entities.Weight;

namespace Core.Domain.Interfaces
{
    public interface IWeightRepo
    {
        Task<WeightEntry> CreateAsync(WeightEntry weightEntry);
        Task<WeightEntry> GetByIdAsync(int id);
        Task<WeightDetail> GetDetailByIdAsync(int detailId);
        Task<IEnumerable<WeightEntry>> GetAllAsync(int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntry>> GetAllComplete(int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntry>> GetByDateRange(DateOnly startDate, DateOnly endDate, int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntry>> GetAllByPartnerAsync(int partnerId, int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntry>> GetPendingWeights(int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntry>> GetPendingWeightsByPartnerAsync(int partnerId);
        Task UpdateAsync(WeightEntry weightEntry, bool force = false);
        Task<WeightDetail> CreateDetailAsync(WeightDetail detail);
        Task UpdateDetailAsync(WeightDetail detail);
        Task<WeightEntry> MarkDetailLoadedAsync(int detailId);
        Task RecomputeBruteWeightAsync(int entryId);
        Task ConcludeEntryAsync(int weightEntryId);
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteDetailAsync(int id);
    }
}

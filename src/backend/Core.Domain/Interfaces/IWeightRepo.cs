using Core.Domain.Entities.Weight;

namespace Core.Domain.Interfaces
{
    public interface IWeightRepo
    {
        Task<WeightEntry> CreateAsync(WeightEntry weightEntry);
        Task<WeightEntry> GetByIdAsync(int id);
        Task<IEnumerable<WeightEntry>> GetAllAsync(int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntry>> GetAllComplete(int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntry>> GetByDateRange(DateOnly startDate, DateOnly endDate, int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntry>> GetAllByPartnerAsync(int partnerId, int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntry>> GetPendingWeights(int top = 30, uint page = 1);
        Task UpdateAsync(WeightEntry weightEntry);
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteDetailAsync(int id);
    }
}

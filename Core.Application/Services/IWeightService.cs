using Core.Application.DTOs;

namespace Core.Application.Services
{
    public interface IWeightService
    {
        Task<WeightEntryDto> CreateAsync(WeightEntryDto weightEntry);
        Task<WeightEntryDto> GetByIdAsync(int id);
        Task<IEnumerable<WeightEntryDto>> GetAllAsync(int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntryDto>> GetPendingWeights(int top = 30, uint page = 1);
        Task UpdateAsync(WeightEntryDto weightEntry);
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteDetailAsync(int id);
    }
}

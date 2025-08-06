using Core.Application.DTOs;
using Core.Domain.Entities;

namespace Core.Domain.Interfaces
{
    public interface IWeightRepo
    {
        Task<WeightEntry> CreateAsync(WeightEntry weightEntry);
        Task<WeightEntry?> GetByIdAsync(int id);
        Task<IEnumerable<WeightEntry>> GetAllAsync(int top = 30, uint page = 1);
        Task<IEnumerable<WeightEntry>> GetPendingWeights(int top = 30, uint page = 1);
        Task UpdateAsync(WeightEntry weightEntry);
        Task<bool> DeleteAsync(int id);
    }
}

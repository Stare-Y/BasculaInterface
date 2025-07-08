using Core.Application.DTOs;
using Core.Domain.Entities;

namespace Core.Application.Interfaces
{
    public interface IWeightRepo
    {
        Task<WeightEntry> CreateAsync(WeightEntryDto weightEntry);
        Task<WeightEntry?> GetByIdAsync(int id);
        Task<IEnumerable<WeightEntry>> GetAllAsync(int top = 21, uint page = 1);
        Task UpdateAsync(WeightEntryDto weightEntry, int id);
        Task<bool> DeleteAsync(int id);
    }
}

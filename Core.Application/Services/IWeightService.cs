using Core.Application.DTOs;

namespace Core.Application.Services
{
    public interface IWeightService<T>
        where T : class
    {
        Task<T> Create(WeightEntryDto weightEntryDto);
        Task<T> GetById(int id);
        Task<T> GetAll(int top = 21, uint page = 1);
        Task<T> Update(WeightEntryDto weightEntryDto, int id);
        Task<T> Delete(int id);
    }
}

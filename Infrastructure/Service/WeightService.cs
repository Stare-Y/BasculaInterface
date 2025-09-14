using Core.Application.DTOs;
using Core.Application.Extensions;
using Core.Application.Services;
using Core.Domain.Entities.Weight;
using Core.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Service
{
    public class WeightService : IWeightService
    {
        private readonly IWeightRepo _weightRepo;
        public WeightService(IWeightRepo weightRepo)
        {
            _weightRepo = weightRepo;
        }
        public async Task<WeightEntryDto> CreateAsync(WeightEntryDto weightEntry)
        {
            WeightEntry newEntry = await _weightRepo.CreateAsync(weightEntry.ConvertToBaseEntry());

            return newEntry.ConvertToDto();
        }

        public async Task<WeightEntryDto> GetByIdAsync(int id)
        {
            WeightEntry entry = await _weightRepo.GetByIdAsync(id);

            return entry.ConvertToDto();
        }

        public async Task<IEnumerable<WeightEntryDto>> GetAllAsync(int top = 30, uint page = 1)
        {
            return WeightExtensions.ConvertRangeToDto(await _weightRepo.GetAllAsync(top, page));
        }

        public async Task<IEnumerable<WeightEntryDto>> GetPendingWeights(int top = 30, uint page = 1)
        {
            return WeightExtensions.ConvertRangeToDto(await _weightRepo.GetPendingWeights(top, page));
        }

        public async Task UpdateAsync(WeightEntryDto weightEntry)
        {
            await _weightRepo.UpdateAsync(weightEntry.ConvertToBaseEntry());
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _weightRepo.DeleteAsync(id);
        }

        public Task<bool> DeleteDetailAsync(int id)
        {
            return _weightRepo.DeleteDetailAsync(id);
        }
    }
}

using Core.Application.DTOs;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class WeightRepo : IWeightRepo
    {
        private readonly WeightDBContext _context;
        public WeightRepo(WeightDBContext context)
        {
            _context = context;
        }

        public async Task<WeightEntry> CreateAsync(WeightEntryDto weightEntryDto)
        {
            WeightEntry weightEntry = new WeightEntry().BuildFromDto(weightEntryDto);

            await _context.WeightEntries.AddAsync(weightEntry);
            await _context.SaveChangesAsync();

            return weightEntry;
        }

        public async Task<WeightEntry?> GetByIdAsync(int id)
        {
            return await _context.WeightEntries
                .Include(w => w.WeightDetails)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<IEnumerable<WeightEntry>> GetAllAsync(int top = 21, uint page = 1)
        {
            return await _context.WeightEntries
                .Include(w => w.WeightDetails)
                .OrderByDescending(w => w.ConcludeDate)
                .Skip(((int)page - 1))
                .Take(top)
                .ToListAsync();
        }

        public async Task UpdateAsync(WeightEntryDto weightEntryDto, int id)
        {
            WeightEntry? existingEntry = await GetByIdAsync(id);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"WeightEntry with ID {id} not found.");
            }
            
            existingEntry.UpdateFromDto(weightEntryDto);
            _context.WeightEntries.Update(existingEntry);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            WeightEntry? weightEntry = await _context.WeightEntries.FindAsync(id);
            if (weightEntry == null)
            {
                return false;
            }
            _context.WeightEntries.Remove(weightEntry);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

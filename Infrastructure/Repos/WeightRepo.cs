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

        public async Task<WeightEntry> CreateAsync(WeightEntry weightEntry)
        {
            await _context.WeightEntries.AddAsync(weightEntry);

            await _context.SaveChangesAsync();

            return weightEntry;
        }

        public async Task<WeightEntry?> GetByIdAsync(int id)
        {
            return await _context.WeightEntries
                .AsNoTracking()
                .Include(w => w.WeightDetails)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<IEnumerable<WeightEntry>> GetAllAsync(int top = 30, uint page = 1)
        {
            return await _context.WeightEntries
                .AsNoTracking()
                .Include(w => w.WeightDetails)
                .OrderByDescending(w => w.ConcludeDate)
                .Skip(((int)page - 1))
                .Take(top)
                .ToListAsync();
        }

        public async Task UpdateAsync(WeightEntry weightEntry)
        {
            if(weightEntry.Id <= 0)
            {
                throw new ArgumentException("WeightEntry ID must be a valid one.", nameof(weightEntry.Id));
            }
            WeightEntry? existingEntry = await GetByIdAsync(weightEntry.Id);
            if (existingEntry == null)
            {
                throw new KeyNotFoundException($"WeightEntry with ID {weightEntry.Id} not found.");
            }
            
            _context.WeightEntries.Update(weightEntry);

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

using Core.Domain.Entities.Weight;
using Core.Domain.Interfaces;
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

        public async Task<WeightEntry> GetByIdAsync(int id)
        {
            WeightEntry? entry = await _context.WeightEntries
                .AsNoTracking()
                .Include(w => w.WeightDetails
                .Where(wd => !wd.IsDeleted))
                .Include(wd => wd.ExternalTargetBehavior)
                .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

            if (entry == null)
            {
                throw new KeyNotFoundException($"WeightEntry with ID {id} not found.");
            }

            return entry;
        }

        public async Task<IEnumerable<WeightEntry>> GetByDateRange(DateOnly startDate, DateOnly endDate, int top = 30, uint page = 1)
        {
            DateTime startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
            DateTime endDateTime = endDate.ToDateTime(TimeOnly.MaxValue);

            return await _context.WeightEntries
                .AsNoTracking()
                .Include(w => w.ExternalTargetBehavior)
                .Include(w => w.WeightDetails)
                .OrderByDescending(w => w.ConcludeDate)
                .Where(w =>
                    w.CreatedAt > startDateTime &&
                    w.CreatedAt < endDateTime &&
                    !w.IsDeleted
                    )
                .Skip((int)page - 1)
                .Take(top)
                .ToListAsync();
        }

        public async Task<IEnumerable<WeightEntry>> GetAllAsync(int top = 30, uint page = 1)
        {
            return await _context.WeightEntries
                .AsNoTracking()
                .Include(w => w.ExternalTargetBehavior)
                .Include(w => w.WeightDetails
                                .Where(wd => !wd.IsDeleted))
                .Where(w => !w.IsDeleted)
                .OrderByDescending(w => w.CreatedAt)
                .Skip((int)page - 1)
                .Take(top)
                .ToListAsync();
        }

        public async Task<IEnumerable<WeightEntry>> GetAllComplete(int top = 30, uint page = 1)
        {
            return await _context.WeightEntries
                .AsNoTracking()
                .Include(w => w.ExternalTargetBehavior)
                .Include(w => w.WeightDetails
                                .Where(wd => !wd.IsDeleted))
                .Where(w => !w.IsDeleted && w.ConcludeDate != null)
                .OrderByDescending(w => w.ConcludeDate)
                .Skip((int)page - 1)
                .Take(top)
                .ToListAsync();
        }

        public async Task<IEnumerable<WeightEntry>> GetAllByPartnerAsync(int partnerId, int top = 30, uint page = 1)
        {
            return await _context.WeightEntries
                .AsNoTracking()
                .Include(w => w.WeightDetails)
                .Include(w => w.ExternalTargetBehavior)
                .OrderByDescending(w => w.ConcludeDate)
                .Skip((int)page - 1)
                .Take(top)
                .ToListAsync();
        }

        public async Task<IEnumerable<WeightEntry>> GetPendingWeights(int top = 30, uint page = 1)
        {
            return await _context.WeightEntries
                .AsNoTracking()
                .Where(w => w.ConcludeDate == null && !w.IsDeleted)
                .Include(w => w.ExternalTargetBehavior)
                .Include(w => w.WeightDetails
                                .Where(wd => !wd.IsDeleted))
                .OrderByDescending(w => w.ConcludeDate)
                .Skip((int)page - 1)
                .Take(top)
                .ToListAsync();
        }

        public async Task<IEnumerable<WeightEntry>> GetPendingWeightsByPartnerAsync(int partnerId)
        {
            return await _context.WeightEntries
                .AsNoTracking()
                .Where(w => w.PartnerId == partnerId && w.ConcludeDate == null && !w.IsDeleted)
                .Include(w => w.WeightDetails
                                .Where(wd => !wd.IsDeleted))
                .ToListAsync();
        }

        public async Task UpdateAsync(WeightEntry weightEntry)
        {
            if (weightEntry.Id <= 0)
            {
                throw new ArgumentException("WeightEntry ID must be a valid one.", nameof(weightEntry.Id));
            }

            WeightEntry existingEntry = await GetByIdAsync(weightEntry.Id);

            weightEntry.CreatedAt = existingEntry.CreatedAt;

            // Preserve CreatedAt for existing WeightDetails and set LastUpdated if changed
            foreach (var detail in weightEntry.WeightDetails)
            {
                var existingDetail = existingEntry.WeightDetails.FirstOrDefault(d => d.Id == detail.Id);
                if (existingDetail != null)
                {
                    detail.CreatedAt = existingDetail.CreatedAt;

                    // Set LastUpdated if any tracked property has changed
                    if (HasDetailChanged(existingDetail, detail))
                    {
                        detail.LastUpdated = DateTime.UtcNow;
                    }
                    else
                    {
                        detail.LastUpdated = existingDetail.LastUpdated;
                    }
                }
                else
                {
                    // New detail being added - no LastUpdated yet (will be set on first update)
                    detail.LastUpdated = null;
                }
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
            weightEntry.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteDetailAsync(int id)
        {
            WeightDetail? weightDetail = await _context.WeightDetails.FindAsync(id);
            if (weightDetail == null)
            {
                return false;
            }
            weightDetail.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        private static bool HasDetailChanged(WeightDetail existing, WeightDetail incoming)
        {
            return existing.Weight != incoming.Weight ||
                   existing.Tare != incoming.Tare ||
                   existing.FK_WeightedProductId != incoming.FK_WeightedProductId ||
                   existing.ProductPrice != incoming.ProductPrice ||
                   existing.WeightedBy != incoming.WeightedBy ||
                   existing.SecondaryTare != incoming.SecondaryTare ||
                   existing.RequiredAmount != incoming.RequiredAmount ||
                   existing.Costales != incoming.Costales;
        }
    }
}

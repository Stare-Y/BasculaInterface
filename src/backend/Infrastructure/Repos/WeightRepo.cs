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

            // Load existing entry as TRACKED to apply only changed properties
            // This avoids _context.Update() which marks ALL properties as Modified
            // and can cause change-tracker side effects with related entities (e.g. ProviderPurchase)
            WeightEntry existingEntry = await _context.WeightEntries
                .Include(w => w.WeightDetails.Where(wd => !wd.IsDeleted))
                .Include(w => w.ExternalTargetBehavior)
                .FirstOrDefaultAsync(w => w.Id == weightEntry.Id && !w.IsDeleted)
                ?? throw new KeyNotFoundException($"WeightEntry with ID {weightEntry.Id} not found.");

            // Apply scalar property changes to the tracked entity
            existingEntry.PartnerId = weightEntry.PartnerId;
            existingEntry.ConptaqiComercialFK = weightEntry.ConptaqiComercialFK;
            existingEntry.ContpaqiComercialFolio = weightEntry.ContpaqiComercialFolio;
            existingEntry.ExternalTargetBehaviorFK = weightEntry.ExternalTargetBehaviorFK;
            existingEntry.TareWeight = weightEntry.TareWeight;
            existingEntry.BruteWeight = weightEntry.BruteWeight;
            existingEntry.ConcludeDate = weightEntry.ConcludeDate;
            existingEntry.VehiclePlate = weightEntry.VehiclePlate;
            existingEntry.Notes = weightEntry.Notes;
            existingEntry.RegisteredBy = weightEntry.RegisteredBy;

            // Update existing details and add new ones
            foreach (var incomingDetail in weightEntry.WeightDetails)
            {
                var existingDetail = existingEntry.WeightDetails.FirstOrDefault(d => d.Id == incomingDetail.Id);
                if (existingDetail != null)
                {
                    // Set LastUpdated if any tracked property has changed
                    if (HasDetailChanged(existingDetail, incomingDetail))
                    {
                        existingDetail.Weight = incomingDetail.Weight;
                        existingDetail.Tare = incomingDetail.Tare;
                        existingDetail.SecondaryTare = incomingDetail.SecondaryTare;
                        existingDetail.FK_WeightedProductId = incomingDetail.FK_WeightedProductId;
                        existingDetail.ProductPrice = incomingDetail.ProductPrice;
                        existingDetail.WeightedBy = incomingDetail.WeightedBy;
                        existingDetail.RequiredAmount = incomingDetail.RequiredAmount;
                        existingDetail.Costales = incomingDetail.Costales;
                        existingDetail.Notes = incomingDetail.Notes;
                        existingDetail.LastUpdated = DateTime.UtcNow;
                    }
                }
                else
                {
                    // New detail being added
                    incomingDetail.LastUpdated = null;
                    existingEntry.WeightDetails.Add(incomingDetail);
                }
            }

            // Remove details that are no longer present
            foreach (var existingDetail in existingEntry.WeightDetails.ToList())
            {
                if (!weightEntry.WeightDetails.Any(d => d.Id == existingDetail.Id))
                {
                    existingDetail.IsDeleted = true;
                }
            }

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

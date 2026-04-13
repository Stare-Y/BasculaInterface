using Core.Domain.Entities.ProviderOrders;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class ProviderPurchaseRepo : IProviderPurchaseRepo
    {
        private readonly WeightDBContext _context;

        public ProviderPurchaseRepo(WeightDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ProviderPurchase> CreateAsync(ProviderPurchase providerPurchase)
        {
            try
            {
                await _context.ProviderPurchases.AddAsync(providerPurchase);
                await _context.SaveChangesAsync();
                return providerPurchase;
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException($"Error creating ProviderPurchase: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }

        public async Task<ProviderPurchase> GetByIdAsync(int id)
        {
            try
            {
                ProviderPurchase? entity = await _context.ProviderPurchases
                    .AsNoTracking()
                    .Include(pp => pp.WeightEntry)
                    .FirstOrDefaultAsync(pp => pp.Id == id && !pp.IsDeleted);

                if (entity == null)
                    throw new KeyNotFoundException($"ProviderPurchase with ID {id} not found.");

                return entity;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving ProviderPurchase with ID {id}: {ex.Message}", ex);
            }
        }

        public async Task<ProviderPurchase?> GetByWeightEntryIdAsync(int weightEntryId)
        {
            try
            {
                return await _context.ProviderPurchases
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pp => pp.WeightEntryId == weightEntryId && !pp.IsDeleted);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving ProviderPurchase by WeightEntryId {weightEntryId}: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<ProviderPurchase>> GetAllAsync(int top = 30, uint page = 1)
        {
            try
            {
                return await _context.ProviderPurchases
                    .AsNoTracking()
                    .Include(pp => pp.WeightEntry)
                    .Where(pp => !pp.IsDeleted)
                    .OrderByDescending(pp => pp.CreatedAt)
                    .Skip((int)(page - 1) * top)
                    .Take(top)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving provider purchases: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<ProviderPurchase>> GetByProviderIdAsync(int providerId, int top = 30, uint page = 1)
        {
            try
            {
                return await _context.ProviderPurchases
                    .AsNoTracking()
                    .Include(pp => pp.WeightEntry)
                    .Where(pp => pp.ProviderId == providerId && !pp.IsDeleted)
                    .OrderByDescending(pp => pp.CreatedAt)
                    .Skip((int)(page - 1) * top)
                    .Take(top)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving provider purchases for provider {providerId}: {ex.Message}", ex);
            }
        }

        public async Task UpdateAsync(ProviderPurchase providerPurchase)
        {
            try
            {
                var existing = await _context.ProviderPurchases
                    .FirstOrDefaultAsync(pp => pp.Id == providerPurchase.Id && !pp.IsDeleted)
                    ?? throw new KeyNotFoundException($"ProviderPurchase with ID {providerPurchase.Id} not found.");

                existing.ProviderId = providerPurchase.ProviderId;
                existing.ProductId = providerPurchase.ProductId;
                existing.RequiredAmount = providerPurchase.RequiredAmount;
                existing.RealAmount = providerPurchase.RealAmount;
                existing.Notes = providerPurchase.Notes;
                existing.WeightEntryId = providerPurchase.WeightEntryId;
                existing.Concluded = providerPurchase.Concluded;
                existing.ExpectedArrival = providerPurchase.ExpectedArrival;
                existing.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException($"Concurrency conflict updating ProviderPurchase with ID {providerPurchase.Id}.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException($"Error updating ProviderPurchase with ID {providerPurchase.Id}: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                ProviderPurchase? entity = await _context.ProviderPurchases
                    .FirstOrDefaultAsync(pp => pp.Id == id && !pp.IsDeleted);

                if (entity == null)
                    return false;

                entity.IsDeleted = true;
                entity.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException($"Error deleting ProviderPurchase with ID {id}: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }
    }
}

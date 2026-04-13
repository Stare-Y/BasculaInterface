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
                providerPurchase.LastUpdated = DateTime.UtcNow;
                _context.ProviderPurchases.Update(providerPurchase);
                await _context.SaveChangesAsync();
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

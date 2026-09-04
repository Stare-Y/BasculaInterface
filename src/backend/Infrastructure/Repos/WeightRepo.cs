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
                .Include(w => w.WeightDetails.Where(wd => !wd.IsDeleted))
                    .ThenInclude(wd => wd.PedidoLine)
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
                .Skip(((int)page - 1) * top)
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
                .Skip(((int)page - 1) * top)
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
                .Skip(((int)page - 1) * top)
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
                .Skip(((int)page - 1) * top)
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
                .Skip(((int)page - 1) * top)
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

        public async Task<WeightDetail> GetDetailByIdAsync(int detailId)
        {
            return await _context.WeightDetails
                .Include(d => d.WeightEntry)
                .FirstOrDefaultAsync(d => d.Id == detailId && !d.IsDeleted)
                ?? throw new KeyNotFoundException($"WeightDetail with ID {detailId} not found.");
        }

        public async Task<WeightDetail> CreateDetailAsync(WeightDetail detail)
        {
            await _context.WeightDetails.AddAsync(detail);
            await _context.SaveChangesAsync();
            return detail;
        }

        public async Task UpdateDetailAsync(WeightDetail detail)
        {
            WeightDetail existing = await _context.WeightDetails
                .FirstOrDefaultAsync(d => d.Id == detail.Id && !d.IsDeleted)
                ?? throw new KeyNotFoundException($"WeightDetail with ID {detail.Id} not found.");

            existing.Weight = detail.Weight;
            existing.Tare = detail.Tare;
            existing.SecondaryTare = detail.SecondaryTare;
            existing.WeightedBy = detail.WeightedBy;
            existing.IsLoaded = detail.IsLoaded;
            existing.FK_WeightedProductId = detail.FK_WeightedProductId;
            existing.ProductPrice = detail.ProductPrice;
            existing.RequiredAmount = detail.RequiredAmount;
            existing.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// The vehicle's current running weight: <c>TareWeight + loadedSum</c> for the normal
        /// case (arrives empty, gets loaded), or <c>TareWeight - loadedSum</c> when
        /// <see cref="WeightEntry.IsDischarge"/> is set (arrives loaded, discharges) — see
        /// design.md Decision 7. Guards against discharging more than the vehicle brought in.
        /// </summary>
        private static double ComputeBruteWeight(WeightEntry entry, double loadedSum)
        {
            if (entry.IsDischarge)
            {
                if (loadedSum > entry.TareWeight)
                    throw new InvalidOperationException("No se puede descargar más peso del que trae el vehículo.");

                return entry.TareWeight - loadedSum;
            }

            return entry.TareWeight + loadedSum;
        }

        public async Task RecomputeBruteWeightAsync(int entryId)
        {
            WeightEntry entry = await _context.WeightEntries
                .Include(w => w.WeightDetails.Where(d => !d.IsDeleted))
                .FirstOrDefaultAsync(w => w.Id == entryId && !w.IsDeleted)
                ?? throw new KeyNotFoundException($"WeightEntry with ID {entryId} not found.");

            double loadedSum = entry.WeightDetails.Where(d => d.IsLoaded).Sum(d => d.Weight);
            entry.BruteWeight = ComputeBruteWeight(entry, loadedSum);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new WeightConcurrencyException("El registro fue modificado por otro terminal. Intente de nuevo.", ex);
            }
        }

        public async Task<WeightEntry> MarkDetailLoadedAsync(int detailId)
        {
            WeightDetail detail = await _context.WeightDetails
                .FirstOrDefaultAsync(d => d.Id == detailId && !d.IsDeleted)
                ?? throw new KeyNotFoundException($"WeightDetail with ID {detailId} not found.");

            WeightEntry entry = await _context.WeightEntries
                .Include(w => w.WeightDetails.Where(d => !d.IsDeleted))
                .FirstOrDefaultAsync(w => w.Id == detail.FK_WeightEntryId && !w.IsDeleted)
                ?? throw new KeyNotFoundException($"WeightEntry for detail {detailId} not found.");

            if (entry.ConcludeDate != null)
                throw new InvalidOperationException("No se puede modificar un proceso ya finalizado.");
            if (detail.Weight <= 0)
                throw new InvalidOperationException("El producto debe tener un peso registrado antes de marcarse como cargado.");
            if (detail.IsLoaded)
                throw new InvalidOperationException("El producto ya está marcado como cargado.");

            detail.IsLoaded = true;
            detail.LastUpdated = DateTime.UtcNow;

            // Include current detail (now IsLoaded=true) in sum by iterating the in-memory collection
            double loadedSum = entry.WeightDetails.Where(d => d.IsLoaded || d.Id == detailId).Sum(d => d.Weight);
            entry.BruteWeight = ComputeBruteWeight(entry, loadedSum);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new WeightConcurrencyException("El registro fue modificado por otro terminal. Intente de nuevo.", ex);
            }

            return entry;
        }

        public async Task ConcludeEntryAsync(int weightEntryId)
        {
            WeightEntry entry = await _context.WeightEntries
                .Include(w => w.WeightDetails.Where(d => !d.IsDeleted))
                .FirstOrDefaultAsync(w => w.Id == weightEntryId && !w.IsDeleted)
                ?? throw new KeyNotFoundException($"WeightEntry with ID {weightEntryId} not found.");

            if (entry.ConcludeDate != null)
                throw new InvalidOperationException("El proceso ya ha sido finalizado.");
            if (entry.WeightDetails.Any(d => !d.IsLoaded))
                throw new InvalidOperationException("Todos los productos deben estar cargados antes de concluir el proceso de pesaje.");
            if (entry.WeightDetails.Count > 1 && (entry.PartnerId == null || entry.PartnerId <= 0))
                throw new InvalidOperationException("Debe seleccionarse un socio antes de concluir el proceso de pesaje con múltiples productos.");

            entry.ConcludeDate = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new WeightConcurrencyException("El registro fue modificado por otro terminal. Intente de nuevo.", ex);
            }
        }

        public async Task UpdateAsync(WeightEntry weightEntry, bool force = false)
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

            if(existingEntry.ConcludeDate != null && !force)
            {
                throw new InvalidOperationException("No se puede modificar un proceso ya finalizado.");
            }

            // Only apply non-integrity fields; BruteWeight, ConcludeDate, and detail weights
            // are owned by dedicated endpoints and must not be overwritten here.
            existingEntry.PartnerId = weightEntry.PartnerId;
            existingEntry.ConptaqiComercialFK = weightEntry.ConptaqiComercialFK;
            existingEntry.ContpaqiComercialFolio = weightEntry.ContpaqiComercialFolio;
            existingEntry.ExternalTargetBehaviorFK = weightEntry.ExternalTargetBehaviorFK;
            existingEntry.TareWeight = weightEntry.TareWeight;
            existingEntry.IsDischarge = weightEntry.IsDischarge;
            double updateLoadedSum = existingEntry.WeightDetails
                .Where(d => d.IsLoaded && !d.IsDeleted)
                .Sum(d => d.Weight);
            existingEntry.BruteWeight = ComputeBruteWeight(existingEntry, updateLoadedSum);
            existingEntry.VehiclePlate = weightEntry.VehiclePlate;
            existingEntry.Notes = weightEntry.Notes;
            existingEntry.RegisteredBy = weightEntry.RegisteredBy;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new WeightConcurrencyException("El registro fue modificado por otro terminal. Intente de nuevo.", ex);
            }
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
            weightDetail.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

    }
}

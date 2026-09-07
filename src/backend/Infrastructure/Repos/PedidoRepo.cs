using Core.Domain.Entities.ProviderOrders;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class PedidoRepo : IPedidoRepo
    {
        private readonly WeightDBContext _context;

        public PedidoRepo(WeightDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Pedido> CreateAsync(Pedido pedido)
        {
            try
            {
                await _context.Pedidos.AddAsync(pedido);
                await _context.SaveChangesAsync();
                return pedido;
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException($"Error creating Pedido: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }

        public async Task<Pedido> GetByIdAsync(int id)
        {
            Pedido? entity = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Lines.Where(l => !l.IsDeleted))
                    .ThenInclude(l => l.WeightDetails.Where(d => !d.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (entity == null)
                throw new KeyNotFoundException($"Pedido with ID {id} not found.");

            return entity;
        }

        public async Task<IEnumerable<Pedido>> GetAllAsync(int top = 30, uint page = 1)
        {
            return await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Lines.Where(l => !l.IsDeleted))
                    .ThenInclude(l => l.WeightDetails.Where(d => !d.IsDeleted))
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((int)(page - 1) * top)
                .Take(top)
                .ToListAsync();
        }

        public async Task<IEnumerable<Pedido>> GetByProviderIdAsync(int providerId, int top = 30, uint page = 1)
        {
            return await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Lines.Where(l => !l.IsDeleted))
                    .ThenInclude(l => l.WeightDetails.Where(d => !d.IsDeleted))
                .Where(p => p.ProviderId == providerId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((int)(page - 1) * top)
                .Take(top)
                .ToListAsync();
        }

        public async Task UpdateAsync(Pedido pedido)
        {
            var existing = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.Id == pedido.Id && !p.IsDeleted)
                ?? throw new KeyNotFoundException($"Pedido with ID {pedido.Id} not found.");

            existing.ProviderId = pedido.ProviderId;
            existing.ExpectedArrival = pedido.ExpectedArrival;
            existing.Notes = pedido.Notes;
            existing.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            Pedido? entity = await _context.Pedidos
                .Include(p => p.Lines.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.LastUpdated = DateTime.UtcNow;

            foreach (PedidoLine line in entity.Lines)
            {
                line.IsDeleted = true;
                line.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

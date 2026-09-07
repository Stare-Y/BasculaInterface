using Core.Domain.Entities.ProviderOrders;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class PedidoLineRepo : IPedidoLineRepo
    {
        private readonly WeightDBContext _context;

        public PedidoLineRepo(WeightDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<PedidoLine> CreateAsync(PedidoLine line)
        {
            bool pedidoExists = await _context.Pedidos.AnyAsync(p => p.Id == line.PedidoId && !p.IsDeleted);
            if (!pedidoExists)
                throw new KeyNotFoundException($"Pedido with ID {line.PedidoId} not found.");

            try
            {
                await _context.PedidoLines.AddAsync(line);
                await _context.SaveChangesAsync();
                return line;
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException($"Error creating PedidoLine: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
        }

        public async Task<PedidoLine> GetByIdAsync(int id)
        {
            PedidoLine? entity = await _context.PedidoLines
                .AsNoTracking()
                .Include(l => l.WeightDetails.Where(d => !d.IsDeleted))
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (entity == null)
                throw new KeyNotFoundException($"PedidoLine with ID {id} not found.");

            return entity;
        }

        public async Task UpdateAsync(PedidoLine line)
        {
            var existing = await _context.PedidoLines
                .FirstOrDefaultAsync(l => l.Id == line.Id && !l.IsDeleted)
                ?? throw new KeyNotFoundException($"PedidoLine with ID {line.Id} not found.");

            existing.ProductId = line.ProductId;
            existing.RequiredAmount = line.RequiredAmount;
            existing.Price = line.Price;
            existing.Notes = line.Notes;
            existing.ManuallyClosed = line.ManuallyClosed;
            existing.RequiresDisTaring = line.RequiresDisTaring;
            existing.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            PedidoLine? entity = await _context.PedidoLines
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CloseAsync(int id)
        {
            PedidoLine existing = await _context.PedidoLines
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted)
                ?? throw new KeyNotFoundException($"PedidoLine with ID {id} not found.");

            existing.ManuallyClosed = true;
            existing.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}

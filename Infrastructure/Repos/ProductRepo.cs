using Core.Domain.Entities.ContpaqiSQL;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class ProductRepo : IProductRepo
    {
        private readonly ContpaqiSQLContext _context;
        public ProductRepo(ContpaqiSQLContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public async Task<IEnumerable<Producto>> SearchByNameAsync(string name)
        {
            return await _context.Productos
                .AsNoTracking()
                .Where(p => p.CNOMBREPRODUCTO.Contains(name))
                .ToListAsync();
        }
        
        public async Task<Producto> GetByIdAsync(int id)
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CIDPRODUCTO == id)
                ?? throw new KeyNotFoundException($"No se encontró el producto con ID: {id}");

            return producto;
        }
    }
}

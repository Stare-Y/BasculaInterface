using Core.Domain.Entities.ContpaqiSQL;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class ProductoRepo : IProductoRepo
    {
        private readonly ContpaqiSQLContext _context;
        public ProductoRepo(ContpaqiSQLContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public async Task<IEnumerable<Producto>> SearchByNameAsync(string name, CancellationToken cancellationToken)
        {
            var productos = await _context.Productos.AsNoTracking().Where(
                p => p.CNOMBREPRODUCTO.Contains(name))
                .ToListAsync(cancellationToken);

            if (productos == null || !productos.Any())
                throw new KeyNotFoundException($"No se encontraron productos con el nombre: {name}");

            return productos;
        }
        
        public async Task<Producto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var producto = await _context.Productos.AsNoTracking()
                .FirstOrDefaultAsync(p => p.CIDPRODUCTO == id, cancellationToken);

            if (producto == null)
                throw new KeyNotFoundException($"No se encontró el producto con ID: {id}");

            return producto;
        }
    }
}

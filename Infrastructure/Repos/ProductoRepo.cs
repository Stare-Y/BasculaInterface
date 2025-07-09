using Core.Application.Interfaces;
using Core.Domain.Entities.ContpaqiSQL;
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
    }
}

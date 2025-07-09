using Core.Application.Interfaces;
using Core.Domain.Entities.ContpaqiSQL;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class ClienteProveedorRepo : IClienteProveedorRepo
    {
        private readonly ContpaqiSQLContext _context;

        public ClienteProveedorRepo(ContpaqiSQLContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public async Task<IEnumerable<ClienteProveedor>> SearchByName(string name, CancellationToken cancellationToken)
        {
            var clientesProveedores = await _context.ClientesProveedores.AsNoTracking().Where(
                cp => cp.CRAZONSOCIAL.Contains(name))
                .ToListAsync(cancellationToken);

            if (clientesProveedores.Count == 0)
                throw new KeyNotFoundException($"No se encontraron clientes/proveedores con el nombre: {name}");

            return clientesProveedores;
        }
    }
}

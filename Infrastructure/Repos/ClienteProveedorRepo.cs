using Azure;
using Core.Domain.Entities.ContpaqiSQL;
using Core.Domain.Interfaces;
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

        public async Task<IEnumerable<ClienteProveedor>> SearchByName(string name, int page = 1,int sizePage = 50, CancellationToken cancellationToken = default)
        {
            return await _context.ClientesProveedores
                .AsNoTracking()
                .Where(
                cp => cp.CRAZONSOCIAL.Contains(name))
                .Skip((page - 1) * sizePage)
                .Take(sizePage)
                .OrderBy(p => p.CRAZONSOCIAL)
                .ToListAsync(cancellationToken);
        }

        public async Task<ClienteProveedor> GetById(int id, CancellationToken cancellationToken = default)
        {
            var clienteProveedor = await _context.ClientesProveedores.AsNoTracking()
                .FirstOrDefaultAsync(cp => cp.CIDCLIENTEPROVEEDOR == id, cancellationToken);

            if (clienteProveedor == null)
                throw new KeyNotFoundException($"Cliente/Proveedor/Partner con ID {id} no encontrado.");

            return clienteProveedor;
        }

    }
}

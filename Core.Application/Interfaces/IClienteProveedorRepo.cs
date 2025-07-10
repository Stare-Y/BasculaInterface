using Core.Application.DTOs;
using Core.Domain.Entities.ContpaqiSQL;

namespace Core.Application.Interfaces
{
    public interface IClienteProveedorRepo
    {
        /// <summary>
        /// Busca todos los clientes y proveedores con el nombre proporcionado
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        Task<IEnumerable<ClienteProveedor>> SearchByName(string name, CancellationToken cancellationToken = default);
        Task<ClienteProveedor> GetById(int id, CancellationToken cancellationToken = default);
    }
}

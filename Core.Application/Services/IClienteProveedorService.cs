using Core.Application.DTOs;
using Core.Domain.Entities.ContpaqiSQL;

namespace Core.Application.Services
{
    public interface IClienteProveedorService
    {
        /// <summary>
        /// Busca todos los clientes y proveedores con el nombre proporcionado
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        Task<IEnumerable<ClienteProveedorDto>> SearchByName(string name);
        Task<ClienteProveedorDto> GetById(int id);
    }
}

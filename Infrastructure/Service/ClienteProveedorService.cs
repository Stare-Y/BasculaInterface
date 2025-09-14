using Core.Application.DTOs;
using Core.Application.Extensions;
using Core.Application.Services;
using Core.Domain.Entities.ContpaqiSQL;
using Core.Domain.Interfaces;

namespace Infrastructure.Service
{
    public class ClienteProveedorService : IClienteProveedorService
    {
        private readonly IClienteProveedorRepo _clienteProveedorRepo;
        public ClienteProveedorService(IClienteProveedorRepo clienteProveedorRepo)
        {
            _clienteProveedorRepo = clienteProveedorRepo;
        }
        public async Task<IEnumerable<ClienteProveedorDto>> SearchByName(string name)
        {
            IEnumerable<ClienteProveedor> clientesProveedores = await _clienteProveedorRepo.SearchByName(name);

            return WeightExtensions.BuildFromBaseEntity(clientesProveedores);
        }
        public async Task<ClienteProveedorDto> GetById(int id)
        {
            ClienteProveedor clienteProveedor = await _clienteProveedorRepo.GetById(id);

            return WeightExtensions.BuildFromBaseEntity([clienteProveedor]).First();
        }
    }
}

using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.ContpaqiSQL;
using Core.Domain.Interfaces;

namespace Infrastructure.Service
{
    public class ClienteProveedorService : IClienteProveedorService
    {
        private readonly IClienteProveedorRepo _clienteProveedorRepo;
        private readonly IDocumentRepo _documentRepo;
        public ClienteProveedorService(IClienteProveedorRepo clienteProveedorRepo, IDocumentRepo documentRepo)
        {
            _clienteProveedorRepo = clienteProveedorRepo;
            _documentRepo = documentRepo;
        }
        public async Task<IEnumerable<ClienteProveedorDto>> SearchByName(string name)
        {
            IEnumerable<ClienteProveedor> clientesProveedores = await _clienteProveedorRepo.SearchByName(name);

            return await Task.WhenAll(clientesProveedores.Select(async c =>
                new ClienteProveedorDto
                {
                    Id = c.CIDCLIENTEPROVEEDOR,
                    RazonSocial = c.CRAZONSOCIAL,
                    RFC = c.CRFC,
                    CreditLimit = c.CLIMITECREDITOCLIENTE,
                    Debt = await _documentRepo.GetClientDebt(c.CIDCLIENTEPROVEEDOR),
                    OrderRequestAllowed = c.CBANCREDITOYCOBRANZA == 1 && c.CBANVENTACREDITO == 1,
                    IgnoreCreditLimit = c.CBANEXCEDERCREDITO == 1
                }
            ));
        }

        public async Task<ClienteProveedorDto> GetById(int id)
        {
            ClienteProveedor clienteProveedor = await _clienteProveedorRepo.GetById(id);

            double debt = await _documentRepo.GetClientDebt(id);

            return new ClienteProveedorDto
            {
                Id = clienteProveedor.CIDCLIENTEPROVEEDOR,
                RazonSocial = clienteProveedor.CRAZONSOCIAL,
                RFC = clienteProveedor.CRFC,
                CreditLimit = clienteProveedor.CLIMITECREDITOCLIENTE,
                Debt = debt,
                OrderRequestAllowed = clienteProveedor.CBANCREDITOYCOBRANZA == 1 && clienteProveedor.CBANVENTACREDITO == 1,
                IgnoreCreditLimit = clienteProveedor.CBANEXCEDERCREDITO == 1,
            };
        }
    }
}
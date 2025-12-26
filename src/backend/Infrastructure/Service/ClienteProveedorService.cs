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

            List<ClienteProveedorDto> clientesProveedoresDto = [];

            foreach (ClienteProveedor client in clientesProveedores)
            {
                clientesProveedoresDto.Add(new ClienteProveedorDto
                {
                    Id = client.CIDCLIENTEPROVEEDOR,
                    Code = client.CCODIGOCLIENTE,
                    RazonSocial = client.CRAZONSOCIAL,
                    RFC = client.CRFC,
                    CreditLimit = client.CLIMITECREDITOCLIENTE,
                    Debt = await _documentRepo.GetClientDebt(client.CIDCLIENTEPROVEEDOR),
                    OrderRequestAllowed = client.CBANCREDITOYCOBRANZA == 1 && client.CBANVENTACREDITO == 1,
                    IgnoreCreditLimit = client.CBANEXCEDERCREDITO == 1,
                    IsProvider = client.CTIPOCLIENTE != 1
                });
            }

            return clientesProveedoresDto;
        }

        public async Task<ClienteProveedorDto> GetById(int id)
        {
            ClienteProveedor clienteProveedor = await _clienteProveedorRepo.GetById(id);

            double debt = await _documentRepo.GetClientDebt(id);

            return new ClienteProveedorDto
            {
                Id = clienteProveedor.CIDCLIENTEPROVEEDOR,
                Code = clienteProveedor.CCODIGOCLIENTE,
                RazonSocial = clienteProveedor.CRAZONSOCIAL,
                RFC = clienteProveedor.CRFC,
                CreditLimit = clienteProveedor.CLIMITECREDITOCLIENTE,
                Debt = await _documentRepo.GetClientDebt(clienteProveedor.CIDCLIENTEPROVEEDOR),
                OrderRequestAllowed = clienteProveedor.CBANCREDITOYCOBRANZA == 1 && clienteProveedor.CBANVENTACREDITO == 1,
                IgnoreCreditLimit = clienteProveedor.CBANEXCEDERCREDITO == 1,
                IsProvider = clienteProveedor.CTIPOCLIENTE != 1
            };
        }
    }
}
using Core.Application.DTOs;
using Core.Application.Extensions;
using Core.Domain.Entities.ContpaqiSQL;
using Core.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class ClienteProveedorController : ControllerBase
    {
        private readonly IClienteProveedorRepo _clienteProveedorRepo;
        public ClienteProveedorController(IClienteProveedorRepo clienteProveedorRepo)
        {
            _clienteProveedorRepo = clienteProveedorRepo ?? throw new ArgumentNullException(nameof(clienteProveedorRepo));
        }

        [HttpGet("ByName/{name}")]
        public async Task<IActionResult> SearchByName(string name, CancellationToken cancellationToken)
        {
            try
            {
                IEnumerable<ClienteProveedor> clientesProveedores = await _clienteProveedorRepo.SearchByName(name, cancellationToken);

                if (clientesProveedores == null || !clientesProveedores.Any())
                {
                    return NotFound($"No clients/providers/partners found with the name '{name}'.");
                }

                IEnumerable<ClienteProveedorDto> dtos = WeightExtensions.BuildFromBaseEntity(clientesProveedores);
                
                return Ok(dtos);
            }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(knfEx.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error searching for clients/providers: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                ClienteProveedor? clienteProveedor = await _clienteProveedorRepo.GetById(id, cancellationToken);
                if (clienteProveedor == null)
                {
                    return NotFound($"Client/Provider/Partner with ID {id} not found.");
                }
                ClienteProveedorDto dto = WeightExtensions.BuildFromBaseEntity(new[] { clienteProveedor }).First();
                
                return Ok(dto);
            }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(knfEx.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving client/provider: {ex.Message}");
            }
        }
    }
}

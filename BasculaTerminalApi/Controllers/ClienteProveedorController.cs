using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Domain.Entities.ContpaqiSQL;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class ClienteProveedorController : ControllerBase
    {
        private readonly IClienteProveedorRepo _clienteProveedorRepo;
        public ClienteProveedorController(IClienteProveedorRepo clienteProveedorRepo)
        {
            _clienteProveedorRepo = clienteProveedorRepo ?? throw new ArgumentNullException(nameof(clienteProveedorRepo));
        }

        [HttpGet]
        public async Task<IActionResult> SearchByName([FromQuery] string name, CancellationToken cancellationToken)
        {
            try
            {
                IEnumerable<ClienteProveedor> clientesProveedores = await _clienteProveedorRepo.SearchByName(name, cancellationToken);

                if (clientesProveedores == null || !clientesProveedores.Any())
                {
                    return NotFound($"No clients/providers/partners found with the name '{name}'.");
                }

                IEnumerable<ClienteProveedorDto> dtos = ClienteProveedorDto.BuildFromBaseEntity(clientesProveedores);
                
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
    }
}

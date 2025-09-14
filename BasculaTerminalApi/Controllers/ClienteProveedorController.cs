using Core.Application.DTOs;
using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class ClienteProveedorController : ControllerBase
    {
        private readonly IClienteProveedorService _clienteProveedorService;
        public ClienteProveedorController(IClienteProveedorService clienteProveedorRepo)
        {
            _clienteProveedorService = clienteProveedorRepo ?? throw new ArgumentNullException(nameof(clienteProveedorRepo));
        }

        [HttpGet("ByName")]
        public async Task<ActionResult<IEnumerable<ClienteProveedorDto>>> SearchByName([FromQuery]string name)
        {
            return Ok(await _clienteProveedorService.SearchByName(name));
        }

        [HttpGet("ById")]
        public async Task<ActionResult<ClienteProveedorDto>> GetById([FromQuery]int id)
        {
            return Ok(await _clienteProveedorService.GetById(id));
        }
    }
}

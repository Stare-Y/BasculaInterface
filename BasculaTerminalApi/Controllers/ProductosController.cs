using Core.Application.DTOs;
using Core.Application.Interfaces;
using Core.Domain.Entities.ContpaqiSQL;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoRepo _productoRepo;
        public ProductosController(IProductoRepo productoRepo)
        {
            _productoRepo = productoRepo ?? throw new ArgumentNullException(nameof(productoRepo));
        }

        [HttpGet]
        public async Task<IActionResult> SearchByName([FromQuery] string name, CancellationToken cancellationToken)
        {
            try
            {
                IEnumerable<Producto> productos = await _productoRepo.SearchByNameAsync(name, cancellationToken);

                if (productos == null || !productos.Any())
                {
                    throw new KeyNotFoundException($"No products found with name: {name}");
                }

                IEnumerable<ProductoDto> dtos = ProductoDto.BuildFromBaseEntity(productos);

                return Ok(dtos);
            }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(knfEx.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error searching for products: {ex.Message}");
            }
        }
    }
}

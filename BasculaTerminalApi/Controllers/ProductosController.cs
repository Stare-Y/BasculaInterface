using Core.Application.DTOs;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Domain.Entities.ContpaqiSQL;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoRepo _productoRepo;
        public ProductosController(IProductoRepo productoRepo)
        {
            _productoRepo = productoRepo ?? throw new ArgumentNullException(nameof(productoRepo));
        }

        [HttpGet("ByName/{name}")]
        public async Task<IActionResult> SearchByName(string name, CancellationToken cancellationToken)
        {
            try
            {
                IEnumerable<Producto> productos = await _productoRepo.SearchByNameAsync(name, cancellationToken);

                if (productos == null || !productos.Any())
                {
                    throw new KeyNotFoundException($"No products found with name: {name}");
                }

                IEnumerable<ProductoDto> dtos = WeightHelper.BuildFromBaseEntity(productos);

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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                Producto? producto = await _productoRepo.GetByIdAsync(id, cancellationToken);

                if (producto == null)
                {
                    throw new KeyNotFoundException($"Product with ID {id} not found.");
                }

                ProductoDto dto = WeightHelper.BuildFromBaseEntity(producto);

                return Ok(dto);
            }
            catch (KeyNotFoundException knfEx)
            {
                return NotFound(knfEx.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving product: {ex.Message}");
            }
        }
    }
}

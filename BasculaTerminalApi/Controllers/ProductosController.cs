using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BasculaTerminalApi.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductosController(IProductService productoRepo)
        {
            _productService = productoRepo ?? throw new ArgumentNullException(nameof(productoRepo));
        }

        [HttpGet("ByName")]
        public async Task<IActionResult> SearchByName([FromQuery]string name)
        {
            return Ok(await _productService.SearchByNameAsync(name));
        }

        [HttpGet("ById")]
        public async Task<IActionResult> GetById([FromQuery]int id)
        {
            return Ok(await _productService.GetByIdAsync(id));
        }
    }
}

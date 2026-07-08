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
        public async Task<IActionResult> SearchByName([FromQuery] string name, [FromQuery] int page = 1, [FromQuery] int sizePage = 50)
        {
            return Ok(await _productService.SearchByNameAsync(name, page, sizePage));
        }

        [HttpGet("ById")]
        public async Task<IActionResult> GetById([FromQuery] int id)
        {
            return Ok(await _productService.GetByIdAsync(id));
        }

        [HttpGet("ByMultipleIds")]
        public async Task<IActionResult> GetByMultipleIds([FromQuery] int[] ids)
        {
            return Ok(await _productService.GetByMultipleIdsAsync(ids));
        }
    }
}

using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.ContpaqiSQL;
using Core.Domain.Interfaces;

namespace Infrastructure.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepo _productRepo;
        public ProductService(IProductRepo productRepo)
        {
            _productRepo = productRepo;
        }

        public async Task<IEnumerable<ProductoDto>> SearchByNameAsync(string name, int page,int sizePage)
        {
            IEnumerable<Producto> productos = await _productRepo.SearchByNameAsync(name,page,sizePage);

            return productos.Select(p => new ProductoDto (p));
        }

        public async Task<ProductoDto> GetByIdAsync(int id)
        {
            Producto producto = await _productRepo.GetByIdAsync(id);

            return new ProductoDto(producto);
        }
    }
}

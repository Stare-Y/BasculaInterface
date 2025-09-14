using Core.Application.DTOs;
using Core.Application.Extensions;
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

        public async Task<IEnumerable<ProductoDto>> SearchByNameAsync(string name)
        {
            IEnumerable<Producto> productos = await _productRepo.SearchByNameAsync(name);

            return WeightExtensions.BuildFromBaseEntity(productos);
        }

        public async Task<ProductoDto> GetByIdAsync(int id)
        {
            Producto producto = await _productRepo.GetByIdAsync(id);

            return WeightExtensions.BuildFromBaseEntity(producto);
        }
    }
}

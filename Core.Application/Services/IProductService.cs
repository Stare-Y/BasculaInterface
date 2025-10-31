using Core.Application.DTOs;

namespace Core.Application.Services
{
    public interface IProductService
    {
        /// <summary>
        /// Busca productos por nombre
        /// </summary>
        /// <param name="name"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        Task<IEnumerable<ProductoDto>> SearchByNameAsync(string name, int page, int sizePage);

        Task<ProductoDto> GetByIdAsync(int id);
    }
}

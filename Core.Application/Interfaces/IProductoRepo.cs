using Core.Domain.Entities.ContpaqiSQL;

namespace Core.Application.Interfaces
{
    public interface IProductoRepo
    {
        /// <summary>
        /// Busca productos por nombre
        /// </summary>
        /// <param name="name"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        Task<IEnumerable<Producto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<Producto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}

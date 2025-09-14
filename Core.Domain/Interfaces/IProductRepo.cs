using Core.Domain.Entities.ContpaqiSQL;

namespace Core.Domain.Interfaces
{
    public interface IProductRepo
    {
        /// <summary>
        /// Busca productos por nombre
        /// </summary>
        /// <param name="name"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        Task<IEnumerable<Producto>> SearchByNameAsync(string name);

        Task<Producto> GetByIdAsync(int id);
    }
}

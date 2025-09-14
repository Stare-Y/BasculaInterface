using Core.Domain.Entities.Turns;

namespace Core.Domain.Interfaces
{
    public interface ITurnRepo
    {
        /// <summary>
        /// Get a new turn, starting from 1 each day
        /// </summary>
        /// <param name="weightId"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        Task<Turn> GetTurn(int? weightId = null, string? description = null);
    }
}

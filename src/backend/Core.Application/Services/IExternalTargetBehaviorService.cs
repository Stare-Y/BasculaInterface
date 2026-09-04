using Core.Application.DTOs;

namespace Core.Application.Services
{
    public interface IExternalTargetBehaviorService
    {
        Task<IEnumerable<ExternalTargetBehaviorDto>> GetAllAsync();
        Task<ExternalTargetBehaviorDto> GetByIdAsync(int id);

        /// <summary>
        /// The hidden behaviors, used as the almacén-target options in the pedido
        /// convert-to-weight dialog (design.md Decision 5). Ordered by almacén code.
        /// </summary>
        Task<IEnumerable<ExternalTargetBehaviorDto>> GetAlmacenTargetsAsync();
    }
}

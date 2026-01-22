using Core.Domain.Entities.Behaviors;

namespace Core.Domain.Interfaces
{
    public interface IExternalTargetBehaviorRepo
    {
        Task<IEnumerable<ExternalTargetBehavior>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ExternalTargetBehavior> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    }
}

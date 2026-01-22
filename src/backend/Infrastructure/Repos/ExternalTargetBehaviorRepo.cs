using Core.Domain.Entities.Behaviors;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class ExternalTargetBehaviorRepo : IExternalTargetBehaviorRepo
    {
        private readonly WeightDBContext _context;
        public ExternalTargetBehaviorRepo(WeightDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<ExternalTargetBehavior>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.ExternalTargetBehaviors
                .AsNoTracking()
                .Where(etb => !etb.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<ExternalTargetBehavior> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var behavior = await _context.ExternalTargetBehaviors
                .AsNoTracking()
                .FirstOrDefaultAsync(etb => etb.Id == id && !etb.IsDeleted, cancellationToken);
            if (behavior == null)
                throw new KeyNotFoundException($"ExternalTargetBehavior con ID {id} no encontrado.");
            return behavior;
        }
    }
}

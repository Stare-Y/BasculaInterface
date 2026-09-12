using Core.Domain.Entities.Audit;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repos
{
    public class AuditLogRepo : IAuditLogRepo
    {
        private readonly WeightDBContext _context;

        public AuditLogRepo(WeightDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<AuditLogEntry> CreateAsync(AuditLogEntry entry)
        {
            _context.AuditLogEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<IEnumerable<AuditLogEntry>> GetForEntityAsync(string entityType, IEnumerable<int> entityIds)
        {
            List<int> ids = entityIds.ToList();
            return await _context.AuditLogEntries
                .AsNoTracking()
                .Where(a => a.EntityType == entityType && ids.Contains(a.EntityId))
                .OrderBy(a => a.Timestamp)
                .ToListAsync();
        }
    }
}

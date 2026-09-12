using Core.Domain.Entities.Audit;

namespace Core.Domain.Interfaces
{
    public interface IAuditLogRepo
    {
        Task<AuditLogEntry> CreateAsync(AuditLogEntry entry);

        /// <summary>All entries for a given entity type + one of a set of ids, ordered by
        /// timestamp — used by the radiography endpoint to pull a WeightEntry's own entries plus
        /// every one of its details' entries in two calls.</summary>
        Task<IEnumerable<AuditLogEntry>> GetForEntityAsync(string entityType, IEnumerable<int> entityIds);
    }
}

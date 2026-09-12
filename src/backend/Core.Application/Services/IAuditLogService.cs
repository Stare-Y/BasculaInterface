using Core.Application.DTOs;

namespace Core.Application.Services
{
    /// <summary>
    /// Records mutating actions (design.md Decision 11) — called explicitly at the end of each
    /// mutating service method, not via a generic EF Core interceptor, so the recorded action name
    /// is the meaningful operation (e.g. "WeightEntry.DeleteSafely") rather than a raw property
    /// diff.
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>Records an action performed by the current request's authenticated user
        /// (resolved via ICurrentUserService). If there is no authenticated user in scope (e.g. a
        /// direct unit-test call to a service method), the entry is not written — every real
        /// caller goes through the fallback-authenticated API, so this only happens in tests.</summary>
        Task RecordAsync(string action, string entityType, int entityId);

        /// <summary>Full radiography for a WeightEntry: the entry regardless of IsDeleted, all of
        /// its details including logically-deleted ones, and every audit entry recorded against
        /// the entry or any of its details, ordered by timestamp (design.md Decision 12).</summary>
        Task<WeightEntryRadiographyDto> GetWeightEntryRadiographyAsync(int weightEntryId);
    }
}

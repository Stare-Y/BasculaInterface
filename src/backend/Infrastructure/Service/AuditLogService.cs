using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Audit;
using Core.Domain.Entities.Weight;
using Core.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Service
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepo _auditLogRepo;
        private readonly IWeightRepo _weightRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(IAuditLogRepo auditLogRepo, IWeightRepo weightRepo, ICurrentUserService currentUserService, ILogger<AuditLogService> logger)
        {
            _auditLogRepo = auditLogRepo;
            _weightRepo = weightRepo;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task RecordAsync(string action, string entityType, int entityId)
        {
            // Every real caller reaches this through the fallback-authenticated API, so UserId is
            // always present in production; only a direct unit-test call to a service method (with
            // no HttpContext in scope) skips recording, deliberately, rather than fabricate an actor.
            int? userId = _currentUserService.UserId;
            if (userId is null)
                return;

            // Failure-isolated (design.md Decision 1 of expand-audit-log-coverage): every call site
            // places this after its real mutation already committed, so a failure writing the audit
            // row itself must never propagate and turn a successful action into an apparent 500.
            try
            {
                await _auditLogRepo.CreateAsync(new AuditLogEntry
                {
                    UserId = userId.Value,
                    Timestamp = DateTime.UtcNow,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record audit log entry for action {Action} on {EntityType} {EntityId}", action, entityType, entityId);
            }
        }

        public async Task<WeightEntryRadiographyDto> GetWeightEntryRadiographyAsync(int weightEntryId)
        {
            WeightEntry entry = await _weightRepo.GetByIdIncludingDeletedAsync(weightEntryId);

            List<int> detailIds = [.. entry.WeightDetails.Select(d => d.Id)];

            IEnumerable<AuditLogEntry> entryAudit = await _auditLogRepo.GetForEntityAsync(nameof(WeightEntry), [weightEntryId]);
            IEnumerable<AuditLogEntry> detailAudit = detailIds.Count > 0
                ? await _auditLogRepo.GetForEntityAsync(nameof(WeightDetail), detailIds)
                : [];

            List<AuditLogEntryDto> auditLog = [.. entryAudit.Concat(detailAudit)
                .OrderBy(a => a.Timestamp)
                .Select(a => new AuditLogEntryDto(a))];

            return new WeightEntryRadiographyDto
            {
                WeightEntry = new WeightEntryDto(entry),
                Details = entry.WeightDetails.Select(d => new WeightDetailDto(d)),
                AuditLog = auditLog,
            };
        }
    }
}

using Core.Domain.Entities.Audit;

namespace Core.Application.DTOs
{
    public class AuditLogEntryDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }

        public AuditLogEntryDto() { }

        public AuditLogEntryDto(AuditLogEntry entity)
        {
            Id = entity.Id;
            UserId = entity.UserId;
            Timestamp = entity.Timestamp;
            Action = entity.Action;
            EntityType = entity.EntityType;
            EntityId = entity.EntityId;
        }
    }

    /// <summary>Full "radiography" of a WeightEntry (design.md Decision 12): the entry regardless
    /// of IsDeleted, all of its details including logically-deleted ones, and every audit entry
    /// recorded against the entry or any of its details, ordered by timestamp.</summary>
    public class WeightEntryRadiographyDto
    {
        public required WeightEntryDto WeightEntry { get; set; }
        public required IEnumerable<WeightDetailDto> Details { get; set; }
        public required IEnumerable<AuditLogEntryDto> AuditLog { get; set; }
    }
}

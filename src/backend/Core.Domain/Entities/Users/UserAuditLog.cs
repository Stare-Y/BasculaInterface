using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Users
{
    public class UserAuditLog
    {
        public int FkUserId { get; set; }
        public required string Action { get; set; } 
        public string? Metadata { get; set; } = null;
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}

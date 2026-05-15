using Core.Domain.Entities.Base;

namespace Core.Domain.Entities.Users
{
    public class User : BaseEntity
    {
        public required string Name { get; set; }
        public required string Code { get; set; }
        public required string HashPassword { get; set; }
        public UserPermissions Permissions { get; set; } = new UserPermissions();
        public virtual ICollection<UserAuditLog> AuditLogs { get; set; } = [];
    }
}

namespace Core.Domain.Entities.Users
{
    public class UserPermissions
    {
        public int FkUserId { get; set; }
        public bool CanWeightManually { get; set; } = false;
        public bool CanBypassTurn { get; set; } = false;
        public bool CanCreateProviderPurchases { get; set; } = false; // Override for Gaby's Role.
        public bool UpdateTerminalSettings { get; set; } = false;
    }
}

namespace Core.Domain.Entities.Identity
{
    /// <summary>
    /// Fixed role set (design.md Decision 5 of add-user-authentication-and-audit-log — an enum,
    /// not a database-driven permissions table). Each role has default values for the two ABAC
    /// permission flags on <see cref="User"/> (design.md Decision 4); either flag can be
    /// overridden per user regardless of role. <see cref="Sudo"/> is a true bypass — it skips
    /// every authorization check unconditionally and is never seeded (design.md Decision 9).
    /// </summary>
    public enum Role
    {
        Operator,
        DispatchingOperator,
        Supervisor,
        Admin,
        Sudo,
    }
}

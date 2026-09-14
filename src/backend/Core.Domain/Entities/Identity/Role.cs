namespace Core.Domain.Entities.Identity
{
    /// <summary>
    /// Fixed role set (design.md Decision 5 of add-user-authentication-and-audit-log — an enum,
    /// not a database-driven permissions table). Each role has default values for the two ABAC
    /// permission flags on <see cref="User"/> (design.md Decision 4) and for the effective
    /// <see cref="TerminalMode"/> (role-driven-terminal-modes design.md Decision 1); either the
    /// ABAC flags or the terminal mode can be overridden per user regardless of role.
    /// <see cref="Sudo"/> is a true authorization bypass — it skips every permission check
    /// unconditionally and is never seeded (design.md Decision 9) — but is NOT special-cased for
    /// terminal mode, which is a UI-behavior concept, not an authorization one.
    ///
    /// <see cref="CustomerService"/> (originally named <c>PurchasingOperator</c>; renamed by
    /// rename-customer-service-role — same stored ordinal, no migration) was appended after
    /// <see cref="Sudo"/>, not inserted earlier: <see cref="Role"/> is stored as a plain int with
    /// no string conversion, so adding a member anywhere but the end would silently shift every
    /// later member's stored value.
    /// </summary>
    public enum Role
    {
        Operator,
        DispatchingOperator,
        Supervisor,
        Admin,
        Sudo,
        CustomerService,
    }
}

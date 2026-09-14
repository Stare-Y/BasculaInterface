namespace Core.Domain.Entities.Identity
{
    /// <summary>
    /// Which behavior a MAUI terminal shows once a user is logged in (role-driven-terminal-modes
    /// design.md Decision 1) — replaces the old device-local <c>SecondaryTerminal</c>/
    /// <c>OnlyPedidos</c>/<c>OnlyFinished</c> <c>Preferences</c> toggles, which had zero
    /// relationship to who was actually using the terminal.
    ///
    /// The effective value for a user is resolved the same way the two ABAC permission flags are:
    /// a role default (<see cref="Role"/>), overridable per user via
    /// <see cref="User.TerminalModeOverride"/> regardless of role. No role defaults to
    /// <see cref="OnlyFinished"/> — it describes a workstation's physical purpose (e.g. a
    /// reprint/lookup kiosk) rather than a person's job, so it is reachable only through an
    /// explicit override.
    /// </summary>
    public enum TerminalMode
    {
        /// <summary>Full flow: conclude, Contpaqi submission. The default for every role except
        /// <see cref="Role.DispatchingOperator"/> and <see cref="Role.PurchasingOperator"/>.</summary>
        Main,

        /// <summary>Weighs products on the secondary scale, marks as loaded. Default for
        /// <see cref="Role.DispatchingOperator"/>.</summary>
        Secondary,

        /// <summary>Creates empty <c>WeightEntry</c>s / adds product slots from pedidos, no full
        /// weighing flow. Default for <see cref="Role.PurchasingOperator"/>.</summary>
        PedidosOnly,

        /// <summary>Only shows/operates on already-concluded entries. No role defaults to this —
        /// only reachable via <see cref="User.TerminalModeOverride"/>.</summary>
        OnlyFinished,
    }
}

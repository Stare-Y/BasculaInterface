using Core.Domain.Entities.Identity;

namespace Core.Application.Services
{
    /// <summary>
    /// Resolves the effective value of an ABAC permission flag for a user: the user's own
    /// tri-state override if set, else the role's default, with <see cref="Role.Sudo"/> always
    /// short-circuiting to true regardless of flag (design.md Decision 4).
    /// </summary>
    public interface IPermissionService
    {
        bool HasPermission(User user, Permission permission);

        /// <summary>The role's default value, ignoring any per-user override.</summary>
        bool GetRoleDefault(Role role, Permission permission);

        /// <summary>Resolves the effective <see cref="TerminalMode"/> for a user: their own
        /// <see cref="User.TerminalModeOverride"/> if set, else their role's default
        /// (role-driven-terminal-modes design.md Decision 1). Unlike <see cref="HasPermission"/>,
        /// <see cref="Role.Sudo"/> is NOT special-cased — it resolves to <see cref="TerminalMode.Main"/>
        /// like any other non-specialized role, since terminal mode is a UI-behavior concept, not
        /// an authorization bypass.</summary>
        TerminalMode GetEffectiveTerminalMode(User user);
    }
}

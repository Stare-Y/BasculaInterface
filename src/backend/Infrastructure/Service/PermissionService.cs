using Core.Application.Services;
using Core.Domain.Entities.Identity;

namespace Infrastructure.Service
{
    public class PermissionService : IPermissionService
    {
        /// <summary>Role-default lookup table (design.md Decision 4). Supervisor defaults both
        /// flags to true; every other role defaults both to false. Sudo's row is never consulted —
        /// <see cref="HasPermission"/> short-circuits before reaching it.</summary>
        private static readonly Dictionary<Role, HashSet<Permission>> RoleDefaults = new()
        {
            [Role.Operator] = [],
            [Role.DispatchingOperator] = [],
            [Role.Supervisor] = [Permission.CanSelfAuthorizeGate, Permission.CanCaptureWeightManually],
            [Role.Admin] = [],
            [Role.Sudo] = [],
            [Role.CustomerService] = [],
        };

        /// <summary>Role default terminal modes (role-driven-terminal-modes design.md Decision 1).
        /// Only <see cref="Role.DispatchingOperator"/> and <see cref="Role.CustomerService"/>
        /// differ from <see cref="TerminalMode.Main"/> — no role defaults to
        /// <see cref="TerminalMode.OnlyFinished"/>, which is only reachable via
        /// <see cref="User.TerminalModeOverride"/>.</summary>
        private static readonly Dictionary<Role, TerminalMode> RoleTerminalModeDefaults = new()
        {
            [Role.Operator] = TerminalMode.Main,
            [Role.DispatchingOperator] = TerminalMode.Secondary,
            [Role.Supervisor] = TerminalMode.Main,
            [Role.Admin] = TerminalMode.Main,
            [Role.Sudo] = TerminalMode.Main,
            [Role.CustomerService] = TerminalMode.PedidosOnly,
        };

        public bool GetRoleDefault(Role role, Permission permission)
        {
            return RoleDefaults.TryGetValue(role, out HashSet<Permission>? granted) && granted.Contains(permission);
        }

        public bool HasPermission(User user, Permission permission)
        {
            ArgumentNullException.ThrowIfNull(user);

            // Sudo is a true bypass — every check is allowed unconditionally, before evaluating
            // role defaults or per-user overrides (design.md Decision 4).
            if (user.Role == Role.Sudo)
                return true;

            bool? overrideValue = permission switch
            {
                Permission.CanSelfAuthorizeGate => user.CanSelfAuthorizeGateOverride,
                Permission.CanCaptureWeightManually => user.CanCaptureWeightManuallyOverride,
                _ => throw new ArgumentOutOfRangeException(nameof(permission)),
            };

            return overrideValue ?? GetRoleDefault(user.Role, permission);
        }

        public TerminalMode GetEffectiveTerminalMode(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            // No Sudo short-circuit here (unlike HasPermission) — terminal mode is a UI-behavior
            // concept, not an authorization bypass; Sudo resolves via the same role-default lookup
            // as everyone else (design.md Decision 1).
            return user.TerminalModeOverride
                ?? RoleTerminalModeDefaults.GetValueOrDefault(user.Role, TerminalMode.Main);
        }
    }
}

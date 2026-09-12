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
    }
}

using Core.Application.DTOs;
using Core.Application.Security;
using Core.Application.Services;
using Core.Domain.Entities.Identity;
using Core.Domain.Interfaces;

namespace Infrastructure.Service
{
    public class UserService : IUserService
    {
        /// <summary>Role-based default inactivity timeout, applied only at user creation when the
        /// request doesn't supply an explicit value (fix-session-inactivity-timeout design.md
        /// Decision 3). Not consulted again afterward — the stored value on the user is what counts
        /// from then on, editable per-user like any other field.</summary>
        private static readonly Dictionary<Role, int> InactivityTimeoutDefaults = new()
        {
            [Role.Sudo] = 2,
            [Role.Admin] = 5,
            [Role.Supervisor] = 5,
            [Role.Operator] = 10,
            [Role.DispatchingOperator] = 20,
        };

        private readonly IUserRepo _userRepo;
        private readonly IPermissionService _permissionService;

        public UserService(IUserRepo userRepo, IPermissionService permissionService)
        {
            _userRepo = userRepo;
            _permissionService = permissionService;
        }

        public async Task<UserDto> CreateAsync(CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ArgumentException("El nombre de usuario es requerido.");
            if (string.IsNullOrWhiteSpace(request.UserCode) || !request.UserCode.All(char.IsLetterOrDigit))
                throw new ArgumentException("El código de usuario debe ser alfanumérico.");
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("La contraseña es requerida.");

            if (await _userRepo.GetByUsernameAsync(request.Username) != null)
                throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");
            if (await _userRepo.GetByUserCodeAsync(request.UserCode) != null)
                throw new InvalidOperationException("Ya existe un usuario con ese código.");

            User user = new()
            {
                Username = request.Username,
                UserCode = request.UserCode,
                PasswordHash = UserPasswordHasher.Hash(request.Password),
                Role = request.Role,
                InactivityTimeoutMinutes = request.InactivityTimeoutMinutes
                    ?? (InactivityTimeoutDefaults.TryGetValue(request.Role, out int roleDefault) ? roleDefault : 5),
            };

            User created = await _userRepo.CreateAsync(user);
            return ToDto(created);
        }

        public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request)
        {
            User user = await _userRepo.GetByIdAsync(id);

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                User? existing = await _userRepo.GetByUsernameAsync(request.Username);
                if (existing != null && existing.Id != id)
                    throw new InvalidOperationException("Ya existe un usuario con ese nombre de usuario.");
                user.Username = request.Username;
            }

            if (!string.IsNullOrWhiteSpace(request.UserCode))
            {
                if (!request.UserCode.All(char.IsLetterOrDigit))
                    throw new ArgumentException("El código de usuario debe ser alfanumérico.");
                User? existing = await _userRepo.GetByUserCodeAsync(request.UserCode);
                if (existing != null && existing.Id != id)
                    throw new InvalidOperationException("Ya existe un usuario con ese código.");
                user.UserCode = request.UserCode;
            }

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
                user.PasswordHash = UserPasswordHasher.Hash(request.NewPassword);

            if (request.Role.HasValue)
                user.Role = request.Role.Value;

            if (request.ResetCanSelfAuthorizeGateOverride)
                user.CanSelfAuthorizeGateOverride = null;
            else if (request.CanSelfAuthorizeGateOverride.HasValue)
                user.CanSelfAuthorizeGateOverride = request.CanSelfAuthorizeGateOverride;

            if (request.ResetCanCaptureWeightManuallyOverride)
                user.CanCaptureWeightManuallyOverride = null;
            else if (request.CanCaptureWeightManuallyOverride.HasValue)
                user.CanCaptureWeightManuallyOverride = request.CanCaptureWeightManuallyOverride;

            if (request.InactivityTimeoutMinutes.HasValue)
                user.InactivityTimeoutMinutes = request.InactivityTimeoutMinutes.Value;

            await _userRepo.UpdateAsync(user);
            return ToDto(user);
        }

        public async Task<UserDto> GetByIdAsync(int id)
        {
            return ToDto(await _userRepo.GetByIdAsync(id));
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            return (await _userRepo.GetAllAsync()).Select(ToDto);
        }

        public async Task DisableAsync(int id)
        {
            User user = await _userRepo.GetByIdAsync(id);
            user.IsDeleted = true;
            await _userRepo.UpdateAsync(user);
        }

        private UserDto ToDto(User user) => new(user)
        {
            CanSelfAuthorizeGate = _permissionService.HasPermission(user, Permission.CanSelfAuthorizeGate),
            CanCaptureWeightManually = _permissionService.HasPermission(user, Permission.CanCaptureWeightManually),
        };
    }
}

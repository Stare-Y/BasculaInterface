using Core.Application.Security;
using Core.Application.Services;
using Core.Domain.Entities.Identity;
using Core.Domain.Interfaces;

namespace Infrastructure.Service
{
    public class GateAuthorizationService : IGateAuthorizationService
    {
        private readonly IUserRepo _userRepo;
        private readonly IPermissionService _permissionService;

        public GateAuthorizationService(IUserRepo userRepo, IPermissionService permissionService)
        {
            _userRepo = userRepo;
            _permissionService = permissionService;
        }

        public async Task<bool> TryAuthorizeAsync(string identifier, string password)
        {
            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
                return false;

            // UserCode first, then Username — same resolution order as login (design.md Decision 6).
            User? user = await _userRepo.GetByUserCodeAsync(identifier) ?? await _userRepo.GetByUsernameAsync(identifier);

            if (user == null || !UserPasswordHasher.Verify(password, user.PasswordHash))
                return false;

            return _permissionService.HasPermission(user, Permission.CanSelfAuthorizeGate);
        }
    }
}

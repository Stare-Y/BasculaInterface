using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Core.Application.DTOs;
using Core.Application.Security;
using Core.Application.Services;
using Core.Application.Settings;
using Core.Domain.Entities.Identity;
using Core.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Service
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepo _userRepo;
        private readonly IPermissionService _permissionService;
        private readonly AuthSettings _authSettings;

        public AuthService(IUserRepo userRepo, IPermissionService permissionService, IOptions<AuthSettings> authSettings)
        {
            _userRepo = userRepo;
            _permissionService = permissionService;
            _authSettings = authSettings.Value;
        }

        public async Task<LoginResponse?> LoginAsync(string identifier, string password)
        {
            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
                return null;

            // UserCode first, then Username (design.md Decision 6).
            User? user = await _userRepo.GetByUserCodeAsync(identifier) ?? await _userRepo.GetByUsernameAsync(identifier);

            if (user == null || !UserPasswordHasher.Verify(password, user.PasswordHash))
                return null;

            string token = IssueToken(user);

            UserDto dto = ToDto(user);
            return new LoginResponse(token, dto);
        }

        private string IssueToken(User user)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(_authSettings.JwtSigningKey);
            SigningCredentials credentials = new(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            List<Claim> claims =
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("usercode", user.UserCode),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
            ];

            JwtSecurityToken token = new(
                issuer: _authSettings.JwtIssuer,
                audience: _authSettings.JwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_authSettings.JwtLifetimeHours),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UserDto ToDto(User user) => new(user)
        {
            CanSelfAuthorizeGate = _permissionService.HasPermission(user, Permission.CanSelfAuthorizeGate),
            CanCaptureWeightManually = _permissionService.HasPermission(user, Permission.CanCaptureWeightManually),
        };
    }
}

using Core.Application.DTOs;

namespace Core.Application.Services
{
    public interface IAuthService
    {
        /// <summary>Resolves <paramref name="identifier"/> by UserCode first, then Username,
        /// verifies the password, and issues a JWT plus the user's effective permissions. Returns
        /// null if the identifier does not resolve, the password is wrong, or the user is
        /// disabled (design.md Decision 6).</summary>
        Task<LoginResponse?> LoginAsync(string identifier, string password);
    }
}

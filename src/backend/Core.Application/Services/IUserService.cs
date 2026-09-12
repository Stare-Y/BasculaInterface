using Core.Application.DTOs;

namespace Core.Application.Services
{
    /// <summary>Admin/Sudo-only user management (design.md Decision 10) — no self-service, no
    /// seeding (design.md Decision 9).</summary>
    public interface IUserService
    {
        Task<UserDto> CreateAsync(CreateUserRequest request);
        Task<UserDto> UpdateAsync(int id, UpdateUserRequest request);
        Task<UserDto> GetByIdAsync(int id);
        Task<IEnumerable<UserDto>> GetAllAsync();

        /// <summary>Soft-deletes the user (IsDeleted = true) — a disabled user fails both login and
        /// the self-authorize gate.</summary>
        Task DisableAsync(int id);
    }
}

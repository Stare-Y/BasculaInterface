using Core.Domain.Entities.Identity;

namespace Core.Domain.Interfaces
{
    public interface IUserRepo
    {
        /// <summary>Non-deleted user by exact UserCode match, or null if none.</summary>
        Task<User?> GetByUserCodeAsync(string userCode);

        /// <summary>Non-deleted user by exact Username match, or null if none.</summary>
        Task<User?> GetByUsernameAsync(string username);

        Task<User> GetByIdAsync(int id);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> CreateAsync(User user);
        Task UpdateAsync(User user);
    }
}

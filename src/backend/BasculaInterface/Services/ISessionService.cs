using Core.Application.DTOs;

namespace BasculaInterface.Services
{
    /// <summary>
    /// Holds the current logged-in operator's session in memory for the whole app to read
    /// synchronously (issue #134) — the JWT itself lives in <see cref="Microsoft.Maui.Storage.SecureStorage"/>,
    /// not here, so it survives an app restart; this is the decoded, ready-to-use view of it.
    /// </summary>
    public interface ISessionService
    {
        bool IsAuthenticated { get; }
        string? Token { get; }
        UserDto? CurrentUser { get; }

        /// <summary>Restores a previously-persisted session (e.g. on app start), if one exists.</summary>
        Task RestoreAsync();

        /// <summary>Persists the session from a successful login response.</summary>
        Task LoginAsync(LoginResponse response);

        /// <summary>Clears the session, both in memory and from SecureStorage — called on manual
        /// logout or when the inactivity timer elapses (design.md Decision 7).</summary>
        Task LogoutAsync();
    }
}

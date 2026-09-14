using System.Text.Json;
using Core.Application.DTOs;

namespace BasculaInterface.Services
{
    public class SessionService : ISessionService
    {
        private const string TokenKey = "auth_token";
        private const string UserKey = "auth_user";

        public bool IsAuthenticated => !string.IsNullOrEmpty(Token) && CurrentUser is not null;
        public string? Token { get; private set; }
        public UserDto? CurrentUser { get; private set; }

        public async Task RestoreAsync()
        {
            try
            {
                Token = await SecureStorage.Default.GetAsync(TokenKey);
                string? userJson = await SecureStorage.Default.GetAsync(UserKey);
                CurrentUser = string.IsNullOrEmpty(userJson) ? null : JsonSerializer.Deserialize<UserDto>(userJson);
            }
            catch
            {
                // SecureStorage can throw on some platforms/states (e.g. a cleared keystore) —
                // treat that the same as "no saved session" rather than crash app startup.
                Token = null;
                CurrentUser = null;
            }
        }

        public async Task LoginAsync(LoginResponse response)
        {
            Token = response.Token;
            CurrentUser = response.User;

            try
            {
                // Persisting to SecureStorage is what lets a session survive an app restart
                // (RestoreAsync) — it must not be able to undo the in-memory login that already
                // succeeded above. Same defensive stance as RestoreAsync itself: SecureStorage can
                // throw on some platforms/states (e.g. first-ever use provisioning a keystore).
                await SecureStorage.Default.SetAsync(TokenKey, response.Token);
                await SecureStorage.Default.SetAsync(UserKey, JsonSerializer.Serialize(response.User));
            }
            catch
            {
                // The in-memory session (Token/CurrentUser, set above) stays valid for this run;
                // only cross-restart persistence is lost.
            }
        }

        public Task LogoutAsync()
        {
            Token = null;
            CurrentUser = null;
            SecureStorage.Default.Remove(TokenKey);
            SecureStorage.Default.Remove(UserKey);
            return Task.CompletedTask;
        }
    }
}

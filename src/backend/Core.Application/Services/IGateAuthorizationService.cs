namespace Core.Application.Services
{
    /// <summary>
    /// The self-authorize gate (design.md Decision 6, issue #134) — replaces the old shared,
    /// unsalted <c>ChangeProductPasswordHash</c> comparison. The authorizer need not be the
    /// currently logged-in operator: any user who resolves from the credential and has
    /// <c>CanSelfAuthorizeGate</c> (or is <c>Sudo</c>) may pass the gate.
    /// </summary>
    public interface IGateAuthorizationService
    {
        /// <summary>Resolves <paramref name="identifier"/> by UserCode first, then Username,
        /// verifies the password, and returns true only if that user's effective
        /// CanSelfAuthorizeGate permission is true (or the user is Sudo). Never throws for an
        /// unresolved identifier or wrong password — just returns false.</summary>
        Task<bool> TryAuthorizeAsync(string identifier, string password);
    }
}

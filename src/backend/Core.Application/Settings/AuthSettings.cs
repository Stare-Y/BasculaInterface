namespace Core.Application.Settings
{
    /// <summary>
    /// JWT issuance + client-side inactivity config (issue #134). The signing key MUST be
    /// overridden per-deployment (e.g. environment variable or user secrets) — the default here
    /// only exists so local development doesn't crash on missing config.
    /// </summary>
    public class AuthSettings
    {
        public string JwtSigningKey { get; set; } = "REPLACE_WITH_A_REAL_SECRET_IN_CONFIGURATION";
        public string JwtIssuer { get; set; } = "BasculaTerminalApi";
        public string JwtAudience { get; set; } = "BasculaInterface";

        /// <summary>Token lifetime — generous on purpose since there is no refresh-token flow; the
        /// client's own inactivity timer (see <see cref="InactivityLogoutMinutes"/>) is what
        /// actually forces re-login in practice (design.md Decision 3).</summary>
        public int JwtLifetimeHours { get; set; } = 12;

        /// <summary>Default inactivity timeout the MAUI client reads and enforces locally — no
        /// server-side session/last-activity tracking (design.md Decision 7).</summary>
        public int InactivityLogoutMinutes { get; set; } = 10;
    }
}

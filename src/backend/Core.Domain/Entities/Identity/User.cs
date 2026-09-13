using Core.Domain.Entities.Base;

namespace Core.Domain.Entities.Identity
{
    /// <summary>
    /// Custom user entity (issue #134) — no external identity provider, no ASP.NET Core Identity.
    /// Login and the self-authorize gate both resolve a typed identifier by trying
    /// <see cref="UserCode"/> first, then falling back to <see cref="Username"/> (design.md
    /// Decision 6). Disabling a user is <see cref="BaseEntity.IsDeleted"/> = true, same convention
    /// as every other entity — a disabled user fails both login and the gate.
    /// </summary>
    public class User : BaseEntity
    {
        public required string Username { get; set; }

        /// <summary>Short, unique, alphanumeric-only (letters and/or digits) identifier — a faster
        /// alternative to typing the full username.</summary>
        public required string UserCode { get; set; }

        /// <summary>Salted PBKDF2 hash (see <c>UserPasswordHasher</c>) — never the shared, unsalted
        /// SHA-256 hash used by the old <c>ChangeProductPasswordHash</c> stopgap.</summary>
        public required string PasswordHash { get; set; }

        public required Role Role { get; set; }

        /// <summary>Tri-state ABAC override: null = inherit the role default, otherwise forces the
        /// effective value regardless of role (design.md Decision 4).</summary>
        public bool? CanSelfAuthorizeGateOverride { get; set; }

        /// <summary>Tri-state ABAC override — see <see cref="CanSelfAuthorizeGateOverride"/>.</summary>
        public bool? CanCaptureWeightManuallyOverride { get; set; }

        /// <summary>Client-side inactivity auto-logout duration for this user (fix-session-inactivity-timeout
        /// design.md Decision 3). Never null: seeded from <see cref="Role"/> at creation time by
        /// <c>UserService</c> when not explicitly supplied, and backed by a database-level default of
        /// 5 for any row inserted outside the app (e.g. a manually-seeded <see cref="Role.Sudo"/> row).</summary>
        public int InactivityTimeoutMinutes { get; set; }

        /// <summary>Person's given name (add-user-name-fields design.md Decision 1). Nullable at the
        /// database level on purpose — unlike <see cref="InactivityTimeoutMinutes"/>, nothing computes
        /// off this value, so there's no reason to force a placeholder onto rows created before this
        /// field existed. <c>UserService</c> requires it to be non-blank for every user created or
        /// updated through the API from this change forward; a pre-existing row simply reads <c>null</c>
        /// until explicitly backfilled.</summary>
        public string? Name { get; set; }

        /// <summary>Person's surname — see <see cref="Name"/> for the nullability rationale.</summary>
        public string? LastName { get; set; }
    }
}

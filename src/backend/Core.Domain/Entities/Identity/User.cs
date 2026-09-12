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
    }
}

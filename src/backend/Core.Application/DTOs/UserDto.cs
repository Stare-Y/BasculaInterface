using Core.Domain.Entities.Identity;

namespace Core.Application.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public Role Role { get; set; }
        public bool? CanSelfAuthorizeGateOverride { get; set; }
        public bool? CanCaptureWeightManuallyOverride { get; set; }

        /// <summary>Resolved role-default-or-override value, computed via IPermissionService.</summary>
        public bool CanSelfAuthorizeGate { get; set; }

        /// <summary>Resolved role-default-or-override value, computed via IPermissionService.</summary>
        public bool CanCaptureWeightManually { get; set; }

        /// <summary>Client-side inactivity auto-logout duration, in minutes (fix-session-inactivity-timeout
        /// design.md Decision 3). Never null by the time it reaches the client.</summary>
        public int InactivityTimeoutMinutes { get; set; }

        /// <summary>Person's given name (add-user-name-fields design.md Decision 3). Reported exactly
        /// as stored, including <c>null</c> for a row created before this field existed — never
        /// coalesced to an empty string.</summary>
        public string? Name { get; set; }

        /// <summary>Person's surname — see <see cref="Name"/>.</summary>
        public string? LastName { get; set; }

        public UserDto() { }

        public UserDto(User entity)
        {
            Id = entity.Id;
            Username = entity.Username;
            UserCode = entity.UserCode;
            Role = entity.Role;
            CanSelfAuthorizeGateOverride = entity.CanSelfAuthorizeGateOverride;
            CanCaptureWeightManuallyOverride = entity.CanCaptureWeightManuallyOverride;
            InactivityTimeoutMinutes = entity.InactivityTimeoutMinutes;
            Name = entity.Name;
            LastName = entity.LastName;
        }
    }

    /// <param name="Name">Required, non-blank (add-user-name-fields design.md Decision 2) — enforced
    /// by <c>UserService.CreateAsync</c>, not by this record's nullability alone.</param>
    /// <param name="LastName">Required, non-blank — see <paramref name="Name"/>.</param>
    /// <param name="InactivityTimeoutMinutes">Explicit override; when null, UserService seeds a
    /// role-based default (fix-session-inactivity-timeout design.md Decision 3).</param>
    public record CreateUserRequest(
        string Username,
        string UserCode,
        string Password,
        Role Role,
        string Name,
        string LastName,
        int? InactivityTimeoutMinutes = null);

    /// <param name="Name">Optional (add-user-name-fields design.md Decision 2) — omitted leaves the
    /// stored value (including a pre-existing null) unchanged; supplied-but-blank is rejected.</param>
    /// <param name="LastName">Optional — see <paramref name="Name"/>.</param>
    public record UpdateUserRequest(
        string? Username,
        string? UserCode,
        string? NewPassword,
        Role? Role,
        bool? CanSelfAuthorizeGateOverride,
        bool ResetCanSelfAuthorizeGateOverride,
        bool? CanCaptureWeightManuallyOverride,
        bool ResetCanCaptureWeightManuallyOverride,
        int? InactivityTimeoutMinutes = null,
        string? Name = null,
        string? LastName = null);

    public record LoginRequest(string Identifier, string Password);

    public record LoginResponse(string Token, UserDto User);

    /// <summary>Credential presented at the self-authorize gate (design.md Decision 6) — replaces
    /// the old shared, unsalted <c>PasswordHash</c> field on every gated request.</summary>
    public record GateCredential(string GateIdentifier, string GatePassword);
}

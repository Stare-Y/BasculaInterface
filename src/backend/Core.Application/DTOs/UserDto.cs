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

        public UserDto() { }

        public UserDto(User entity)
        {
            Id = entity.Id;
            Username = entity.Username;
            UserCode = entity.UserCode;
            Role = entity.Role;
            CanSelfAuthorizeGateOverride = entity.CanSelfAuthorizeGateOverride;
            CanCaptureWeightManuallyOverride = entity.CanCaptureWeightManuallyOverride;
        }
    }

    public record CreateUserRequest(string Username, string UserCode, string Password, Role Role);

    public record UpdateUserRequest(
        string? Username,
        string? UserCode,
        string? NewPassword,
        Role? Role,
        bool? CanSelfAuthorizeGateOverride,
        bool ResetCanSelfAuthorizeGateOverride,
        bool? CanCaptureWeightManuallyOverride,
        bool ResetCanCaptureWeightManuallyOverride);

    public record LoginRequest(string Identifier, string Password);

    public record LoginResponse(string Token, UserDto User);

    /// <summary>Credential presented at the self-authorize gate (design.md Decision 6) — replaces
    /// the old shared, unsalted <c>PasswordHash</c> field on every gated request.</summary>
    public record GateCredential(string GateIdentifier, string GatePassword);
}

using Core.Domain.Entities.Identity;
using Infrastructure.Service;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// The hybrid RBAC/ABAC resolution rule (issue #134 / design.md Decision 4 of
    /// add-user-authentication-and-audit-log): a user's own tri-state override wins when set,
    /// otherwise the role's default applies — and Sudo bypasses both unconditionally.
    /// </summary>
    public class PermissionServiceTests
    {
        private readonly PermissionService _sut = new();

        private static User MakeUser(Role role, bool? gateOverride = null, bool? manualWeightOverride = null) => new()
        {
            Username = "u",
            UserCode = "U1",
            PasswordHash = "irrelevant",
            Role = role,
            CanSelfAuthorizeGateOverride = gateOverride,
            CanCaptureWeightManuallyOverride = manualWeightOverride,
        };

        [Theory]
        [InlineData(Role.Operator, false)]
        [InlineData(Role.DispatchingOperator, false)]
        [InlineData(Role.Supervisor, true)]
        [InlineData(Role.Admin, false)]
        public void Role_default_for_CanSelfAuthorizeGate_matches_the_table(Role role, bool expected)
        {
            Assert.Equal(expected, _sut.GetRoleDefault(role, Permission.CanSelfAuthorizeGate));
        }

        [Theory]
        [InlineData(Role.Operator, false)]
        [InlineData(Role.DispatchingOperator, false)]
        [InlineData(Role.Supervisor, true)]
        [InlineData(Role.Admin, false)]
        public void Role_default_for_CanCaptureWeightManually_matches_the_table(Role role, bool expected)
        {
            Assert.Equal(expected, _sut.GetRoleDefault(role, Permission.CanCaptureWeightManually));
        }

        [Theory]
        [InlineData(Role.Operator)]
        [InlineData(Role.DispatchingOperator)]
        [InlineData(Role.Supervisor)]
        [InlineData(Role.Admin)]
        public void No_override_falls_back_to_the_role_default(Role role)
        {
            User user = MakeUser(role);

            Assert.Equal(_sut.GetRoleDefault(role, Permission.CanSelfAuthorizeGate), _sut.HasPermission(user, Permission.CanSelfAuthorizeGate));
            Assert.Equal(_sut.GetRoleDefault(role, Permission.CanCaptureWeightManually), _sut.HasPermission(user, Permission.CanCaptureWeightManually));
        }

        [Fact]
        public void An_Operator_can_be_individually_granted_the_gate_via_override()
        {
            User user = MakeUser(Role.Operator, gateOverride: true);

            Assert.True(_sut.HasPermission(user, Permission.CanSelfAuthorizeGate));
        }

        [Fact]
        public void A_Supervisor_can_have_the_gate_individually_revoked_via_override()
        {
            User user = MakeUser(Role.Supervisor, gateOverride: false);

            Assert.False(_sut.HasPermission(user, Permission.CanSelfAuthorizeGate));
        }

        [Fact]
        public void An_Operator_can_be_individually_granted_manual_weight_via_override()
        {
            User user = MakeUser(Role.Operator, manualWeightOverride: true);

            Assert.True(_sut.HasPermission(user, Permission.CanCaptureWeightManually));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public void Sudo_bypasses_every_check_regardless_of_override(bool? gateOverride)
        {
            User sudo = MakeUser(Role.Sudo, gateOverride: gateOverride);

            Assert.True(_sut.HasPermission(sudo, Permission.CanSelfAuthorizeGate));
            Assert.True(_sut.HasPermission(sudo, Permission.CanCaptureWeightManually));
        }
    }
}

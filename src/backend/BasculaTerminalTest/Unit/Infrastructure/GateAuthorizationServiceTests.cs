using Core.Application.Security;
using Core.Application.Services;
using Core.Domain.Entities.Identity;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// The self-authorize gate's identity resolution (issue #134 / design.md Decision 6 of
    /// add-user-authentication-and-audit-log): UserCode is tried first, then Username; the
    /// resolved user must both have the right password and the CanSelfAuthorizeGate permission.
    /// </summary>
    public class GateAuthorizationServiceTests
    {
        private const string Password = "correct-password";

        private readonly IUserRepo _userRepo = Substitute.For<IUserRepo>();
        private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();

        private GateAuthorizationService CreateSut() => new(_userRepo, _permissionService);

        private static User MakeUser(string userCode, string username, Role role = Role.Supervisor) => new()
        {
            Username = username,
            UserCode = userCode,
            PasswordHash = UserPasswordHasher.Hash(Password),
            Role = role,
        };

        [Fact]
        public async Task Resolves_by_UserCode_first()
        {
            User user = MakeUser("CODE1", "someuser");
            _userRepo.GetByUserCodeAsync("CODE1").Returns(user);
            _permissionService.HasPermission(user, Permission.CanSelfAuthorizeGate).Returns(true);

            bool result = await CreateSut().TryAuthorizeAsync("CODE1", Password);

            Assert.True(result);
            await _userRepo.DidNotReceive().GetByUsernameAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task Falls_back_to_Username_when_UserCode_does_not_match()
        {
            User user = MakeUser("CODE1", "someuser");
            _userRepo.GetByUserCodeAsync("someuser").Returns((User?)null);
            _userRepo.GetByUsernameAsync("someuser").Returns(user);
            _permissionService.HasPermission(user, Permission.CanSelfAuthorizeGate).Returns(true);

            bool result = await CreateSut().TryAuthorizeAsync("someuser", Password);

            Assert.True(result);
        }

        [Fact]
        public async Task Returns_false_when_neither_UserCode_nor_Username_resolves()
        {
            _userRepo.GetByUserCodeAsync(Arg.Any<string>()).Returns((User?)null);
            _userRepo.GetByUsernameAsync(Arg.Any<string>()).Returns((User?)null);

            bool result = await CreateSut().TryAuthorizeAsync("nobody", Password);

            Assert.False(result);
        }

        [Fact]
        public async Task Returns_false_on_a_wrong_password_without_checking_permission()
        {
            User user = MakeUser("CODE1", "someuser");
            _userRepo.GetByUserCodeAsync("CODE1").Returns(user);

            bool result = await CreateSut().TryAuthorizeAsync("CODE1", "wrong-password");

            Assert.False(result);
            _permissionService.DidNotReceive().HasPermission(Arg.Any<User>(), Arg.Any<Permission>());
        }

        [Fact]
        public async Task Returns_false_for_a_resolved_user_lacking_the_permission()
        {
            User user = MakeUser("CODE1", "someuser", role: Role.Operator);
            _userRepo.GetByUserCodeAsync("CODE1").Returns(user);
            _permissionService.HasPermission(user, Permission.CanSelfAuthorizeGate).Returns(false);

            bool result = await CreateSut().TryAuthorizeAsync("CODE1", Password);

            Assert.False(result);
        }
    }
}

using Core.Application.DTOs;
using Core.Application.Security;
using Core.Application.Services;
using Core.Application.Settings;
using Core.Domain.Entities.Identity;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// Regression coverage for rename-customer-service-role: <see cref="AuthService.LoginAsync"/>'s
    /// returned <c>UserDto.TerminalMode</c> must be the resolved, per-user effective value from
    /// <see cref="IPermissionService.GetEffectiveTerminalMode"/> — not the enum's unset default
    /// (<see cref="TerminalMode.Main"/>), which is what every login silently returned before this
    /// fix regardless of the user's actual role or override.
    /// </summary>
    public class AuthServiceTerminalModeTests
    {
        private const string Password = "correct-password";

        private readonly IUserRepo _userRepo = Substitute.For<IUserRepo>();
        private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();

        private AuthService CreateSut() =>
            new(_userRepo, _permissionService, Options.Create(new AuthSettings()));

        private static User MakeUser(Role role) => new()
        {
            Username = "someuser",
            UserCode = "CODE1",
            PasswordHash = UserPasswordHasher.Hash(Password),
            Role = role,
        };

        [Theory]
        [InlineData(TerminalMode.Main)]
        [InlineData(TerminalMode.Secondary)]
        [InlineData(TerminalMode.PedidosOnly)]
        [InlineData(TerminalMode.OnlyFinished)]
        public async Task Login_response_carries_the_resolved_terminal_mode_not_the_enum_default(TerminalMode resolved)
        {
            User user = MakeUser(Role.DispatchingOperator);
            _userRepo.GetByUserCodeAsync("CODE1").Returns(user);
            _permissionService.GetEffectiveTerminalMode(user).Returns(resolved);

            LoginResponse? response = await CreateSut().LoginAsync("CODE1", Password);

            Assert.NotNull(response);
            Assert.Equal(resolved, response!.User.TerminalMode);
        }
    }
}

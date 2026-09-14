using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Identity;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// Per-role default inactivity timeout, applied only at user creation
    /// (fix-session-inactivity-timeout design.md Decision 3): an explicit value always wins; absent
    /// one, the role table applies; the update path never overwrites an unspecified value.
    /// </summary>
    public class UserServiceInactivityTimeoutTests
    {
        private readonly IUserRepo _userRepo = Substitute.For<IUserRepo>();
        private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();

        private UserService CreateSut() => new(_userRepo, _permissionService, Substitute.For<IAuditLogService>());

        public UserServiceInactivityTimeoutTests()
        {
            // CreateAsync/UpdateAsync just persist whatever entity they're handed for this service.
            _userRepo.CreateAsync(Arg.Any<User>()).Returns(callInfo => callInfo.Arg<User>());
        }

        private static CreateUserRequest MakeCreateRequest(Role role, int? inactivityTimeoutMinutes = null) =>
            new("someuser", "CODE1", "password123", role, "Nombre", "Apellido", inactivityTimeoutMinutes);

        [Theory]
        [InlineData(Role.Sudo, 2)]
        [InlineData(Role.Admin, 5)]
        [InlineData(Role.Supervisor, 5)]
        [InlineData(Role.Operator, 10)]
        [InlineData(Role.DispatchingOperator, 20)]
        public async Task Create_without_an_explicit_value_seeds_the_role_default(Role role, int expectedMinutes)
        {
            UserDto created = await CreateSut().CreateAsync(MakeCreateRequest(role));

            Assert.Equal(expectedMinutes, created.InactivityTimeoutMinutes);
        }

        [Fact]
        public async Task Create_with_an_explicit_value_overrides_the_role_default()
        {
            UserDto created = await CreateSut().CreateAsync(MakeCreateRequest(Role.Operator, inactivityTimeoutMinutes: 42));

            Assert.Equal(42, created.InactivityTimeoutMinutes);
        }

        [Fact]
        public async Task Update_without_an_explicit_value_leaves_the_existing_value_untouched()
        {
            User existing = new()
            {
                Id = 1,
                Username = "someuser",
                UserCode = "CODE1",
                PasswordHash = "irrelevant",
                Role = Role.Operator,
                InactivityTimeoutMinutes = 10,
            };
            _userRepo.GetByIdAsync(1).Returns(existing);

            UpdateUserRequest request = new(null, null, null, null, null, false, null, false);
            UserDto updated = await CreateSut().UpdateAsync(1, request);

            Assert.Equal(10, updated.InactivityTimeoutMinutes);
        }

        [Fact]
        public async Task Update_with_an_explicit_value_overwrites_the_existing_value()
        {
            User existing = new()
            {
                Id = 1,
                Username = "someuser",
                UserCode = "CODE1",
                PasswordHash = "irrelevant",
                Role = Role.Operator,
                InactivityTimeoutMinutes = 10,
            };
            _userRepo.GetByIdAsync(1).Returns(existing);

            UpdateUserRequest request = new(null, null, null, null, null, false, null, false, InactivityTimeoutMinutes: 30);
            UserDto updated = await CreateSut().UpdateAsync(1, request);

            Assert.Equal(30, updated.InactivityTimeoutMinutes);
        }
    }
}

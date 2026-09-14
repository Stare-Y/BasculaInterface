using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Identity;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// New audit call sites added to <see cref="UserService"/> by expand-audit-log-coverage §4 —
    /// User management had zero audit trail despite being the single most sensitive,
    /// Admin/Sudo-only capability in the system.
    /// </summary>
    public class UserServiceAuditLogTests
    {
        private readonly IUserRepo _userRepo = Substitute.For<IUserRepo>();
        private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();

        private UserService CreateSut() => new(_userRepo, _permissionService, _auditLogService);

        public UserServiceAuditLogTests()
        {
            _userRepo.CreateAsync(Arg.Any<User>()).Returns(ci =>
            {
                User u = ci.Arg<User>();
                u.Id = 8;
                return u;
            });
        }

        private static CreateUserRequest MakeCreateRequest() =>
            new("someuser", "CODE1", "password123", Role.Operator, "Nombre", "Apellido");

        [Fact]
        public async Task CreateAsync_records_User_Create()
        {
            await CreateSut().CreateAsync(MakeCreateRequest());

            await _auditLogService.Received(1).RecordAsync("User.Create", nameof(User), 8);
        }

        [Fact]
        public async Task CreateAsync_writes_no_audit_row_when_validation_fails()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => CreateSut().CreateAsync(MakeCreateRequest() with { Username = "" }));

            await _auditLogService.DidNotReceive().RecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
        }

        [Fact]
        public async Task UpdateAsync_records_User_Update()
        {
            User existing = new()
            {
                Id = 1,
                Username = "someuser",
                UserCode = "CODE1",
                PasswordHash = "irrelevant",
                Role = Role.Operator,
            };
            _userRepo.GetByIdAsync(1).Returns(existing);

            UpdateUserRequest request = new(null, null, null, null, null, false, null, false, Name: "Nuevo");
            await CreateSut().UpdateAsync(1, request);

            await _auditLogService.Received(1).RecordAsync("User.Update", nameof(User), 1);
        }

        [Fact]
        public async Task DisableAsync_records_User_Disable()
        {
            User existing = new()
            {
                Id = 1,
                Username = "someuser",
                UserCode = "CODE1",
                PasswordHash = "irrelevant",
                Role = Role.Operator,
            };
            _userRepo.GetByIdAsync(1).Returns(existing);

            await CreateSut().DisableAsync(1);

            await _auditLogService.Received(1).RecordAsync("User.Disable", nameof(User), 1);
            Assert.True(existing.IsDeleted);
        }
    }
}

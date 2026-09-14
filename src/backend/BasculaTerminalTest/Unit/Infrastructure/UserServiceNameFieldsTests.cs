using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Identity;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// Name/LastName are required through the create/update API but nullable at the database level
    /// (add-user-name-fields design.md Decisions 1-2): a pre-existing row's null values are left
    /// alone by an update that doesn't touch them, and an explicitly-blank value is rejected rather
    /// than silently ignored — distinguishing "omitted" from "cleared to blank" is the whole point.
    /// </summary>
    public class UserServiceNameFieldsTests
    {
        private readonly IUserRepo _userRepo = Substitute.For<IUserRepo>();
        private readonly IPermissionService _permissionService = Substitute.For<IPermissionService>();

        private UserService CreateSut() => new(_userRepo, _permissionService, Substitute.For<IAuditLogService>());

        public UserServiceNameFieldsTests()
        {
            _userRepo.CreateAsync(Arg.Any<User>()).Returns(callInfo => callInfo.Arg<User>());
        }

        private static CreateUserRequest MakeCreateRequest(string name = "Nombre", string lastName = "Apellido") =>
            new("someuser", "CODE1", "password123", Role.Operator, name, lastName);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_rejects_a_missing_or_blank_Name(string? blankName)
        {
            CreateUserRequest request = MakeCreateRequest(name: blankName!);

            await Assert.ThrowsAsync<ArgumentException>(() => CreateSut().CreateAsync(request));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_rejects_a_missing_or_blank_LastName(string? blankLastName)
        {
            CreateUserRequest request = MakeCreateRequest(lastName: blankLastName!);

            await Assert.ThrowsAsync<ArgumentException>(() => CreateSut().CreateAsync(request));
        }

        [Fact]
        public async Task Create_with_both_fields_present_succeeds_and_stores_them()
        {
            UserDto created = await CreateSut().CreateAsync(MakeCreateRequest("Juan", "Perez"));

            Assert.Equal("Juan", created.Name);
            Assert.Equal("Perez", created.LastName);
        }

        private static User MakeLegacyUser() => new()
        {
            Id = 1,
            Username = "legacyuser",
            UserCode = "LEGACY1",
            PasswordHash = "irrelevant",
            Role = Role.Operator,
            Name = null,
            LastName = null,
        };

        private static UpdateUserRequest MakeUpdateRequest(string? name = null, string? lastName = null) =>
            new(Username: null, UserCode: null, NewPassword: null, Role: null,
                CanSelfAuthorizeGateOverride: null, ResetCanSelfAuthorizeGateOverride: false,
                CanCaptureWeightManuallyOverride: null, ResetCanCaptureWeightManuallyOverride: false,
                Name: name, LastName: lastName);

        [Fact]
        public async Task Update_without_Name_or_LastName_leaves_a_preexisting_null_untouched()
        {
            User existing = MakeLegacyUser();
            _userRepo.GetByIdAsync(1).Returns(existing);

            UserDto result = await CreateSut().UpdateAsync(1, MakeUpdateRequest());

            Assert.Null(result.Name);
            Assert.Null(result.LastName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Update_with_a_blank_Name_is_rejected_and_leaves_the_stored_value_unchanged(string blankName)
        {
            User existing = MakeLegacyUser();
            existing.Name = "Existing";
            _userRepo.GetByIdAsync(1).Returns(existing);

            await Assert.ThrowsAsync<ArgumentException>(() => CreateSut().UpdateAsync(1, MakeUpdateRequest(name: blankName)));
            Assert.Equal("Existing", existing.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Update_with_a_blank_LastName_is_rejected_and_leaves_the_stored_value_unchanged(string blankLastName)
        {
            User existing = MakeLegacyUser();
            existing.LastName = "Existing";
            _userRepo.GetByIdAsync(1).Returns(existing);

            await Assert.ThrowsAsync<ArgumentException>(() => CreateSut().UpdateAsync(1, MakeUpdateRequest(lastName: blankLastName)));
            Assert.Equal("Existing", existing.LastName);
        }

        [Fact]
        public async Task Update_with_a_non_blank_Name_and_LastName_applies_them()
        {
            User existing = MakeLegacyUser();
            _userRepo.GetByIdAsync(1).Returns(existing);

            UserDto result = await CreateSut().UpdateAsync(1, MakeUpdateRequest("Juan", "Perez"));

            Assert.Equal("Juan", result.Name);
            Assert.Equal("Perez", result.LastName);
        }
    }
}

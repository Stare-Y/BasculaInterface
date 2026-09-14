using Core.Domain.Entities.Identity;
using Infrastructure.Service;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// TerminalMode resolution (role-driven-terminal-modes design.md Decision 1): same
    /// role-default-or-override shape as the two ABAC permission flags, except Sudo is NOT
    /// special-cased — terminal mode is a UI-behavior concept, not an authorization bypass.
    /// </summary>
    public class PermissionServiceTerminalModeTests
    {
        private readonly PermissionService _sut = new();

        private static User MakeUser(Role role, TerminalMode? terminalModeOverride = null) => new()
        {
            Username = "u",
            UserCode = "U1",
            PasswordHash = "irrelevant",
            Role = role,
            TerminalModeOverride = terminalModeOverride,
        };

        [Theory]
        [InlineData(Role.Operator, TerminalMode.Main)]
        [InlineData(Role.DispatchingOperator, TerminalMode.Secondary)]
        [InlineData(Role.Supervisor, TerminalMode.Main)]
        [InlineData(Role.Admin, TerminalMode.Main)]
        [InlineData(Role.Sudo, TerminalMode.Main)]
        [InlineData(Role.PurchasingOperator, TerminalMode.PedidosOnly)]
        public void Role_default_terminal_mode_matches_the_table(Role role, TerminalMode expected)
        {
            User user = MakeUser(role);

            Assert.Equal(expected, _sut.GetEffectiveTerminalMode(user));
        }

        [Fact]
        public void Sudo_is_not_special_cased_it_gets_the_plain_role_default()
        {
            User sudo = MakeUser(Role.Sudo);

            Assert.Equal(TerminalMode.Main, _sut.GetEffectiveTerminalMode(sudo));
        }

        [Theory]
        [InlineData(TerminalMode.Main)]
        [InlineData(TerminalMode.Secondary)]
        [InlineData(TerminalMode.PedidosOnly)]
        [InlineData(TerminalMode.OnlyFinished)]
        public void An_override_wins_regardless_of_role(TerminalMode overrideValue)
        {
            // Operator's own default (Main) would only coincidentally match some override values,
            // so also exercise a role whose default differs from every override case that matters.
            User user = MakeUser(Role.DispatchingOperator, terminalModeOverride: overrideValue);

            Assert.Equal(overrideValue, _sut.GetEffectiveTerminalMode(user));
        }

        [Fact]
        public void OnlyFinished_has_no_role_default_and_is_reachable_only_via_override()
        {
            foreach (Role role in Enum.GetValues<Role>())
            {
                User withoutOverride = MakeUser(role);
                Assert.NotEqual(TerminalMode.OnlyFinished, _sut.GetEffectiveTerminalMode(withoutOverride));
            }

            User withOverride = MakeUser(Role.Operator, terminalModeOverride: TerminalMode.OnlyFinished);
            Assert.Equal(TerminalMode.OnlyFinished, _sut.GetEffectiveTerminalMode(withOverride));
        }
    }
}

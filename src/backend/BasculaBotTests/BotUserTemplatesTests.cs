using Core.Domain.Entities.Identity;

namespace BasculaBotTests
{
    /// <summary>Pure unit-level check — no app, no API — that the BOT* template fixture matches
    /// design.md's table exactly: six rows, one per role, role-only (no overrides).</summary>
    public class BotUserTemplatesTests
    {
        [Fact]
        public void Fixture_has_exactly_six_templates_matching_the_design_table()
        {
            var expected = new (string UserCode, Role Role)[]
            {
                ("BOTOPERATOR", Role.Operator),
                ("BOTDISPATCH", Role.DispatchingOperator),
                ("BOTSUPERVISOR", Role.Supervisor),
                ("BOTADMIN", Role.Admin),
                ("BOTSUDO", Role.Sudo),
                ("BOTCUSTSVC", Role.CustomerService),
            };

            Assert.Equal(expected.Length, BotUserTemplates.All.Count);

            foreach ((string userCode, Role role) in expected)
            {
                BotUserTemplate template = Assert.Single(BotUserTemplates.All, t => t.UserCode == userCode);
                Assert.Equal(role, template.Role);
            }
        }

        [Fact]
        public void Every_role_has_exactly_one_template()
        {
            foreach (Role role in Enum.GetValues<Role>())
            {
                BotUserTemplate template = BotUserTemplates.ForRole(role);
                Assert.Equal(role, template.Role);
            }
        }
    }
}

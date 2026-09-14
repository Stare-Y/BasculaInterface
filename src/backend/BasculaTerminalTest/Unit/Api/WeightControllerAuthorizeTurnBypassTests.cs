using BasculaTerminalApi.Controllers;
using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Api
{
    /// <summary>
    /// AuthorizeTurnBypass (role-driven-terminal-modes design.md Decision 3): a thin, side-effect-free
    /// pass-through to IGateAuthorizationService backing the BypasTurn per-use gate. The
    /// resolve/verify/permission logic itself is already covered by GateAuthorizationServiceTests —
    /// this just pins the controller's wiring.
    /// </summary>
    public class WeightControllerAuthorizeTurnBypassTests
    {
        private readonly IWeightService _weightService = Substitute.For<IWeightService>();
        private readonly IWeightLogisticService _weightLogisticService = Substitute.For<IWeightLogisticService>();
        private readonly IGateAuthorizationService _gateAuthorizationService = Substitute.For<IGateAuthorizationService>();
        private readonly ILogger<WeightController> _logger = Substitute.For<ILogger<WeightController>>();

        private WeightController CreateSut() =>
            new(_weightService, _weightLogisticService, _gateAuthorizationService, _logger);

        [Fact]
        public async Task Returns_true_when_the_gate_authorizes()
        {
            _gateAuthorizationService.TryAuthorizeAsync("SUP1", "correct").Returns(true);

            ActionResult<bool> result = await CreateSut().AuthorizeTurnBypass(new AuthorizeTurnBypassRequest("SUP1", "correct"));

            Assert.True(Assert.IsType<OkObjectResult>(result.Result).Value as bool?);
        }

        [Theory]
        [InlineData("SUP1", "wrong")]
        [InlineData("nonexistent", "whatever")]
        public async Task Returns_false_without_throwing_when_the_gate_rejects(string identifier, string password)
        {
            _gateAuthorizationService.TryAuthorizeAsync(identifier, password).Returns(false);

            ActionResult<bool> result = await CreateSut().AuthorizeTurnBypass(new AuthorizeTurnBypassRequest(identifier, password));

            Assert.False(Assert.IsType<OkObjectResult>(result.Result).Value as bool?);
        }
    }
}

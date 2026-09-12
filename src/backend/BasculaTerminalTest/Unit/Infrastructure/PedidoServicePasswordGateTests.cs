using BasculaTerminalTest.TestDoubles;
using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// <see cref="PedidoService.DeleteSafelyAsync"/> and <see cref="PedidoService.DeleteLineSafelyAsync"/>
    /// reuse the exact same self-authorize gate as every WeightEntry/WeightDetail guarded mutation
    /// (issue #134 — replaces the shared SHA-256 password from issue #133 /
    /// extend-delete-password-gate). These tests pin the gate itself: a failed
    /// <see cref="IGateAuthorizationService"/> check must throw
    /// <see cref="UnauthorizedAccessException"/> before any repo work happens, and a passed check
    /// must let the call through to the repo.
    /// </summary>
    public class PedidoServicePasswordGateTests
    {
        private static readonly GateCredential Credential = new("some-user", "some-password");

        public enum GatedAction
        {
            DeleteSafely,
            DeleteLineSafely,
        }

        private readonly IPedidoRepo _pedidoRepo = Substitute.For<IPedidoRepo>();
        private readonly IPedidoLineRepo _pedidoLineRepo = Substitute.For<IPedidoLineRepo>();
        private readonly IWeightRepo _weightRepo = Substitute.For<IWeightRepo>();
        private readonly IExternalTargetBehaviorRepo _behaviorRepo = Substitute.For<IExternalTargetBehaviorRepo>();
        private readonly IGateAuthorizationService _gateAuthorizationService = Substitute.For<IGateAuthorizationService>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();

        private PedidoService CreateSut() => new(
            _pedidoRepo,
            _pedidoLineRepo,
            _weightRepo,
            _behaviorRepo,
            _gateAuthorizationService,
            _auditLogService,
            Options.Create(TestData.WeightSettings()));

        private Task<bool> Invoke(PedidoService sut, GatedAction action) => action switch
        {
            GatedAction.DeleteSafely => sut.DeleteSafelyAsync(id: 1, Credential),
            GatedAction.DeleteLineSafely => sut.DeleteLineSafelyAsync(id: 1, Credential),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

        [Theory]
        [InlineData(GatedAction.DeleteSafely)]
        [InlineData(GatedAction.DeleteLineSafely)]
        public async Task Rejects_when_the_gate_check_fails_without_touching_the_repo(GatedAction action)
        {
            _gateAuthorizationService.TryAuthorizeAsync(Credential.GateIdentifier, Credential.GatePassword).Returns(false);

            PedidoService sut = CreateSut();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Invoke(sut, action));

            Assert.Empty(_pedidoRepo.ReceivedCalls());
            Assert.Empty(_pedidoLineRepo.ReceivedCalls());
        }

        [Theory]
        [InlineData(GatedAction.DeleteSafely)]
        [InlineData(GatedAction.DeleteLineSafely)]
        public async Task Accepts_when_the_gate_check_passes_and_proceeds_to_the_repo(GatedAction action)
        {
            _gateAuthorizationService.TryAuthorizeAsync(Credential.GateIdentifier, Credential.GatePassword).Returns(true);
            _pedidoRepo.DeleteAsync(1).Returns(true);
            _pedidoLineRepo.DeleteAsync(1).Returns(true);

            PedidoService sut = CreateSut();

            bool result = await Invoke(sut, action);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteSafelyAsync_returns_false_when_the_repo_reports_not_found()
        {
            _gateAuthorizationService.TryAuthorizeAsync(Credential.GateIdentifier, Credential.GatePassword).Returns(true);
            _pedidoRepo.DeleteAsync(1).Returns(false);

            PedidoService sut = CreateSut();

            bool result = await sut.DeleteSafelyAsync(1, Credential);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteLineSafelyAsync_returns_false_when_the_repo_reports_not_found()
        {
            _gateAuthorizationService.TryAuthorizeAsync(Credential.GateIdentifier, Credential.GatePassword).Returns(true);
            _pedidoLineRepo.DeleteAsync(1).Returns(false);

            PedidoService sut = CreateSut();

            bool result = await sut.DeleteLineSafelyAsync(1, Credential);

            Assert.False(result);
        }
    }
}

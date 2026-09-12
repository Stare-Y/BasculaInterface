using BasculaTerminalTest.TestDoubles;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// <see cref="PedidoService.DeleteSafelyAsync"/> and <see cref="PedidoService.DeleteLineSafelyAsync"/>
    /// reuse the exact same shared SHA-256 password gate as every WeightEntry/WeightDetail guarded
    /// mutation (issue #133 / extend-delete-password-gate, design.md Decision 3: <c>PedidoService</c>
    /// gains an <c>IOptions&lt;WeightSettings&gt;</c> dependency for this). These tests pin the gate
    /// itself: a wrong or unconfigured password must throw <see cref="UnauthorizedAccessException"/>
    /// before any repo work happens, and a correct password must let the call through to the repo.
    /// </summary>
    public class PedidoServicePasswordGateTests
    {
        private const string CorrectHash = TestData.PasswordHash;
        private const string WrongHash = "0000000000000000000000000000000000000000000000000000000000000000";

        public enum GatedAction
        {
            DeleteSafely,
            DeleteLineSafely,
        }

        private readonly IPedidoRepo _pedidoRepo = Substitute.For<IPedidoRepo>();
        private readonly IPedidoLineRepo _pedidoLineRepo = Substitute.For<IPedidoLineRepo>();
        private readonly IWeightRepo _weightRepo = Substitute.For<IWeightRepo>();
        private readonly IExternalTargetBehaviorRepo _behaviorRepo = Substitute.For<IExternalTargetBehaviorRepo>();

        private PedidoService CreateSut(string configuredHash) => new(
            _pedidoRepo,
            _pedidoLineRepo,
            _weightRepo,
            _behaviorRepo,
            Options.Create(TestData.WeightSettings(configuredHash)));

        private Task<bool> Invoke(PedidoService sut, GatedAction action, string password) => action switch
        {
            GatedAction.DeleteSafely => sut.DeleteSafelyAsync(id: 1, password),
            GatedAction.DeleteLineSafely => sut.DeleteLineSafelyAsync(id: 1, password),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

        [Theory]
        [InlineData(GatedAction.DeleteSafely)]
        [InlineData(GatedAction.DeleteLineSafely)]
        public async Task Rejects_a_wrong_password_without_touching_the_repo(GatedAction action)
        {
            PedidoService sut = CreateSut(configuredHash: CorrectHash);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Invoke(sut, action, WrongHash));

            Assert.Empty(_pedidoRepo.ReceivedCalls());
            Assert.Empty(_pedidoLineRepo.ReceivedCalls());
        }

        [Theory]
        [InlineData(GatedAction.DeleteSafely)]
        [InlineData(GatedAction.DeleteLineSafely)]
        public async Task Rejects_when_unconfigured_even_if_the_submitted_hash_is_also_empty(GatedAction action)
        {
            PedidoService sut = CreateSut(configuredHash: string.Empty);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Invoke(sut, action, string.Empty));

            Assert.Empty(_pedidoRepo.ReceivedCalls());
            Assert.Empty(_pedidoLineRepo.ReceivedCalls());
        }

        [Theory]
        [InlineData(GatedAction.DeleteSafely)]
        [InlineData(GatedAction.DeleteLineSafely)]
        public async Task Accepts_a_correct_password_case_insensitively_and_proceeds_to_the_repo(GatedAction action)
        {
            _pedidoRepo.DeleteAsync(1).Returns(true);
            _pedidoLineRepo.DeleteAsync(1).Returns(true);

            PedidoService sut = CreateSut(configuredHash: CorrectHash);

            bool result = await Invoke(sut, action, CorrectHash.ToUpperInvariant());

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteSafelyAsync_returns_false_when_the_repo_reports_not_found()
        {
            _pedidoRepo.DeleteAsync(1).Returns(false);

            PedidoService sut = CreateSut(configuredHash: CorrectHash);

            bool result = await sut.DeleteSafelyAsync(1, CorrectHash);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteLineSafelyAsync_returns_false_when_the_repo_reports_not_found()
        {
            _pedidoLineRepo.DeleteAsync(1).Returns(false);

            PedidoService sut = CreateSut(configuredHash: CorrectHash);

            bool result = await sut.DeleteLineSafelyAsync(1, CorrectHash);

            Assert.False(result);
        }
    }
}

using BasculaTerminalTest.TestDoubles;
using Core.Application.Services;
using Core.Domain.Entities.Weight;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// The five manager-override actions on <see cref="WeightService"/> all sit behind the same
    /// shared SHA-256 password gate (issue #122, extended to whole-entry delete by issue #133 /
    /// extend-delete-password-gate). These tests pin the gate itself: a wrong or unconfigured
    /// password must throw <see cref="UnauthorizedAccessException"/> before any repo work happens,
    /// and a correct password must let the call through to the repo.
    /// </summary>
    public class WeightServicePasswordGateTests
    {
        private const string CorrectHash = TestData.PasswordHash; // sha256("password")
        private const string WrongHash = "0000000000000000000000000000000000000000000000000000000000000000";

        public enum GatedAction
        {
            ChangeDetailProduct,
            ChangePartner,
            ChangeDetailAmount,
            DeleteDetailSafely,
            DeleteSafely,
        }

        private readonly IWeightRepo _weightRepo = Substitute.For<IWeightRepo>();

        private WeightService CreateSut(string configuredHash)
        {
            return new WeightService(
                _weightRepo,
                Substitute.For<IExternalTargetBehaviorService>(),
                Substitute.For<IProductService>(),
                Substitute.For<IClienteProveedorService>(),
                Substitute.For<IApiService>(),
                Options.Create(TestData.ComercialSdkSettings()),
                Options.Create(TestData.WeightSettings(configuredHash)));
        }

        private static Task Invoke(WeightService sut, GatedAction action, string password) => action switch
        {
            GatedAction.ChangeDetailProduct => sut.ChangeDetailProductAsync(detailId: 1, newProductId: 2, password),
            GatedAction.ChangePartner => sut.ChangePartnerAsync(weightId: 1, newPartnerId: 2, password),
            GatedAction.ChangeDetailAmount => sut.ChangeDetailAmountAsync(detailId: 1, newWeight: 10.0, newRequiredAmount: null, password),
            GatedAction.DeleteDetailSafely => sut.DeleteDetailSafelyAsync(detailId: 1, password),
            GatedAction.DeleteSafely => sut.DeleteSafelyAsync(id: 1, password),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

        [Theory]
        [InlineData(GatedAction.ChangeDetailProduct)]
        [InlineData(GatedAction.ChangePartner)]
        [InlineData(GatedAction.ChangeDetailAmount)]
        [InlineData(GatedAction.DeleteDetailSafely)]
        [InlineData(GatedAction.DeleteSafely)]
        public async Task Rejects_a_wrong_password_without_touching_the_repo(GatedAction action)
        {
            WeightService sut = CreateSut(configuredHash: CorrectHash);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Invoke(sut, action, WrongHash));

            Assert.Empty(_weightRepo.ReceivedCalls());
        }

        [Theory]
        [InlineData(GatedAction.ChangeDetailProduct)]
        [InlineData(GatedAction.ChangePartner)]
        [InlineData(GatedAction.ChangeDetailAmount)]
        [InlineData(GatedAction.DeleteDetailSafely)]
        [InlineData(GatedAction.DeleteSafely)]
        public async Task Rejects_when_unconfigured_even_if_the_submitted_hash_is_also_empty(GatedAction action)
        {
            WeightService sut = CreateSut(configuredHash: string.Empty);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Invoke(sut, action, string.Empty));

            Assert.Empty(_weightRepo.ReceivedCalls());
        }

        [Theory]
        [InlineData(GatedAction.ChangeDetailProduct)]
        [InlineData(GatedAction.ChangePartner)]
        [InlineData(GatedAction.ChangeDetailAmount)]
        [InlineData(GatedAction.DeleteDetailSafely)]
        [InlineData(GatedAction.DeleteSafely)]
        public async Task Accepts_a_correct_password_case_insensitively_and_proceeds_to_the_repo(GatedAction action)
        {
            // A real detail/entry so code past the gate reaches the repo instead of NRE-ing first.
            var detail = new WeightDetail { Id = 1, FK_WeightEntryId = 1, WeightEntry = new WeightEntry { Id = 1 } };
            _weightRepo.GetDetailByIdAsync(1).Returns(detail);
            _weightRepo.GetByIdAsync(1).Returns(detail.WeightEntry);

            WeightService sut = CreateSut(configuredHash: CorrectHash);

            Exception? ex = await Record.ExceptionAsync(() => Invoke(sut, action, CorrectHash.ToUpperInvariant()));

            Assert.IsNotType<UnauthorizedAccessException>(ex);
            Assert.NotEmpty(_weightRepo.ReceivedCalls());
        }
    }
}

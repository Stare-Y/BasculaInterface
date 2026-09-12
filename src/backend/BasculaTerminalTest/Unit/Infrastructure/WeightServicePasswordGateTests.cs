using BasculaTerminalTest.TestDoubles;
using Core.Application.DTOs;
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
    /// self-authorize gate (issue #134 — replaces the shared SHA-256 password from issue #122 /
    /// #133). These tests pin the gate itself: a failed <see cref="IGateAuthorizationService"/>
    /// check must throw <see cref="UnauthorizedAccessException"/> before any repo work happens,
    /// and a passed check must let the call through to the repo and record an audit entry.
    /// </summary>
    public class WeightServicePasswordGateTests
    {
        private static readonly GateCredential Credential = new("some-user", "some-password");

        public enum GatedAction
        {
            ChangeDetailProduct,
            ChangePartner,
            ChangeDetailAmount,
            DeleteDetailSafely,
            DeleteSafely,
        }

        private readonly IWeightRepo _weightRepo = Substitute.For<IWeightRepo>();
        private readonly IGateAuthorizationService _gateAuthorizationService = Substitute.For<IGateAuthorizationService>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();

        private WeightService CreateSut()
        {
            return new WeightService(
                _weightRepo,
                Substitute.For<IExternalTargetBehaviorService>(),
                Substitute.For<IProductService>(),
                Substitute.For<IClienteProveedorService>(),
                Substitute.For<IApiService>(),
                _gateAuthorizationService,
                _auditLogService,
                Options.Create(TestData.ComercialSdkSettings()),
                Options.Create(TestData.WeightSettings()));
        }

        private static Task Invoke(WeightService sut, GatedAction action) => action switch
        {
            GatedAction.ChangeDetailProduct => sut.ChangeDetailProductAsync(detailId: 1, newProductId: 2, Credential),
            GatedAction.ChangePartner => sut.ChangePartnerAsync(weightId: 1, newPartnerId: 2, Credential),
            GatedAction.ChangeDetailAmount => sut.ChangeDetailAmountAsync(detailId: 1, newWeight: 10.0, newRequiredAmount: null, Credential),
            GatedAction.DeleteDetailSafely => sut.DeleteDetailSafelyAsync(detailId: 1, Credential),
            GatedAction.DeleteSafely => sut.DeleteSafelyAsync(id: 1, Credential),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

        [Theory]
        [InlineData(GatedAction.ChangeDetailProduct)]
        [InlineData(GatedAction.ChangePartner)]
        [InlineData(GatedAction.ChangeDetailAmount)]
        [InlineData(GatedAction.DeleteDetailSafely)]
        [InlineData(GatedAction.DeleteSafely)]
        public async Task Rejects_when_the_gate_check_fails_without_touching_the_repo(GatedAction action)
        {
            _gateAuthorizationService.TryAuthorizeAsync(Credential.GateIdentifier, Credential.GatePassword).Returns(false);

            WeightService sut = CreateSut();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Invoke(sut, action));

            Assert.Empty(_weightRepo.ReceivedCalls());
        }

        [Theory]
        [InlineData(GatedAction.ChangeDetailProduct)]
        [InlineData(GatedAction.ChangePartner)]
        [InlineData(GatedAction.ChangeDetailAmount)]
        [InlineData(GatedAction.DeleteDetailSafely)]
        [InlineData(GatedAction.DeleteSafely)]
        public async Task Accepts_when_the_gate_check_passes_and_proceeds_to_the_repo(GatedAction action)
        {
            _gateAuthorizationService.TryAuthorizeAsync(Credential.GateIdentifier, Credential.GatePassword).Returns(true);

            // A real detail/entry so code past the gate reaches the repo instead of NRE-ing first.
            var detail = new WeightDetail { Id = 1, FK_WeightEntryId = 1, WeightEntry = new WeightEntry { Id = 1 } };
            _weightRepo.GetDetailByIdAsync(1).Returns(detail);
            _weightRepo.GetByIdAsync(1).Returns(detail.WeightEntry);

            WeightService sut = CreateSut();

            Exception? ex = await Record.ExceptionAsync(() => Invoke(sut, action));

            Assert.IsNotType<UnauthorizedAccessException>(ex);
            Assert.NotEmpty(_weightRepo.ReceivedCalls());
        }
    }
}

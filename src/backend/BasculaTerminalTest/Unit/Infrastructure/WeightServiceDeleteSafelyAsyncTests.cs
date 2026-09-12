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
    /// Behavior specific to <see cref="WeightService.DeleteSafelyAsync"/> beyond the self-authorize
    /// gate (covered by <see cref="WeightServicePasswordGateTests"/>): the Contpaqi-document block
    /// (new for this action — design.md Decision 2 of extend-delete-password-gate), the
    /// concluded-entry bypass, and not-found propagation.
    /// </summary>
    public class WeightServiceDeleteSafelyAsyncTests
    {
        private static readonly GateCredential Credential = new("some-user", "some-password");

        private readonly IWeightRepo _weightRepo = Substitute.For<IWeightRepo>();
        private readonly IGateAuthorizationService _gateAuthorizationService = Substitute.For<IGateAuthorizationService>();

        private WeightService CreateSut()
        {
            _gateAuthorizationService.TryAuthorizeAsync(Credential.GateIdentifier, Credential.GatePassword).Returns(true);
            return new(
                _weightRepo,
                Substitute.For<IExternalTargetBehaviorService>(),
                Substitute.For<IProductService>(),
                Substitute.For<IClienteProveedorService>(),
                Substitute.For<IApiService>(),
                _gateAuthorizationService,
                Substitute.For<IAuditLogService>(),
                Options.Create(TestData.ComercialSdkSettings()),
                Options.Create(TestData.WeightSettings()));
        }

        [Fact]
        public async Task Rejects_when_the_entry_already_has_a_Contpaqi_document()
        {
            _weightRepo.GetByIdAsync(1).Returns(new WeightEntry { Id = 1, ConptaqiComercialFK = 99 });

            WeightService sut = CreateSut();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.DeleteSafelyAsync(1, Credential));

            await _weightRepo.DidNotReceive().DeleteAsync(Arg.Any<int>());
        }

        [Fact]
        public async Task Succeeds_for_a_concluded_entry_with_no_Contpaqi_document()
        {
            _weightRepo.GetByIdAsync(1).Returns(new WeightEntry { Id = 1, ConcludeDate = DateTime.UtcNow, ConptaqiComercialFK = null });
            _weightRepo.DeleteAsync(1).Returns(true);

            WeightService sut = CreateSut();

            await sut.DeleteSafelyAsync(1, Credential);

            await _weightRepo.Received(1).DeleteAsync(1);
        }

        [Fact]
        public async Task Propagates_not_found_for_a_missing_entry()
        {
            _weightRepo.GetByIdAsync(1).Returns(Task.FromException<WeightEntry>(new KeyNotFoundException("WeightEntry with ID 1 not found.")));

            WeightService sut = CreateSut();

            await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.DeleteSafelyAsync(1, Credential));

            await _weightRepo.DidNotReceive().DeleteAsync(Arg.Any<int>());
        }
    }
}

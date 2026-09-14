using BasculaTerminalTest.TestDoubles;
using Core.Application.DTOs;
using Core.Application.Services;
using Core.Domain.Entities.Behaviors;
using Core.Domain.Entities.ProviderOrders;
using Core.Domain.Entities.Weight;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// <see cref="PedidoService.ConvertLineToWeightAsync"/> is the branchiest piece of business
    /// logic in the backend: it computes a line's still-pending amount from its weight details,
    /// rejects a range of invalid requests, and then either appends a detail to an open weight
    /// entry or creates a fresh discharge entry against a picked almacén target.
    /// </summary>
    public class PedidoServiceConvertLineToWeightTests
    {
        private const int LineId = 1;
        private const int ProductId = 55;
        private const int PedidoId = 9;
        private const int ProviderId = 42;
        private const int BehaviorId = 5;

        private readonly IPedidoRepo _pedidoRepo = Substitute.For<IPedidoRepo>();
        private readonly IPedidoLineRepo _pedidoLineRepo = Substitute.For<IPedidoLineRepo>();
        private readonly IWeightRepo _weightRepo = Substitute.For<IWeightRepo>();
        private readonly IExternalTargetBehaviorRepo _behaviorRepo = Substitute.For<IExternalTargetBehaviorRepo>();

        private PedidoService CreateSut() => new(
            _pedidoRepo,
            _pedidoLineRepo,
            _weightRepo,
            _behaviorRepo,
            Substitute.For<IGateAuthorizationService>(),
            Substitute.For<IAuditLogService>(),
            Options.Create(TestData.WeightSettings()));

        private static PedidoLine Line(decimal requiredAmount, bool manuallyClosed = false, params WeightDetail[] details) => new()
        {
            Id = LineId,
            PedidoId = PedidoId,
            ProductId = ProductId,
            RequiredAmount = requiredAmount,
            Price = 12.5m,
            ManuallyClosed = manuallyClosed,
            WeightDetails = details.ToList(),
        };

        private static WeightDetail LoadedDetail(double weight, bool isLoaded = true, bool isDeleted = false) => new()
        {
            Weight = weight,
            IsLoaded = isLoaded,
            IsDeleted = isDeleted,
        };

        private void GivenLine(PedidoLine line) => _pedidoLineRepo.GetByIdAsync(LineId).Returns(line);

        // --- rejections -------------------------------------------------------

        [Fact]
        public async Task Throws_when_the_line_is_manually_closed()
        {
            GivenLine(Line(requiredAmount: 100m, manuallyClosed: true));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateSut().ConvertLineToWeightAsync(LineId, null, null, "5"));
        }

        [Fact]
        public async Task Throws_when_the_line_is_already_fully_received()
        {
            GivenLine(Line(requiredAmount: 100m, false, LoadedDetail(60), LoadedDetail(40)));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateSut().ConvertLineToWeightAsync(LineId, null, null, "5"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task Throws_when_the_explicit_target_amount_is_not_positive(int target)
        {
            GivenLine(Line(requiredAmount: 100m));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateSut().ConvertLineToWeightAsync(LineId, null, target, "5"));
        }

        [Fact]
        public async Task Throws_when_the_target_amount_exceeds_the_pending_amount()
        {
            GivenLine(Line(requiredAmount: 100m, false, LoadedDetail(70)));

            // pending is 30; asking for 40 must be rejected.
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateSut().ConvertLineToWeightAsync(LineId, null, 40m, "5"));
        }

        [Fact]
        public async Task Throws_when_appending_to_an_already_concluded_weight_entry()
        {
            GivenLine(Line(requiredAmount: 100m));
            _weightRepo.GetByIdAsync(7).Returns(new WeightEntry { Id = 7, ConcludeDate = DateTime.UtcNow });

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateSut().ConvertLineToWeightAsync(LineId, weightEntryId: 7, null, null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-an-int")]
        public async Task Throws_when_creating_a_new_entry_without_a_valid_almacen_target(string? externalTarget)
        {
            GivenLine(Line(requiredAmount: 100m));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateSut().ConvertLineToWeightAsync(LineId, null, null, externalTarget));
        }

        [Fact]
        public async Task Throws_when_the_picked_target_is_not_a_hidden_almacen_behavior()
        {
            GivenLine(Line(requiredAmount: 100m));
            _behaviorRepo.GetByIdAsync(BehaviorId).Returns(new ExternalTargetBehavior { Id = BehaviorId, Hidden = false });

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateSut().ConvertLineToWeightAsync(LineId, null, null, BehaviorId.ToString()));
        }

        // --- pending-amount computation -------------------------------------

        [Fact]
        public async Task Pending_amount_ignores_unloaded_and_deleted_details()
        {
            GivenLine(Line(requiredAmount: 100m, false,
                LoadedDetail(30),
                LoadedDetail(999, isLoaded: false),
                LoadedDetail(999, isDeleted: true)));
            _pedidoRepo.GetByIdAsync(PedidoId).Returns(new Pedido { Id = PedidoId, ProviderId = ProviderId });
            _behaviorRepo.GetByIdAsync(BehaviorId).Returns(new ExternalTargetBehavior { Id = BehaviorId, Hidden = true });
            _weightRepo.CreateAsync(Arg.Any<WeightEntry>()).Returns(ci => ci.Arg<WeightEntry>());

            await CreateSut().ConvertLineToWeightAsync(LineId, null, null, BehaviorId.ToString());

            // Only the single loaded, non-deleted 30 counts, so pending (and the new detail) is 70.
            await _weightRepo.Received(1).CreateAsync(Arg.Is<WeightEntry>(
                e => e.WeightDetails.Single().RequiredAmount == 70.0));
        }

        // --- append to an existing open entry ------------------------------

        [Fact]
        public async Task Appending_to_an_open_entry_creates_a_detail_on_that_entry_and_no_new_entry()
        {
            GivenLine(Line(requiredAmount: 100m, false, LoadedDetail(60)));
            _weightRepo.GetByIdAsync(7).Returns(new WeightEntry { Id = 7, ConcludeDate = null });
            _weightRepo.CreateDetailAsync(Arg.Any<WeightDetail>()).Returns(ci => ci.Arg<WeightDetail>());

            await CreateSut().ConvertLineToWeightAsync(LineId, weightEntryId: 7, targetAmount: null, externalTarget: null);

            await _weightRepo.Received(1).CreateDetailAsync(Arg.Is<WeightDetail>(d =>
                d.FK_WeightEntryId == 7 &&
                d.FK_PedidoLineId == LineId &&
                d.FK_WeightedProductId == ProductId &&
                d.RequiredAmount == 40.0)); // 100 required - 60 received
            await _weightRepo.DidNotReceive().CreateAsync(Arg.Any<WeightEntry>());
        }

        // --- create a fresh discharge entry --------------------------------

        [Fact]
        public async Task Creating_a_new_entry_marks_it_as_a_discharge_against_the_provider_and_target()
        {
            GivenLine(Line(requiredAmount: 100m, false, LoadedDetail(30)));
            _pedidoRepo.GetByIdAsync(PedidoId).Returns(new Pedido { Id = PedidoId, ProviderId = ProviderId });
            _behaviorRepo.GetByIdAsync(BehaviorId).Returns(new ExternalTargetBehavior { Id = BehaviorId, Hidden = true });
            _weightRepo.CreateAsync(Arg.Any<WeightEntry>()).Returns(ci => ci.Arg<WeightEntry>());

            WeightEntryDto result = await CreateSut().ConvertLineToWeightAsync(LineId, null, targetAmount: 25m, BehaviorId.ToString());

            Assert.True(result.IsDischarge);
            Assert.Equal(ProviderId, result.PartnerId);
            Assert.Equal(BehaviorId, result.ExternalTargetBehaviorFK);
            WeightDetailDto detail = Assert.Single(result.WeightDetails);
            Assert.Equal(25.0, detail.RequiredAmount);
            Assert.Equal(ProductId, detail.FK_WeightedProductId);
            Assert.Equal(LineId, detail.FK_PedidoLineId);
        }
    }
}

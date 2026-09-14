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
    /// New audit call sites added to <see cref="PedidoService"/> by expand-audit-log-coverage §3 —
    /// covering the full pedido lifecycle (create, create line, close line, convert a line to
    /// weight), not only the already-gated delete actions.
    /// </summary>
    public class PedidoServiceAuditLogTests
    {
        private readonly IPedidoRepo _pedidoRepo = Substitute.For<IPedidoRepo>();
        private readonly IPedidoLineRepo _pedidoLineRepo = Substitute.For<IPedidoLineRepo>();
        private readonly IWeightRepo _weightRepo = Substitute.For<IWeightRepo>();
        private readonly IExternalTargetBehaviorRepo _behaviorRepo = Substitute.For<IExternalTargetBehaviorRepo>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();

        private PedidoService CreateSut() => new(
            _pedidoRepo,
            _pedidoLineRepo,
            _weightRepo,
            _behaviorRepo,
            Substitute.For<IGateAuthorizationService>(),
            _auditLogService,
            Options.Create(TestData.WeightSettings()));

        [Fact]
        public async Task CreateAsync_records_Pedido_Create()
        {
            _pedidoRepo.CreateAsync(Arg.Any<Pedido>()).Returns(ci =>
            {
                Pedido p = ci.Arg<Pedido>();
                p.Id = 7;
                return p;
            });
            _pedidoRepo.GetByIdAsync(7).Returns(new Pedido { Id = 7, ProviderId = 1 });

            await CreateSut().CreateAsync(new PedidoDto { ProviderId = 1 });

            await _auditLogService.Received(1).RecordAsync("Pedido.Create", nameof(Pedido), 7);
        }

        [Fact]
        public async Task CreateLineAsync_records_PedidoLine_Create()
        {
            _pedidoLineRepo.CreateAsync(Arg.Any<PedidoLine>()).Returns(ci =>
            {
                PedidoLine l = ci.Arg<PedidoLine>();
                l.Id = 3;
                return l;
            });
            _pedidoLineRepo.GetByIdAsync(3).Returns(new PedidoLine { Id = 3, PedidoId = 7, ProductId = 1, RequiredAmount = 10 });

            await CreateSut().CreateLineAsync(new PedidoLineDto { PedidoId = 7, ProductId = 1, RequiredAmount = 10 });

            await _auditLogService.Received(1).RecordAsync("PedidoLine.Create", nameof(PedidoLine), 3);
        }

        [Fact]
        public async Task CloseLineAsync_records_PedidoLine_Close()
        {
            await CreateSut().CloseLineAsync(5);

            await _auditLogService.Received(1).RecordAsync("PedidoLine.Close", nameof(PedidoLine), 5);
        }

        [Fact]
        public async Task ConvertLineToWeightAsync_appending_to_an_open_entry_records_both_the_line_and_the_new_detail()
        {
            PedidoLine line = new() { Id = 1, PedidoId = 9, ProductId = 55, RequiredAmount = 100m, Price = 12.5m };
            _pedidoLineRepo.GetByIdAsync(1).Returns(line);
            _weightRepo.GetByIdAsync(7).Returns(new WeightEntry { Id = 7, ConcludeDate = null });
            _weightRepo.CreateDetailAsync(Arg.Any<WeightDetail>()).Returns(ci =>
            {
                WeightDetail d = ci.Arg<WeightDetail>();
                d.Id = 44;
                return d;
            });

            await CreateSut().ConvertLineToWeightAsync(1, weightEntryId: 7, targetAmount: null, externalTarget: null);

            await _auditLogService.Received(1).RecordAsync("WeightDetail.Create", nameof(WeightDetail), 44);
            await _auditLogService.Received(1).RecordAsync("PedidoLine.ConvertToWeight", nameof(PedidoLine), 1);
            await _auditLogService.DidNotReceive().RecordAsync("WeightEntry.Create", Arg.Any<string>(), Arg.Any<int>());
        }

        [Fact]
        public async Task ConvertLineToWeightAsync_creating_a_new_entry_records_both_the_line_and_the_new_entry()
        {
            const int BehaviorId = 5;
            PedidoLine line = new() { Id = 1, PedidoId = 9, ProductId = 55, RequiredAmount = 100m, Price = 12.5m };
            _pedidoLineRepo.GetByIdAsync(1).Returns(line);
            _pedidoRepo.GetByIdAsync(9).Returns(new Pedido { Id = 9, ProviderId = 42 });
            _behaviorRepo.GetByIdAsync(BehaviorId).Returns(new ExternalTargetBehavior { Id = BehaviorId, Hidden = true });
            _weightRepo.CreateAsync(Arg.Any<WeightEntry>()).Returns(ci =>
            {
                WeightEntry e = ci.Arg<WeightEntry>();
                e.Id = 21;
                return e;
            });

            await CreateSut().ConvertLineToWeightAsync(1, weightEntryId: null, targetAmount: 25m, externalTarget: BehaviorId.ToString());

            await _auditLogService.Received(1).RecordAsync("WeightEntry.Create", nameof(WeightEntry), 21);
            await _auditLogService.Received(1).RecordAsync("PedidoLine.ConvertToWeight", nameof(PedidoLine), 1);
        }

        [Fact]
        public async Task ConvertLineToWeightAsync_writes_no_audit_row_when_rejected()
        {
            PedidoLine line = new() { Id = 1, PedidoId = 9, ProductId = 55, RequiredAmount = 100m, ManuallyClosed = true };
            _pedidoLineRepo.GetByIdAsync(1).Returns(line);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateSut().ConvertLineToWeightAsync(1, null, null, "5"));

            await _auditLogService.DidNotReceive().RecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
        }
    }
}

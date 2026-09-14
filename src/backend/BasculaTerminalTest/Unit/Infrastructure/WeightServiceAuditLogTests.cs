using BasculaTerminalTest.TestDoubles;
using Core.Application.DTOs;
using Core.Application.DTOs.ContpaqiComercial;
using Core.Application.Services;
using Core.Domain.Entities.Behaviors;
using Core.Domain.Entities.Weight;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// New audit call sites added to <see cref="WeightService"/> by expand-audit-log-coverage §2 —
    /// covering the full weighing lifecycle (create entry, add detail, record a weight, mark
    /// loaded, conclude, ERP submission), not only the already-gated delete/change actions.
    /// </summary>
    public class WeightServiceAuditLogTests
    {
        private readonly IWeightRepo _weightRepo = Substitute.For<IWeightRepo>();
        private readonly IClienteProveedorService _clienteProveedorService = Substitute.For<IClienteProveedorService>();
        private readonly IProductService _productService = Substitute.For<IProductService>();
        private readonly IApiService _apiService = Substitute.For<IApiService>();
        private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();

        private WeightService CreateSut() => new(
            _weightRepo,
            Substitute.For<IExternalTargetBehaviorService>(),
            _productService,
            _clienteProveedorService,
            _apiService,
            Substitute.For<IGateAuthorizationService>(),
            _auditLogService,
            Options.Create(TestData.ComercialSdkSettings()),
            Options.Create(TestData.WeightSettings()));

        [Fact]
        public async Task CreateAsync_records_WeightEntry_Create_for_a_new_entry()
        {
            _weightRepo.CreateAsync(Arg.Any<WeightEntry>()).Returns(ci =>
            {
                WeightEntry e = ci.Arg<WeightEntry>();
                e.Id = 5;
                return e;
            });

            await CreateSut().CreateAsync(new WeightEntryDto { Id = 0 });

            await _auditLogService.Received(1).RecordAsync("WeightEntry.Create", nameof(WeightEntry), 5);
        }

        [Fact]
        public async Task CreateDetailAsync_records_WeightDetail_Create()
        {
            _weightRepo.GetByIdAsync(1).Returns(new WeightEntry { Id = 1 });
            _weightRepo.CreateDetailAsync(Arg.Any<WeightDetail>()).Returns(ci =>
            {
                WeightDetail d = ci.Arg<WeightDetail>();
                d.Id = 9;
                return d;
            });

            await CreateSut().CreateDetailAsync(new WeightDetailDto { FK_WeightEntryId = 1 });

            await _auditLogService.Received(1).RecordAsync("WeightDetail.Create", nameof(WeightDetail), 9);
        }

        [Fact]
        public async Task CreateDetailAsync_writes_no_audit_row_when_the_entry_is_already_concluded()
        {
            _weightRepo.GetByIdAsync(1).Returns(new WeightEntry { Id = 1, ConcludeDate = DateTime.UtcNow });

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateSut().CreateDetailAsync(new WeightDetailDto { FK_WeightEntryId = 1 }));

            await _auditLogService.DidNotReceive().RecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
        }

        [Fact]
        public async Task RecordWeightAsync_records_WeightDetail_RecordWeight()
        {
            _weightRepo.GetDetailByIdAsync(2).Returns(new WeightDetail { Id = 2, WeightEntry = new WeightEntry() });

            await CreateSut().RecordWeightAsync(2, 15.0, "operator1");

            await _auditLogService.Received(1).RecordAsync("WeightDetail.RecordWeight", nameof(WeightDetail), 2);
        }

        [Fact]
        public async Task RecordWeightAsync_writes_no_audit_row_for_a_non_positive_weight()
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => CreateSut().RecordWeightAsync(2, 0, "operator1"));

            await _auditLogService.DidNotReceive().RecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
        }

        [Fact]
        public async Task MarkDetailLoadedAsync_records_WeightDetail_MarkLoaded()
        {
            _weightRepo.MarkDetailLoadedAsync(3).Returns(new WeightEntry { Id = 1 });

            await CreateSut().MarkDetailLoadedAsync(3);

            await _auditLogService.Received(1).RecordAsync("WeightDetail.MarkLoaded", nameof(WeightDetail), 3);
        }

        [Fact]
        public async Task ConcludeAsync_records_WeightEntry_Conclude()
        {
            // PartnerId <= 0 keeps this focused on the Conclude call — it skips the internal
            // Contpaqi-submission branch (which would record its own separate action).
            _weightRepo.GetByIdAsync(4).Returns(new WeightEntry { Id = 4, PartnerId = 0 });

            await CreateSut().ConcludeAsync(4);

            await _auditLogService.Received(1).RecordAsync("WeightEntry.Conclude", nameof(WeightEntry), 4);
        }

        [Fact]
        public async Task SendToContpaqiComercial_records_the_action_only_on_success()
        {
            WeightEntry entry = new()
            {
                Id = 6,
                PartnerId = 42,
                ExternalTargetBehaviorFK = 1,
                ExternalTargetBehavior = new ExternalTargetBehavior
                {
                    Id = 1,
                    TargetSerie = "A",
                    TargetAlmacen = "1",
                    TargetConcept = "C",
                },
                WeightDetails = [new WeightDetail { FK_WeightedProductId = 55, Weight = 10 }],
            };
            _weightRepo.GetByIdAsync(6).Returns(entry);
            _clienteProveedorService.GetById(42).Returns(new ClienteProveedorDto { Id = 42, Code = "P1", RazonSocial = "Provider" });
            _productService.GetByIdAsync(55).Returns(new ProductoDto { Id = 55, Code = "PR1", Nombre = "Producto" });
            _apiService.PostAsync<GenericResponse<ContpaqiComercialResult>>(Arg.Any<string>(), Arg.Any<object>())
                .Returns(new GenericResponse<ContpaqiComercialResult>
                {
                    Data = new ContpaqiComercialResult { ResultingId = 100, ResultingFolio = "F1" },
                    Message = "ok",
                });

            await CreateSut().SendToContpaqiComercial(6);

            await _auditLogService.Received(1).RecordAsync("WeightEntry.SendToContpaqiComercial", nameof(WeightEntry), 6);
        }

        [Fact]
        public async Task SendToContpaqiComercial_writes_no_audit_row_when_rejected_up_front()
        {
            // No partner assigned — rejected before any SDK call is attempted.
            _weightRepo.GetByIdAsync(6).Returns(new WeightEntry { Id = 6, PartnerId = 0 });

            await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().SendToContpaqiComercial(6));

            await _auditLogService.DidNotReceive().RecordAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
        }
    }
}

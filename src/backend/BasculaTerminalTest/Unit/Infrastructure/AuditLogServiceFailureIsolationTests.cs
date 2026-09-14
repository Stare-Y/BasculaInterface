using Core.Application.Services;
using Core.Domain.Entities.Audit;
using Core.Domain.Interfaces;
using Infrastructure.Service;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BasculaTerminalTest.Unit.Infrastructure
{
    /// <summary>
    /// <see cref="AuditLogService.RecordAsync"/> is failure-isolated (expand-audit-log-coverage
    /// design.md Decision 1): every call site places it after its real mutation already committed,
    /// so a failure writing the audit row itself must never propagate and turn a successful action
    /// into an apparent 500.
    /// </summary>
    public class AuditLogServiceFailureIsolationTests
    {
        private readonly IAuditLogRepo _auditLogRepo = Substitute.For<IAuditLogRepo>();
        private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
        private readonly ILogger<AuditLogService> _logger = Substitute.For<ILogger<AuditLogService>>();

        private AuditLogService CreateSut() => new(
            _auditLogRepo,
            Substitute.For<IWeightRepo>(),
            _currentUserService,
            _logger);

        [Fact]
        public async Task RecordAsync_does_not_throw_when_the_repo_write_fails()
        {
            _currentUserService.UserId.Returns(1);
            _auditLogRepo.CreateAsync(Arg.Any<AuditLogEntry>())
                .Returns(Task.FromException<AuditLogEntry>(new InvalidOperationException("DB down")));

            AuditLogService sut = CreateSut();

            // Must complete normally — a caller running RecordAsync after its own mutation
            // already committed can't be allowed to see this exception.
            await sut.RecordAsync("WeightEntry.Create", "WeightEntry", 1);
        }

        [Fact]
        public async Task RecordAsync_logs_the_exception_when_the_repo_write_fails()
        {
            _currentUserService.UserId.Returns(1);
            InvalidOperationException thrown = new("DB down");
            _auditLogRepo.CreateAsync(Arg.Any<AuditLogEntry>()).Returns(Task.FromException<AuditLogEntry>(thrown));

            AuditLogService sut = CreateSut();

            await sut.RecordAsync("WeightEntry.Create", "WeightEntry", 1);

            _logger.Received(1).Log(
                LogLevel.Error,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                thrown,
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public async Task RecordAsync_still_writes_normally_when_the_repo_succeeds()
        {
            _currentUserService.UserId.Returns(1);

            AuditLogService sut = CreateSut();

            await sut.RecordAsync("WeightEntry.Create", "WeightEntry", 1);

            await _auditLogRepo.Received(1).CreateAsync(Arg.Is<AuditLogEntry>(
                e => e.UserId == 1 && e.Action == "WeightEntry.Create" && e.EntityType == "WeightEntry" && e.EntityId == 1));
        }
    }
}

using Core.Domain.Entities.Base;

namespace Core.Domain.Entities.Audit
{
    /// <summary>
    /// One recorded mutating action (issue #134 / design.md Decision 11). Written explicitly by
    /// the service method that performs the action — not a generic EF Core SaveChanges
    /// interceptor — so <see cref="Action"/> names the meaningful operation (e.g.
    /// "WeightEntry.DeleteSafely") rather than a raw property diff.
    /// </summary>
    public class AuditLogEntry : BaseEntity
    {
        /// <summary>The acting user — always present, since every mutating endpoint requires
        /// authentication under the fallback policy (design.md Decision 3).</summary>
        public required int UserId { get; set; }

        public required DateTime Timestamp { get; set; }

        /// <summary>e.g. "WeightEntry.DeleteSafely", "PedidoLine.Delete".</summary>
        public required string Action { get; set; }

        /// <summary>e.g. "WeightEntry", "WeightDetail", "Pedido", "PedidoLine".</summary>
        public required string EntityType { get; set; }

        public required int EntityId { get; set; }
    }
}

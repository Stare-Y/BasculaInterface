using Core.Domain.Entities.Base;
using Core.Domain.Entities.Weight;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.ProviderOrders
{
    /// <summary>
    /// One product line on a <see cref="Pedido"/>. Received/pending amounts are
    /// computed from the <c>WeightDetail</c> rows linked to this line via
    /// <c>WeightDetail.FK_PedidoLineId</c> — never stored here, to avoid a second
    /// mutation step that could fall out of sync (see design.md Decision 2).
    /// </summary>
    public class PedidoLine : BaseEntity
    {
        public required int PedidoId { get; set; }
        public required int ProductId { get; set; }
        public required decimal RequiredAmount { get; set; }
        public decimal? Price { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Explicit force-close for a short/cancelled shipment. A line is also
        /// considered concluded automatically once its computed pending amount
        /// reaches zero — see design.md Decision 4. This flag only covers the
        /// manual-override case.
        /// </summary>
        public bool ManuallyClosed { get; set; } = false;

        /// <summary>
        /// Operator's declaration that this line's product needs dis-taring ("destare").
        /// Set when building or editing the order; inherited onto each <c>WeightDetail</c>
        /// converted from this line (and still adjustable per conversion). There is no
        /// product-level dis-taring configuration — see design.md Decision 6.
        /// </summary>
        public bool RequiresDisTaring { get; set; } = false;

        public DateTime? LastUpdated { get; set; }

        [ForeignKey(nameof(PedidoId))]
        public virtual Pedido Pedido { get; set; } = null!;

        /// <summary>
        /// All weight details ever converted from this line, across any number of
        /// separate weight entries/visits. Received/pending amounts are computed
        /// from this collection (loaded, non-deleted details only) — see
        /// design.md Decision 2.
        /// </summary>
        public virtual ICollection<WeightDetail> WeightDetails { get; set; } = [];
    }
}

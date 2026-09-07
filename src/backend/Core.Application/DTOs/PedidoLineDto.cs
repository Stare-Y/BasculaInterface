using Core.Domain.Entities.ProviderOrders;

namespace Core.Application.DTOs
{
    public class PedidoLineDto
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public int ProductId { get; set; }
        public decimal RequiredAmount { get; set; }
        public decimal? Price { get; set; }
        public string? Notes { get; set; }
        public bool ManuallyClosed { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Sum of Weight across this line's non-deleted, IsLoaded WeightDetails.
        /// Computed at read time — never stored (design.md Decision 2).
        /// </summary>
        public decimal ReceivedAmount { get; set; }

        /// <summary>RequiredAmount - ReceivedAmount, floored at 0 for display.</summary>
        public decimal PendingAmount => Math.Max(RequiredAmount - ReceivedAmount, 0);

        /// <summary>
        /// True once fully received, or once force-closed. Computed — no stored
        /// header-level flag beyond ManuallyClosed (design.md Decision 4).
        /// </summary>
        public bool Concluded => ManuallyClosed || (RequiredAmount - ReceivedAmount) <= 0;

        /// <summary>
        /// Operator's declaration that this line's product needs dis-taring. Stored on the
        /// line; inherited onto each converted WeightDetail (design.md Decision 6).
        /// </summary>
        public bool RequiresDisTaring { get; set; }

        public PedidoLineDto() { }

        public PedidoLineDto(PedidoLine entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            Id = entity.Id;
            PedidoId = entity.PedidoId;
            ProductId = entity.ProductId;
            RequiredAmount = entity.RequiredAmount;
            Price = entity.Price;
            Notes = entity.Notes;
            ManuallyClosed = entity.ManuallyClosed;
            RequiresDisTaring = entity.RequiresDisTaring;
            LastUpdated = entity.LastUpdated;
            CreatedAt = entity.CreatedAt;
            ReceivedAmount = entity.WeightDetails
                .Where(d => d.IsLoaded && !d.IsDeleted)
                .Sum(d => (decimal)d.Weight);
        }

        public PedidoLine ToEntity()
        {
            return new PedidoLine
            {
                Id = Id,
                PedidoId = PedidoId,
                ProductId = ProductId,
                RequiredAmount = RequiredAmount,
                Price = Price,
                Notes = Notes,
                ManuallyClosed = ManuallyClosed,
                RequiresDisTaring = RequiresDisTaring,
                LastUpdated = LastUpdated
            };
        }
    }
}

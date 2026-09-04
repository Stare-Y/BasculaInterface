using Core.Domain.Entities.Base;

namespace Core.Domain.Entities.ProviderOrders
{
    /// <summary>
    /// A provider purchase order header. Groups one or more <see cref="PedidoLine"/>s
    /// (one per product) under a single provider. Replaces the flat, single-product
    /// <c>ProviderPurchase</c> model.
    /// </summary>
    public class Pedido : BaseEntity
    {
        public required int ProviderId { get; set; }
        public DateTime ExpectedArrival { get; set; } = DateTime.Now.AddDays(7);
        public string? Notes { get; set; }
        public DateTime? LastUpdated { get; set; }

        public ICollection<PedidoLine> Lines { get; set; } = [];
    }
}

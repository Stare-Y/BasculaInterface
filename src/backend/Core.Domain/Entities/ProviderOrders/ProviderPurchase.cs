using Core.Domain.Entities.Base;
using Core.Domain.Entities.Weight;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.ProviderOrders
{
    public class ProviderPurchase : BaseEntity
    {
        public int ProviderId { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int ProductId { get; set; }
        public decimal RequiredAmount { get; set; }
        public string? Notes { get; set; }
        public int? WeightEntryId { get; set; }

        [ForeignKey(nameof(WeightEntryId))]
        public virtual WeightEntry? WeightEntry { get; set; }
    }
}

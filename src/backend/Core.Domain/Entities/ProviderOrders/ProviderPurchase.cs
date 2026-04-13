using Core.Domain.Entities.Base;
using Core.Domain.Entities.Weight;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.ProviderOrders
{
    public class ProviderPurchase : BaseEntity
    {
        public required int ProviderId { get; set; }
        public DateTime? LastUpdated { get; set; }
        public required int ProductId { get; set; }
        public required decimal RequiredAmount { get; set; }
        public decimal? RealAmount { get; set; } 
        public string? Notes { get; set; }
        public int? WeightEntryId { get; set; }
        public bool Concluded { get; set; } = false;
        public DateTime ExpectedArrival { get; set; } = DateTime.Now.AddDays(7);


        [ForeignKey(nameof(WeightEntryId))]
        public virtual WeightEntry? WeightEntry { get; set; }
    }
}

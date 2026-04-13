using Core.Domain.Entities.ProviderOrders;

namespace Core.Application.DTOs
{
    public class ProviderPurchaseDto
    {
        public int Id { get; set; }
        public int ProviderId { get; set; }
        public int ProductId { get; set; }
        public decimal RequiredAmount { get; set; }
        public decimal? RealAmount { get; set; }
        public string? Notes { get; set; }
        public int? WeightEntryId { get; set; }
        public bool Concluded { get; set; }
        public DateTime ExpectedArrival { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? CreatedAt { get; set; }

        public ProviderPurchaseDto() { }

        public ProviderPurchaseDto(ProviderPurchase entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Id = entity.Id;
            ProviderId = entity.ProviderId;
            ProductId = entity.ProductId;
            RequiredAmount = entity.RequiredAmount;
            RealAmount = entity.RealAmount;
            Notes = entity.Notes;
            WeightEntryId = entity.WeightEntryId;
            Concluded = entity.Concluded;
            ExpectedArrival = entity.ExpectedArrival;
            LastUpdated = entity.LastUpdated;
            CreatedAt = entity.CreatedAt;
        }

        public ProviderPurchase ToEntity()
        {
            return new ProviderPurchase
            {
                Id = Id,
                ProviderId = ProviderId,
                ProductId = ProductId,
                RequiredAmount = RequiredAmount,
                RealAmount = RealAmount,
                Notes = Notes,
                WeightEntryId = WeightEntryId,
                Concluded = Concluded,
                ExpectedArrival = ExpectedArrival,
                LastUpdated = LastUpdated
            };
        }
    }
}

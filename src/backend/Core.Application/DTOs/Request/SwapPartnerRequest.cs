namespace Core.Application.DTOs.Request
{
    public record SwapPartnerRequest
    {
        public int WeightId { get; init; }
        public int CurrentPartnerId { get; init; }
        public int NewPartnerId { get; init; }
    }
}

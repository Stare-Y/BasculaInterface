using Core.Domain.Entities;

namespace Core.Application.DTOs
{
    public class WeightDetailDto
    {
        public int Id { get; set; }
        public int FK_WeightEntryId { get; set; }
        public double Weight { get; set; } = 0;
        public int? FK_WeightedProductId { get; set; }
    }
}

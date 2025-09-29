using Core.Domain.Entities;

namespace Core.Application.DTOs
{
    public class WeightDetailDto
    {
        public int Id { get; set; }
        public int FK_WeightEntryId { get; set; }
        public double Tare { get; set; } = 0;
        public double Weight { get; set; } = 0;
        public int? FK_WeightedProductId { get; set; }
        public string? WeightedBy { get; set; }
        public double? SecondaryTare { get; set; } = null;
        public double? RequiredAmount { get; set; } = null;
        public double? ProductPrice { get; set; } = null;
    }
}

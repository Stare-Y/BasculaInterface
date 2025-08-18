using Core.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities
{
    public class WeightDetail : BaseEntity
    {
        [Required]
        public int FK_WeightEntryId { get; set; }
        public double Weight { get; set; } = 0;
        public double Tare { get; set; } = 0;
        public int? FK_WeightedProductId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? WeightedBy { get; set; }
        public double? SecondaryTare { get; set; } = null;

        [ForeignKey("FK_WeightEntryId")]
        public WeightEntry WeightEntry { get; set; } = null!;
    }
}

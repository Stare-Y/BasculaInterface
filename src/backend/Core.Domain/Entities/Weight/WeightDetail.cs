using Core.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Weight
{
    public class WeightDetail : BaseEntity
    {
        public int FK_WeightEntryId { get; set; }
        public double Weight { get; set; } = 0;
        public double Tare { get; set; } = 0;
        public int? FK_WeightedProductId { get; set; } = null;
        public double? ProductPrice { get; set; } = null;
        public string? WeightedBy { get; set; }
        public double? SecondaryTare { get; set; } = null;
        public double? RequiredAmount { get; set; } = null;

        [ForeignKey("FK_WeightEntryId")]
        public virtual WeightEntry WeightEntry { get; set; } = null!;
        public override string ToString()
        {
            return $"Weight: {Weight}, Tare: {Tare}, ProductId: {FK_WeightedProductId}, Price: {ProductPrice}";
        }
    }
}

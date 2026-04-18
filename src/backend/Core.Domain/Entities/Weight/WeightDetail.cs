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
        public int? Costales { get; set; } = null;
        public DateTime? LastUpdated { get; set; } = null;
        public string? Notes { get; set; } = null;

        /// <summary>
        /// If false, dont count its weight in the total and next weight entries.
        /// </summary>
        public bool IsLoaded { get; set; } = true;

        [ForeignKey("FK_WeightEntryId")]
        public virtual WeightEntry WeightEntry { get; set; } = null!;
        public override string ToString()
        {
            return $"Weight: {Weight}, Tare: {Tare}, ProductId: {FK_WeightedProductId}, Price: {ProductPrice}, IsLoaded: {IsLoaded}";
        }
    }
}

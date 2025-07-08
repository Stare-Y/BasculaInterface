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
        public int? FK_WeightedProductId { get; set; }

        [ForeignKey("FK_WeightEntryId")]
        public WeightEntry WeightEntry { get; set; } = null!;
    }
}

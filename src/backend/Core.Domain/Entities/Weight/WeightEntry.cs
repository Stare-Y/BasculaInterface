using Core.Domain.Entities.Base;
using Core.Domain.Entities.Behaviors;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Weight
{
    public class WeightEntry : BaseEntity
    {
        public int? PartnerId { get; set; }
        public int? ConptaqiComercialFK { get; set; }
        public int? ExternalTargetBehaviorFK { get; set; }
        [ForeignKey(nameof(ExternalTargetBehaviorFK))]
        public virtual ExternalTargetBehavior? ExternalTargetBehavior { get; set; }
        public double TareWeight { get; set; } = 0;
        public double BruteWeight { get; set; } = 0;
        private DateTime? _concludeDate;
        public DateTime? ConcludeDate { get => _concludeDate; set => _concludeDate = value; }
        public string VehiclePlate { get; set; } = string.Empty;
        public string? Notes { get; set; } 
        public string? RegisteredBy { get; set; }
        public ICollection<WeightDetail> WeightDetails { get; set; } = [];
        public override string ToString()
        {
            return $"ID: {Id}, Brute: {BruteWeight}, Plate: {VehiclePlate}, Partner: {PartnerId}, ExternalTargetBehavior: {ExternalTargetBehavior}.";
        }
    }
}

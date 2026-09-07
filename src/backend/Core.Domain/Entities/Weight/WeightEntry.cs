using Core.Domain.Entities.Base;
using Core.Domain.Entities.Behaviors;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities.Weight
{
    public class WeightEntry : BaseEntity
    {
        public int? PartnerId { get; set; }
        public int? ConptaqiComercialFK { get; set; }
        public string? ContpaqiComercialFolio { get; set; }
        public int? ExternalTargetBehaviorFK { get; set; }
        [ForeignKey(nameof(ExternalTargetBehaviorFK))]
        public virtual ExternalTargetBehavior? ExternalTargetBehavior { get; set; }
        public double TareWeight { get; set; } = 0;
        public double BruteWeight { get; set; } = 0;

        /// <summary>
        /// True when the vehicle arrived already loaded and is discharging (e.g. a pedido
        /// delivery), rather than the usual case of arriving empty and being loaded. When
        /// true, <see cref="BruteWeight"/> is computed by subtracting each captured detail's
        /// weight from <see cref="TareWeight"/> instead of adding it — the running total
        /// counts down toward the vehicle's actual empty weight instead of climbing.
        /// Automatically set for weight entries created from a Pedido conversion.
        /// </summary>
        public bool IsDischarge { get; set; } = false;
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

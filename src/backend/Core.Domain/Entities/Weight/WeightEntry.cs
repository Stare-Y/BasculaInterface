using Core.Domain.Entities.Base;

namespace Core.Domain.Entities.Weight
{
    public class WeightEntry : BaseEntity
    {
        public int? PartnerId { get; set; }
        public double TareWeight { get; set; } = 0;
        public double BruteWeight { get; set; } = 0;
        private DateTime? _concludeDate;
        public DateTime? ConcludeDate { get => _concludeDate?.ToLocalTime(); set => _concludeDate = value?.ToUniversalTime(); }
        public string VehiclePlate { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? RegisteredBy { get; set; }
        public ICollection<WeightDetail> WeightDetails { get; set; } = [];
        public override string ToString()
        {
            return $"Brute: {BruteWeight}, Plate: {VehiclePlate}, Partner: {PartnerId}, WeightDetails: {WeightDetails.Count}";
        }
    }
}

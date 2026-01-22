namespace Core.Application.DTOs
{
    public class WeightEntryDto
    {
        public int Id { get; set; }
        public int? PartnerId { get; set; }
        public int? ConptaqiComercialFK { get; set; }
        public int? ExternalTargetBehaviorFK { get; set; }
        public double TareWeight { get; set; } = 0; //initial weight
        public double BruteWeight { get; set; } = 0; //final weight
        public DateTime? ConcludeDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? RegisteredBy { get; set; }
        public ExternalTargetBehaviorDto? ExternalTargetBehavior { get; set; }
        public ICollection<WeightDetailDto> WeightDetails { get; set; } = [];
    }
}

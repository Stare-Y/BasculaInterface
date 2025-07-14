namespace Core.Application.DTOs
{
    public class WeightEntryDto
    {
        public int Id { get; set; }
        public int? PartnerId { get; set; }
        public double TareWeight { get; set; } = 0;
        public double BruteWeight { get; set; } = 0;
        public DateTime? ConcludeDate { get; set; }
        public string VehiclePlate { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public ICollection<WeightDetailDto> WeightDetails { get; set; } = new List<WeightDetailDto>();
    }
}

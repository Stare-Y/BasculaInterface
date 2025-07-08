using Core.Domain.Entities;

namespace Core.Application.DTOs
{
    public class WeightEntryDto
    {
        public int? PartnerId { get; set; }
        public double TareWeight { get; set; } = 0;
        public double NetWeight { get; set; } = 0;
        public DateTime? ConcludeDate { get; set; }
        public string? Notes { get; set; }
        public ICollection<WeightDetailDto> WeightDetails { get; set; } = new List<WeightDetailDto>();
    }
}

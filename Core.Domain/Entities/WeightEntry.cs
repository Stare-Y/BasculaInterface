using Core.Domain.Entities.Base;

namespace Core.Domain.Entities
{
    public class WeightEntry : BaseEntity
    {
        public int? PartnerId { get; set; }
        public double TareWeight { get; set; } = 0;
        public double NetWeight { get; set; } = 0;
        public DateTime? ConcludeDate { get; set; }
        public string? Notes { get; set; }
        public ICollection<WeightDetail> WeightDetails { get; set; } = new List<WeightDetail>();
        
    }
}

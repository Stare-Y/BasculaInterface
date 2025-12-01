using Core.Domain.Entities.Base;

namespace Core.Domain.Entities.Turns
{
    public class Turn : BaseEntity
    {
        public int Number { get; set; }
        public string? Description { get; set; }
        public int? WeightEntryId { get; set; }
    }
}

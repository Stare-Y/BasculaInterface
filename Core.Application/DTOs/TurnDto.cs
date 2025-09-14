namespace Core.Application.DTOs
{
    public class TurnDto
    {
        public int Number { get; set; }
        public string? Description { get; set; }
        public int? WeightEntryId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

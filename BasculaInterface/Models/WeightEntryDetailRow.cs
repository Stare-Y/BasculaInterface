namespace BasculaInterface.Models
{
    public class WeightEntryDetailRow
    {
        public int Id { get; set; } = 0;
        public int OrderIndex { get; set; } = 0;
        public double Tare { get; set; } = 0;
        public double Weight { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
    }
}

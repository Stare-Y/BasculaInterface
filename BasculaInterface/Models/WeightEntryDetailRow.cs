namespace BasculaInterface.Models
{
    public class WeightEntryDetailRow
    {
        public int Id { get; set; } = 0;
        public int OrderIndex { get; set; } = 0;
        public double Tare { get; set; } = 0;
        public double Weight { get; set; } = 0;
        private string _description = string.Empty;
        public int? FK_WeightedProductId { get; set; } = null;

        public string Description
        {
            get => _description.Length > 13 ? _description.Substring(0, 13) : _description;
            set => _description = value ?? string.Empty;
        }
    }
}

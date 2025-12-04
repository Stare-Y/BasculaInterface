namespace BasculaInterface.Models
{
    public class WeightEntryDetailRow
    {
        public int Id { get; set; } = 0;
        public int OrderIndex { get; set; } = 0;
        public double Tare { get; set; } = 0;
        public double Weight { get; set; } = 0;
        public double? SecondaryTare { get; set; }
        public string? WeightedBy { get; set; }
        private string _description = string.Empty;
        public int? FK_WeightedProductId { get; set; } = null;
        public bool IsSecondaryTerminal => Preferences.Get("SecondaryTerminal", false);
        public double? RequiredAmount { get; set; } = null;
        public double? ProductPrice { get; set; } = null;
        public string RequiredAmountText => RequiredAmount.HasValue 
            ? "Cantidad Solicitada: " + RequiredAmount.Value.ToString("F2") 
            : string.Empty;

        public string Description
        {
            get => _description.Length > 13 ? _description.Substring(0, 13) : _description;
            set => _description = value ?? string.Empty;
        }
    }
}

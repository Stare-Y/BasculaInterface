using Microsoft.IdentityModel.Tokens;

namespace BasculaInterface.Models
{
    public class WeightEntryDetailRow
    {
        public int Id { get; set; } = 0;
        public int OrderIndex { get; set; } = 0;
        public double Tare { get; set; } = 0;
        public bool IsGranel { get; set; } = true;
        public string TareHeader => IsGranel ? "Tara" : string.Empty;
        public string TareValue => IsGranel ? Tare.ToString() + " kg" : string.Empty;
        public double Weight { get; set; } = 0;
        public string WeightHeader => IsGranel ? "Peso" : "Cantidad";
        public string WeightValue => IsGranel ? Weight.ToString() + " kg" : RequiredAmount!.Value.ToString();
        public double? SecondaryTare { get; set; }
        private string? _weightedBy;
        public string? WeightedBy 
        {
            get => _weightedBy.IsNullOrEmpty() ? string.Empty : $"Pesado por: {_weightedBy}"; 
            set => _weightedBy = value; 
        }
        private string _description = string.Empty;
        public int? FK_WeightedProductId { get; set; } = null;
        public bool IsSecondaryTerminal => Preferences.Get("SecondaryTerminal", false);
        public double? RequiredAmount { get; set; } = null;
        public double? ProductPrice { get; set; } = null;
        public string RequiredAmountText => RequiredAmount.HasValue && IsGranel
            ? "Cantidad Solicitada: " + RequiredAmount.Value.ToString("F2") + " kg."
            : string.Empty;

        public string Description
        {
            get => _description.Length > 13 ? _description.Substring(0, 13) : _description;
            set => _description = value ?? string.Empty;
        }
    }
}

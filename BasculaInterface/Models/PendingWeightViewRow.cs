using Core.Application.DTOs;

namespace BasculaInterface.Models
{
    public class PendingWeightViewRow
    {
        public WeightEntryDto WeightEntry { get; set; }
        public ClienteProveedorDto Partner { get; set; }
        public string HistoryText { get; set; } = string.Empty;
        public PendingWeightViewRow(WeightEntryDto weightEntry, ClienteProveedorDto partner, string historyText)
        {
            WeightEntry = weightEntry ?? throw new ArgumentNullException(nameof(weightEntry));
            Partner = partner ?? throw new ArgumentNullException(nameof(partner));
            HistoryText = historyText ?? throw new ArgumentNullException(nameof(historyText));
        }
    }
}

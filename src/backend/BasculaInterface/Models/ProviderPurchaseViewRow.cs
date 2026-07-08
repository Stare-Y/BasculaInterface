using Core.Application.DTOs;

namespace BasculaInterface.Models
{
    public class ProviderPurchaseViewRow
    {
        public ProviderPurchaseDto Purchase { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string AmountText => $"{Purchase.RequiredAmount:N2} kg";
        public string ExpectedArrivalText => Purchase.ExpectedArrival.ToLocalTime().ToString("dd/MM/yyyy");

        public string StatusText => Purchase.Concluded
            ? "Completado"
            : Purchase.WeightEntryId.HasValue
                ? "En pesaje"
                : "Pendiente";

        public Color StatusColor => Purchase.Concluded
            ? Colors.Green
            : Purchase.WeightEntryId.HasValue
                ? Colors.Orange
                : Colors.Gray;

        public ProviderPurchaseViewRow(ProviderPurchaseDto purchase, string providerName, string productName)
        {
            Purchase = purchase ?? throw new ArgumentNullException(nameof(purchase));
            ProviderName = providerName;
            ProductName = productName;
        }
    }
}

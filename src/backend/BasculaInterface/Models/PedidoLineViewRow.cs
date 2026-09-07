using Core.Application.DTOs;

namespace BasculaInterface.Models
{
    /// <summary>One product line row within the pedido form's lines list.</summary>
    public class PedidoLineViewRow
    {
        public PedidoLineDto Line { get; set; }
        public string ProductName { get; set; } = string.Empty;

        /// <summary>The product's classification-derived default almacén code, used to pre-select the convert-to-weight picker. Null if unknown.</summary>
        public string? DefaultAlmacenCode { get; set; }

        public string AmountsText => $"{Line.ReceivedAmount:N2} / {Line.RequiredAmount:N2} kg";
        public string PendingText => $"Pendiente: {Line.PendingAmount:N2} kg";

        public string StatusText => Line.Concluded
            ? (Line.ManuallyClosed ? "Cerrado" : "Completado")
            : Line.ReceivedAmount > 0
                ? "Parcial"
                : "Pendiente";

        public Color StatusColor => Line.Concluded
            ? (Line.ManuallyClosed ? Colors.Gray : Colors.Green)
            : Line.ReceivedAmount > 0
                ? Colors.Orange
                : Colors.Gray;

        public bool CanConvert => !Line.Concluded;
        public bool CanClose => !Line.Concluded;

        public bool RequiresDisTaring => Line.RequiresDisTaring;

        public PedidoLineViewRow(PedidoLineDto line, string productName, string? defaultAlmacenCode = null)
        {
            Line = line ?? throw new ArgumentNullException(nameof(line));
            ProductName = productName;
            DefaultAlmacenCode = defaultAlmacenCode;
        }
    }
}

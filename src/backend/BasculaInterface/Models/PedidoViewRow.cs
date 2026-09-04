using Core.Application.DTOs;

namespace BasculaInterface.Models
{
    /// <summary>Header row for the pedido list. Replaces ProviderPurchaseViewRow.</summary>
    public class PedidoViewRow
    {
        public PedidoDto Pedido { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string LineCountText => Pedido.Lines.Count == 1 ? "1 producto" : $"{Pedido.Lines.Count} productos";
        public string ExpectedArrivalText => Pedido.ExpectedArrival.ToLocalTime().ToString("dd/MM/yyyy");

        private int PendingLineCount => Pedido.Lines.Count(l => !l.Concluded);
        private bool AnyReceived => Pedido.Lines.Any(l => l.ReceivedAmount > 0);

        public string StatusText => Pedido.Concluded
            ? "Completado"
            : AnyReceived
                ? $"Parcial ({PendingLineCount} pendiente(s))"
                : "Pendiente";

        public Color StatusColor => Pedido.Concluded
            ? Colors.Green
            : AnyReceived
                ? Colors.Orange
                : Colors.Gray;

        public PedidoViewRow(PedidoDto pedido, string providerName)
        {
            Pedido = pedido ?? throw new ArgumentNullException(nameof(pedido));
            ProviderName = providerName;
        }
    }
}

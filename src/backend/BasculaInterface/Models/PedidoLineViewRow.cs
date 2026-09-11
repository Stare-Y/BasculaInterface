using Core.Application.DTOs;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BasculaInterface.Models
{
    /// <summary>
    /// One product line row within the pedido form's lines list. Collapsed to a single
    /// summary row by default (issue #129 follow-up: the previous always-expanded card
    /// stacked too many lines and buttons per item); tapping the row toggles <see cref="IsExpanded"/>
    /// to reveal the full detail and the Pesar/Cerrar actions.
    /// </summary>
    public class PedidoLineViewRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public PedidoLineDto Line { get; set; }
        public string ProductName { get; set; } = string.Empty;

        /// <summary>The product's classification-derived default almacén code, used to pre-select the convert-to-weight picker. Null if unknown.</summary>
        public string? DefaultAlmacenCode { get; set; }

        private bool _isExpanded;
        /// <summary>Whether the row's detail (full amounts, dis-tare note, Pesar/Cerrar) is shown. Collapsed by default.</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ChevronGlyph));
                }
            }
        }

        public string ChevronGlyph => IsExpanded ? "▾" : "▸";

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

using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BasculaInterface.Models
{
    public class WeightEntryDetailRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int Id { get; set; } = 0;
        public int OrderIndex { get; set; } = 0;

        private double _tare = 0;
        public double Tare
        {
            get => _tare;
            set
            {
                if (_tare != value)
                {
                    _tare = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TareValue));
                    OnPropertyChanged(nameof(IsRowComplete));
                }
            }
        }

        public bool IsGranel { get; set; } = true;
        public string TareHeader => IsGranel ? "Tara" : string.Empty;
        public string TareValue => IsGranel ? Tare.ToString() + " kg" : string.Empty;

        private double _weight = 0;
        public double Weight
        {
            get => _weight;
            set
            {
                if (_weight != value)
                {
                    _weight = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WeightValue));
                    OnPropertyChanged(nameof(IsRowComplete));
                }
            }
        }

        public string WeightHeader => IsGranel ? "Peso" : "Cantidad";
        public string WeightValue => IsGranel ? Weight.ToString() + " kg" : RequiredAmount?.ToString() ?? "0";

        private double? _secondaryTare;
        public double? SecondaryTare
        {
            get => _secondaryTare;
            set
            {
                if (_secondaryTare != value)
                {
                    _secondaryTare = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? _weightedBy;
        public string? WeightedByDecorated
        {
            get => _weightedBy.IsNullOrEmpty() ? string.Empty : $"Pesado por: {_weightedBy}";
            set
            {
                if (_weightedBy != value)
                {
                    _weightedBy = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WeightedBy));
                }
            }
        }

        public string? WeightedBy => _weightedBy;
        private string _description = string.Empty;
        public int? FK_WeightedProductId { get; set; } = null;
        public bool IsSecondaryTerminal => Preferences.Get("SecondaryTerminal", false);

        private double? _requiredAmount = null;
        public double? RequiredAmount
        {
            get => _requiredAmount;
            set
            {
                if (_requiredAmount != value)
                {
                    _requiredAmount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RequiredAmountText));
                    OnPropertyChanged(nameof(WeightValue));
                }
            }
        }

        private int? _costales = null;
        public int? Costales
        {
            get => _costales;
            set
            {
                if (_costales != value)
                {
                    _costales = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RequiredCostalesText));
                }
            }
        }

        public double? ProductPrice { get; set; } = null;

        private bool _isLoaded = true;
        public bool IsLoaded
        {
            get => _isLoaded;
            set
            {
                if (_isLoaded != value)
                {
                    _isLoaded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsRowComplete));
                }
            }
        }

        public bool IsRowComplete => !IsGranel || (Weight > 0 && Tare > 0 && ( IsLoaded && SecondaryTare > 0));

        public string RequiredAmountText => RequiredAmount > 0 && IsGranel
            ? "Cantidad Solicitada: " + RequiredAmount.Value.ToString("F2") + " kg."
            : string.Empty;

        public string RequiredCostalesText => Costales.HasValue
            ? "Costales: " + Costales.Value.ToString()
            : string.Empty;

        public string Description
        {
            get => _description;
            set => _description = value ?? string.Empty;
        }
    }
}
